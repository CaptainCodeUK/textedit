using System.Globalization;
using System.Reflection;
using System.Linq;
using TextEdit.Core.Abstractions;
using TextEdit.Core.Updates;
using ElectronNET.API;
using ElectronNET.API.Entities;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Headers;

namespace TextEdit.Infrastructure.Updates;

/// <summary>
/// Service for managing application auto-updates via Electron's autoUpdater API.
/// Wraps Electron.NET AutoUpdater with domain-friendly interface.
/// </summary>
public class AutoUpdateService
{
    private readonly IAppLogger? _logger;
    private UpdateStatus _status = UpdateStatus.Idle;
    private UpdateMetadata? _availableUpdate;
    private string? _lastError;
    private int _downloadPercent = 0;

    /// <summary>
    /// Raised when update status changes (checking, available, downloading, ready, error).
    /// </summary>
    public event Action<UpdateStatus, UpdateMetadata?>? StatusChanged;

    /// <summary>
    /// Raised during download progress (0-100).
    /// </summary>
    public event Action<int>? DownloadProgress;

    public UpdateStatus CurrentStatus => _status;
    public UpdateMetadata? AvailableUpdate => _availableUpdate;
    public string? LastError => _lastError;
    public int DownloadPercent => _downloadPercent;

    public AutoUpdateService(IAppLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize the auto-updater with update server URL (e.g., GitHub releases feed).
    /// Must be called before checking for updates.
    /// </summary>
    /// <param name="feedUrl">Update server URL (platform-specific manifest location). Note: with Electron.NET, feed configuration is typically handled by the packager; this parameter is informational.</param>
    public void Initialize(string feedUrl)
    {
        if (string.IsNullOrWhiteSpace(feedUrl))
        {
            _logger?.LogWarning("AutoUpdateService.Initialize called with empty feedUrl, skipping");
            return;
        }

        try
        {
            _logger?.LogInformation("AutoUpdateService initialized with feed URL: {FeedUrl}", feedUrl);
            
            // Wire up Electron.AutoUpdater events if in Electron context
            // For testing purposes on this branch we enable the auto-updater even in Debug builds.
            // This is a temporary, local-only change to allow testing of the AutoUpdater behavior while
            // running the development app. It can be reverted before merging to the main branch.
            bool enableAutoUpdaterInDebug = true; // set to false if you want the default Debug behaviour

            if (HybridSupport.IsElectronActive && (enableAutoUpdaterInDebug || !System.Diagnostics.Debugger.IsAttached))
            {
                try
                {
                    // Wire events; feed URL is typically provided by packaging config (GitHub provider)
                    Electron.AutoUpdater.OnCheckingForUpdate += () =>
                    {
                        _logger?.LogInformation("[AutoUpdater] Checking for updates...");
                        SetStatus(UpdateStatus.Checking, null);
                    };

                    Electron.AutoUpdater.OnUpdateAvailable += (UpdateInfo info) =>
                    {
                        _logger?.LogInformation("[AutoUpdater] Update available: {Version}. Downloading...", info?.Version ?? string.Empty);
                        SetStatus(UpdateStatus.Downloading, null);
                    };

                    Electron.AutoUpdater.OnUpdateNotAvailable += (UpdateInfo info) =>
                    {
                        _logger?.LogInformation("[AutoUpdater] No update available");
                        SetStatus(UpdateStatus.UpToDate, null);
                    };

                    Electron.AutoUpdater.OnDownloadProgress += (ProgressInfo info) =>
                    {
                        // Percent may be a string; parse defensively
                        var percentStr = info?.Percent?.ToString() ?? "0";
                        if (!int.TryParse(percentStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                        {
                            if (double.TryParse(percentStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var dbl))
                                parsed = (int)System.Math.Round(dbl);
                        }
                        parsed = System.Math.Clamp(parsed, 0, 100);
                        _downloadPercent = parsed;
                        _logger?.LogDebug("[AutoUpdater] Download progress: {Percent}%", parsed);
                        DownloadProgress?.Invoke(parsed);
                    };

                    Electron.AutoUpdater.OnUpdateDownloaded += (UpdateInfo info) =>
                    {
                        _logger?.LogInformation("[AutoUpdater] Update downloaded: {Version}", info?.Version ?? string.Empty);
                        // ReleaseNotes is an array; concatenate into a single string
                        string releaseNotes = string.Empty;
                        try
                        {
                            if (info?.ReleaseNotes is ReleaseNoteInfo[] notes && notes.Length > 0)
                            {
                                releaseNotes = string.Join("\n\n", notes.Select(n =>
                                {
                                    var title = string.IsNullOrWhiteSpace(n?.Version) ? string.Empty : $"{n!.Version}\n";
                                    var body = n?.Note ?? string.Empty;
                                    return $"{title}{body}".Trim();
                                }).Where(s => !string.IsNullOrWhiteSpace(s)));
                            }
                            else if (info?.ReleaseNotes != null)
                            {
                                // Fallback: if serialized to string somehow
                                releaseNotes = info.ReleaseNotes.ToString() ?? string.Empty;
                            }
                        }
                        catch { /* ignore formatting errors */ }

                        var metadata = new UpdateMetadata
                        {
                            Version = info?.Version ?? string.Empty,
                            ReleaseNotes = releaseNotes,
                            ReleasedAt = DateTimeOffset.UtcNow // Electron doesn't provide this
                        };
                        SetStatus(UpdateStatus.Ready, metadata);
                    };

                    Electron.AutoUpdater.OnError += (message) =>
                    {
                        _logger?.LogError("[AutoUpdater] Error: {Message}", message ?? string.Empty);
                        SetStatus(UpdateStatus.Error, null);
                        _lastError = message;
                    };

                    _logger?.LogInformation("AutoUpdater events registered successfully");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to register AutoUpdater events (may not be supported on this platform)");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize AutoUpdateService");
            SetStatus(UpdateStatus.Error, null);
            _lastError = ex.Message;
        }
    }

    /// <summary>
    /// Check for available updates. If auto-download is enabled, downloads automatically.
    /// </summary>
    /// <param name="autoDownload">Whether to download automatically if update found</param>
    public async Task CheckForUpdatesAsync(bool autoDownload = true)
    {
        if (_status == UpdateStatus.Checking || _status == UpdateStatus.Downloading)
        {
            _logger?.LogDebug("Update check already in progress, skipping");
            return;
        }

        try
        {
            SetStatus(UpdateStatus.Checking, null);
            _logger?.LogInformation("Checking for updates (autoDownload={AutoDownload})...", autoDownload);

            // Allow update check in debug on local testing branch when enabled
            // Allow the updater to be enabled in Debug via environment variable for testing without changing code.
            // To enable: set ENABLE_AUTO_UPDATER_IN_DEBUG=true in your environment when running the dev shell.
            var enableAutoUpdaterInDebugEnv = Environment.GetEnvironmentVariable("ENABLE_AUTO_UPDATER_IN_DEBUG");
            bool enableAutoUpdaterInDebug = bool.TryParse(enableAutoUpdaterInDebugEnv, out var envVal) && envVal;
            if (HybridSupport.IsElectronActive && (enableAutoUpdaterInDebug || !System.Diagnostics.Debugger.IsAttached))
            {
                try
                {
                    // Call whichever API exists in this ElectronNET version: CheckForUpdates or CheckForUpdatesAndNotify
                    var updater = Electron.AutoUpdater;
                    var updaterType = updater.GetType();
                    var method = updaterType.GetMethod("CheckForUpdates", Type.EmptyTypes)
                                 ?? updaterType.GetMethod("CheckForUpdatesAndNotify", Type.EmptyTypes);

                    if (method is not null)
                    {
                        method.Invoke(updater, null);
                        _logger?.LogInformation("Update check initiated via Electron.AutoUpdater ({Method})", method.Name);
                    }
                    else
                    {
                        _logger?.LogWarning("No suitable update check method found on Electron.AutoUpdater");
                    }
                    // Status will be updated via event handlers
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Electron.AutoUpdater update check invocation failed");
                    SetStatus(UpdateStatus.Error, null);
                    _lastError = ex.Message;
                }

                // If running in DEBUG and the AutoUpdater doesn't call back (dev context), perform a lightweight
                // GitHub Releases check as a fallback so devs can test update behavior without packaging.
                if (enableAutoUpdaterInDebug)
                {
                    try
                    {
                        // Wait briefly to allow the native auto-updater to respond if it will
                        await Task.Delay(TimeSpan.FromSeconds(5));

                        if (_status == UpdateStatus.Checking)
                        {
                            _logger?.LogInformation("AutoUpdater native check timed out; performing GitHub fallback check");
                            await QueryGitHubLatestReleaseAsync("CaptainCodeUK", "textedit");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "GitHub fallback update check failed");
                        // Do not override normal Error flow; leave status as-is
                    }
                }
            }
            else
            {
                _logger?.LogDebug("Not in Electron context, simulating no updates");
                await Task.Delay(500);
                SetStatus(UpdateStatus.UpToDate, null);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking for updates");
            SetStatus(UpdateStatus.Error, null);
            _lastError = ex.Message;
        }
    }

    private async Task QueryGitHubLatestReleaseAsync(string owner, string repo)
    {
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TextEditUpdater", "1.0"));
            var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            _logger?.LogDebug("Querying GitHub Releases: {Url}", url);

            var resp = await http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                _logger?.LogWarning("GitHub release check returned {Status}", resp.StatusCode);
                return;
            }

            using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagElem))
            {
                _logger?.LogWarning("GitHub release JSON missing tag_name");
                return;
            }

            var tag = tagElem.GetString() ?? string.Empty;
            // Trim leading v if present
            var tagVersion = tag.TrimStart('v', 'V');
            Version? latestVer = null;
            Version? currentVer = null;

            try { Version.TryParse(tagVersion, out var v1); latestVer = v1; }
            catch { }

            try { var av = Assembly.GetEntryAssembly()?.GetName().Version; if (av != null) currentVer = av; } catch { }

            // If a newer version is available, prefer a non-Windows asset for direct download (AppImage/DEB/DMG/ZIP).
            if (latestVer != null && currentVer != null && latestVer.CompareTo(currentVer) > 0)
            {
                var updateMetadata = new UpdateMetadata
                {
                    Version = tagVersion,
                    ReleaseNotes = root.TryGetProperty("body", out var body) ? body.GetString() ?? string.Empty : string.Empty,
                    ReleasedAt = DateTimeOffset.UtcNow,
                };

                // Find preferred asset for non-Windows platforms
                try
                {
                    if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                    {
                        string[] preferredExts = new[] { ".AppImage", ".deb", ".dmg", ".zip", ".tar.gz", ".tar.xz" };
                        // Look for non-Windows assets
                        foreach (var ext in preferredExts)
                        {
                            var matching = assets.EnumerateArray().FirstOrDefault(a =>
                                a.TryGetProperty("name", out var n) && n.GetString()?.EndsWith(ext, StringComparison.OrdinalIgnoreCase) == true);

                            if (!matching.Equals(default(JsonElement)))
                            {
                                if (matching.TryGetProperty("browser_download_url", out var downloadUrl))
                                {
                                    updateMetadata.DownloadUrl = downloadUrl.GetString() ?? string.Empty;
                                }
                                if (matching.TryGetProperty("size", out var sizeProp) && sizeProp.TryGetInt64(out var size))
                                {
                                    updateMetadata.FileSizeBytes = size;
                                }
                                break;
                            }
                        }
                        // If no preferred ext found, fallback to the first non-.nupkg (avoid windows installers)
                        if (string.IsNullOrWhiteSpace(updateMetadata.DownloadUrl))
                        {
                            var fallback = assets.EnumerateArray().FirstOrDefault(a =>
                                a.TryGetProperty("name", out var n) && !(n.GetString()?.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase) == true));
                            if (!fallback.Equals(default(JsonElement)) && fallback.TryGetProperty("browser_download_url", out var downloadUrl))
                            {
                                updateMetadata.DownloadUrl = downloadUrl.GetString() ?? string.Empty;
                            }
                        }
                    }
                }
                catch { /* ignore asset parsing errors */ }

                _logger?.LogInformation("GitHub fallback: update available {Version} > {Current}", tagVersion, currentVer);
                SetStatus(UpdateStatus.Available, updateMetadata);
            }
            else
            {
                _logger?.LogInformation("GitHub fallback: no update available (latest: {Latest}, current: {Current})", tagVersion, currentVer?.ToString() ?? "?");
                SetStatus(UpdateStatus.UpToDate, null);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "GitHub fallback check failed");
        }
    }

    /// <summary>
    /// Quit application and install pending update.
    /// Should only be called when status is Ready.
    /// </summary>
    public void QuitAndInstall()
    {
        if (_status != UpdateStatus.Ready)
        {
            _logger?.LogWarning("QuitAndInstall called but status is {Status}, ignoring", _status);
            return;
        }

        try
        {
            _logger?.LogInformation("Quitting and installing update...");
#if !DEBUG
            if (HybridSupport.IsElectronActive)
            {
                // Electron autoUpdater will quit the app and install the update
                Electron.AutoUpdater.QuitAndInstall();
            }
            else
            {
                _logger?.LogWarning("Not in Electron context, cannot install update");
            }
#else
            _logger?.LogDebug("DEBUG build, QuitAndInstall simulated");
#endif
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during quit and install");
            SetStatus(UpdateStatus.Error, null);
            _lastError = ex.Message;
        }
    }

    private void SetStatus(UpdateStatus newStatus, UpdateMetadata? metadata)
    {
        _status = newStatus;
        _availableUpdate = metadata;
        StatusChanged?.Invoke(_status, metadata);
    }
}

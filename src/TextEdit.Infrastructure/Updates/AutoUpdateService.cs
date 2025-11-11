using System.Globalization;
using System.Linq;
using TextEdit.Core.Abstractions;
using TextEdit.Core.Updates;
using ElectronNET.API;
using ElectronNET.API.Entities;

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
#if !DEBUG
            if (HybridSupport.IsElectronActive)
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
                        _logger?.LogInformation("[AutoUpdater] Update available: {Version}. Downloading...", info?.Version);
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
                        _logger?.LogInformation("[AutoUpdater] Update downloaded: {Version}", info?.Version);
                        // ReleaseNotes is an array; concatenate into a single string
                        string releaseNotes = string.Empty;
                        try
                        {
                            if (info?.ReleaseNotes is ReleaseNoteInfo[] notes && notes.Length > 0)
                            {
                                releaseNotes = string.Join("\n\n", notes.Select(n =>
                                {
                                    var title = string.IsNullOrWhiteSpace(n?.Version) ? string.Empty : $"{n!.Version}\n";
                                    var body = n?.ReleaseNotes ?? n?.Note ?? string.Empty;
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
                        _logger?.LogError("[AutoUpdater] Error: {Message}", message);
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
            else
            {
                _logger?.LogDebug("Not in Electron context, AutoUpdater disabled");
            }
#else
            _logger?.LogDebug("DEBUG build, AutoUpdater disabled");
#endif
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

#if !DEBUG
            if (HybridSupport.IsElectronActive)
            {
                try
                {
                    // Electron updater will download automatically (no separate step)
                    Electron.AutoUpdater.CheckForUpdatesAndNotify();
                    _logger?.LogInformation("Update check initiated via Electron.AutoUpdater (CheckForUpdatesAndNotify)");
                    // Status will be updated via event handlers
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Electron.AutoUpdater.CheckForUpdatesAndNotify failed");
                    SetStatus(UpdateStatus.Error, null);
                    _lastError = ex.Message;
                }
            }
            else
            {
                _logger?.LogDebug("Not in Electron context, simulating no updates");
                await Task.Delay(500);
                SetStatus(UpdateStatus.UpToDate, null);
            }
#else
            _logger?.LogDebug("DEBUG build, simulating no updates available");
            await Task.Delay(500);
            SetStatus(UpdateStatus.UpToDate, null);
#endif
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking for updates");
            SetStatus(UpdateStatus.Error, null);
            _lastError = ex.Message;
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

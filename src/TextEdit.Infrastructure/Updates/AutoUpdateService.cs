using TextEdit.Core.Abstractions;
using TextEdit.Core.Updates;

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
    /// <param name="feedUrl">Update server URL (platform-specific manifest location)</param>
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
            if (ElectronNET.API.HybridSupport.IsElectronActive)
            {
                try
                {
                    ElectronNET.API.Electron.AutoUpdater.SetFeedURL(new ElectronNET.API.Entities.FeedURLOptions
                    {
                        Url = feedUrl
                    });

                    ElectronNET.API.Electron.AutoUpdater.OnCheckingForUpdate += () =>
                    {
                        _logger?.LogInformation("[AutoUpdater] Checking for updates...");
                        SetStatus(UpdateStatus.Checking, null);
                    };

                    ElectronNET.API.Electron.AutoUpdater.OnUpdateAvailable += () =>
                    {
                        _logger?.LogInformation("[AutoUpdater] Update available, downloading...");
                        SetStatus(UpdateStatus.Downloading, null);
                    };

                    ElectronNET.API.Electron.AutoUpdater.OnUpdateNotAvailable += () =>
                    {
                        _logger?.LogInformation("[AutoUpdater] No update available");
                        SetStatus(UpdateStatus.UpToDate, null);
                    };

                    ElectronNET.API.Electron.AutoUpdater.OnDownloadProgress += (info) =>
                    {
                        _logger?.LogDebug("[AutoUpdater] Download progress: {Percent}%", info.Percent);
                        _downloadPercent = (int)info.Percent;
                        DownloadProgress?.Invoke((int)info.Percent);
                    };

                    ElectronNET.API.Electron.AutoUpdater.OnUpdateDownloaded += (info) =>
                    {
                        _logger?.LogInformation("[AutoUpdater] Update downloaded: {Version}", info.Version);
                        var metadata = new UpdateMetadata
                        {
                            Version = info.Version,
                            ReleaseNotes = info.ReleaseNotes ?? "",
                            ReleasedAt = DateTimeOffset.UtcNow // Electron doesn't provide this
                        };
                        SetStatus(UpdateStatus.Ready, metadata);
                    };

                    ElectronNET.API.Electron.AutoUpdater.OnError += (message) =>
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
            if (ElectronNET.API.HybridSupport.IsElectronActive)
            {
                try
                {
                    // Electron's autoUpdater automatically downloads if update found (no separate download step)
                    ElectronNET.API.Electron.AutoUpdater.CheckForUpdates();
                    _logger?.LogInformation("Update check initiated via Electron.AutoUpdater");
                    // Status will be updated via event handlers
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Electron.AutoUpdater.CheckForUpdates failed");
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
            if (ElectronNET.API.HybridSupport.IsElectronActive)
            {
                // Electron autoUpdater will quit the app and install the update
                ElectronNET.API.Electron.AutoUpdater.QuitAndInstall();
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

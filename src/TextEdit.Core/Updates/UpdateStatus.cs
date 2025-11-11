namespace TextEdit.Core.Updates;

/// <summary>
/// Status of an update operation.
/// </summary>
public enum UpdateStatus
{
    /// <summary>
    /// No update check has been performed or update process is idle.
    /// </summary>
    Idle,

    /// <summary>
    /// Checking for available updates from the server.
    /// </summary>
    Checking,

    /// <summary>
    /// An update is available but not yet downloaded.
    /// </summary>
    Available,

    /// <summary>
    /// Downloading the update package in the background.
    /// </summary>
    Downloading,

    /// <summary>
    /// Update package has been downloaded and is ready to install on restart.
    /// </summary>
    Ready,

    /// <summary>
    /// Error occurred during update check or download.
    /// </summary>
    Error,

    /// <summary>
    /// No updates are available (running latest version).
    /// </summary>
    UpToDate
}

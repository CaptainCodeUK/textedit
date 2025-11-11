namespace TextEdit.Core.Updates;

/// <summary>
/// User preferences for automatic update behavior.
/// </summary>
public class UpdatePreferences
{
    /// <summary>
    /// Whether to automatically download updates when available.
    /// If false, user must manually trigger download.
    /// </summary>
    public bool AutoDownload { get; set; } = true;

    /// <summary>
    /// Whether to check for updates on startup.
    /// </summary>
    public bool CheckOnStartup { get; set; } = true;

    /// <summary>
    /// Interval in hours between automatic update checks (default 24h).
    /// </summary>
    public int CheckIntervalHours { get; set; } = 24;

    /// <summary>
    /// Last time an update check was performed (UTC).
    /// </summary>
    public DateTimeOffset? LastCheckTime { get; set; }
}

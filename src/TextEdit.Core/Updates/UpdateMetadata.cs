namespace TextEdit.Core.Updates;

/// <summary>
/// Metadata about an available application update.
/// </summary>
public class UpdateMetadata
{
    /// <summary>
    /// Version number of the available update (e.g., "1.2.0").
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Release notes describing changes in this version.
    /// </summary>
    public string ReleaseNotes { get; set; } = string.Empty;

    /// <summary>
    /// Download URL for the update package.
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// SHA256 checksum for integrity verification.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a critical security update requiring immediate installation.
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// Date/time when this version was released.
    /// </summary>
    public DateTimeOffset ReleasedAt { get; set; }
}

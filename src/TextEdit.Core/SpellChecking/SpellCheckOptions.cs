namespace TextEdit.Core.SpellChecking;

public class SpellCheckOptions
{
    public string? DefaultDicUrl { get; set; }
    public string? DefaultAffUrl { get; set; }
    /// <summary>
    /// When true, the host will attempt to download the default dictionaries at startup if embedded or custom dictionaries are not present.
    /// </summary>
    public bool AutoDownloadOnStartup { get; set; } = true;

    /// <summary>
    /// Number of times to retry downloading the default dictionaries when AutoDownloadOnStartup is enabled.
    /// </summary>
    public int DownloadRetryCount { get; set; } = 2;

    /// <summary>
    /// Timeout (in seconds) for each dictionary download attempt.
    /// </summary>
    public int DownloadTimeoutSeconds { get; set; } = 10;
    // Minimum number of dictionary lines considered as a 'production' dictionary.
    // If the loaded dictionary contains fewer entries than this, Startup will prefer the DemoSpellChecker fallback.
    public int MinDictionaryWords { get; set; } = 50;
}

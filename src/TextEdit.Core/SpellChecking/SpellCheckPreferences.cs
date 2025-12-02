namespace TextEdit.Core.SpellChecking;

/// <summary>
/// User preferences for spell checking behavior.
/// </summary>
public class SpellCheckPreferences
{
    /// <summary>
    /// Gets or sets a value indicating whether spell checking is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the debounce interval (milliseconds) for spell checking after user stops typing.
    /// </summary>
    public int DebounceIntervalMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets a value indicating whether to check code blocks and markdown fenced sections.
    /// </summary>
    public bool CheckCodeBlocks { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum word length to check (0 = no limit).
    /// </summary>
    public int MaxWordLengthToCheck { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether to show suggestions automatically.
    /// </summary>
    public bool ShowSuggestionsAutomatically { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of suggestions to show per word.
    /// </summary>
    public int MaxSuggestions { get; set; } = 5;
}

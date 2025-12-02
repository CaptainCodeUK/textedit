namespace TextEdit.Core.SpellChecking;

/// <summary>
/// Represents a single spelling suggestion for a misspelled word.
/// </summary>
public class SpellCheckSuggestion
{
    /// <summary>
    /// Gets the suggested word.
    /// </summary>
    public required string Word { get; init; }

    /// <summary>
    /// Gets the suggestion confidence (0-100).
    /// Higher values indicate better matches.
    /// </summary>
    public required int Confidence { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is the highest confidence suggestion.
    /// </summary>
    public bool IsPrimary { get; init; }
}

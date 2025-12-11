namespace TextEdit.Core.SpellChecking;

/// <summary>
/// Represents the spell check result for a single word.
/// </summary>
public class SpellCheckResult
{
    /// <summary>
    /// Gets the misspelled word.
    /// </summary>
    public required string Word { get; init; }

    /// <summary>
    /// Gets the zero-based start position of the word in the text.
    /// </summary>
    public required int StartPosition { get; init; }

    /// <summary>
    /// Gets the zero-based end position (exclusive) of the word in the text.
    /// </summary>
    public required int EndPosition { get; init; }

    /// <summary>
    /// Gets the line number where the misspelling occurs (1-based).
    /// </summary>
    public required int LineNumber { get; init; }

    /// <summary>
    /// Gets the column number where the misspelling starts (0-based).
    /// Implementation note: this is zero-based to match internal indexes (Regex.Match.Index).
    /// Convert to Monaco 1-based column values in the decoration service.
    /// </summary>
    public required int ColumnNumber { get; init; }

    /// <summary>
    /// Gets the suggested corrections for this misspelled word.
    /// </summary>
    public IReadOnlyList<SpellCheckSuggestion> Suggestions { get; init; } = Array.Empty<SpellCheckSuggestion>();
}

using System.Collections.Generic;
using System.Linq;
using TextEdit.Core.SpellChecking;

namespace TextEdit.Infrastructure.SpellChecking;

/// <summary>
/// Converts spell check results into Monaco Editor decoration models.
/// Handles the transformation of misspelled words into visual indicators (red wavy underlines).
/// </summary>
public class SpellCheckDecorationService
{
    /// <summary>
    /// Decoration class name for misspelled words in Monaco Editor.
    /// </summary>
    private const string MisspellingClassName = "spell-check-error";

    /// <summary>
    /// Decoration class name for spell check suggestions popup.
    /// </summary>
    private const string SuggestionClassName = "spell-check-suggestion";

    /// <summary>
    /// Converts spell check results to Monaco decoration objects.
    /// Each misspelling becomes a red wavy underline decoration.
    /// </summary>
    /// <param name="results">Spell check results to convert</param>
    /// <returns>List of decoration objects compatible with Monaco Editor</returns>
    public IReadOnlyList<MonacoDecoration> ConvertToDecorations(IEnumerable<SpellCheckResult> results)
    {
        var decorations = new List<MonacoDecoration>();

        foreach (var result in results)
        {
            // Monaco uses 1-based line numbers and 0-based column positions
            decorations.Add(new MonacoDecoration
            {
                Range = new MonacoRange
                {
                    StartLineNumber = result.LineNumber,
                    StartColumn = result.ColumnNumber + 1, // Convert to 1-based
                    EndLineNumber = result.LineNumber,
                    EndColumn = result.ColumnNumber + result.Word.Length + 1 // 1-based, inclusive
                },
                Options = new MonacoDecorationOptions
                {
                    IsWholeLine = false,
                    // Use inline class to apply styling only to the text span (Monaco inline classes)
                    InlineClassName = MisspellingClassName,
                    GlyphMarginClassName = null,
                    GlyphMarginHoverMessage = string.Empty,
                    InlineClassNameAffectsLetterSpacing = false,
                    BeforeContentClassName = null,
                    AfterContentClassName = null,
                    Message = result.Word,
                    Suggestions = result.Suggestions
                        .OrderByDescending(s => s.Confidence)
                        .ThenBy(s => s.Word)
                        .ToList()
                }
            });
        }

        return decorations.AsReadOnly();
    }

    /// <summary>
    /// Clears all spell check decorations from the editor.
    /// Used when spell checking is disabled or content changes.
    /// </summary>
    /// <returns>Empty decoration list for Monaco setDecorations</returns>
    public IReadOnlyList<MonacoDecoration> ClearDecorations()
    {
        return new List<MonacoDecoration>().AsReadOnly();
    }
}

/// <summary>
/// Represents a single decoration in Monaco Editor.
/// Decorations provide visual feedback for misspellings.
/// </summary>
public class MonacoDecoration
{
    /// <summary>
    /// The range in the editor where the decoration should be applied.
    /// </summary>
    public MonacoRange Range { get; set; } = new();

    /// <summary>
    /// Options controlling how the decoration is rendered.
    /// </summary>
    public MonacoDecorationOptions Options { get; set; } = new();
}

/// <summary>
/// Represents a range in Monaco Editor (line and column positions).
/// </summary>
public class MonacoRange
{
    /// <summary>
    /// Starting line number (1-based in Monaco).
    /// </summary>
    public int StartLineNumber { get; set; }

    /// <summary>
    /// Starting column position (1-based in Monaco).
    /// </summary>
    public int StartColumn { get; set; }

    /// <summary>
    /// Ending line number (1-based in Monaco).
    /// </summary>
    public int EndLineNumber { get; set; }

    /// <summary>
    /// Ending column position (1-based in Monaco).
    /// </summary>
    public int EndColumn { get; set; }
}

/// <summary>
/// Options for rendering a Monaco decoration.
/// Controls the visual appearance and behavior of the underline.
/// </summary>
public class MonacoDecorationOptions
{
    /// <summary>
    /// Whether this decoration spans the entire line.
    /// </summary>
    public bool IsWholeLine { get; set; }

    /// <summary>
    /// CSS class name for the decoration (e.g., "spell-check-error" for red wavy underline).
    /// </summary>
    public string? ClassName { get; set; }

    /// <summary>
    /// CSS class name for glyph margin (left margin icon area).
    /// </summary>
    public string? GlyphMarginClassName { get; set; }

    /// <summary>
    /// Hover message displayed in the glyph margin.
    /// </summary>
    public string? GlyphMarginHoverMessage { get; set; }

    /// <summary>
    /// CSS class name for inline content (text formatting).
    /// </summary>
    public string? InlineClassName { get; set; }

    /// <summary>
    /// Whether inline class affects letter spacing.
    /// </summary>
    public bool InlineClassNameAffectsLetterSpacing { get; set; }

    /// <summary>
    /// CSS class name for content before the range.
    /// </summary>
    public string? BeforeContentClassName { get; set; }

    /// <summary>
    /// CSS class name for content after the range.
    /// </summary>
    public string? AfterContentClassName { get; set; }

    /// <summary>
    /// Message shown in hover tooltip (the misspelled word).
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// List of spelling suggestions for this misspelling.
    /// Populated for context menu display.
    /// </summary>
    public List<SpellCheckSuggestion> Suggestions { get; set; } = new();
}

using System.Threading.Tasks;
using TextEdit.Core.Documents;

namespace TextEdit.UI.Services;

/// <summary>
/// Provides markdown formatting operations (bold, italic, code, lists, headings) for text editor.
/// Wraps selected text with markdown syntax or inserts markers at cursor position.
/// </summary>
public class MarkdownFormattingService
{
    /// <summary>
    /// Markdown format types
    /// </summary>
    public enum MarkdownFormat
    {
        H1,
        H2,
        Bold,
        Italic,
        Code,
        BulletedList,
        NumberedList
    }

    /// <summary>
    /// Apply markdown format to current selection or insert markers at cursor.
    /// </summary>
    /// <param name="currentContent">Current document content</param>
    /// <param name="selectionStart">Start index of selection (or cursor position)</param>
    /// <param name="selectionEnd">End index of selection (or cursor position)</param>
    /// <param name="format">Format type to apply</param>
    /// <returns>Result with new content and selection positions</returns>
    public FormattingResult ApplyFormat(
        string currentContent,
        int selectionStart,
        int selectionEnd,
        MarkdownFormat format)
    {
        var hasSelection = selectionEnd > selectionStart;
        var selectedText = hasSelection 
            ? currentContent.Substring(selectionStart, selectionEnd - selectionStart) 
            : string.Empty;

        var (prefix, suffix) = format switch
        {
            MarkdownFormat.H1 => ("# ", ""),
            MarkdownFormat.H2 => ("## ", ""),
            MarkdownFormat.Bold => ("**", "**"),
            MarkdownFormat.Italic => ("*", "*"),
            MarkdownFormat.Code => ("`", "`"),
            MarkdownFormat.BulletedList => ("- ", ""),
            MarkdownFormat.NumberedList => ("1. ", ""),
            _ => ("", "")
        };

        if (hasSelection)
        {
            // Wrap selection
            var before = currentContent.Substring(0, selectionStart);
            var after = currentContent.Substring(selectionEnd);
            var newContent = before + prefix + selectedText + suffix + after;
            var newSelStart = selectionStart + prefix.Length;
            var newSelEnd = newSelStart + selectedText.Length;
            return new FormattingResult(newContent, newSelStart, newSelEnd);
        }
        else
        {
            // Insert markers at cursor
            var before = currentContent.Substring(0, selectionStart);
            var after = currentContent.Substring(selectionStart);
            var newContent = before + prefix + suffix + after;
            var newCursor = selectionStart + prefix.Length; // Position cursor between markers
            return new FormattingResult(newContent, newCursor, newCursor);
        }
    }

    /// <summary>
    /// Result of applying a markdown format operation
    /// </summary>
    public record FormattingResult(string NewContent, int NewSelectionStart, int NewSelectionEnd);
}

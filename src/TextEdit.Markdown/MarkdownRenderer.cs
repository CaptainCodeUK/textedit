using Markdig;

namespace TextEdit.Markdown;

/// <summary>
/// Service for rendering markdown text content to HTML using Markdig.
/// Supports rendering any text content, whether markdown syntax is present or not.
/// </summary>
public class MarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownRenderer()
    {
        // Configure Markdig pipeline with common extensions
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // GFM tables, task lists, etc.
            .UseSoftlineBreakAsHardlineBreak() // Better for plain text display
            .Build();
    }

    /// <summary>
    /// Renders markdown text to HTML.
    /// If the text contains no markdown syntax, it will be wrapped in paragraph tags.
    /// </summary>
    /// <param name="markdownText">The markdown or plain text content to render</param>
    /// <returns>HTML string representation of the rendered content</returns>
    public string RenderToHtml(string markdownText)
    {
        if (string.IsNullOrEmpty(markdownText))
        {
            return string.Empty;
        }

        return Markdig.Markdown.ToHtml(markdownText, _pipeline);
    }

    /// <summary>
    /// Checks if rendering would be expensive (for large files).
    /// Returns true if the content exceeds recommended size for auto-refresh.
    /// </summary>
    /// <param name="text">Text content to check</param>
    /// <param name="thresholdKb">Size threshold in KB (default 100KB)</param>
    /// <returns>True if content is large and should use manual refresh</returns>
    public bool IsLargeContent(string text, int thresholdKb = 100)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(text);
        var sizeKb = sizeBytes / 1024.0;
        return sizeKb > thresholdKb;
    }
}

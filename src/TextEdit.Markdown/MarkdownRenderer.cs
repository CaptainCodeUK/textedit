using Markdig;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace TextEdit.Markdown;

/// <summary>
/// Service for rendering markdown text content to HTML using Markdig.
/// Supports rendering any text content, whether markdown syntax is present or not.
/// Includes result caching to avoid redundant rendering of identical content.
/// </summary>
public class MarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly int _maxCacheEntries;

    private record CacheEntry(string Html, DateTime LastAccessed);

    public MarkdownRenderer()
        : this(maxCacheEntries: 100)
    {
    }

    public MarkdownRenderer(int maxCacheEntries)
    {
        // Configure Markdig pipeline with common extensions
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // GFM tables, task lists, etc.
            .UseSoftlineBreakAsHardlineBreak() // Better for plain text display
            .Build();

        _maxCacheEntries = maxCacheEntries;
        _cache = new ConcurrentDictionary<string, CacheEntry>();
    }

    /// <summary>
    /// Renders markdown text to HTML.
    /// If the text contains no markdown syntax, it will be wrapped in paragraph tags.
    /// Results are cached based on content hash to avoid redundant rendering.
    /// </summary>
    /// <param name="markdownText">The markdown or plain text content to render</param>
    /// <returns>HTML string representation of the rendered content</returns>
    public string RenderToHtml(string markdownText)
    {
        if (string.IsNullOrEmpty(markdownText))
        {
            return string.Empty;
        }

        // Generate content hash for cache lookup
        var contentHash = ComputeHash(markdownText);

        // Check cache first
        if (_cache.TryGetValue(contentHash, out var cached))
        {
            // Update last accessed time
            _cache[contentHash] = cached with { LastAccessed = DateTime.UtcNow };
            return cached.Html;
        }

        // Render markdown to HTML
        var html = Markdig.Markdown.ToHtml(markdownText, _pipeline);

        // Store in cache (with eviction if needed)
        if (_cache.Count >= _maxCacheEntries)
        {
            EvictOldestEntry();
        }

        _cache[contentHash] = new CacheEntry(html, DateTime.UtcNow);

        return html;
    }

    /// <summary>
    /// Clears the result cache. Useful for testing or memory management.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets the current number of cached results.
    /// </summary>
    public int CacheCount => _cache.Count;

    private static string ComputeHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private void EvictOldestEntry()
    {
        var oldest = _cache
            .OrderBy(kvp => kvp.Value.LastAccessed)
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(oldest.Key))
        {
            _cache.TryRemove(oldest.Key, out _);
        }
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

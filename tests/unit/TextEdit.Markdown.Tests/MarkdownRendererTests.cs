using System;
using System.Reflection;
using System.Collections.Generic;
using TextEdit.Markdown;
using Xunit;

public class MarkdownRendererTests
{
    [Fact]
    public void RenderToHtml_BasicMarkdown_ProducesExpectedHtml()
    {
        var renderer = new MarkdownRenderer();
        var html = renderer.RenderToHtml("# Hello\n\n- Item 1\n- Item 2");
        Assert.Contains("<h1>", html);
        Assert.Contains("Item 1", html);
        Assert.Contains("<ul>", html);
    }

    [Fact]
    public void RenderToHtml_CachesResults_ReturnsSameHtmlOnRepeat()
    {
        var renderer = new MarkdownRenderer();
        var input = "# Title\n\nText";
        var html1 = renderer.RenderToHtml(input);
        var html2 = renderer.RenderToHtml(input);
        Assert.Equal(html1, html2);
    }

    [Fact]
    public void RenderToHtml_DifferentInputs_ProduceDifferentHtml()
    {
        var renderer = new MarkdownRenderer();
        var html1 = renderer.RenderToHtml("# Title");
        var html2 = renderer.RenderToHtml("# Other");
        Assert.NotEqual(html1, html2);
    }

    [Fact]
    public void Cache_EvictsOnCapacityExceeded_ReflectsPrivateCacheSize()
    {
        var renderer = new MarkdownRenderer();
        var field = typeof(MarkdownRenderer).GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var cache = (IDictionary<string, object>)field.GetValue(renderer)!;
        for (int i = 0; i < 110; i++)
        {
            renderer.RenderToHtml($"# Title {i}");
        }
        // Should not exceed 100 entries
        Assert.True(cache.Count <= 100);
    }
}

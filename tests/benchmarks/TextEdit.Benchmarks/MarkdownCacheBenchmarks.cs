using BenchmarkDotNet.Attributes;
using TextEdit.Markdown;

namespace TextEdit.Benchmarks;

/// <summary>
/// Benchmarks to measure the effectiveness of MarkdownRenderer caching.
/// Compares cold (first render) vs warm (cached) performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class MarkdownCacheBenchmarks
{
    private MarkdownRenderer _renderer = null!;
    private string _smallMarkdown = string.Empty;
    private string _mediumMarkdown = string.Empty;
    private string _largeMarkdown = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _renderer = new MarkdownRenderer();
        
        // Generate test data
        _smallMarkdown = GenerateSimpleMarkdown(50); // ~2KB
        _mediumMarkdown = GenerateMediumMarkdown(150); // ~10KB
        _largeMarkdown = GenerateComplexMarkdown(500); // ~50KB
    }

    [IterationSetup(Target = nameof(Render_Small_ColdCache))]
    public void ClearCacheForSmall() => _renderer.ClearCache();

    [IterationSetup(Target = nameof(Render_Medium_ColdCache))]
    public void ClearCacheForMedium() => _renderer.ClearCache();

    [IterationSetup(Target = nameof(Render_Large_ColdCache))]
    public void ClearCacheForLarge() => _renderer.ClearCache();

    [Benchmark]
    public string Render_Small_ColdCache()
    {
        return _renderer.RenderToHtml(_smallMarkdown);
    }

    [Benchmark]
    public string Render_Small_WarmCache()
    {
        // First render to populate cache
        _renderer.RenderToHtml(_smallMarkdown);
        // Measure cached access
        return _renderer.RenderToHtml(_smallMarkdown);
    }

    [Benchmark]
    public string Render_Medium_ColdCache()
    {
        return _renderer.RenderToHtml(_mediumMarkdown);
    }

    [Benchmark]
    public string Render_Medium_WarmCache()
    {
        _renderer.RenderToHtml(_mediumMarkdown);
        return _renderer.RenderToHtml(_mediumMarkdown);
    }

    [Benchmark]
    public string Render_Large_ColdCache()
    {
        return _renderer.RenderToHtml(_largeMarkdown);
    }

    [Benchmark]
    public string Render_Large_WarmCache()
    {
        _renderer.RenderToHtml(_largeMarkdown);
        return _renderer.RenderToHtml(_largeMarkdown);
    }

    [Benchmark]
    public void Render_MultipleDocuments_CacheEffectiveness()
    {
        // Simulate editing session with 10 unique documents, each rendered 5 times
        var documents = new List<string>(10);
        for (int i = 0; i < 10; i++)
        {
            documents.Add(GenerateSimpleMarkdown(50 + i * 10));
        }

        // Render each document multiple times (simulating preview updates)
        foreach (var doc in documents)
        {
            for (int i = 0; i < 5; i++)
            {
                _renderer.RenderToHtml(doc);
            }
        }
    }

    private static string GenerateSimpleMarkdown(int paragraphs)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < paragraphs; i++)
        {
            sb.AppendLine($"## Heading {i + 1}");
            sb.AppendLine();
            sb.AppendLine($"This is **paragraph {i + 1}** with some *italic* text and a [link](https://example.com/{i}).");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string GenerateMediumMarkdown(int sections)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < sections; i++)
        {
            sb.AppendLine($"### Section {i + 1}");
            sb.AppendLine();
            sb.AppendLine($"- Item 1 for section {i + 1}");
            sb.AppendLine($"- Item 2 for section {i + 1}");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine($"var x = {i};");
            sb.AppendLine("Console.WriteLine(x);");
            sb.AppendLine("```");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string GenerateComplexMarkdown(int rows)
    {
        var sb = new System.Text.StringBuilder();
        
        // Add tables (expensive to render)
        sb.AppendLine("| Column 1 | Column 2 | Column 3 |");
        sb.AppendLine("|----------|----------|----------|");
        for (int i = 0; i < rows / 5; i++)
        {
            sb.AppendLine($"| Data {i}a | Data {i}b | Data {i}c |");
        }
        sb.AppendLine();

        // Add nested lists
        for (int i = 0; i < rows / 10; i++)
        {
            sb.AppendLine($"- [ ] Task {i + 1}");
            sb.AppendLine($"  - Subtask {i + 1}.1");
            sb.AppendLine($"    - Subtask {i + 1}.1.1");
        }
        
        return sb.ToString();
    }
}

using BenchmarkDotNet.Attributes;
using System.Text;
using TextEdit.Markdown;

namespace TextEdit.Benchmarks;

/// <summary>
/// Benchmarks for MarkdownRenderer to identify performance characteristics
/// and optimization opportunities for preview rendering.
/// Tests various document sizes and markdown complexity levels.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class MarkdownRendererBenchmarks
{
    private MarkdownRenderer _renderer = null!;
    
    // Test data of various sizes and complexities
    private string _plainText = null!;
    private string _simpleMarkdown = null!;
    private string _mediumMarkdown = null!;
    private string _complexMarkdown = null!;
    private string _largeDocument = null!;
    private string _veryLargeDocument = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _renderer = new MarkdownRenderer();
        
        // Plain text (1KB) - no markdown formatting
        _plainText = string.Concat(Enumerable.Repeat("This is plain text without any markdown formatting. ", 20));
        
        // Simple markdown (2KB) - basic formatting only
        _simpleMarkdown = GenerateSimpleMarkdown(2048);
        
        // Medium markdown (10KB) - moderate complexity
        _mediumMarkdown = GenerateMediumMarkdown(10 * 1024);
        
        // Complex markdown (50KB) - tables, lists, code blocks, links
        _complexMarkdown = GenerateComplexMarkdown(50 * 1024);
        
        // Large document (100KB) - threshold for manual refresh
        _largeDocument = GenerateComplexMarkdown(100 * 1024);
        
        // Very large document (500KB) - stress test
        _veryLargeDocument = GenerateComplexMarkdown(500 * 1024);
    }
    
    private static string GenerateSimpleMarkdown(int targetSize)
    {
        var sb = new StringBuilder(targetSize);
        var iteration = 0;
        
        while (sb.Length < targetSize)
        {
            sb.AppendLine($"# Heading {iteration}");
            sb.AppendLine();
            sb.AppendLine("This is a paragraph with **bold** and *italic* text.");
            sb.AppendLine();
            sb.AppendLine("Another paragraph with a [link](https://example.com).");
            sb.AppendLine();
            iteration++;
        }
        
        return sb.ToString(0, Math.Min(sb.Length, targetSize));
    }
    
    private static string GenerateMediumMarkdown(int targetSize)
    {
        var sb = new StringBuilder(targetSize);
        var iteration = 0;
        
        while (sb.Length < targetSize)
        {
            sb.AppendLine($"## Section {iteration}");
            sb.AppendLine();
            sb.AppendLine("Paragraph with **bold**, *italic*, and `code` formatting.");
            sb.AppendLine();
            sb.AppendLine("- Bullet point 1");
            sb.AppendLine("- Bullet point 2 with **bold**");
            sb.AppendLine("- Bullet point 3 with *italic*");
            sb.AppendLine();
            sb.AppendLine("1. Numbered item");
            sb.AppendLine("2. Another item");
            sb.AppendLine("3. Third item");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine("public class Example {");
            sb.AppendLine("    public void Method() { }");
            sb.AppendLine("}");
            sb.AppendLine("```");
            sb.AppendLine();
            iteration++;
        }
        
        return sb.ToString(0, Math.Min(sb.Length, targetSize));
    }
    
    private static string GenerateComplexMarkdown(int targetSize)
    {
        var sb = new StringBuilder(targetSize);
        var iteration = 0;
        
        while (sb.Length < targetSize)
        {
            sb.AppendLine($"## Complex Section {iteration}");
            sb.AppendLine();
            
            // Tables (expensive to render)
            sb.AppendLine("| Column 1 | Column 2 | Column 3 | Column 4 |");
            sb.AppendLine("|----------|----------|----------|----------|");
            for (int i = 0; i < 5; i++)
            {
                sb.AppendLine($"| Cell {i},0 | Cell {i},1 | Cell {i},2 | Cell {i},3 |");
            }
            sb.AppendLine();
            
            // Nested lists
            sb.AppendLine("- Level 1 item");
            sb.AppendLine("  - Level 2 item with **bold**");
            sb.AppendLine("    - Level 3 item with *italic*");
            sb.AppendLine("      - Level 4 item with `code`");
            sb.AppendLine();
            
            // Task lists (GFM extension)
            sb.AppendLine("- [x] Completed task");
            sb.AppendLine("- [ ] Pending task");
            sb.AppendLine("- [x] Another completed task");
            sb.AppendLine();
            
            // Code blocks with syntax
            sb.AppendLine("```javascript");
            sb.AppendLine("function example(param) {");
            sb.AppendLine("  const result = param * 2;");
            sb.AppendLine("  return result;");
            sb.AppendLine("}");
            sb.AppendLine("```");
            sb.AppendLine();
            
            // Block quotes
            sb.AppendLine("> This is a quote");
            sb.AppendLine("> with multiple lines");
            sb.AppendLine("> and **formatting**");
            sb.AppendLine();
            
            // Links and images
            sb.AppendLine("Check out [this link](https://example.com) and ![this image](https://example.com/image.png).");
            sb.AppendLine();
            
            // Horizontal rule
            sb.AppendLine("---");
            sb.AppendLine();
            
            iteration++;
        }
        
        return sb.ToString(0, Math.Min(sb.Length, targetSize));
    }
    
    [Benchmark]
    public string Render_PlainText_1KB()
    {
        return _renderer.RenderToHtml(_plainText);
    }
    
    [Benchmark]
    public string Render_SimpleMarkdown_2KB()
    {
        return _renderer.RenderToHtml(_simpleMarkdown);
    }
    
    [Benchmark]
    public string Render_MediumMarkdown_10KB()
    {
        return _renderer.RenderToHtml(_mediumMarkdown);
    }
    
    [Benchmark]
    public string Render_ComplexMarkdown_50KB()
    {
        return _renderer.RenderToHtml(_complexMarkdown);
    }
    
    [Benchmark]
    public string Render_LargeDocument_100KB()
    {
        // This is the threshold for manual refresh
        return _renderer.RenderToHtml(_largeDocument);
    }
    
    [Benchmark]
    public string Render_VeryLargeDocument_500KB()
    {
        // Stress test - would definitely need manual refresh or optimization
        return _renderer.RenderToHtml(_veryLargeDocument);
    }
    
    [Benchmark]
    public bool CheckIsLargeContent_Small()
    {
        return _renderer.IsLargeContent(_simpleMarkdown);
    }
    
    [Benchmark]
    public bool CheckIsLargeContent_Large()
    {
        return _renderer.IsLargeContent(_largeDocument);
    }
}

using BenchmarkDotNet.Attributes;
using System.Text;
using TextEdit.Core.Abstractions;
using TextEdit.Core.Documents;
using TextEdit.Core.Editing;
using TextEdit.Infrastructure.FileSystem;

namespace TextEdit.Benchmarks;

/// <summary>
/// Benchmarks for DocumentService operations: OpenAsync, SaveAsync, and UpdateContent.
/// Covers small files, large files (streaming), and various encoding scenarios.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class DocumentServiceBenchmarks
{
    private IFileSystem _fileSystem = null!;
    private UndoRedoService _undoRedo = null!;
    private DocumentService _documentService = null!;
    
    private string _smallFilePath = null!;
    private string _largeFilePath = null!;
    private string _veryLargeFilePath = null!;
    
    private const int SmallFileSize = 10 * 1024; // 10KB
    private const int LargeFileSize = 5 * 1024 * 1024; // 5MB (below 10MB threshold)
    private const int VeryLargeFileSize = 15 * 1024 * 1024; // 15MB (above 10MB threshold - triggers streaming)
    
    [GlobalSetup]
    public void Setup()
    {
        _fileSystem = new FileSystemService();
        _undoRedo = new UndoRedoService();
        _documentService = new DocumentService(_fileSystem, _undoRedo);
        
        // Create temp files with different sizes
        _smallFilePath = Path.GetTempFileName();
        _largeFilePath = Path.GetTempFileName();
        _veryLargeFilePath = Path.GetTempFileName();
        
        // Small file: 10KB of text
        var smallContent = new string('a', SmallFileSize);
        File.WriteAllText(_smallFilePath, smallContent, Encoding.UTF8);
        
        // Large file: 5MB (under threshold)
        var largeContent = GenerateRealisticContent(LargeFileSize);
        File.WriteAllText(_largeFilePath, largeContent, Encoding.UTF8);
        
        // Very large file: 15MB (over 10MB threshold - will trigger streaming)
        var veryLargeContent = GenerateRealisticContent(VeryLargeFileSize);
        File.WriteAllText(_veryLargeFilePath, veryLargeContent, Encoding.UTF8);
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_smallFilePath)) File.Delete(_smallFilePath);
        if (File.Exists(_largeFilePath)) File.Delete(_largeFilePath);
        if (File.Exists(_veryLargeFilePath)) File.Delete(_veryLargeFilePath);
    }
    
    private static string GenerateRealisticContent(int targetSize)
    {
        // Generate realistic text content with line breaks and words
        var sb = new StringBuilder(targetSize);
        var words = new[] { "Lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit" };
        var random = new Random(42); // Fixed seed for consistency
        
        while (sb.Length < targetSize)
        {
            // Add a sentence
            for (int i = 0; i < 10 && sb.Length < targetSize; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(words[random.Next(words.Length)]);
            }
            
            sb.AppendLine(".");
        }
        
        return sb.ToString(0, Math.Min(sb.Length, targetSize));
    }
    
    [Benchmark]
    public async Task<Document> OpenAsync_SmallFile()
    {
        return await _documentService.OpenAsync(_smallFilePath);
    }
    
    [Benchmark]
    public async Task<Document> OpenAsync_LargeFile_BelowThreshold()
    {
        // 5MB - uses standard ReadAllTextAsync
        return await _documentService.OpenAsync(_largeFilePath);
    }
    
    [Benchmark]
    public async Task<Document> OpenAsync_VeryLargeFile_AboveThreshold()
    {
        // 15MB - triggers streaming with ReadLargeFileAsync
        return await _documentService.OpenAsync(_veryLargeFilePath);
    }
    
    [Benchmark]
    public async Task SaveAsync_SmallFile()
    {
        var doc = await _documentService.OpenAsync(_smallFilePath);
        doc.SetContent(doc.Content + " modified");
        await _documentService.SaveAsync(doc);
        return;
    }
    
    [Benchmark]
    public async Task SaveAsync_LargeFile_BelowThreshold()
    {
        var doc = await _documentService.OpenAsync(_largeFilePath);
        doc.SetContent(doc.Content + " modified");
        await _documentService.SaveAsync(doc);
        return;
    }
    
    [Benchmark]
    public void UpdateContent_SmallDocument()
    {
        var doc = new Document();
        doc.SetContentInternal(new string('x', SmallFileSize));
        _undoRedo.Attach(doc, doc.Content);
        
        // Simulate typing
        for (int i = 0; i < 100; i++)
        {
            _documentService.UpdateContent(doc, doc.Content + "a");
        }
    }
    
    [Benchmark]
    public void UpdateContent_LargeDocument()
    {
        var doc = new Document();
        var initialContent = GenerateRealisticContent(LargeFileSize);
        doc.SetContentInternal(initialContent);
        _undoRedo.Attach(doc, doc.Content);
        
        // Simulate typing on large document
        for (int i = 0; i < 50; i++)
        {
            _documentService.UpdateContent(doc, doc.Content + "a");
        }
    }
}

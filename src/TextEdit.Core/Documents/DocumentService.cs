namespace TextEdit.Core.Documents;

using System.Text;
using System.IO;
using TextEdit.Core.Abstractions;
using TextEdit.Core.Editing;

/// <summary>
/// Handles creation, loading, saving, and updating of documents.
/// </summary>
public class DocumentService
{
    private readonly IFileSystem _fs;
    private readonly IUndoRedoService _undo;

    public DocumentService(IFileSystem fs, IUndoRedoService undo)
    {
        _fs = fs;
        _undo = undo;
    }

    public Document NewDocument()
    {
        var doc = new Document();
        _undo.Attach(doc, "");
        return doc;
    }

    public async Task<Document> OpenAsync(string path, Encoding? encoding = null)
    {
        if (!_fs.FileExists(path)) throw new FileNotFoundException("File not found", path);
        
        // T060: Check file size and mark large files as read-only
        var fileInfo = new FileInfo(path);
        const long largeFileThreshold = 10 * 1024 * 1024; // 10MB
        bool isLargeFile = fileInfo.Length >= largeFileThreshold;
        
        var doc = new Document();
        var enc = encoding ?? doc.Encoding;
        var text = await _fs.ReadAllTextAsync(path, enc);
        
        // Normalize EOL to \n in memory
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        
        // Use internal method to bypass read-only check during initial load
        doc.SetContentInternal(text);
        doc.MarkSaved(path);
        
        if (isLargeFile)
        {
            doc.MarkReadOnly(true);
            Console.WriteLine($"[DocumentService] Large file detected ({fileInfo.Length} bytes), opening in read-only mode: {path}");
        }
        
        _undo.Attach(doc, doc.Content);
        // Opening should not mark dirty
        doc.MarkSaved(path);
        return doc;
    }

    public void UpdateContent(Document doc, string content)
    {
        if (doc.Content == content) return;
        doc.SetContent(content);
        _undo.Push(doc, content);
    }

    public async Task SaveAsync(Document doc, string? path = null)
    {
        var targetPath = path ?? doc.FilePath;
        if (string.IsNullOrWhiteSpace(targetPath)) throw new InvalidOperationException("No path specified for save.");
        
        // T057: Detect conflicts - check if file was modified externally since last load
        if (!string.IsNullOrWhiteSpace(doc.FilePath) && _fs.FileExists(doc.FilePath))
        {
            try
            {
                var currentDiskContent = await _fs.ReadAllTextAsync(doc.FilePath, doc.Encoding);
                // Normalize for comparison
                currentDiskContent = currentDiskContent.Replace("\r\n", "\n").Replace('\r', '\n');
                
                // If doc was originally loaded from this file and content on disk differs from original
                // This indicates an external modification - potential conflict
                // For now, just log it. Full conflict resolution requires UI prompting.
                if (currentDiskContent != doc.Content && !doc.IsDirty)
                {
                    Console.WriteLine($"[DocumentService] Warning: File may have been modified externally: {doc.FilePath}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Can't read for comparison - proceed with save attempt
            }
            catch (IOException)
            {
                // Can't read for comparison - proceed with save attempt  
            }
        }
        
        var text = doc.Content;
        // Apply desired EOL
        if (doc.Eol != "\n")
        {
            text = text.Replace("\n", doc.Eol);
        }
        
        // T059: Handle permission-denied on save
        // UnauthorizedAccessException will bubble up to AppState where it's caught
        await _fs.WriteAllTextAsync(targetPath!, text, doc.Encoding);
        doc.MarkSaved(targetPath);
    }
}

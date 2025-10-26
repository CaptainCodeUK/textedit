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
        var doc = new Document();
        var enc = encoding ?? doc.Encoding;
        var text = await _fs.ReadAllTextAsync(path, enc);
        // Normalize EOL to \n in memory
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        doc.SetContent(text);
        doc.MarkSaved(path);
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
        var text = doc.Content;
        // Apply desired EOL
        if (doc.Eol != "\n")
        {
            text = text.Replace("\n", doc.Eol);
        }
        await _fs.WriteAllTextAsync(targetPath!, text, doc.Encoding);
        doc.MarkSaved(targetPath);
    }
}

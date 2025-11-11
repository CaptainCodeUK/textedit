namespace TextEdit.Core.Documents;

using System.Text;
using System.IO;
using TextEdit.Core.Abstractions;
using TextEdit.Core.Editing;

/// <summary>
/// Handles creation, loading, saving, and updating of documents.
/// Optimized for large file handling (>10MB) with streaming.
/// </summary>
/// <summary>
/// Service responsible for creating, opening, updating, and saving <see cref="Document"/> instances.
/// Uses streaming I/O for large files and normalizes in-memory line endings to <c>\n</c>.
/// </summary>
public class DocumentService
{
    private readonly IFileSystem _fs;
    private readonly IUndoRedoService _undo;
    private readonly TextEdit.Core.Searching.ReplaceService? _replace;
    private readonly IAppLogger? _logger;
    private const long LargeFileThreshold = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Initializes a new instance of <see cref="DocumentService"/>.
    /// </summary>
    /// <param name="fs">File system abstraction for I/O operations.</param>
    /// <param name="undo">Undo/redo service to attach to documents.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public DocumentService(IFileSystem fs, IUndoRedoService undo, IAppLogger? logger = null, TextEdit.Core.Searching.ReplaceService? replace = null)
    {
        _fs = fs;
        _undo = undo;
        _logger = logger;
        _replace = replace; // optional to keep backward compatibility in tests
    }

    /// <summary>
    /// Creates a new unsaved document and attaches it to the undo/redo stack.
    /// </summary>
    /// <returns>The new document instance.</returns>
    public Document NewDocument()
    {
        var doc = new Document();
        _undo.Attach(doc, "");
        return doc;
    }

    /// <summary>
    /// Opens a document from disk. Large files (>= 10MB) are read using streaming and marked read-only.
    /// </summary>
    /// <param name="path">Absolute path to the file.</param>
    /// <param name="encoding">Optional text encoding. Defaults to UTF-8 without BOM.</param>
    /// <param name="progress">Optional progress reporter (0-100) for large reads.</param>
    /// <param name="cancellationToken">Cancellation token for I/O operations.</param>
    /// <returns>The loaded <see cref="Document"/>.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the path does not exist.</exception>
    public async Task<Document> OpenAsync(string path, Encoding? encoding = null, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Opening file: {Path}", path);
        
        if (!_fs.FileExists(path))
        {
            _logger?.LogError("File not found: {Path}", path);
            throw new FileNotFoundException("File not found", path);
        }
        
        var fileSize = _fs.GetFileSize(path);
        bool isLargeFile = fileSize >= LargeFileThreshold;
        
        _logger?.LogDebug("File size: {Size} bytes, IsLarge: {IsLarge}", fileSize, isLargeFile);
        
        var doc = new Document();
        var enc = encoding ?? doc.Encoding;
        
        // Use streaming for large files
        string text;
        if (isLargeFile)
        {
            _logger?.LogInformation("Using streaming read for large file: {Path}", path);
            text = await _fs.ReadLargeFileAsync(path, enc, progress, cancellationToken);
        }
        else
        {
            text = await _fs.ReadAllTextAsync(path, enc);
        }
        
        // Normalize EOL to \n in memory
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        
        // Use internal method to bypass read-only check during initial load
        doc.SetContentInternal(text);
        doc.MarkSaved(path);
        
        if (isLargeFile)
        {
            _logger?.LogInformation("File marked as read-only due to size: {Path}", path);
            doc.MarkReadOnly(true);
        }
        
        _undo.Attach(doc, doc.Content);
        // Opening should not mark dirty
        doc.MarkSaved(path);
        
        _logger?.LogInformation("File opened successfully: {Path}", path);
        return doc;
    }

    /// <summary>
    /// Updates the content of a document and records an undo checkpoint when the content changes.
    /// </summary>
    /// <param name="doc">Target document.</param>
    /// <param name="content">New content to set.</param>
    public void UpdateContent(Document doc, string content)
    {
        if (doc.Content == content) return;
        doc.SetContent(content);
        _undo.Push(doc, content);
    }

    /// <summary>
    /// Applies a Replace All operation to the document content as a single atomic change (one undo step).
    /// </summary>
    /// <param name="doc">Target document.</param>
    /// <param name="operation">Replace operation.</param>
    /// <returns>The number of replacements made.</returns>
    /// <exception cref="InvalidOperationException">Thrown if ReplaceService was not provided.</exception>
    public int ReplaceAll(Document doc, TextEdit.Core.Searching.ReplaceOperation operation)
    {
        if (_replace is null) throw new InvalidOperationException("ReplaceService is not available.");
        var (newText, count) = _replace.ReplaceAll(doc.Content, operation);
        if (count > 0)
        {
            UpdateContent(doc, newText);
        }
        return count;
    }

    /// <summary>
    /// Replaces the next match at or after the given caret position as a single atomic change.
    /// </summary>
    /// <param name="doc">Target document.</param>
    /// <param name="operation">Replace operation.</param>
    /// <param name="caretPosition">Caret index to start searching from.</param>
    /// <param name="newCaretPosition">Outputs the caret index after the replacement (at end of inserted replacement).</param>
    /// <returns>True when a replacement was performed; otherwise false.</returns>
    /// <exception cref="InvalidOperationException">Thrown if ReplaceService was not provided.</exception>
    public bool ReplaceNext(Document doc, TextEdit.Core.Searching.ReplaceOperation operation, int caretPosition, out int newCaretPosition)
    {
        if (_replace is null) throw new InvalidOperationException("ReplaceService is not available.");
        var (newText, replacedIndex, replacedLength) = _replace.ReplaceNextAtOrAfter(doc.Content, operation, caretPosition);
        if (replacedIndex < 0)
        {
            newCaretPosition = caretPosition;
            return false;
        }

        UpdateContent(doc, newText);
        newCaretPosition = replacedIndex + operation.Replacement.Length;
        return true;
    }

    /// <summary>
    /// Saves a document to disk. Uses streaming for large content and applies the document's preferred EOL.
    /// </summary>
    /// <param name="doc">Document to save.</param>
    /// <param name="path">Optional override path. When null, uses <see cref="Document.FilePath"/>.</param>
    /// <param name="progress">Optional progress reporter (0-100) for large writes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when no path is available.</exception>
    public async Task SaveAsync(Document doc, string? path = null, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        var targetPath = path ?? doc.FilePath;
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            _logger?.LogError("No path specified for save");
            throw new InvalidOperationException("No path specified for save.");
        }
        
        _logger?.LogInformation("Saving document to: {Path}", targetPath);
        
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
                    _logger?.LogWarning("External modification detected for: {Path}", doc.FilePath);
                    // External modification detected; UI will surface conflicts when needed
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger?.LogWarning(ex, "Cannot read file for conflict detection: {Path}", doc.FilePath);
                // Can't read for comparison - proceed with save attempt
            }
            catch (IOException ex)
            {
                _logger?.LogWarning(ex, "Cannot read file for conflict detection: {Path}", doc.FilePath);
                // Can't read for comparison - proceed with save attempt  
            }
        }
        
        var text = doc.Content;
        // Apply desired EOL
        if (doc.Eol != "\n")
        {
            text = text.Replace("\n", doc.Eol);
        }
        
        // Use streaming for large content
        var contentSize = text.Length * doc.Encoding.GetByteCount("a"); // Approximate byte size
        if (contentSize >= LargeFileThreshold)
        {
            _logger?.LogInformation("Using streaming write for large file: {Path}", targetPath);
            await _fs.WriteLargeFileAsync(targetPath!, text, doc.Encoding, progress, cancellationToken);
        }
        else
        {
            // T059: Handle permission-denied on save
            // UnauthorizedAccessException will bubble up to AppState where it's caught
            await _fs.WriteAllTextAsync(targetPath!, text, doc.Encoding);
        }
        
        doc.MarkSaved(targetPath);
        _logger?.LogInformation("Document saved successfully: {Path}", targetPath);
    }
}

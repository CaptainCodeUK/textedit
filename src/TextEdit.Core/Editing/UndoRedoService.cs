namespace TextEdit.Core.Editing;

using TextEdit.Core.Documents;

/// <summary>
/// Contract for per-document undo/redo operations.
/// Implementations may choose different storage strategies (full-text snapshots, diffs, etc.).
/// </summary>
public interface IUndoRedoService
{
    /// <summary>
    /// Initializes undo/redo tracking for the given document.
    /// </summary>
    /// <param name="doc">Document to track.</param>
    /// <param name="initialContent">Initial content checkpoint; defaults to empty.</param>
    void Attach(Document doc, string initialContent = "");

    /// <summary>
    /// Adds a checkpoint to the undo stack and clears redo history.
    /// </summary>
    /// <param name="doc">Document being modified.</param>
    /// <param name="content">Document content to record.</param>
    void Push(Document doc, string content);

    /// <summary>
    /// Returns whether an undo operation is available for the document.
    /// </summary>
    /// <param name="documentId">Target document identifier.</param>
    bool CanUndo(Guid documentId);

    /// <summary>
    /// Returns whether a redo operation is available for the document.
    /// </summary>
    /// <param name="documentId">Target document identifier.</param>
    bool CanRedo(Guid documentId);

    /// <summary>
    /// Performs an undo and returns the previous content, or null when not possible.
    /// </summary>
    /// <param name="documentId">Target document identifier.</param>
    /// <returns>The content to restore, or null if undo is not available.</returns>
    string? Undo(Guid documentId);

    /// <summary>
    /// Performs a redo and returns the next content, or null when not possible.
    /// </summary>
    /// <param name="documentId">Target document identifier.</param>
    /// <returns>The content to restore, or null if redo is not available.</returns>
    string? Redo(Guid documentId);

    /// <summary>
    /// Clears both undo and redo history for the document.
    /// </summary>
    /// <param name="documentId">Target document identifier.</param>
    void Clear(Guid documentId);
}

public class UndoRedoService : IUndoRedoService
{
    private readonly Dictionary<Guid, Stack<string>> _undo = new();
    private readonly Dictionary<Guid, Stack<string>> _redo = new();

    /// <summary>
    /// Initializes stacks for a document and optionally seeds the undo stack with an initial checkpoint.
    /// </summary>
    /// <param name="doc">Document to track.</param>
    /// <param name="initialContent">Optional initial content.</param>
    public void Attach(Document doc, string initialContent = "")
    {
        _undo[doc.Id] = new Stack<string>();
        _redo[doc.Id] = new Stack<string>();
        // Always push the initial content to establish a baseline for undo
        _undo[doc.Id].Push(initialContent);
    }

    /// <summary>
    /// Pushes a new content snapshot onto the undo stack and clears redo history.
    /// </summary>
    /// <param name="doc">Document being modified.</param>
    /// <param name="content">Current content to record.</param>
    public void Push(Document doc, string content)
    {
        if (!_undo.ContainsKey(doc.Id)) Attach(doc);
        _undo[doc.Id].Push(content);
        _redo[doc.Id].Clear();
    }

    /// <inheritdoc />
    public bool CanUndo(Guid documentId) => _undo.TryGetValue(documentId, out var s) && s.Count > 1;

    /// <inheritdoc />
    public bool CanRedo(Guid documentId) => _redo.TryGetValue(documentId, out var s) && s.Count > 0;

    /// <summary>
    /// Pops the current content to redo stack and returns the previous snapshot.
    /// </summary>
    /// <param name="documentId">Target document identifier.</param>
    /// <returns>Previous content or null if not available.</returns>
    public string? Undo(Guid documentId)
    {
        if (!CanUndo(documentId)) return null;
        var current = _undo[documentId].Pop();
        _redo[documentId].Push(current);
        return _undo[documentId].Peek();
    }

    /// <summary>
    /// Applies the next content from redo stack and returns it.
    /// </summary>
    /// <param name="documentId">Target document identifier.</param>
    /// <returns>Next content or null if not available.</returns>
    public string? Redo(Guid documentId)
    {
        if (!CanRedo(documentId)) return null;
        var next = _redo[documentId].Pop();
        _undo[documentId].Push(next);
        return next;
    }

    /// <summary>
    /// Clears all history for the specified document.
    /// </summary>
    /// <param name="documentId">Target document identifier.</param>
    public void Clear(Guid documentId)
    {
        if (_undo.ContainsKey(documentId)) _undo[documentId].Clear();
        if (_redo.ContainsKey(documentId)) _redo[documentId].Clear();
    }
}

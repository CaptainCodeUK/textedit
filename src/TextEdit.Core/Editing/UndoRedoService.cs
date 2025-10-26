namespace TextEdit.Core.Editing;

using TextEdit.Core.Documents;

/// <summary>
/// Simple undo/redo stack per document. Stores full-text snapshots for now.
/// </summary>
public interface IUndoRedoService
{
    void Attach(Document doc, string initialContent = "");
    void Push(Document doc, string content);
    bool CanUndo(Guid documentId);
    bool CanRedo(Guid documentId);
    string? Undo(Guid documentId);
    string? Redo(Guid documentId);
    void Clear(Guid documentId);
}

public class UndoRedoService : IUndoRedoService
{
    private readonly Dictionary<Guid, Stack<string>> _undo = new();
    private readonly Dictionary<Guid, Stack<string>> _redo = new();

    public void Attach(Document doc, string initialContent = "")
    {
        _undo[doc.Id] = new Stack<string>();
        _redo[doc.Id] = new Stack<string>();
        if (initialContent is not null)
        {
            _undo[doc.Id].Push(initialContent);
        }
    }

    public void Push(Document doc, string content)
    {
        if (!_undo.ContainsKey(doc.Id)) Attach(doc);
        _undo[doc.Id].Push(content);
        _redo[doc.Id].Clear();
    }

    public bool CanUndo(Guid documentId) => _undo.TryGetValue(documentId, out var s) && s.Count > 1;
    public bool CanRedo(Guid documentId) => _redo.TryGetValue(documentId, out var s) && s.Count > 0;

    public string? Undo(Guid documentId)
    {
        if (!CanUndo(documentId)) return null;
        var current = _undo[documentId].Pop();
        _redo[documentId].Push(current);
        return _undo[documentId].Peek();
    }

    public string? Redo(Guid documentId)
    {
        if (!CanRedo(documentId)) return null;
        var next = _redo[documentId].Pop();
        _undo[documentId].Push(next);
        return next;
    }

    public void Clear(Guid documentId)
    {
        if (_undo.ContainsKey(documentId)) _undo[documentId].Clear();
        if (_redo.ContainsKey(documentId)) _redo[documentId].Clear();
    }
}

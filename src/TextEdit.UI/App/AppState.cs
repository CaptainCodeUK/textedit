using Microsoft.AspNetCore.Components;
using TextEdit.Core.Documents;
using TextEdit.Core.Editing;
using TextEdit.Infrastructure.Ipc;
using TextEdit.Infrastructure.Persistence;

namespace TextEdit.UI.App;

/// <summary>
/// UI application state: manages open documents and tabs and exposes active selection.
/// </summary>
public class AppState
{
    private readonly DocumentService _docs;
    private readonly TabService _tabs;
    private readonly IpcBridge _ipc;
    private readonly PersistenceService _persistence;

    private readonly Dictionary<Guid, Document> _open = new();

    public AppState(DocumentService docs, TabService tabs, IpcBridge ipc, PersistenceService persistence)
    {
        _docs = docs;
        _tabs = tabs;
        _ipc = ipc;
        _persistence = persistence;
        EditorState = new EditorState();
    }

    public IReadOnlyList<Tab> Tabs => _tabs.Tabs;
    public Tab? ActiveTab => _tabs.Tabs.FirstOrDefault(t => t.IsActive);
    public Document? ActiveDocument => ActiveTab != null && _open.TryGetValue(ActiveTab.DocumentId, out var d) ? d : null;
    public IEnumerable<Document> AllDocuments => _open.Values;
    public EditorState EditorState { get; }

    public event Action? Changed;
    private void NotifyChanged() => Changed?.Invoke();

    // Notify UI that document state (e.g., dirty flag) changed
    public void NotifyDocumentUpdated() => NotifyChanged();

    public async Task RestoreSessionAsync()
    {
        // Idempotence: if we already have tabs/documents, assume we've been restored/initialized
        if (_tabs.Tabs.Count > 0 || _open.Count > 0)
        {
            return;
        }

        var restored = await _persistence.RestoreAsync();
        foreach (var doc in restored)
        {
            _open[doc.Id] = doc;
            _tabs.AddTab(doc);
        }
        
        // If no documents were restored, create a new one
        if (_tabs.Tabs.Count == 0)
        {
            CreateNew();
        }
        else
        {
            NotifyChanged();
        }
    }

    public async Task PersistSessionAsync()
    {
        Console.WriteLine($"[AppState] PersistSessionAsync: open={_open.Count}, tabs={_tabs.Tabs.Count}.");
        // Persist documents in current tab order for stable restore
        var order = _tabs.Tabs.Select(t => t.DocumentId).ToList();
        await _persistence.PersistAsync(_open.Values, order);
    }

    public void DeleteSessionFile(Guid documentId)
    {
        _persistence.DeleteSessionFile(documentId);
    }

    public Document CreateNew()
    {
        var doc = _docs.NewDocument();
        _open[doc.Id] = doc;
        _tabs.AddTab(doc);
        NotifyChanged();
        return doc;
    }

    public async Task<Document?> OpenAsync()
    {
        var path = await _ipc.ShowOpenFileDialogAsync();
        if (string.IsNullOrWhiteSpace(path)) return null;
        var doc = await _docs.OpenAsync(path!);
        _open[doc.Id] = doc;
        _tabs.AddTab(doc);
        NotifyChanged();
        return doc;
    }

    public async Task SaveActiveAsync()
    {
        if (ActiveDocument is null) return;
        if (string.IsNullOrWhiteSpace(ActiveDocument.FilePath))
        {
            await SaveAsActiveAsync();
            return;
        }
        await _docs.SaveAsync(ActiveDocument);
        // Clean up session file after successful save
        DeleteSessionFile(ActiveDocument.Id);
        NotifyChanged();
    }

    public async Task<bool> SaveAsActiveAsync()
    {
        if (ActiveDocument is null) return false;
        var path = await _ipc.ShowSaveFileDialogAsync();
        if (string.IsNullOrWhiteSpace(path)) return false;
        await _docs.SaveAsync(ActiveDocument, path);
        // Clean up session file after successful save
        DeleteSessionFile(ActiveDocument.Id);
        NotifyChanged();
        return true;
    }

    public void ActivateTab(Guid tabId)
    {
        _tabs.ActivateTab(tabId);
        NotifyChanged();
    }

    public async Task<bool> CloseTabAsync(Guid tabId)
    {
        var tab = _tabs.Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab is null) return false;

        if (_open.TryGetValue(tab.DocumentId, out var doc) && doc is not null && doc.IsDirty)
        {
            // Confirm close for dirty documents
            var decision = await _ipc.ConfirmCloseDirtyAsync(doc.Name);
            if (decision == IpcBridge.CloseDecision.Cancel)
            {
                return false; // abort close
            }

            if (decision == IpcBridge.CloseDecision.Save)
            {
                // Save existing or Save As for untitled (for the specific doc being closed)
                if (string.IsNullOrWhiteSpace(doc.FilePath))
                {
                    var path = await _ipc.ShowSaveFileDialogAsync();
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        return false; // user cancelled save-as
                    }
                    await _docs.SaveAsync(doc, path);
                    // Clean up session file after successful save
                    DeleteSessionFile(doc.Id);
                }
                else
                {
                    await _docs.SaveAsync(doc);
                    DeleteSessionFile(doc.Id);
                }
            }
            // If Don't Save: proceed to close without saving
        }
        _tabs.CloseTab(tabId);
        _open.Remove(tab.DocumentId);
        if (_tabs.Tabs.Count == 0)
        {
            // Always keep an editor available
            CreateNew();
        }
        else
        {
            NotifyChanged();
        }
        return true;
    }

    public async Task CloseOthersAsync(Guid keepTabId)
    {
        // Snapshot current order to avoid index shifting while closing
        var toClose = _tabs.Tabs.Where(t => t.Id != keepTabId).Select(t => t.Id).ToList();
        // Close from rightmost to leftmost for stability
        foreach (var id in toClose.AsEnumerable().Reverse())
        {
            var ok = await CloseTabAsync(id);
            if (!ok) break; // stop on cancel
        }
    }

    public async Task CloseRightAsync(Guid fromTabId)
    {
        var list = _tabs.Tabs.ToList();
        var idx = list.FindIndex(t => t.Id == fromTabId);
        if (idx < 0) return;
        var toClose = list.Skip(idx + 1).Select(t => t.Id).ToList();
        // Close from rightmost to leftmost
        foreach (var id in toClose.AsEnumerable().Reverse())
        {
            var ok = await CloseTabAsync(id);
            if (!ok) break; // stop on cancel
        }
    }

    public Document? GetDocument(Guid id)
        => _open.TryGetValue(id, out var d) ? d : null;

    public void ActivateNextTab()
    {
        if (_tabs.Tabs.Count == 0) return;
        var idx = 0;
        for (var i = 0; i < _tabs.Tabs.Count; i++)
        {
            if (_tabs.Tabs[i].IsActive) { idx = i; break; }
        }
        var next = (idx + 1) % _tabs.Tabs.Count;
        _tabs.ActivateTab(_tabs.Tabs[next].Id);
        NotifyChanged();
    }

    public void ActivatePreviousTab()
    {
        if (_tabs.Tabs.Count == 0) return;
        var idx = 0;
        for (var i = 0; i < _tabs.Tabs.Count; i++)
        {
            if (_tabs.Tabs[i].IsActive) { idx = i; break; }
        }
        var prev = (idx - 1 + _tabs.Tabs.Count) % _tabs.Tabs.Count;
        _tabs.ActivateTab(_tabs.Tabs[prev].Id);
        NotifyChanged();
    }
}

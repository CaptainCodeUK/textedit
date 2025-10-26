using Microsoft.AspNetCore.Components;
using TextEdit.Core.Documents;
using TextEdit.Infrastructure.Ipc;

namespace TextEdit.UI.App;

/// <summary>
/// UI application state: manages open documents and tabs and exposes active selection.
/// </summary>
public class AppState
{
    private readonly DocumentService _docs;
    private readonly TabService _tabs;
    private readonly IpcBridge _ipc;

    private readonly Dictionary<Guid, Document> _open = new();

    public AppState(DocumentService docs, TabService tabs, IpcBridge ipc)
    {
        _docs = docs;
        _tabs = tabs;
        _ipc = ipc;
    }

    public IReadOnlyList<Tab> Tabs => _tabs.Tabs;
    public Tab? ActiveTab => _tabs.Tabs.FirstOrDefault(t => t.IsActive);
    public Document? ActiveDocument => ActiveTab != null && _open.TryGetValue(ActiveTab.DocumentId, out var d) ? d : null;

    public event Action? Changed;
    private void NotifyChanged() => Changed?.Invoke();

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
        NotifyChanged();
    }

    public async Task<bool> SaveAsActiveAsync()
    {
        if (ActiveDocument is null) return false;
        var path = await _ipc.ShowSaveFileDialogAsync();
        if (string.IsNullOrWhiteSpace(path)) return false;
        await _docs.SaveAsync(ActiveDocument, path);
        NotifyChanged();
        return true;
    }

    public void ActivateTab(Guid tabId)
    {
        _tabs.ActivateTab(tabId);
        NotifyChanged();
    }

    public void CloseTab(Guid tabId)
    {
        var tab = _tabs.Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab is null) return;
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

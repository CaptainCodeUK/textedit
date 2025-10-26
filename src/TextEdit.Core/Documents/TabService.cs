namespace TextEdit.Core.Documents;

/// <summary>
/// Manages a collection of tabs and active selection.
/// </summary>
public class TabService
{
    private readonly List<Tab> _tabs = new();
    public IReadOnlyList<Tab> Tabs => _tabs;

    public Tab AddTab(Document doc)
    {
        var tab = new Tab { DocumentId = doc.Id, Title = doc.Name };
        _tabs.Add(tab);
        ActivateTab(tab.Id);
        return tab;
    }

    public void CloseTab(Guid tabId)
    {
        var idx = _tabs.FindIndex(t => t.Id == tabId);
        if (idx >= 0)
        {
            var wasActive = _tabs[idx].IsActive;
            _tabs.RemoveAt(idx);
            if (wasActive && _tabs.Count > 0)
            {
                _tabs[Math.Clamp(idx, 0, _tabs.Count - 1)].IsActive = true;
            }
        }
    }

    public void ActivateTab(Guid tabId)
    {
        foreach (var t in _tabs) t.IsActive = false;
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab is not null) tab.IsActive = true;
    }
}

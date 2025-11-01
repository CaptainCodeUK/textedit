namespace TextEdit.Core.Documents;

/// <summary>
/// Manages a collection of editor tabs and tracks the active tab.
/// </summary>
public class TabService
{
    private readonly List<Tab> _tabs = new();
    /// <summary>
    /// Current ordered list of tabs.
    /// </summary>
    public IReadOnlyList<Tab> Tabs => _tabs;

    /// <summary>
    /// Adds a new tab for the specified document and activates it.
    /// </summary>
    /// <param name="doc">Document to attach to the new tab.</param>
    /// <returns>The created tab.</returns>
    public Tab AddTab(Document doc)
    {
        var tab = new Tab { DocumentId = doc.Id, Title = doc.Name };
        _tabs.Add(tab);
        ActivateTab(tab.Id);
        return tab;
    }

    /// <summary>
    /// Closes a tab by identifier. If the closed tab was active, activates the nearest remaining tab.
    /// </summary>
    /// <param name="tabId">Identifier of the tab to close.</param>
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

    /// <summary>
    /// Marks the tab with the given identifier as active and deactivates all others.
    /// </summary>
    /// <param name="tabId">Identifier of the tab to activate.</param>
    public void ActivateTab(Guid tabId)
    {
        foreach (var t in _tabs) t.IsActive = false;
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab is not null) tab.IsActive = true;
    }
}

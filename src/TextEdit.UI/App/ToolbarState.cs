namespace TextEdit.UI.App;

/// <summary>
/// Tracks toolbar button enabled/disabled states based on current editor context.
/// </summary>
public class ToolbarState
{
    /// <summary>
    /// Can Save - enabled when active document has unsaved changes
    /// </summary>
    public bool CanSave { get; set; }

    /// <summary>
    /// Can Cut - enabled when there is a text selection
    /// </summary>
    public bool CanCut { get; set; }

    /// <summary>
    /// Can Copy - enabled when there is a text selection
    /// </summary>
    public bool CanCopy { get; set; }

    /// <summary>
    /// Can Paste - always true (clipboard content is OS-managed)
    /// </summary>
    public bool CanPaste { get; set; } = true;

    /// <summary>
    /// Update all states based on current document and selection
    /// </summary>
    public void Update(bool hasUnsavedChanges, bool hasSelection)
    {
        CanSave = hasUnsavedChanges;
        CanCut = hasSelection;
        CanCopy = hasSelection;
        // CanPaste always true
    }
}

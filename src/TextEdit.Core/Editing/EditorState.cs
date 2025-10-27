using System;
using System.Collections.Generic;

namespace TextEdit.Core.Editing;

/// <summary>
/// Editor state settings and live cursor/selection info.
/// </summary>
public class EditorState
{
    public bool WordWrap { get; set; } = true;
    public bool ShowPreview { get; set; } = false;
    public int CaretLine { get; set; } = 1;
    public int CaretColumn { get; set; } = 1;
    public int CharacterCount { get; set; } = 0;

    // Remember caret position per document so switching tabs restores position
    public Dictionary<Guid, int> CaretIndexByDocument { get; } = new();

    // Local UI change notifications (e.g., StatusBar) that shouldn't trigger full AppState updates
    public event Action? Changed;
    public void NotifyChanged() => Changed?.Invoke();
}

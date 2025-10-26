namespace TextEdit.Core.Editing;

/// <summary>
/// Editor state settings and live cursor/selection info.
/// </summary>
public class EditorState
{
    public bool WordWrap { get; set; } = true;
    public int CaretLine { get; set; } = 1;
    public int CaretColumn { get; set; } = 1;
    public int CharacterCount { get; set; } = 0;
}

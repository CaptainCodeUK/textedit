namespace TextEdit.UI.Components.Editor;

/// <summary>
/// Central hub for Editor commands triggered outside the component (e.g., Electron menus).
/// The TextEditor instance assigns these delegates on initialization.
/// </summary>
public static class EditorCommandHub
{
    public static Func<Task>? NewRequested { get; set; }
    public static Func<Task>? OpenRequested { get; set; }
    public static Func<Task>? SaveRequested { get; set; }
    public static Func<Task>? SaveAsRequested { get; set; }

    public static Task InvokeSafe(Func<Task>? action)
        => action is null ? Task.CompletedTask : action();
}

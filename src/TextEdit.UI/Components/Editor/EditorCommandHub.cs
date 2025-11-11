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
    public static Func<Task>? UndoRequested { get; set; }
    public static Func<Task>? RedoRequested { get; set; }
    public static Func<Task>? NextTabRequested { get; set; }
    public static Func<Task>? PrevTabRequested { get; set; }
    public static Func<Task>? CloseTabRequested { get; set; }
    public static Func<Task>? CloseOthersRequested { get; set; }
    public static Func<Task>? CloseRightRequested { get; set; }
    public static Func<Task>? ToggleWordWrapRequested { get; set; }
    public static Func<Task>? TogglePreviewRequested { get; set; }
    public static Func<Task>? ToggleToolbarRequested { get; set; }
    public static Func<Task>? AboutRequested { get; set; } // T055: About dialog
    public static Func<Task>? OptionsRequested { get; set; } // US3: Options dialog
    // Find commands (US1)
    public static Func<Task>? FindRequested { get; set; }
    public static Func<Task>? FindNextRequested { get; set; }
    public static Func<Task>? FindPreviousRequested { get; set; }
    // Replace commands (US2)
    public static Func<Task>? ReplaceRequested { get; set; }
    
    // Format menu commands
    public static Func<Task>? FormatHeading1Requested { get; set; }
    public static Func<Task>? FormatHeading2Requested { get; set; }
    public static Func<Task>? FormatBoldRequested { get; set; }
    public static Func<Task>? FormatItalicRequested { get; set; }
    public static Func<Task>? FormatCodeRequested { get; set; }
    public static Func<Task>? FormatBulletListRequested { get; set; }
    public static Func<Task>? FormatNumberedListRequested { get; set; }

    /// <summary>
    /// Safely invokes an asynchronous delegate when non-null; otherwise returns a completed task.
    /// </summary>
    /// <param name="action">Delegate to invoke.</param>
    public static Task InvokeSafe(Func<Task>? action)
        => action is null ? Task.CompletedTask : action();
}

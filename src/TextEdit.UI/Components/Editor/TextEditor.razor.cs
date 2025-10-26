using Microsoft.AspNetCore.Components;
using TextEdit.Core.Documents;
using TextEdit.Core.Editing;
using TextEdit.Infrastructure.Ipc;
using TextEdit.UI.App;

namespace TextEdit.UI.Components.Editor;

public partial class TextEditor : ComponentBase, IDisposable
{
    [Inject] protected DocumentService DocumentService { get; set; } = default!;
    [Inject] protected IUndoRedoService UndoRedo { get; set; } = default!;
    [Inject] protected IpcBridge Ipc { get; set; } = default!;
    [Inject] protected AppState AppState { get; set; } = default!;

    protected Document? CurrentDoc => AppState.ActiveDocument;
    protected EditorState State { get; } = new();
    private bool _suppressUndoPush;

    protected string Content
    {
        get => CurrentDoc?.Content ?? string.Empty;
        set
        {
            if (CurrentDoc is null) return;
            if (value != CurrentDoc.Content)
            {
                if (_suppressUndoPush)
                {
                    CurrentDoc.SetContent(value);
                }
                else
                {
                    DocumentService.UpdateContent(CurrentDoc, value);
                }
                State.CharacterCount = value?.Length ?? 0;
            }
        }
    }

    protected bool CanSave => CurrentDoc != null && (CurrentDoc.IsDirty || string.IsNullOrEmpty(CurrentDoc.FilePath) == false);

    protected override void OnInitialized()
    {
        if (AppState.ActiveDocument is null)
        {
            AppState.CreateNew();
        }
        AppState.Changed += OnAppStateChanged;
        // Register handlers for application menu integration
        EditorCommandHub.NewRequested = HandleNew;
        EditorCommandHub.OpenRequested = HandleOpen;
        EditorCommandHub.SaveRequested = HandleSave;
        EditorCommandHub.SaveAsRequested = HandleSaveAs;
        EditorCommandHub.UndoRequested = HandleUndo;
        EditorCommandHub.RedoRequested = HandleRedo;
        EditorCommandHub.NextTabRequested = HandleNextTab;
        EditorCommandHub.PrevTabRequested = HandlePrevTab;
    }

    private void OnAppStateChanged() => InvokeAsync(StateHasChanged);

    protected async Task HandleNew()
    {
        AppState.CreateNew();
        State.CharacterCount = 0;
        await InvokeAsync(StateHasChanged);
    }

    protected async Task HandleOpen()
    {
        await AppState.OpenAsync();
        State.CharacterCount = CurrentDoc?.Content.Length ?? 0;
        await InvokeAsync(StateHasChanged);
    }

    protected async Task HandleSave()
    {
        await AppState.SaveActiveAsync();
        await InvokeAsync(StateHasChanged);
    }

    protected async Task HandleSaveAs()
    {
        await AppState.SaveAsActiveAsync();
        await InvokeAsync(StateHasChanged);
    }

    protected Task HandleUndo()
    {
        if (CurrentDoc is null) return Task.CompletedTask;
        var text = UndoRedo.Undo(CurrentDoc.Id);
        if (text is not null)
        {
            _suppressUndoPush = true;
            Content = text;
            _suppressUndoPush = false;
        }
        return InvokeAsync(StateHasChanged);
    }

    protected Task HandleRedo()
    {
        if (CurrentDoc is null) return Task.CompletedTask;
        var text = UndoRedo.Redo(CurrentDoc.Id);
        if (text is not null)
        {
            _suppressUndoPush = true;
            Content = text;
            _suppressUndoPush = false;
        }
        return InvokeAsync(StateHasChanged);
    }

    protected Task HandleNextTab()
    {
        AppState.ActivateNextTab();
        return InvokeAsync(StateHasChanged);
    }

    protected Task HandlePrevTab()
    {
        AppState.ActivatePreviousTab();
        return InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        AppState.Changed -= OnAppStateChanged;
    }
}

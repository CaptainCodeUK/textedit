using Microsoft.AspNetCore.Components;
using TextEdit.Core.Documents;
using TextEdit.Core.Editing;
using TextEdit.Infrastructure.Ipc;

namespace TextEdit.UI.Components.Editor;

public partial class TextEditor : ComponentBase
{
    [Inject] protected DocumentService DocumentService { get; set; } = default!;
    [Inject] protected IUndoRedoService UndoRedo { get; set; } = default!;
    [Inject] protected IpcBridge Ipc { get; set; } = default!;

    protected Document CurrentDoc { get; private set; } = default!;
    protected EditorState State { get; } = new();

    protected string Content
    {
        get => CurrentDoc.Content;
        set
        {
            if (value != CurrentDoc.Content)
            {
                DocumentService.UpdateContent(CurrentDoc, value);
                State.CharacterCount = value?.Length ?? 0;
            }
        }
    }

    protected bool CanSave => CurrentDoc.IsDirty || string.IsNullOrEmpty(CurrentDoc.FilePath) == false;

    protected override void OnInitialized()
    {
        CurrentDoc = DocumentService.NewDocument();
        // Register handlers for application menu integration
        EditorCommandHub.NewRequested = HandleNew;
        EditorCommandHub.OpenRequested = HandleOpen;
        EditorCommandHub.SaveRequested = HandleSave;
        EditorCommandHub.SaveAsRequested = HandleSaveAs;
    }

    protected async Task HandleNew()
    {
        CurrentDoc = DocumentService.NewDocument();
        State.CharacterCount = 0;
        await InvokeAsync(StateHasChanged);
    }

    protected async Task HandleOpen()
    {
        var path = await Ipc.ShowOpenFileDialogAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            CurrentDoc = await DocumentService.OpenAsync(path!);
            State.CharacterCount = CurrentDoc.Content.Length;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected async Task HandleSave()
    {
        if (string.IsNullOrWhiteSpace(CurrentDoc.FilePath))
        {
            await HandleSaveAs();
            return;
        }
        await DocumentService.SaveAsync(CurrentDoc);
        await InvokeAsync(StateHasChanged);
    }

    protected async Task HandleSaveAs()
    {
        var path = await Ipc.ShowSaveFileDialogAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            await DocumentService.SaveAsync(CurrentDoc, path);
            await InvokeAsync(StateHasChanged);
        }
    }
}

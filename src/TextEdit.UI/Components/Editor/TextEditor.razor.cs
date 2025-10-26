using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
    private CancellationTokenSource? _undoCts;
    private Guid? _lastEditedDocId;
    private string? _beforeEditContent;
    private static readonly TimeSpan _undoDebounce = TimeSpan.FromMilliseconds(400);

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
                    // Update model immediately and debounce undo snapshot pushes
                    CurrentDoc.SetContent(value);
                    ScheduleUndoPush(CurrentDoc, value);
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
        EditorCommandHub.CloseTabRequested = HandleCloseTab;
    }

    private void OnAppStateChanged()
    {
        // flush any pending snapshot for the previous document before switching
        FlushPendingUndoPush();
        InvokeAsync(StateHasChanged);
    }

    private void FlushPendingUndoPush()
    {
        try
        {
            if (_undoCts is not null)
            {
                _undoCts.Cancel();
                _undoCts.Dispose();
                _undoCts = null;
            }

            // Push the final snapshot if we had started editing
            if (_lastEditedDocId.HasValue && _beforeEditContent is not null)
            {
                var doc = AppState.GetDocument(_lastEditedDocId.Value);
                if (doc is not null && doc.Content != _beforeEditContent)
                {
                    // Push the current state as the final snapshot
                    UndoRedo.Push(doc, doc.Content);
                }
            }
        }
        finally
        {
            _lastEditedDocId = null;
            _beforeEditContent = null;
        }
    }

    private void ScheduleUndoPush(Document doc, string content)
    {
        // If this is the first edit for this document, capture the "before" state
        if (_lastEditedDocId != doc.Id)
        {
            // Flush any previous document's pending state first
            if (_lastEditedDocId.HasValue && _beforeEditContent is not null)
            {
                var prevDoc = AppState.GetDocument(_lastEditedDocId.Value);
                if (prevDoc is not null && prevDoc.Content != _beforeEditContent)
                {
                    UndoRedo.Push(prevDoc, prevDoc.Content);
                }
            }
            
            // Capture the state before this edit begins
            _lastEditedDocId = doc.Id;
            _beforeEditContent = content;
        }

        // Cancel previous pending push and start new debounce
        _undoCts?.Cancel();
        _undoCts?.Dispose();
        _undoCts = new CancellationTokenSource();
        var ct = _undoCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_undoDebounce, ct);
                if (!ct.IsCancellationRequested)
                {
                    // Push snapshot after typing pause
                    await InvokeAsync(() =>
                    {
                        if (_lastEditedDocId == doc.Id && _beforeEditContent is not null)
                        {
                            // Only push if content actually changed from before-edit state
                            if (doc.Content != _beforeEditContent)
                            {
                                UndoRedo.Push(doc, doc.Content);
                                _beforeEditContent = doc.Content; // Update for next edit burst
                            }
                        }
                    });
                }
            }
            catch (TaskCanceledException) { }
        });
    }

    protected async Task HandleNew()
    {
        FlushPendingUndoPush();
        AppState.CreateNew();
        State.CharacterCount = 0;
        await InvokeAsync(StateHasChanged);
    }

    protected async Task HandleOpen()
    {
        FlushPendingUndoPush();
        await AppState.OpenAsync();
        State.CharacterCount = CurrentDoc?.Content.Length ?? 0;
        await InvokeAsync(StateHasChanged);
    }

    protected async Task HandleSave()
    {
        FlushPendingUndoPush();
        await AppState.SaveActiveAsync();
        await InvokeAsync(StateHasChanged);
    }

    protected async Task HandleSaveAs()
    {
        FlushPendingUndoPush();
        await AppState.SaveAsActiveAsync();
        await InvokeAsync(StateHasChanged);
    }

    protected Task HandleUndo()
    {
        if (CurrentDoc is null) return Task.CompletedTask;
        // Flush any pending changes first
        FlushPendingUndoPush();
        
        var text = UndoRedo.Undo(CurrentDoc.Id);
        if (text is not null)
        {
            _suppressUndoPush = true;
            Content = text;
            _suppressUndoPush = false;
            // Reset edit tracking so next edit starts fresh
            _lastEditedDocId = CurrentDoc.Id;
            _beforeEditContent = text;
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
            // Reset edit tracking so next edit starts fresh
            _lastEditedDocId = CurrentDoc.Id;
            _beforeEditContent = text;
        }
        return InvokeAsync(StateHasChanged);
    }

    protected Task HandleNextTab()
    {
        FlushPendingUndoPush();
        AppState.ActivateNextTab();
        return InvokeAsync(StateHasChanged);
    }

    protected Task HandlePrevTab()
    {
        FlushPendingUndoPush();
        AppState.ActivatePreviousTab();
        return InvokeAsync(StateHasChanged);
    }

    protected Task HandleCloseTab()
    {
        if (AppState.ActiveTab is null) return Task.CompletedTask;
        FlushPendingUndoPush();
        AppState.CloseTab(AppState.ActiveTab.Id);
        return InvokeAsync(StateHasChanged);
    }

    protected void OnBlur(FocusEventArgs _)
    {
        FlushPendingUndoPush();
    }

    public void Dispose()
    {
        FlushPendingUndoPush();
        AppState.Changed -= OnAppStateChanged;
    }
}

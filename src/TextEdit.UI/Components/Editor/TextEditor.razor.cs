using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using TextEdit.Core.Documents;
using TextEdit.Core.Editing;
using TextEdit.Infrastructure.Ipc;
using TextEdit.UI.App;
using TextEdit.UI.Services;
using TextEdit.UI.Components.Editor;
using TextEdit.Core.Searching;
using TextEdit.Infrastructure.Logging;

namespace TextEdit.UI.Components.Editor;

public partial class TextEditor : ComponentBase, IDisposable
{
    [Inject] protected DocumentService DocumentService { get; set; } = default!;
    [Inject] protected IUndoRedoService UndoRedo { get; set; } = default!;
    [Inject] protected IpcBridge Ipc { get; set; } = default!;
    [Inject] protected AppState AppState { get; set; } = default!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] protected DialogService DialogService { get; set; } = default!; // For About dialog
    [Inject] protected MarkdownFormattingService FormattingService { get; set; } = default!;
    [Inject] protected TextEdit.Infrastructure.Logging.IAppLoggerFactory AppLoggerFactory { get; set; } = default!;

    protected Document? CurrentDoc => AppState.ActiveDocument;
    protected EditorState State => AppState.EditorState;
    private bool _suppressUndoPush;
    private CancellationTokenSource? _undoCts;
    private Guid? _lastEditedDocId;
    private string? _beforeEditContent;
    private static readonly TimeSpan _undoDebounce = TimeSpan.FromMilliseconds(400);
    private bool _pendingCaretSync;
    private Guid? _lastActiveDocId;

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
                State.NotifyChanged();
                AppState.NotifyDocumentUpdated();
            }
        }
    }

    protected bool CanSave => CurrentDoc != null && (CurrentDoc.IsDirty || string.IsNullOrEmpty(CurrentDoc.FilePath) == false);

    protected override void OnInitialized()
    {
        // Set current instance for JSInvokable static methods
        _currentInstance = this;
        
        // Don't create a document here - App.razor's RestoreSessionAsync handles initialization
        AppState.Changed += OnAppStateChanged;
        // Register handlers for application menu integration
        EditorCommandHub.NewRequested = HandleNew;
        EditorCommandHub.OpenRequested = HandleOpen;
        EditorCommandHub.SaveRequested = HandleSave;
        EditorCommandHub.SaveAsRequested = HandleSaveAs;
        // Undo/Redo handled natively by Monaco
        EditorCommandHub.NextTabRequested = HandleNextTab;
        EditorCommandHub.PrevTabRequested = HandlePrevTab;
        EditorCommandHub.CloseTabRequested = HandleCloseTab;
        EditorCommandHub.CloseOthersRequested = HandleCloseOthers;
        EditorCommandHub.CloseRightRequested = HandleCloseRight;
        EditorCommandHub.ToggleWordWrapRequested = HandleToggleWordWrap;
        EditorCommandHub.TogglePreviewRequested = HandleTogglePreview;
        EditorCommandHub.ToggleToolbarRequested = HandleToggleToolbar;
        EditorCommandHub.AboutRequested = HandleAboutRequested; // T055
        EditorCommandHub.OptionsRequested = HandleOptionsRequested; // US3
        
        // Format menu commands (Monaco handles find/replace/undo natively)
        EditorCommandHub.FormatHeading1Requested = () => HandleFormatCommand(MarkdownFormattingService.MarkdownFormat.H1);
        EditorCommandHub.FormatHeading2Requested = () => HandleFormatCommand(MarkdownFormattingService.MarkdownFormat.H2);
        EditorCommandHub.FormatBoldRequested = () => HandleFormatCommand(MarkdownFormattingService.MarkdownFormat.Bold);
        EditorCommandHub.FormatItalicRequested = () => HandleFormatCommand(MarkdownFormattingService.MarkdownFormat.Italic);
        EditorCommandHub.FormatCodeRequested = () => HandleFormatCommand(MarkdownFormattingService.MarkdownFormat.Code);
        EditorCommandHub.FormatBulletListRequested = () => HandleFormatCommand(MarkdownFormattingService.MarkdownFormat.BulletedList);
        EditorCommandHub.FormatNumberedListRequested = () => HandleFormatCommand(MarkdownFormattingService.MarkdownFormat.NumberedList);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Initialize the Tab key handler for the editor (Monaco handles find/replace/undo natively)
            try
            {
                await JSRuntime.InvokeVoidAsync("editorFocus.initialize", "main-editor-textarea");
                // Set up listeners for Ctrl+Tab navigation (guard against duplicate registration)
                await JSRuntime.InvokeVoidAsync("eval", @"
                    if (!window._texteditTabNavListenersInstalled) {
                        window._texteditTabNavListenersInstalled = true;
                        console.log('[TextEditor.razor] Setting up blazor-next-tab and blazor-prev-tab listeners');
                        document.addEventListener('blazor-next-tab', () => {
                            console.log('[TextEditor.razor] blazor-next-tab fired - calling HandleNextTabFromJS');
                            DotNet.invokeMethodAsync('TextEdit.UI', 'HandleNextTabFromJS');
                        });
                        document.addEventListener('blazor-prev-tab', () => {
                            console.log('[TextEditor.razor] blazor-prev-tab fired - calling HandlePrevTabFromJS');
                            DotNet.invokeMethodAsync('TextEdit.UI', 'HandlePrevTabFromJS');
                        });
                    }
                ");
            }
            catch { /* ignore */ }
            
            await FocusEditorAsync();
            // Initialize counts and caret on first render
            State.CharacterCount = CurrentDoc?.Content.Length ?? 0;
            _lastActiveDocId = CurrentDoc?.Id;
            await UpdateCaretPosition();
        }
        if (_pendingCaretSync)
        {
            _pendingCaretSync = false;
            // After a tab switch/render, restore caret and update status.
            // Always use Monaco editor focus
            await FocusEditorAsync();
            if (CurrentDoc is not null)
            {
                var docId = CurrentDoc.Id;
                var desiredIndex = 0;
                if (State.CaretIndexByDocument.TryGetValue(docId, out var idx))
                {
                    desiredIndex = idx;
                }
                try
                {
                    await JSRuntime.InvokeVoidAsync("editorFocus.setCaretPosition", "main-editor-textarea", desiredIndex);
                }
                catch { /* ignore */ }
            }
            await UpdateCaretPosition();
        }
    }

    [Microsoft.JSInterop.JSInvokable]
    [Microsoft.JSInterop.JSInvokable]
    public static Task OpenOptionsDialogFromJS()
    {
        if (_currentInstance == null) return Task.CompletedTask;
        _currentInstance.DialogService?.ShowOptionsDialog();
        return Task.CompletedTask;
    }

    private Task HandleOptionsRequested()
    {
        DialogService.ShowOptionsDialog();
        return Task.CompletedTask;
    }

    private void OnAppStateChanged()
    {
        // flush any pending snapshot for the previous document before switching
        FlushPendingUndoPush();
        _ = InvokeAsync(async () =>
        {
            var newActiveId = CurrentDoc?.Id;
            if (newActiveId != _lastActiveDocId)
            {
                // Active tab switched; defer caret restore until after render
                State.CharacterCount = CurrentDoc?.Content.Length ?? 0;
                _pendingCaretSync = true;
                _lastActiveDocId = newActiveId;
                StateHasChanged();
            }
            StateHasChanged();
            try
            {
                // Update focus manager to use Monaco editor
                await JSRuntime.InvokeVoidAsync("editorFocus.setActiveEditor", "monaco-editor");
            }
            catch { }
        });
    }

    private async Task FocusEditorAsync()
    {
        try
        {
            // Always use Monaco editor focus
            await JSRuntime.InvokeVoidAsync("editorFocus.focusActiveEditor");
        }
        catch
        {
            // Ignore focus errors (e.g., during prerender or if JS not loaded)
        }
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
        await FocusEditorAsync();
    }

    protected async Task HandleOpen()
    {
        FlushPendingUndoPush();
        await AppState.OpenAsync();
        State.CharacterCount = CurrentDoc?.Content.Length ?? 0;
        await InvokeAsync(StateHasChanged);
        await FocusEditorAsync();
    }

    protected async Task HandleSave()
    {
        FlushPendingUndoPush();
        await AppState.SaveActiveAsync();
        await InvokeAsync(StateHasChanged);
        await FocusEditorAsync();
    }

    protected async Task HandleSaveAs()
    {
        FlushPendingUndoPush();
        await AppState.SaveAsActiveAsync();
        await InvokeAsync(StateHasChanged);
        await FocusEditorAsync();
    }

    protected async Task HandleNextTab()
    {
        FlushPendingUndoPush();
        AppState.ActivateNextTab();
        await InvokeAsync(StateHasChanged);
        await FocusEditorAsync();
    }

    protected async Task HandlePrevTab()
    {
        FlushPendingUndoPush();
        AppState.ActivatePreviousTab();
        await InvokeAsync(StateHasChanged);
        await FocusEditorAsync();
    }

    protected async Task HandleCloseTab()
    {
        if (AppState.ActiveTab is null) return;
        FlushPendingUndoPush();
        await AppState.CloseTabAsync(AppState.ActiveTab.Id);
        await InvokeAsync(StateHasChanged);
        await FocusEditorAsync();
    }

    protected async Task HandleCloseOthers()
    {
        if (AppState.ActiveTab is null) return;
        FlushPendingUndoPush();
        await AppState.CloseOthersAsync(AppState.ActiveTab.Id);
        await InvokeAsync(StateHasChanged);
        await FocusEditorAsync();
    }

    protected async Task HandleCloseRight()
    {
        if (AppState.ActiveTab is null) return;
        FlushPendingUndoPush();
        await AppState.CloseRightAsync(AppState.ActiveTab.Id);
        await InvokeAsync(StateHasChanged);
        await FocusEditorAsync();
    }

    protected Task HandleToggleWordWrap()
    {
        State.WordWrap = !State.WordWrap;
        State.NotifyChanged(); // Notify so menu checkmarks update
        AppState.PersistEditorPreferences();
        return InvokeAsync(StateHasChanged);
    }

    protected Task HandleTogglePreview()
    {
        State.ShowPreview = !State.ShowPreview;
        State.NotifyChanged(); // Notify so menu checkmarks update
        AppState.NotifyDocumentUpdated(); // Notify so layout updates
        AppState.PersistEditorPreferences();
        return InvokeAsync(StateHasChanged);
    }

    protected Task HandleToggleToolbar()
    {
    AppState.Preferences.ToolbarVisible = !AppState.Preferences.ToolbarVisible;
    var logger = AppLoggerFactory.CreateLogger<TextEditor>();
    logger?.LogInformation("Toolbar visibility set to {Visible}", AppState.Preferences.ToolbarVisible);
    _ = AppState.SavePreferencesAsync();
        return InvokeAsync(StateHasChanged);
    }

    protected async Task HandleFormatCommand(MarkdownFormattingService.MarkdownFormat format)
    {
        if (CurrentDoc is null) return;
        
        try
        {
            var start = await JSRuntime.InvokeAsync<int>("eval", "document.getElementById('main-editor-textarea')?.selectionStart ?? 0");
            var end = await JSRuntime.InvokeAsync<int>("eval", "document.getElementById('main-editor-textarea')?.selectionEnd ?? 0");
            
            // Push current state to undo before applying format
            UndoRedo.Push(CurrentDoc, CurrentDoc.Content);
            
            var result = FormattingService.ApplyFormat(CurrentDoc.Content, start, end, format);
            
            CurrentDoc.SetContent(result.NewContent);
            AppState.NotifyDocumentUpdated();
            
            // Update textarea and restore selection
            await JSRuntime.InvokeVoidAsync("eval", 
                $"{{ const el = document.getElementById('main-editor-textarea'); if (el) {{ el.value = {System.Text.Json.JsonSerializer.Serialize(result.NewContent)}; el.setSelectionRange({result.NewSelectionStart}, {result.NewSelectionEnd}); }} }}");
        }
        catch
        {
            // Ignore formatting errors
        }
    }

    protected void OnBlur(FocusEventArgs _)
    {
        FlushPendingUndoPush();
    }



    private async Task OnReplaceNextRequested(TextEdit.Core.Searching.ReplaceOperation op)
    {
        if (CurrentDoc is null) return;
        
        // Flush any pending typed edits before performing replace operation
        FlushPendingUndoPush();
        
        try
        {
            var caret = await JSRuntime.InvokeAsync<int>("eval", "document.getElementById('main-editor-textarea')?.selectionEnd ?? 0");
            var replaced = DocumentService.ReplaceNext(CurrentDoc, op, caret, out var newCaret);
            if (replaced)
            {
                // Clear pending edit tracking since ReplaceNext already pushed undo state
                _lastEditedDocId = null;
                _beforeEditContent = null;
                
                // Sync UI
                AppState.NotifyDocumentUpdated();
                await InvokeAsync(StateHasChanged);
                // Update selection/caret only; do not change focus
                await JSRuntime.InvokeVoidAsync("eval", $"{{ const el = document.getElementById('main-editor-textarea'); if (el) {{ try {{ el.setSelectionRange({newCaret}, {newCaret}); }} catch(e){{}} }} }}");
                await UpdateCaretPosition();
            }
        }
        catch { /* ignore */ }
    }

    private async Task OnReplaceAllRequested(TextEdit.Core.Searching.ReplaceOperation op)
    {
        if (CurrentDoc is null) return;
        
        // Flush any pending typed edits before performing replace operation
        FlushPendingUndoPush();
        
        try
        {
            var count = DocumentService.ReplaceAll(CurrentDoc, op);
            if (count > 0)
            {
                // Clear pending edit tracking since ReplaceAll already pushed undo state
                _lastEditedDocId = null;
                _beforeEditContent = null;
                
                AppState.NotifyDocumentUpdated();
                await InvokeAsync(StateHasChanged);
                await UpdateCaretPosition();
            }
        }
        catch { /* ignore */ }
    }

    protected async Task OnFocus(FocusEventArgs _)
    {
        // When editor gains focus, update caret and counts immediately
        State.CharacterCount = CurrentDoc?.Content.Length ?? 0;
        await UpdateCaretPosition();
    }

    protected async Task UpdateCaretPosition()
    {
        try
        {
            var position = await JSRuntime.InvokeAsync<CaretPosition>("editorFocus.getCaretPosition", "main-editor-textarea");
            State.CaretLine = position.Line;
            State.CaretColumn = position.Column;
            if (CurrentDoc is not null)
            {
                State.CaretIndexByDocument[CurrentDoc.Id] = position.Index;
                
                // Update toolbar state based on selection and dirty state
                // Check if there's a selection by comparing start/end
                bool hasSelection = false;
                try
                {
                    var selEnd = await JSRuntime.InvokeAsync<int>("eval", "document.getElementById('main-editor-textarea')?.selectionEnd ?? 0");
                    hasSelection = position.Index != selEnd;
                }
                catch { /* ignore */ }
                
                AppState.ToolbarState.Update(CurrentDoc.IsDirty, hasSelection);
            }
            // Notify StatusBar only; avoid causing full AppState change that re-renders editor
            State.NotifyChanged();
        }
        catch
        {
            // Ignore errors getting caret position
        }
    }

    protected Task HandleAboutRequested()
    {
        // Show About dialog via DialogService (T055)
        DialogService.ShowAboutDialog();
        return Task.CompletedTask;
    }

    [Microsoft.JSInterop.JSInvokable]
    public static Task HandleNextTabFromJS()
    {
        return EditorCommandHub.InvokeSafe(EditorCommandHub.NextTabRequested);
    }

    [Microsoft.JSInterop.JSInvokable]
    public static Task HandlePrevTabFromJS()
    {
        return EditorCommandHub.InvokeSafe(EditorCommandHub.PrevTabRequested);
    }

        private static TextEditor? _currentInstance;
    
    [Microsoft.JSInterop.JSInvokable]
    public static async Task HandleTabInsertionFromJS(int selectionStart, int selectionEnd)
    {
        if (_currentInstance?.CurrentDoc == null) return;
        
        var instance = _currentInstance;
        var doc = instance.CurrentDoc;
        
        // Flush any pending undo push before inserting tab (makes tab its own undo unit)
        instance.FlushPendingUndoPush();
        
        // Insert tab at caret position, replacing any selected text
        var content = doc.Content ?? "";
        var newContent = content.Substring(0, selectionStart) + "\t" + content.Substring(selectionEnd);
        
        // Push IMMEDIATELY to undo stack (not debounced)
        instance.UndoRedo.Push(doc, newContent);
        
        // Update Content property with suppression flag (updates UI but doesn't schedule undo)
        // FIXME-TAB-UNDO: This workaround still fails to isolate the tab as a standalone undo unit on Linux.
        // Root Cause (current hypothesis): The preceding typing burst's state tracking (_beforeEditContent)
        // remains "hello" and subsequent typing after tab coalesces unexpectedly.
        // Planned Remediation: Refactor undo model to:
        //   1. Introduce explicit IEditorCommand pipeline (InsertText, InsertTab, ReplaceSelection).
        //   2. Each structural command performs: FlushDebounce → ImmediatePush(before) → Apply → ImmediatePush(after).
        //   3. Typing continues to use debounced grouping; structural edits become atomic.
        // Testing Needed: Multi-step sequence (text, tab, text) yields 3 distinct undo points.
        // Tracking Tag: FIXME-TAB-UNDO
        instance._suppressUndoPush = true;
        instance.Content = newContent;
        instance._suppressUndoPush = false;
        
        // Clear edit tracking so next edit starts fresh undo unit
        instance._lastEditedDocId = null;
        instance._beforeEditContent = null;
        
        // Update UI and caret
        await instance.InvokeAsync(instance.StateHasChanged);
        
        try
        {
            var newCaretPos = selectionStart + 1;
            await instance.JSRuntime.InvokeVoidAsync("editorFocus.setCaretPosition", "main-editor-textarea", newCaretPos);
        }
        catch { /* ignore */ }
    }

    public void Dispose()
    {
        FlushPendingUndoPush();
        AppState.Changed -= OnAppStateChanged;
        if (_currentInstance == this) _currentInstance = null;
    }

    private class CaretPosition
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int Index { get; set; }
    }

    
}

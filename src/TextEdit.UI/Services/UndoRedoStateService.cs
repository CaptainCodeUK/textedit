using Microsoft.JSInterop;
using System;

namespace TextEdit.UI.Services;

/// <summary>
/// Undo/redo state tracker for Monaco editor.
/// Always keeps buttons enabled - Monaco internally validates whether undo/redo is actually available.
/// </summary>
public class UndoRedoStateService
{
    private readonly IJSRuntime _jsRuntime;

    /// <summary>
    /// Raised when service initializes (for UI update).
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Always true - Monaco handles validation internally
    /// </summary>
    public bool CanUndo => true;

    /// <summary>
    /// Always true - Monaco handles validation internally
    /// </summary>
    public bool CanRedo => true;

    public UndoRedoStateService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Start service - triggers initial event
    /// </summary>
    public void StartPolling(string editorId = "monaco-editor")
    {
        Console.WriteLine($"[UndoRedoStateService] Service started");
        Changed?.Invoke();
    }

    /// <summary>
    /// Stop service - no-op since we don't actually poll
    /// </summary>
    public void StopPolling()
    {
        // No-op
    }

    /// <summary>
    /// Update state - no-op since buttons are always enabled
    /// </summary>
    public async Task UpdateStateAsync(string editorId = "monaco-editor")
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Mark the current editor content as a save point.
    /// </summary>
    public async Task MarkSavePointAsync(string editorId = "monaco-editor")
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("textEditMonaco.markSavePoint", editorId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UndoRedoStateService] Error marking save point: {ex}");
        }
    }
}
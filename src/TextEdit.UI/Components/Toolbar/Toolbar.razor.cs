using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TextEdit.Core.Editing;
using TextEdit.UI.App;
using TextEdit.UI.Services;
using TextEdit.UI.Components.Editor;
using static TextEdit.UI.Services.MarkdownFormattingService;

namespace TextEdit.UI.Components.Toolbar;

/// <summary>
/// Toolbar component providing quick access to common operations.
/// Includes file operations, clipboard commands, font selection, and markdown formatting.
/// </summary>
public partial class Toolbar : ComponentBase, IDisposable
{
    [Inject] protected AppState AppState { get; set; } = default!;
    [Inject] protected MarkdownFormattingService FormattingService { get; set; } = default!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] protected IUndoRedoService UndoRedo { get; set; } = default!;

    protected override void OnInitialized()
    {
        // Re-render when global app state changes (e.g., toolbar visibility preference toggled)
        AppState.Changed += OnAppStateChanged;
    }

    private void OnAppStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        AppState.Changed -= OnAppStateChanged;
    }

    /// <summary>
    /// Returns platform-specific curated list of monospace fonts with generic fallback.
    /// </summary>
    private IEnumerable<string> GetSystemFonts()
    {
        // Platform detection via runtime check would be complex in Blazor
        // For now, provide a comprehensive list of common monospace fonts across platforms
        // User can select what's available on their system
        return new[]
        {
            // Cross-platform
            "monospace",
            "Courier New",
            
            // Windows
            "Consolas",
            "Cascadia Mono",
            "Cascadia Code",
            
            // macOS
            "SF Mono",
            "Menlo",
            "Monaco",
            
            // Linux
            "Liberation Mono",
            "DejaVu Sans Mono",
            "Ubuntu Mono",
            "Fira Code",
            "JetBrains Mono"
        };
    }

    private async Task HandleNew()
    {
        AppState.CreateNew();
        await Task.CompletedTask;
    }

    private async Task HandleOpen()
    {
        await AppState.OpenAsync();
    }

    private async Task HandleSave()
    {
        await AppState.SaveActiveAsync();
    }

    private async Task HandleUndo()
    {
        // Invoke Monaco's native undo command
        await JSRuntime.InvokeVoidAsync("textEditMonaco.executeCommand", "monaco-editor", "undo");
    }

    private async Task HandleRedo()
    {
        // Invoke Monaco's native redo command
        await JSRuntime.InvokeVoidAsync("textEditMonaco.executeCommand", "monaco-editor", "redo");
    }

    private async Task HandleCut()
    {
        if (AppState.ActiveDocument is null) return;
        
        try
        {
            // Get selection from Monaco editor
            var selectionObj = await JSRuntime.InvokeAsync<Dictionary<string, int>>("textEditMonaco.getSelectionRange", "monaco-editor");
            int start = selectionObj?.ContainsKey("start") == true ? selectionObj["start"] : 0;
            int end = selectionObj?.ContainsKey("end") == true ? selectionObj["end"] : 0;
            
            if (start == end) return; // No selection
            
            var doc = AppState.ActiveDocument;
            var selection = doc.Content.Substring(start, end - start);
            
            // Copy to clipboard
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", selection);
            
            // Remove selection from document
            var newContent = doc.Content.Substring(0, start) + doc.Content.Substring(end);
            
            // IMPORTANT: Use applyEdit for Monaco undo/redo integration ONLY
            // Do NOT update doc.SetContent() or AppState here - let Monaco's change event do it
            // This creates the undo point properly in Monaco's undo stack
            await JSRuntime.InvokeVoidAsync("textEditMonaco.applyEdit", "monaco-editor", new
            {
                content = newContent,
                selectionStart = start,
                selectionEnd = start
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HandleCut] Error: {ex}");
        }
    }

    private async Task HandleCopy()
    {
        try
        {
            // Get selection from Monaco editor
            var selectionObj = await JSRuntime.InvokeAsync<Dictionary<string, int>>("textEditMonaco.getSelectionRange", "monaco-editor");
            int start = selectionObj?.ContainsKey("start") == true ? selectionObj["start"] : 0;
            int end = selectionObj?.ContainsKey("end") == true ? selectionObj["end"] : 0;
            
            if (start == end) return; // No selection
            
            if (AppState.ActiveDocument is not null)
            {
                var selection = AppState.ActiveDocument.Content.Substring(start, end - start);
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", selection);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HandleCopy] Error: {ex}");
        }
    }

    private async Task HandlePaste()
    {
        if (AppState.ActiveDocument is null) return;
        
        try
        {
            var text = await JSRuntime.InvokeAsync<string>("navigator.clipboard.readText");
            
            if (!string.IsNullOrEmpty(text))
            {
                var doc = AppState.ActiveDocument;
                
                // Get selection from Monaco editor
                var selectionObj = await JSRuntime.InvokeAsync<Dictionary<string, int>>("textEditMonaco.getSelectionRange", "monaco-editor");
                int start = selectionObj?.ContainsKey("start") == true ? selectionObj["start"] : 0;
                int end = selectionObj?.ContainsKey("end") == true ? selectionObj["end"] : 0;
                
                var newContent = doc.Content.Substring(0, start) + text + doc.Content.Substring(end);
                
                // IMPORTANT: Use applyEdit for Monaco undo/redo integration ONLY
                // Do NOT update doc.SetContent() or AppState here - let Monaco's change event do it
                // This creates the undo point properly in Monaco's undo stack
                var newCaretPos = start + text.Length;
                await JSRuntime.InvokeVoidAsync("textEditMonaco.applyEdit", "monaco-editor", new
                {
                    content = newContent,
                    selectionStart = newCaretPos,
                    selectionEnd = newCaretPos
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HandlePaste] Error: {ex}");
        }
    }

    private async Task HandleFontFamilyChange(ChangeEventArgs e)
    {
        var fontFamily = e.Value?.ToString() ?? string.Empty;
        AppState.Preferences.FontFamily = fontFamily;
        await AppState.SavePreferencesAsync();
        
        // Apply font to Monaco editor via EditorCommandHub
        if (EditorCommandHub.FontFamilyChanged is not null)
        {
            await EditorCommandHub.FontFamilyChanged(fontFamily);
        }
        
        StateHasChanged();
    }

    private async Task HandleFontSizeChange(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int size))
        {
            AppState.Preferences.FontSize = size;
            await AppState.SavePreferencesAsync();
            
            // Apply font size to Monaco editor via EditorCommandHub
            if (EditorCommandHub.FontSizeChanged is not null)
            {
                await EditorCommandHub.FontSizeChanged(size);
            }
            
            StateHasChanged();
        }
    }

    private async Task HandleFormat(MarkdownFormat format)
    {
        if (AppState.ActiveDocument is null) return;
        
        try
        {
            var doc = AppState.ActiveDocument;
            
            // Get selection from Monaco editor
            var selectionObj = await JSRuntime.InvokeAsync<Dictionary<string, int>>("textEditMonaco.getSelectionRange", "monaco-editor");
            int start = selectionObj?.ContainsKey("start") == true ? selectionObj["start"] : 0;
            int end = selectionObj?.ContainsKey("end") == true ? selectionObj["end"] : 0;
            
            Console.WriteLine($"[Toolbar.HandleFormat] Selection: start={start}, end={end}");
            
            var result = FormattingService.ApplyFormat(doc.Content, start, end, format);
            Console.WriteLine($"[Toolbar.HandleFormat] After format: NewContent length={result.NewContent.Length}");
            
            // IMPORTANT: Use applyEdit for Monaco undo/redo integration ONLY
            // Do NOT call doc.SetContent() or AppState.NotifyDocumentUpdated() here - let Monaco's change event do it
            // This creates the undo point properly in Monaco's undo stack
            await JSRuntime.InvokeVoidAsync("textEditMonaco.applyEdit", "monaco-editor", new
            {
                content = result.NewContent,
                selectionStart = result.NewSelectionStart,
                selectionEnd = result.NewSelectionEnd
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Toolbar.HandleFormat] Error: {ex}");
        }
    }
}

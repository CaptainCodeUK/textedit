using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TextEdit.Core.Editing;
using TextEdit.UI.App;
using TextEdit.UI.Services;
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

    private async Task HandleCut()
    {
        if (AppState.ActiveDocument is null) return;
        
        try
        {
            // Get selection from textarea
            var selection = await JSRuntime.InvokeAsync<string>(
                "eval",
                @"document.getElementById('main-editor-textarea')?.value.substring(
                    document.getElementById('main-editor-textarea')?.selectionStart ?? 0,
                    document.getElementById('main-editor-textarea')?.selectionEnd ?? 0
                ) ?? ''"
            );
            
            if (!string.IsNullOrEmpty(selection))
            {
                // Copy to clipboard
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", selection);
                
                // Remove selection from document
                var doc = AppState.ActiveDocument;
                var start = await JSRuntime.InvokeAsync<int>("eval", "document.getElementById('main-editor-textarea')?.selectionStart ?? 0");
                var end = await JSRuntime.InvokeAsync<int>("eval", "document.getElementById('main-editor-textarea')?.selectionEnd ?? 0");
                
                // Push current state to undo before cutting
                UndoRedo.Push(doc, doc.Content);
                
                var newContent = doc.Content.Substring(0, start) + doc.Content.Substring(end);
                doc.SetContent(newContent);
                AppState.NotifyDocumentUpdated();
                
                // Update textarea and restore caret
                await JSRuntime.InvokeVoidAsync("eval", 
                    $"{{ const el = document.getElementById('main-editor-textarea'); if (el) {{ el.value = {System.Text.Json.JsonSerializer.Serialize(newContent)}; el.setSelectionRange({start}, {start}); }} }}");
            }
        }
        catch
        {
            // Ignore clipboard errors
        }
    }

    private async Task HandleCopy()
    {
        try
        {
            var selection = await JSRuntime.InvokeAsync<string>(
                "eval",
                @"document.getElementById('main-editor-textarea')?.value.substring(
                    document.getElementById('main-editor-textarea')?.selectionStart ?? 0,
                    document.getElementById('main-editor-textarea')?.selectionEnd ?? 0
                ) ?? ''"
            );
            
            if (!string.IsNullOrEmpty(selection))
            {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", selection);
            }
        }
        catch
        {
            // Ignore clipboard errors
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
                var start = await JSRuntime.InvokeAsync<int>("eval", "document.getElementById('main-editor-textarea')?.selectionStart ?? 0");
                var end = await JSRuntime.InvokeAsync<int>("eval", "document.getElementById('main-editor-textarea')?.selectionEnd ?? 0");
                
                // Push current state to undo before pasting
                UndoRedo.Push(doc, doc.Content);
                
                var newContent = doc.Content.Substring(0, start) + text + doc.Content.Substring(end);
                doc.SetContent(newContent);
                AppState.NotifyDocumentUpdated();
                
                // Update textarea and set caret after pasted text
                var newCaretPos = start + text.Length;
                await JSRuntime.InvokeVoidAsync("eval", 
                    $"{{ const el = document.getElementById('main-editor-textarea'); if (el) {{ el.value = {System.Text.Json.JsonSerializer.Serialize(newContent)}; el.setSelectionRange({newCaretPos}, {newCaretPos}); }} }}");
            }
        }
        catch
        {
            // Ignore clipboard errors
        }
    }

    private async Task HandleFontFamilyChange(ChangeEventArgs e)
    {
        var fontFamily = e.Value?.ToString() ?? string.Empty;
        AppState.Preferences.FontFamily = fontFamily;
        await AppState.SavePreferencesAsync();
        
        // Apply font to editor immediately
        await JSRuntime.InvokeVoidAsync("eval", 
            $@"{{ const el = document.getElementById('main-editor-textarea'); 
                 if (el) {{ el.style.fontFamily = {System.Text.Json.JsonSerializer.Serialize(fontFamily)}; }} }}");
        
        StateHasChanged();
    }

    private async Task HandleFontSizeChange(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int size))
        {
            AppState.Preferences.FontSize = size;
            await AppState.SavePreferencesAsync();
            
            // Apply font size to editor immediately
            await JSRuntime.InvokeVoidAsync("eval", 
                $@"{{ const el = document.getElementById('main-editor-textarea'); 
                     if (el) {{ el.style.fontSize = '{size}px'; }} }}");
            
            StateHasChanged();
        }
    }

    private async Task HandleFormat(MarkdownFormat format)
    {
        if (AppState.ActiveDocument is null) return;
        
        try
        {
            var doc = AppState.ActiveDocument;
            var start = await JSRuntime.InvokeAsync<int>("eval", "document.getElementById('main-editor-textarea')?.selectionStart ?? 0");
            var end = await JSRuntime.InvokeAsync<int>("eval", "document.getElementById('main-editor-textarea')?.selectionEnd ?? 0");
            
            // Push current state to undo before applying format
            UndoRedo.Push(doc, doc.Content);
            
            var result = FormattingService.ApplyFormat(doc.Content, start, end, format);
            
            doc.SetContent(result.NewContent);
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
}

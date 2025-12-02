using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TextEdit.Core.Documents;
using TextEdit.Core.SpellChecking;
using TextEdit.Infrastructure.SpellChecking;
using TextEdit.UI.App;

namespace TextEdit.UI.Components;

public partial class MonacoEditor : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;
    [Inject] private SpellCheckingService SpellCheckingService { get; set; } = default!;

    private DotNetObjectReference<MonacoEditor>? _dotNetRef;
    private bool _initialized;
    private string? _lastValue;
    private SpellCheckDecorationService _decorationService = new();
    private CancellationTokenSource? _spellCheckCts;
    private bool _isClientInitialized;

    protected override void OnInitialized()
    {
        // Don't do anything here - wait for OnAfterRenderAsync on client
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // In ServerPrerendered mode:
        // - firstRender=true on server (no JS available, can't initialize)
        // - firstRender=false on client after hydration (JS available, initialize)
        // 
        // Only initialize on client (when JS is available)
        // The firstRender parameter tells us this is the FIRST render in the component lifecycle
        // But in ServerPrerendered, we need to check if JS is available
        
        if (!_initialized)
        {
            _initialized = true; // Set immediately to prevent duplicate calls
            _dotNetRef ??= DotNetObjectReference.Create(this);
            try
            {
                System.Diagnostics.Debug.WriteLine($"[MonacoEditor] Attempting to initialize Monaco (firstRender={firstRender})");
                await JS.InvokeVoidAsync("textEditMonaco.loadMonaco");
                await JS.InvokeVoidAsync("textEditMonaco.createEditor", "monaco-editor", _dotNetRef, new { value = Value ?? string.Empty, language = "markdown" });
                _lastValue = Value;

                // Apply persisted settings
                await ApplyPersistedSettings();
                
                try {
                    await JS.InvokeVoidAsync("editorFocus.setActiveEditor", "monaco-editor");
                    await JS.InvokeVoidAsync("editorFocus.focusActiveEditor");
                } catch { }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the component
                System.Diagnostics.Debug.WriteLine($"[MonacoEditor] Monaco editor initialization error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MonacoEditor] Stack trace: {ex.StackTrace}");
                // Don't throw - let the component render without Monaco
            }
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_initialized && _lastValue != Value)
        {
            await JS.InvokeVoidAsync("textEditMonaco.setValue", "monaco-editor", Value ?? string.Empty);
            _lastValue = Value;
        }
        await base.OnParametersSetAsync();
    }

    [JSInvokable]
    public async Task OnEditorContentChanged(string content)
    {
        _cachedValue = content;
        _lastValue = content;
        Value = content;
        await ValueChanged.InvokeAsync(content);
        
        // Trigger spell checking on content change
        _ = UpdateSpellCheckAsync(content);
    }

    // Runtime option updates
    public async Task SetLineNumbersAsync(bool show)
    {
        if (_initialized)
            await JS.InvokeVoidAsync("textEditMonaco.setLineNumbers", "monaco-editor", show);
    }

    public async Task SetMinimapAsync(bool enabled)
    {
        if (_initialized)
            await JS.InvokeVoidAsync("textEditMonaco.setMinimap", "monaco-editor", enabled);
    }

    public async Task SetWordWrapAsync(bool enabled)
    {
        if (_initialized)
            await JS.InvokeVoidAsync("textEditMonaco.setWordWrap", "monaco-editor", enabled);
    }

    public async Task SetFontSizeAsync(int size)
    {
        if (_initialized)
            await JS.InvokeVoidAsync("textEditMonaco.setFontSize", "monaco-editor", size);
    }

    public async Task SetFontFamilyAsync(string fontFamily)
    {
        if (_initialized)
            await JS.InvokeVoidAsync("textEditMonaco.setFontFamily", "monaco-editor", fontFamily);
    }

    public async Task SetThemeAsync(string theme)
    {
        if (_initialized)
            await JS.InvokeVoidAsync("textEditMonaco.setTheme", "monaco-editor", theme);
    }

    public async Task SetLanguageAsync(string language)
    {
        if (_initialized)
            await JS.InvokeVoidAsync("textEditMonaco.setLanguage", "monaco-editor", language);
    }

    /// <summary>
    /// Updates spell check decorations in real-time as user edits.
    /// Debounced to avoid performance issues during rapid typing.
    /// </summary>
    private async Task UpdateSpellCheckAsync(string text)
    {
        try
        {
            // Cancel previous spell check if still running
            _spellCheckCts?.Cancel();
            _spellCheckCts = new CancellationTokenSource();

            // Check spelling with the debounced service
            var results = await SpellCheckingService.CheckSpellingAsync(text, _spellCheckCts.Token);

            // Convert results to Monaco decorations
            var decorations = _decorationService.ConvertToDecorations(results);

            // Apply decorations to editor via JavaScript
            await JS.InvokeVoidAsync(
                "textEditMonaco.setSpellCheckDecorations",
                "monaco-editor",
                decorations
            );
        }
        catch (OperationCanceledException)
        {
            // Spell check was cancelled (new change came in), which is expected
        }
        catch (Exception ex)
        {
            // Log but don't crash - spell checking is non-critical
            System.Diagnostics.Debug.WriteLine($"Spell check error: {ex.Message}");
        }
    }

    private async Task ApplyPersistedSettings()
    {
        if (!_initialized) return;
        
        try
        {
            // Apply line numbers setting
            await JS.InvokeVoidAsync("textEditMonaco.setLineNumbers", "monaco-editor", AppState.Preferences.ShowLineNumbers);
            
            // Apply minimap setting
            await JS.InvokeVoidAsync("textEditMonaco.setMinimap", "monaco-editor", AppState.Preferences.ShowMinimap);
            
            // Apply font size setting
            await JS.InvokeVoidAsync("textEditMonaco.setFontSize", "monaco-editor", AppState.Preferences.FontSize);
            
            // Apply font family setting
            if (!string.IsNullOrEmpty(AppState.Preferences.FontFamily))
                await JS.InvokeVoidAsync("textEditMonaco.setFontFamily", "monaco-editor", AppState.Preferences.FontFamily);
            
            // Apply theme setting
            var monacoTheme = AppState.Preferences.Theme switch
            {
                TextEdit.Core.Preferences.ThemeMode.Dark => "vs-dark",
                TextEdit.Core.Preferences.ThemeMode.Light => "vs",
                _ => "vs-dark" // System -> Dark (interim default)
            };
            await JS.InvokeVoidAsync("textEditMonaco.setTheme", "monaco-editor", monacoTheme);
        }
        catch { /* ignore */ }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            // Clear spell check decorations first
            await JS.InvokeVoidAsync("textEditMonaco.clearSpellCheckDecorations", "monaco-editor");
            
            // Then dispose editor
            await JS.InvokeVoidAsync("textEditMonaco.disposeEditor", "monaco-editor");
        }
        catch { }
        
        // Cancel any pending spell check operations
        _spellCheckCts?.Cancel();
        _spellCheckCts?.Dispose();
        
        _dotNetRef?.Dispose();
    }
}


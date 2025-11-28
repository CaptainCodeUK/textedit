using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TextEdit.Core.Documents;
using TextEdit.UI.App;

namespace TextEdit.UI.Components;

public partial class MonacoEditor : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;

    private DotNetObjectReference<MonacoEditor>? _dotNetRef;
    private bool _initialized;
    private string? _lastValue;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            try
            {
                await JS.InvokeVoidAsync("textEditMonaco.loadMonaco");
                await JS.InvokeVoidAsync("textEditMonaco.createEditor", "monaco-editor", _dotNetRef, new { value = Value ?? string.Empty, language = "markdown" });
                _initialized = true;
                _lastValue = Value;
                
                // Apply persisted settings
                await ApplyPersistedSettings();
                
                try {
                    await JS.InvokeVoidAsync("editorFocus.setActiveEditor", "monaco-editor");
                    await JS.InvokeVoidAsync("editorFocus.focusActiveEditor");
                } catch { }
            }
            catch
            {
                // Ignore monaco load failures for prototype; fallback silently
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
            await JS.InvokeVoidAsync("textEditMonaco.disposeEditor", "monaco-editor");
        }
        catch { }
        _dotNetRef?.Dispose();
    }
}

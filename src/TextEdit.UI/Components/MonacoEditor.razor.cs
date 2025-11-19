using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TextEdit.Core.Documents;

namespace TextEdit.UI.Components;

public partial class MonacoEditor : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

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

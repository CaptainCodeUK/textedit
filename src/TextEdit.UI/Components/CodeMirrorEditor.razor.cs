using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace TextEdit.UI.Components;

public partial class CodeMirrorEditor : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private DotNetObjectReference<CodeMirrorEditor>? _dotNetRef;
    private bool _initialized;
    private string? _lastValue;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            try
            {
                await JS.InvokeVoidAsync("textEditCodeMirror.loadCodeMirror");
                await JS.InvokeVoidAsync("textEditCodeMirror.createEditor", "codemirror-editor", _dotNetRef,
                    new { value = Value ?? string.Empty, mode = "markdown" });
                _initialized = true;
                _lastValue = Value;
                try {
                    await JS.InvokeVoidAsync("editorFocus.setActiveEditor", "codemirror-editor");
                    await JS.InvokeVoidAsync("editorFocus.focusActiveEditor");
                } catch { }
            }
            catch
            {
                // Ignore load failures for prototype
            }
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_initialized && _lastValue != Value)
        {
            await JS.InvokeVoidAsync("textEditCodeMirror.setValue", "codemirror-editor", Value ?? string.Empty);
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
            await JS.InvokeVoidAsync("textEditCodeMirror.disposeEditor", "codemirror-editor");
        }
        catch { }
        _dotNetRef?.Dispose();
    }
}

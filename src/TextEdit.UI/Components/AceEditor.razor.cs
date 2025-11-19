using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TextEdit.Core.Documents;

namespace TextEdit.UI.Components;

public partial class AceEditor : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private DotNetObjectReference<AceEditor>? _dotNetRef;
    private bool _initialized;
    private string? _lastValue;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            try
            {
                await JS.InvokeVoidAsync("textEditAce.loadAce");
                await JS.InvokeVoidAsync("textEditAce.createEditor", "ace-editor", _dotNetRef, new { value = Value ?? string.Empty, mode = "markdown" });
                _initialized = true;
                _lastValue = Value;
                try {
                    await JS.InvokeVoidAsync("editorFocus.setActiveEditor", "ace-editor");
                    await JS.InvokeVoidAsync("editorFocus.focusActiveEditor");
                } catch { }
            }
            catch
            {
                // Ignore ACE load failures for prototype; fallback silently
            }
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_initialized && _lastValue != Value)
        {
            await JS.InvokeVoidAsync("textEditAce.setValue", "ace-editor", Value ?? string.Empty);
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
            await JS.InvokeVoidAsync("textEditAce.disposeEditor", "ace-editor");
        }
        catch { }
        _dotNetRef?.Dispose();
    }
}

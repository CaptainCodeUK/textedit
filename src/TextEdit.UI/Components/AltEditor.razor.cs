using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TextEdit.Core.Documents;
using TextEdit.UI.App;

namespace TextEdit.UI.Components;

public partial class AltEditor : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private DotNetObjectReference<AltEditor>? _dotNetRef;
    private bool _initialized;
    private string? _lastValue;

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            try
            {
                await JS.InvokeVoidAsync("textEditMonaco.loadMonaco");
                await JS.InvokeVoidAsync("textEditMonaco.createEditor", "alt-monaco", _dotNetRef, new { value = Value ?? string.Empty, language = "markdown" });
                _initialized = true;
                _lastValue = Value;
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
            // Update Monaco editor only when parent changes content (not editor-originated changes)
            await JS.InvokeVoidAsync("textEditMonaco.setValue", "alt-monaco", Value ?? string.Empty);
            _lastValue = Value;
        }
        await base.OnParametersSetAsync();
    }

    [JSInvokable]
    public async Task OnEditorContentChanged(string content)
    {
        _cachedValue = content;
        // Mark last value so we don't reapply to the monaco editor when parent re-renders
        _lastValue = content;
        Value = content;
        await ValueChanged.InvokeAsync(content);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("textEditMonaco.disposeEditor", "alt-monaco");
        }
        catch { }
        _dotNetRef?.Dispose();
    }
}
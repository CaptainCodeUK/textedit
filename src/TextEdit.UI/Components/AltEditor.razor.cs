using Microsoft.AspNetCore.Components;

namespace TextEdit.UI.Components;

public partial class AltEditor : IAsyncDisposable
{
    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
    // Legacy alt editor implementation removed; use `MonacoEditor` instead.
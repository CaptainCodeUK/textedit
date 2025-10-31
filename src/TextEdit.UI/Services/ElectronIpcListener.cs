using Microsoft.JSInterop;
using TextEdit.UI.App;

namespace TextEdit.UI.Services;

/// <summary>
/// Listens for IPC messages from Electron and dispatches to AppState.
/// Implements contracts from specs/002-v1-1-enhancements/contracts/
/// </summary>
public class ElectronIpcListener : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly AppState _appState;
    private DotNetObjectReference<ElectronIpcListener>? _objRef;
    private bool _initialized;

    public ElectronIpcListener(IJSRuntime js, AppState appState)
    {
        _js = js;
        _appState = appState;
    }

    /// <summary>
    /// Initialize IPC listeners. Call this from App.razor or main component OnAfterRenderAsync.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            _objRef = DotNetObjectReference.Create(this);
            
            // Register listeners for Electron IPC channels
            await _js.InvokeVoidAsync("electronIpc.register", _objRef, "cli-file-args");
            
            _initialized = true;
        }
        catch (Exception)
        {
            // Initialization failed - intentionally silent to avoid noisy console output
        }
    }

    /// <summary>
    /// Called from JavaScript when cli-file-args IPC message is received.
    /// Per contracts/cli-file-args.md
    /// </summary>
    [JSInvokable]
    public async Task OnCliFileArgs(CliFileArgsMessage message)
    {
        try
        {
            // Received CLI file args

            // Open valid files
            if (message.ValidFiles?.Length > 0)
            {
                await _appState.OpenFilesAsync(message.ValidFiles);
            }

            // Show invalid files summary
            if (message.InvalidFiles?.Length > 0)
            {
                _appState.SetCliInvalidFiles(
                    message.InvalidFiles.Select(f => (f.Path, f.Reason))
                );
            }
        }
        catch (Exception)
        {
            // Swallow errors; avoid console noise
        }
    }

    // OS theme change detection deferred – no theme-changed IPC handling

    public async ValueTask DisposeAsync()
    {
        if (_objRef != null)
        {
            try
            {
                await _js.InvokeVoidAsync("electronIpc.unregister", "cli-file-args");
                // No theme-changed listener registered
            }
            catch
            {
                // Ignore errors during disposal
            }

            _objRef.Dispose();
        }
    }

    /// <summary>
    /// Message structure per contracts/cli-file-args.md
    /// </summary>
    public class CliFileArgsMessage
    {
        public string[] ValidFiles { get; set; } = Array.Empty<string>();
        public InvalidFileInfo[] InvalidFiles { get; set; } = Array.Empty<InvalidFileInfo>();
        public string LaunchType { get; set; } = "initial";
    }

    public class InvalidFileInfo
    {
        public string Path { get; set; } = "";
        public string Reason { get; set; } = "";
    }

    // ThemeChangedMessage deferred – no longer used
}

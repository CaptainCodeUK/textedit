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
            await _js.InvokeVoidAsync("electronIpc.register", _objRef, "theme-changed");
            
            _initialized = true;
            Console.WriteLine("[IPC] ElectronIpcListener initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IPC] Failed to initialize ElectronIpcListener: {ex.Message}");
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
            Console.WriteLine($"[IPC] Received cli-file-args: {message.ValidFiles?.Length ?? 0} valid, {message.InvalidFiles?.Length ?? 0} invalid, launch: {message.LaunchType}");

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
        catch (Exception ex)
        {
            Console.WriteLine($"[IPC] Error processing cli-file-args: {ex.Message}");
        }
    }

    /// <summary>
    /// Called from JavaScript when theme-changed IPC message is received.
    /// Per contracts/theme-changed.md
    /// </summary>
    [JSInvokable]
    public Task OnThemeChanged(ThemeChangedMessage message)
    {
        try
        {
            Console.WriteLine($"[IPC] Received theme-changed: {message.Theme} at {message.Timestamp}");

            // Only apply if user preference is System
            if (_appState.Preferences.Theme == TextEdit.Core.Preferences.ThemeMode.System)
            {
                // Apply the theme directly via ThemeManager
                // Note: We access ThemeManager through AppState's internal state
                // For now, we'll trigger a re-apply which will check system theme
                _ = _appState.ApplyThemeAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IPC] Error processing theme-changed: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_objRef != null)
        {
            try
            {
                await _js.InvokeVoidAsync("electronIpc.unregister", "cli-file-args");
                await _js.InvokeVoidAsync("electronIpc.unregister", "theme-changed");
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

    /// <summary>
    /// Message structure per contracts/theme-changed.md
    /// </summary>
    public class ThemeChangedMessage
    {
        public string Theme { get; set; } = "light";
        public string Timestamp { get; set; } = "";
    }
}

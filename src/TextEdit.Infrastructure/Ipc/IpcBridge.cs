namespace TextEdit.Infrastructure.Ipc;

/// <summary>
/// Bridge for invoking native file dialogs and IPC operations. Stub for Phase 2.
/// Actual Electron calls will be placed in TextEdit.App where Electron is available.
/// </summary>
public class IpcBridge
{
    public Task<string?> ShowOpenFileDialogAsync()
    {
        // Placeholder: actual implementation will use Electron in App layer
        return Task.FromResult<string?>(null);
    }

    public Task<string?> ShowSaveFileDialogAsync()
    {
        // Placeholder
        return Task.FromResult<string?>(null);
    }
}

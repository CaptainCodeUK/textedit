using ElectronNET.API;
using ElectronNET.API.Entities;

namespace TextEdit.Infrastructure.Ipc;

/// <summary>
/// Bridge for invoking native file dialogs via Electron.
/// </summary>
public class IpcBridge
{
    public async Task<string?> ShowOpenFileDialogAsync()
    {
        if (!HybridSupport.IsElectronActive)
        {
            return null;
        }

        var options = new OpenDialogOptions
        {
            Properties = new[] { OpenDialogProperty.openFile },
            Filters = new[]
            {
                new FileFilter { Name = "Text Files", Extensions = new[] { "txt", "md", "log", "csv" } },
                new FileFilter { Name = "All Files", Extensions = new[] { "*" } }
            }
        };

    var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
        var result = await Electron.Dialog.ShowOpenDialogAsync(window, options);
        if (result is null || result.Length == 0)
        {
            return null;
        }
        return result[0];
    }

    public async Task<string?> ShowSaveFileDialogAsync()
    {
        if (!HybridSupport.IsElectronActive)
        {
            return null;
        }

        var options = new SaveDialogOptions
        {
            Filters = new[]
            {
                new FileFilter { Name = "Text Files", Extensions = new[] { "txt" } },
                new FileFilter { Name = "All Files", Extensions = new[] { "*" } }
            }
        };

    var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
        var result = await Electron.Dialog.ShowSaveDialogAsync(window, options);
        if (string.IsNullOrWhiteSpace(result))
        {
            return null;
        }
        return result;
    }
}

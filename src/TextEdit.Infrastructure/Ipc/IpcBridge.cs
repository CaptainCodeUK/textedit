using ElectronNET.API;
using ElectronNET.API.Entities;

namespace TextEdit.Infrastructure.Ipc;

/// <summary>
/// Bridge for invoking native file dialogs via Electron.
/// </summary>
public class IpcBridge
{
    public enum CloseDecision
    {
        Save,
        DontSave,
        Cancel
    }

    public async Task<CloseDecision> ConfirmCloseDirtyAsync(string? name)
    {
        if (!HybridSupport.IsElectronActive)
        {
            return CloseDecision.Cancel;
        }

        var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
        var opts = new MessageBoxOptions($"Save changes to '{(string.IsNullOrWhiteSpace(name) ? "Untitled" : name)}'?")
        {
            Type = MessageBoxType.question,
            Buttons = new[] { "Save", "Don't Save", "Cancel" },
            DefaultId = 0,
            CancelId = 2,
            NoLink = true
        };
        MessageBoxResult resp;
        if (window is null)
        {
            // Fallback when window reference is unavailable
            resp = await Electron.Dialog.ShowMessageBoxAsync(opts);
        }
        else
        {
            resp = await Electron.Dialog.ShowMessageBoxAsync(window, opts);
        }
        return resp.Response switch
        {
            0 => CloseDecision.Save,
            1 => CloseDecision.DontSave,
            _ => CloseDecision.Cancel
        };
    }
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
        
        // If no extension, append .txt
        if (!Path.HasExtension(result))
        {
            result = result + ".txt";
        }
        
        return result;
    }
}

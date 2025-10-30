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

    public enum ExternalChangeDecision
    {
        Reload,
        Keep,
        Cancel
    }

    public virtual async Task<CloseDecision> ConfirmCloseDirtyAsync(string? name)
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
    public virtual async Task<string?> ShowOpenFileDialogAsync()
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

    public virtual async Task<string?> ShowSaveFileDialogAsync()
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

    public virtual async Task<ExternalChangeDecision> ConfirmReloadExternalAsync(string? name)
    {
        if (!HybridSupport.IsElectronActive)
        {
            return ExternalChangeDecision.Cancel;
        }

        var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
        var opts = new MessageBoxOptions($"'{(string.IsNullOrWhiteSpace(name) ? "Untitled" : name)}' was modified on disk. Reload?")
        {
            Type = MessageBoxType.warning,
            Buttons = new[] { "Reload", "Keep", "Cancel" },
            DefaultId = 0,
            CancelId = 2,
            NoLink = true
        };
        MessageBoxResult resp;
        if (window is null)
        {
            resp = await Electron.Dialog.ShowMessageBoxAsync(opts);
        }
        else
        {
            resp = await Electron.Dialog.ShowMessageBoxAsync(window, opts);
        }
        return resp.Response switch
        {
            0 => ExternalChangeDecision.Reload,
            1 => ExternalChangeDecision.Keep,
            _ => ExternalChangeDecision.Cancel
        };
    }

    /// <summary>
    /// Send CLI file arguments to Blazor via IPC (per contracts/cli-file-args.md).
    /// </summary>
    public virtual void SendCliFileArgs(string[] validFiles, CliInvalidFileInfo[] invalidFiles, string launchType)
    {
        if (!HybridSupport.IsElectronActive)
        {
            return;
        }

        var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
        if (window is null)
        {
            Console.WriteLine("[IPC] No BrowserWindow available to send cli-file-args");
            return;
        }

        var message = new
        {
            validFiles = validFiles ?? Array.Empty<string>(),
            invalidFiles = invalidFiles ?? Array.Empty<CliInvalidFileInfo>(),
            launchType = launchType
        };

        try
        {
            Electron.IpcMain.Send(window, "cli-file-args", message);
            Console.WriteLine($"[IPC] Sent cli-file-args: {validFiles?.Length ?? 0} valid, {invalidFiles?.Length ?? 0} invalid, launch: {launchType}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IPC] Failed to send cli-file-args: {ex.Message}");
        }
    }

    /// <summary>
    /// Send theme change notification to Blazor via IPC (per contracts/theme-changed.md).
    /// </summary>
    public virtual void SendThemeChanged(string theme)
    {
        if (!HybridSupport.IsElectronActive)
        {
            return;
        }

        var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
        if (window is null)
        {
            Console.WriteLine("[IPC] No BrowserWindow available to send theme-changed");
            return;
        }

        var message = new
        {
            theme = theme, // "light" or "dark"
            timestamp = DateTime.UtcNow.ToString("O") // ISO 8601
        };

        try
        {
            Electron.IpcMain.Send(window, "theme-changed", message);
            Console.WriteLine($"[IPC] Sent theme-changed: {theme}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IPC] Failed to send theme-changed: {ex.Message}");
        }
    }

    /// <summary>
    /// Update the Electron window title (T056-T060).
    /// </summary>
    public virtual void SetWindowTitle(string title)
    {
        if (!HybridSupport.IsElectronActive)
        {
            return;
        }

        var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
        if (window is null)
        {
            return;
        }

        try
        {
            window.SetTitle(title);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IPC] Failed to set window title: {ex.Message}");
        }
    }

    /// <summary>
    /// Open a URL in the system's default external browser.
    /// </summary>
    public virtual async Task OpenExternalAsync(string url)
    {
        if (!HybridSupport.IsElectronActive)
        {
            return;
        }

        try
        {
            Console.WriteLine($"[IpcBridge] Opening external URL: {url}");
            // Use Process.Start as the most reliable cross-platform method
            await Task.Run(() =>
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            });
            Console.WriteLine($"[IpcBridge] Successfully launched URL");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IpcBridge] Failed to open external URL: {ex.Message}");
        }
    }

    /// <summary>
    /// Info about a file that couldn't be opened (for IPC messages).
    /// </summary>
    public record CliInvalidFileInfo(string Path, string Reason);
}

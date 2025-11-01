using ElectronNET.API;
using ElectronNET.API.Entities;
using TextEdit.Core.Preferences;
using TextEdit.Infrastructure.Lifecycle;

namespace TextEdit.Infrastructure.Ipc;

/// <summary>
/// Bridge for invoking native file dialogs via Electron.
/// </summary>
public class IpcBridge
{
    private readonly IPreferencesRepository _prefsRepo;

    /// <summary>
    /// Create a new IPC bridge using the provided preferences repository.
    /// </summary>
    /// <param name="prefsRepo">Preferences repository to read file filters from.</param>
    public IpcBridge(IPreferencesRepository prefsRepo)
    {
        _prefsRepo = prefsRepo;
    }

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

    /// <summary>
    /// Show a native confirmation dialog for closing a dirty document.
    /// </summary>
    /// <param name="name">Document display name or null/empty for Untitled.</param>
    /// <returns>User decision for Save/Don't Save/Cancel.</returns>
    public virtual async Task<CloseDecision> ConfirmCloseDirtyAsync(string? name)
    {
        if (AppShutdown.IsShuttingDown)
        {
            // During shutdown, avoid dialogs; indicate cancel to prevent disruptive flows
            return CloseDecision.Cancel;
        }
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
    /// <summary>
    /// Show the native Open File dialog and return the selected file path or null when cancelled.
    /// </summary>
    public virtual async Task<string?> ShowOpenFileDialogAsync()
    {
        if (!HybridSupport.IsElectronActive)
        {
            return null;
        }

        // Get file extensions from preferences
        var prefs = await _prefsRepo.LoadAsync();
        var extensions = prefs.FileExtensions
            .Select(ext => ext.TrimStart('.')) // Remove leading dot for Electron filter
            .Where(ext => !string.IsNullOrWhiteSpace(ext))
            .ToArray();

        var options = new OpenDialogOptions
        {
            Properties = new[] { OpenDialogProperty.openFile },
            Filters = new[]
            {
                new FileFilter { Name = "Text Files", Extensions = extensions.Length > 0 ? extensions : new[] { "txt", "md" } },
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

    /// <summary>
    /// Show the native Save File dialog and return the chosen path or null when cancelled.
    /// Appends a default extension when missing.
    /// </summary>
    public virtual async Task<string?> ShowSaveFileDialogAsync()
    {
        if (!HybridSupport.IsElectronActive)
        {
            return null;
        }

        // Get file extensions from preferences
        var prefs = await _prefsRepo.LoadAsync();
        var extensions = prefs.FileExtensions
            .Select(ext => ext.TrimStart('.')) // Remove leading dot for Electron filter
            .Where(ext => !string.IsNullOrWhiteSpace(ext))
            .ToArray();

        var options = new SaveDialogOptions
        {
            Filters = new[]
            {
                new FileFilter { Name = "Text Files", Extensions = extensions.Length > 0 ? extensions : new[] { "txt" } },
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

    /// <summary>
    /// Ask the user what to do when a file is changed externally (Reload/Keep/Cancel).
    /// </summary>
    /// <param name="name">Document display name or null/empty for Untitled.</param>
    public virtual async Task<ExternalChangeDecision> ConfirmReloadExternalAsync(string? name)
    {
        if (AppShutdown.IsShuttingDown)
        {
            // During shutdown, avoid dialogs; keep current in-memory content
            return ExternalChangeDecision.Keep;
        }
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
        if (AppShutdown.IsShuttingDown)
        {
            return;
        }
        if (!HybridSupport.IsElectronActive)
        {
            return;
        }

        var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
        if (window is null)
        {
            // No BrowserWindow available - silently ignore
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
        }
        catch
        {
            // Ignore IPC send failures to avoid console spam
        }
    }

    /// <summary>
    /// Send theme change notification to Blazor via IPC (per contracts/theme-changed.md).
    /// </summary>
    public virtual void SendThemeChanged(string theme)
    {
        if (AppShutdown.IsShuttingDown)
        {
            return;
        }
        if (!HybridSupport.IsElectronActive)
        {
            return;
        }

        var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
        if (window is null)
        {
            // No BrowserWindow available - silently ignore
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
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// Update the Electron window title (T056-T060).
    /// </summary>
    public virtual void SetWindowTitle(string title)
    {
        if (AppShutdown.IsShuttingDown)
        {
            return;
        }
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
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// Open a URL in the system's default external browser.
    /// </summary>
    public virtual async Task OpenExternalAsync(string url)
    {
        if (AppShutdown.IsShuttingDown)
        {
            return;
        }
        if (!HybridSupport.IsElectronActive)
        {
            return;
        }

        try
        {
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
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// Info about a file that couldn't be opened (for IPC messages).
    /// </summary>
    public record CliInvalidFileInfo(string Path, string Reason);
}

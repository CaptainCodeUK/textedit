using ElectronNET.API;
using ElectronNET.API.Entities;
using TextEdit.UI.App;
using TextEdit.UI.Components.Editor;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using TextEdit.Infrastructure.Ipc;

namespace TextEdit.App;

/// <summary>
/// Manages Electron window lifecycle and native integration
/// Phase 1: Basic window setup
/// Phase 2: File dialogs and IPC
/// </summary>
public static partial class ElectronHost
{
    private static WebApplication? _app;
    private static AppState? _appState;
    private static string[] _initialArgs = Array.Empty<string>();

    /// <summary>
    /// Initialize Electron window and native features - Phase 1
    /// </summary>
    /// <param name="app">The web application instance</param>
    /// <param name="args">Command-line arguments from Program.cs (T041)</param>
    public static void Initialize(WebApplication app, string[] args)
    {
        _app = app;
        _appState = app.Services.GetRequiredService<AppState>();
        _initialArgs = args ?? Array.Empty<string>();
        
        // Subscribe to editor state changes to update menu checkmarks
        _appState.EditorState.Changed += OnEditorStateChanged;
        
        // Phase 3: Single-instance enforcement
        _ = SetupSingleInstanceAsync();
        
        _ = CreateMainWindowAsync();
        RegisterIpcHandlers();
    }

    /// <summary>
    /// Setup single-instance enforcement. Second launches focus existing window and forward args.
    /// </summary>
    private static async Task SetupSingleInstanceAsync()
    {
        if (!HybridSupport.IsElectronActive) return;

        var gotLock = await Electron.App.RequestSingleInstanceLockAsync((args, workingDirectory) =>
        {
            
            _ = Task.Run(async () =>
            {
                try
                {
                    var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
                    if (window != null)
                    {
                        if (await window.IsMinimizedAsync())
                        {
                            window.Unmaximize(); // Restore from minimized (sync call)
                        }
                        window.Show();
                        window.Focus();
                    }

                    // Process command-line args from second instance
                    var cliArgs = args.Skip(1).ToArray(); // Skip executable path
                    if (_app != null && cliArgs.Length > 0)
                    {
                        var (valid, invalid) = CliArgProcessor.ParseAndValidate(cliArgs);
                        using var scope = _app.Services.CreateScope();
                        var ipcBridge = scope.ServiceProvider.GetRequiredService<IpcBridge>();
                        
                        // Send via IPC per contracts/cli-file-args.md
                        ipcBridge.SendCliFileArgs(
                            valid.ToArray(),
                            invalid.Select(i => new IpcBridge.CliInvalidFileInfo(i.Path, i.Reason)).ToArray(),
                            "second-instance"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ElectronHost] Second-instance handling failed: {ex.Message}");
                }
            });
        });
        
        if (!gotLock)
        {
            // This is a second instance - it will exit automatically
            
            Electron.App.Exit();
            return;
        }
    }

    private static void OnEditorStateChanged()
    {
        ConfigureMenus();
    }

    /// <summary>
    /// Create the main BrowserWindow and configure lifecycle hooks.
    /// </summary>
    private static async Task CreateMainWindowAsync()
    {
        var swStartup = Stopwatch.StartNew();
        var window = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
        {
            Width = 1200,
            Height = 800,
            MinWidth = 800,
            MinHeight = 600,
            Title = "Scrappy Text Editor",
            WebPreferences = new WebPreferences
            {
                NodeIntegration = false,
                ContextIsolation = true
            }
        });

        // Window lifecycle events
        // Persist on both Close (before) and Closed (after) to be safe on all platforms
        window.OnClose += () => PersistAndQuit();
        window.OnClosed += () => PersistAndQuit();

        // Application menu will be configured in Phase 6 (US3)
        ConfigureMenus();

        swStartup.Stop();
        

        // IPC handlers will be registered here in Phase 2
        // RegisterIpcHandlers();

        // Phase 3: Process initial CLI file arguments (non-blocking)
        _ = Task.Run(async () =>
        {
            try
            {
                await ElectronHost.ProcessInitialCliArgsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLI] Processing initial args failed: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Configure application menus (File/Edit/View) - Phase 6
    /// </summary>
    private static void ConfigureMenus()
    {
        // Determine platform-specific quit accelerator
        var quitAccelerator = Environment.OSVersion.Platform == PlatformID.Win32NT ? "Alt+F4" : "CmdOrCtrl+Q";

        var fileMenu = new MenuItem
        {
            Label = "File",
            Submenu = new MenuItem[]
            {
                new MenuItem { Label = "New", Accelerator = "CmdOrCtrl+N", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.NewRequested); } },
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Label = "Open…", Accelerator = "CmdOrCtrl+O", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.OpenRequested); } },
                new MenuItem { Label = "Save", Accelerator = "CmdOrCtrl+S", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.SaveRequested); } },
                new MenuItem { Label = "Save As…", Accelerator = "CmdOrCtrl+Shift+S", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.SaveAsRequested); } },
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Label = "Close Tab", Accelerator = "CmdOrCtrl+W", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.CloseTabRequested); } },
                new MenuItem { Type = MenuType.separator },
                // Ensure we persist the session even when quitting via menu/accelerator
                new MenuItem { Label = "Quit", Accelerator = quitAccelerator, Click = () => PersistAndQuit() }
            }
        };

        var editMenu = new MenuItem
        {
            Label = "Edit",
            Submenu = new MenuItem[]
            {
                new MenuItem { Label = "Undo", Accelerator = "CmdOrCtrl+Z", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.UndoRequested); } },
                new MenuItem { Label = "Redo", Accelerator = "CmdOrCtrl+Y", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.RedoRequested); } },
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Role = MenuRole.cut },
                new MenuItem { Role = MenuRole.copy },
                new MenuItem { Role = MenuRole.paste },
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Label = OperatingSystem.IsMacOS() ? "Preferences…" : "Options…", Accelerator = OperatingSystem.IsMacOS() ? "Cmd+," : "Ctrl+,", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.OptionsRequested); } },
            }
        };

        var windowMenu = new MenuItem
        {
            Label = "Window",
            Submenu = new MenuItem[]
            {
                new MenuItem { Label = "Next Tab", Accelerator = "Ctrl+Tab", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.NextTabRequested); } },
                new MenuItem { Label = "Previous Tab", Accelerator = "Ctrl+Shift+Tab", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.PrevTabRequested); } },
                // Keep accelerators but hide these entries from the menu (if Visible is supported)
                new MenuItem { Label = "Next Tab (PageDown)", Accelerator = "Ctrl+PageDown", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.NextTabRequested); }, Visible = false },
                new MenuItem { Label = "Previous Tab (PageUp)", Accelerator = "Ctrl+PageUp", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.PrevTabRequested); }, Visible = false },
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Label = "Close Other Tabs", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.CloseOthersRequested); } },
                new MenuItem { Label = "Close Tabs to the Right", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.CloseRightRequested); } },
            }
        };

        var viewMenu = new MenuItem
        {
            Label = "View",
            Submenu = new MenuItem[]
            {
                new MenuItem { Label = "Toggle Word Wrap", Accelerator = "Alt+Z", Type = MenuType.checkbox, Checked = _appState?.EditorState.WordWrap ?? false, Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.ToggleWordWrapRequested); } },
                new MenuItem { Label = "Toggle Markdown Preview", Accelerator = "Alt+P", Type = MenuType.checkbox, Checked = _appState?.EditorState.ShowPreview ?? false, Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.TogglePreviewRequested); } },
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Role = MenuRole.reload },
                new MenuItem { Role = MenuRole.toggledevtools },
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Role = MenuRole.togglefullscreen }
            }
        };

        var helpMenu = new MenuItem
        {
            Label = "Help",
            Submenu = new MenuItem[]
            {
                new MenuItem { Label = "About Scrappy Text Editor", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.AboutRequested); } }
            }
        };

        Electron.Menu.SetApplicationMenu(new[] { fileMenu, editMenu, viewMenu, windowMenu, helpMenu });
    }

    private static void PersistAndQuit()
    {
        var swQuit = Stopwatch.StartNew();
        if (_app != null)
        {
            try
            {
                using var scope = _app.Services.CreateScope();
                var appState = scope.ServiceProvider.GetService<AppState>();
                if (appState != null)
                {
                    // Synchronous wait - we're shutting down anyway
                    appState.PersistSessionAsync().GetAwaiter().GetResult();
                    appState.PersistEditorPreferences();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ElectronHost] Failed to persist session on quit: {ex.Message}");
            }
        }
        swQuit.Stop();
        
        Electron.App.Quit();
    }

    /// <summary>
    /// Register IPC message handlers for file dialogs - Phase 2
    /// </summary>
    private static void RegisterIpcHandlers()
    {
        // Channel: openFileDialog.request -> openFileDialog.response
        // Implements contracts/ipc.openFileDialog.* schemas
        if (HybridSupport.IsElectronActive)
        {
            Electron.IpcMain.RemoveAllListeners("openFileDialog.request");
            Electron.IpcMain.On("openFileDialog.request", async _ =>
            {
                try
                {
                    if (_app is null)
                    {
                        
                        return;
                    }

                    using var scope = _app.Services.CreateScope();
                    var ipc = scope.ServiceProvider.GetRequiredService<IpcBridge>();
                    var selectedPath = await ipc.ShowOpenFileDialogAsync();

                    var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
                    if (window is null)
                    {
                        
                        return;
                    }

                    var response = new
                    {
                        canceled = string.IsNullOrWhiteSpace(selectedPath),
                        filePaths = string.IsNullOrWhiteSpace(selectedPath) ? Array.Empty<string>() : new[] { selectedPath! }
                    };

                    Electron.IpcMain.Send(window, "openFileDialog.response", response);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[IPC] openFileDialog.request failed: {ex.Message}");
                }
            });

            // Channel: saveFileDialog.request -> saveFileDialog.response
            // Implements contracts/ipc.saveFileDialog.* schemas
            Electron.IpcMain.RemoveAllListeners("saveFileDialog.request");
            Electron.IpcMain.On("saveFileDialog.request", async _ =>
            {
                try
                {
                    if (_app is null)
                    {
                        
                        return;
                    }

                    using var scope = _app.Services.CreateScope();
                    var ipc = scope.ServiceProvider.GetRequiredService<IpcBridge>();
                    var selectedPath = await ipc.ShowSaveFileDialogAsync();

                    var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
                    if (window is null)
                    {
                        
                        return;
                    }

                    var response = new
                    {
                        canceled = string.IsNullOrWhiteSpace(selectedPath),
                        filePath = selectedPath
                    };

                    Electron.IpcMain.Send(window, "saveFileDialog.response", response);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[IPC] saveFileDialog.request failed: {ex.Message}");
                }
            });

            // Placeholders for future Phase 10 tasks (T071c–T071d)
            Electron.IpcMain.RemoveAllListeners("persistUnsaved.request");
            Electron.IpcMain.On("persistUnsaved.request", _ =>
            {
                // For now, AppState handles autosave/session; this channel can be wired in T071c.
                Console.WriteLine("[IPC] persistUnsaved.request received (noop placeholder)");
            });

            Electron.IpcMain.RemoveAllListeners("restoreSession.request");
            Electron.IpcMain.On("restoreSession.request", _ =>
            {
                // In Phase 10 T071d, respond with records per contracts/ipc.restoreSession.response.schema.json
                Console.WriteLine("[IPC] restoreSession.request received (noop placeholder)");
            });

            // Channel: cli-file-args (Electron -> Blazor notification)
            // Implements contracts/cli-file-args.md
            // Note: This is sent from Electron side (ElectronHost), received by Blazor (via JSInterop)
            // Registration here is for documentation; actual reception happens in Blazor component
            

            // Channel: theme-changed (Electron -> Blazor notification)
            // Implements contracts/theme-changed.md
            // Will be sent by ThemeDetectionService when OS theme changes
            

            // Channel: shell:openExternal - Open URL in default browser
            Electron.IpcMain.RemoveAllListeners("shell:openExternal");
            Electron.IpcMain.On("shell:openExternal", async (args) =>
            {
                try
                {
                    if (args is string url && !string.IsNullOrWhiteSpace(url))
                    {
                        
                        
                        // Try using Process.Start as a workaround for Electron.NET shell issues
                        // This should work cross-platform
                        try
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = url,
                                UseShellExecute = true
                            };
                            Process.Start(psi);
                            
                        }
                        catch (Exception)
                        {
                            
                            // Fallback to Electron.NET API
                            var options = new OpenExternalOptions { Activate = true };
                            await Electron.Shell.OpenExternalAsync(url, options);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[IPC] Failed to open external URL: {ex.Message}");
                }
            });

            // Channel: theme:setThemeSource (Renderer -> Main)
            // Sets Electron nativeTheme.themeSource to align native menus with app theme
            Electron.IpcMain.RemoveAllListeners("theme:setThemeSource");
            Electron.IpcMain.On("theme:setThemeSource", async (args) =>
            {
                try
                {
                    if (args is string source && !string.IsNullOrWhiteSpace(source))
                    {
                        var s = source.Trim().ToLowerInvariant(); // "light" | "dark" | "system"
                        // Prefer explicit light/dark to avoid host inversion bugs; 'system' is accepted
                        var mode = s switch
                        {
                            "dark" => ThemeSourceMode.Dark,
                            "light" => ThemeSourceMode.Light,
                            _ => ThemeSourceMode.System
                        };
                        // Workaround: some Linux environments report inverted native theme; flip mapping for shell only on Linux
                        if (OperatingSystem.IsLinux())
                        {
                            mode = mode switch
                            {
                                ThemeSourceMode.Dark => ThemeSourceMode.Light,
                                ThemeSourceMode.Light => ThemeSourceMode.Dark,
                                _ => mode
                            };
                        }
                        Electron.NativeTheme.SetThemeSource(mode);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[IPC] Failed to set nativeTheme.themeSource: {ex.Message}");
                }
            });
        }
    }
}

/// <summary>
/// Message format for CLI arguments per contracts/cli-file-args.md
/// </summary>
internal record CommandLineArgs(
    string[] ValidFiles,
    InvalidFileInfo[] InvalidFiles,
    string LaunchType // "initial" or "second-instance"
);

/// <summary>
/// Info about a file that couldn't be opened.
/// </summary>
internal record InvalidFileInfo(string Path, string Reason);

internal static class CliArgProcessor
{
    internal static (List<string> valid, List<InvalidFileInfo> invalid) ParseAndValidate(IEnumerable<string> args)
    {
        var valid = new List<string>();
        var invalid = new List<InvalidFileInfo>();

        foreach (var raw in args)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            // Treat all arguments as file paths for simplicity per spec
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(raw);
            }
            catch
            {
                invalid.Add(new InvalidFileInfo(raw, "Invalid path"));
                continue;
            }

            try
            {
                if (!File.Exists(fullPath))
                {
                    invalid.Add(new InvalidFileInfo(fullPath, "File not found"));
                    continue;
                }

                // Check readability by attempting to open for read
                using var fs = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                valid.Add(fullPath);
            }
            catch (UnauthorizedAccessException)
            {
                invalid.Add(new InvalidFileInfo(fullPath, "Permission denied"));
            }
            catch
            {
                invalid.Add(new InvalidFileInfo(fullPath, "Unreadable"));
            }
        }

        return (valid, invalid);
    }
}

public static partial class ElectronHost
{
    private static Task ProcessInitialCliArgsAsync()
    {
        if (_app is null) return Task.CompletedTask;
        
        // Use args passed from Program.cs (T041)
        // Filter out common Electron internal args if needed
        var args = _initialArgs
            .Where(arg => !string.IsNullOrWhiteSpace(arg) && 
                         !arg.StartsWith("--inspect") && 
                         !arg.StartsWith("--remote-debugging"))
            .ToArray();
        
        var (valid, invalid) = CliArgProcessor.ParseAndValidate(args);

        try
        {
            using var scope = _app.Services.CreateScope();
            var ipcBridge = scope.ServiceProvider.GetRequiredService<IpcBridge>();
            
            // Send via IPC per contracts/cli-file-args.md
            ipcBridge.SendCliFileArgs(
                valid.ToArray(),
                invalid.Select(i => new IpcBridge.CliInvalidFileInfo(i.Path, i.Reason)).ToArray(),
                "initial"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CLI] Failed to send CLI args via IPC: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }
}

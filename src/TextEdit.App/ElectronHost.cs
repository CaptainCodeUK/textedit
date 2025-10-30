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
    public static void Initialize(WebApplication app)
    {
        _app = app;
        _appState = app.Services.GetRequiredService<AppState>();
        
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
            Console.WriteLine("[ElectronHost] Second instance launch detected, focusing window...");
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
                        var state = scope.ServiceProvider.GetRequiredService<AppState>();
                        if (valid.Count > 0)
                        {
                            await state.OpenFilesAsync(valid);
                        }
                        if (invalid.Count > 0)
                        {
                            state.SetCliInvalidFiles(invalid.Select(i => (i.Path, i.Reason)));
                        }
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
            Console.WriteLine("[ElectronHost] Second instance detected, exiting...");
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
            Title = "TextEdit",
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
        Console.WriteLine($"[Perf] Startup: main window created and menus configured in {swStartup.ElapsedMilliseconds} ms");

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

        Electron.Menu.SetApplicationMenu(new[] { fileMenu, editMenu, viewMenu, windowMenu });
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
        Console.WriteLine($"[Perf] Quit: session persisted in {swQuit.ElapsedMilliseconds} ms");
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
                        Console.WriteLine("[IPC] openFileDialog.request received before app initialized");
                        return;
                    }

                    using var scope = _app.Services.CreateScope();
                    var ipc = scope.ServiceProvider.GetRequiredService<IpcBridge>();
                    var selectedPath = await ipc.ShowOpenFileDialogAsync();

                    var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
                    if (window is null)
                    {
                        Console.WriteLine("[IPC] No BrowserWindow available to send openFileDialog.response");
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
                        Console.WriteLine("[IPC] saveFileDialog.request received before app initialized");
                        return;
                    }

                    using var scope = _app.Services.CreateScope();
                    var ipc = scope.ServiceProvider.GetRequiredService<IpcBridge>();
                    var selectedPath = await ipc.ShowSaveFileDialogAsync();

                    var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
                    if (window is null)
                    {
                        Console.WriteLine("[IPC] No BrowserWindow available to send saveFileDialog.response");
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
        }
    }
}

internal static class CliArgProcessor
{
    internal record InvalidFileInfo(string Path, string Reason);

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
    private static async Task ProcessInitialCliArgsAsync()
    {
        if (_app is null) return;
        // Environment.GetCommandLineArgs includes the executable path as first entry
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        if (args.Length == 0) return;

        var (valid, invalid) = CliArgProcessor.ParseAndValidate(args);

        try
        {
            using var scope = _app.Services.CreateScope();
            var state = scope.ServiceProvider.GetRequiredService<AppState>();
            if (valid.Count > 0)
            {
                await state.OpenFilesAsync(valid);
            }
            if (invalid.Count > 0)
            {
                // Wire invalid files to AppState for UI display
                state.SetCliInvalidFiles(invalid.Select(i => (i.Path, i.Reason)));
                Console.WriteLine($"[CLI] Invalid files: {string.Join(", ", invalid.Select(i => i.Path + " (" + i.Reason + ")"))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CLI] Failed to forward CLI files to AppState: {ex.Message}");
        }
    }
}

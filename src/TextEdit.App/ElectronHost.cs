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
public static class ElectronHost
{
    private static WebApplication? _app;
    private static AppState? _appState;

    /// <summary>
    /// Initialize Electron window and native features - Phase 1
    /// </summary>
    public static void Initialize(WebApplication app)
    {
        _app = app;
        _appState = app.Services.GetRequiredService<AppState>();
        
        // Subscribe to editor state changes to update menu checkmarks
        _appState.EditorState.Changed += OnEditorStateChanged;
        
        _ = CreateMainWindowAsync();
        RegisterIpcHandlers();
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

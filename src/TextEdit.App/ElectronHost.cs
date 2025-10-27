using ElectronNET.API;
using ElectronNET.API.Entities;
using TextEdit.UI.App;
using TextEdit.UI.Components.Editor;
using Microsoft.Extensions.DependencyInjection;

namespace TextEdit.App;

/// <summary>
/// Manages Electron window lifecycle and native integration
/// Phase 1: Basic window setup
/// Phase 2: File dialogs and IPC
/// </summary>
public static class ElectronHost
{
    private static WebApplication? _app;

    /// <summary>
    /// Initialize Electron window and native features - Phase 1
    /// </summary>
    public static void Initialize(WebApplication app)
    {
        _app = app;
        _ = CreateMainWindowAsync();
        RegisterIpcHandlers();
    }

    /// <summary>
    /// Create the main BrowserWindow and configure lifecycle hooks.
    /// </summary>
    private static async Task CreateMainWindowAsync()
    {
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ElectronHost] Failed to persist session on quit: {ex.Message}");
            }
        }
        Electron.App.Quit();
    }

    /// <summary>
    /// Register IPC message handlers for file dialogs - Phase 2
    /// </summary>
    private static void RegisterIpcHandlers()
    {
        // TODO: Phase 2 - T022
        // openFileDialog
        // saveFileDialog
        // persistUnsaved
        // restoreSession
    }
}

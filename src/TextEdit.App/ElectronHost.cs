using ElectronNET.API;
using ElectronNET.API.Entities;
using TextEdit.UI.Components.Editor;

namespace TextEdit.App;

/// <summary>
/// Manages Electron window lifecycle and native integration
/// </summary>
public static class ElectronHost
{
    /// <summary>
    /// Wire up Electron window creation to occur only after the ASP.NET Core host has fully started.
    /// This avoids Electron trying to load the URL before Kestrel is listening (ERR_CONNECTION_REFUSED).
    /// </summary>
    public static void Initialize(WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(CreateMainWindowAsync);
        });
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
        window.OnClosed += () =>
        {
            // Session persistence will be triggered here in Phase 5 (US4)
            Electron.App.Quit();
        };

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
        string quitAccelerator = "CmdOrCtrl+Q";
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            quitAccelerator = "Alt+F4";
        }

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
                new MenuItem { Label = "Quit", Accelerator = quitAccelerator, Click = () => Electron.App.Quit() }
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
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Label = "Next Tab (PageDown)", Accelerator = "Ctrl+PageDown", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.NextTabRequested); } },
                new MenuItem { Label = "Previous Tab (PageUp)", Accelerator = "Ctrl+PageUp", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.PrevTabRequested); } },
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

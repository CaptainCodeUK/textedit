using ElectronNET.API;
using ElectronNET.API.Entities;

namespace TextEdit.App;

/// <summary>
/// Manages Electron window lifecycle and native integration
/// </summary>
public static class ElectronHost
{
    /// <summary>
    /// Initialize Electron window and configure menus
    /// </summary>
    public static async Task InitializeAsync(WebApplication app)
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
        // ConfigureMenus();

        // IPC handlers will be registered here in Phase 2
        // RegisterIpcHandlers();
    }

    /// <summary>
    /// Configure application menus (File/Edit/View) - Phase 6
    /// </summary>
    private static void ConfigureMenus()
    {
        // TODO: Phase 6 - T043
        // File menu: New, Open, Save, Save As, Exit
        // Edit menu: Undo, Redo, Cut, Copy, Paste
        // View menu: Word Wrap toggle
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

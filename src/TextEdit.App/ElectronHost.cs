using ElectronNET.API;
using ElectronNET.API.Entities;
using TextEdit.UI.App;
using TextEdit.UI.Components.Editor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TextEdit.Infrastructure.Ipc;
using TextEdit.Core.Preferences;

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
    private static ILogger? _logger;
    private static string[] _initialArgs = Array.Empty<string>();
    private static BrowserWindow? _mainWindow;

    /// <summary>
    /// Initialize Electron window and native features - Phase 1
    /// </summary>
    /// <param name="app">The web application instance</param>
    /// <param name="args">Command-line arguments from Program.cs (T041)</param>
    public static void Initialize(WebApplication app, string[] args)
    {
        _app = app;
        _appState = app.Services.GetRequiredService<AppState>();
        _logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ElectronHost");
        _initialArgs = args ?? Array.Empty<string>();
        
        // Setup global error handlers for Electron/JavaScript errors
        SetupGlobalErrorHandlers();
        
        // Subscribe to editor state changes to update menu checkmarks
        _appState.EditorState.Changed += OnEditorStateChanged;
        
        // Subscribe to app state changes to update menu checkmarks (for preferences like toolbar visibility)
        _appState.Changed += OnAppStateChanged;
        
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
                    _logger?.LogInformation("Second instance detected, focusing existing window");
                    
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
                        _logger?.LogInformation("Processing {Count} CLI arguments from second instance", cliArgs.Length);
                        
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
                    // Log but don't crash on second-instance processing errors
                    _logger?.LogError(ex, "Error processing second instance");
                }
            });
        });
        
        if (!gotLock)
        {
            // This is a second instance - it will exit automatically
            _logger?.LogInformation("Second instance detected, exiting");
            Electron.App.Exit();
            return;
        }
    }

    private static void OnEditorStateChanged()
    {
        ConfigureMenus();
    }

        private static void OnAppStateChanged()
        {
            ConfigureMenus();
        }

    /// <summary>
    /// Setup global error handlers to catch and log unhandled JavaScript and Electron errors
    /// </summary>
    private static void SetupGlobalErrorHandlers()
    {
        if (!HybridSupport.IsElectronActive) return;

        try
        {
            // Handle uncaught JavaScript errors from renderer process
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                _logger?.LogCritical(exception, "Unhandled exception in AppDomain: {IsTerminating}", e.IsTerminating);
            };

            // Handle unobserved task exceptions
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                _logger?.LogError(e.Exception, "Unobserved task exception");
                e.SetObserved(); // Prevent process termination
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to setup global error handlers");
        }
    }

    /// <summary>
    /// Create the main BrowserWindow and configure lifecycle hooks.
    /// </summary>
    private static async Task CreateMainWindowAsync()
    {
        try
        {
            var swStartup = Stopwatch.StartNew();
            _logger?.LogInformation("Creating main Electron window");
            // Load previous window state (non-blocking fallback to defaults)
            WindowState priorState;
            try
            {
                using var scope = _app!.Services.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<TextEdit.Infrastructure.Persistence.WindowStateRepository>();
                priorState = await repo.LoadAsync();
            }
            catch { priorState = new WindowState(); }
            priorState.ClampToMinimums();

            var opts = new BrowserWindowOptions
            {
                Width = priorState.Width,
                Height = priorState.Height,
                MinWidth = 800,
                MinHeight = 600,
                Title = "Scrappy Text Editor",
                WebPreferences = new WebPreferences
                {
                    NodeIntegration = false,
                    ContextIsolation = true
                }
            };
            if (priorState.X > 0 || priorState.Y > 0)
            {
                opts.X = priorState.X;
                opts.Y = priorState.Y;
            }
            var window = await Electron.WindowManager.CreateWindowAsync(opts);

            // Apply maximized/fullscreen state after creation
            try
            {
                if (priorState.IsMaximized)
                {
                    window.Maximize();
                }
                else if (priorState.IsFullScreen)
                {
                    window.SetFullScreen(true);
                }
            }
            catch { /* ignore */ }

            // Store window reference for shutdown
            _mainWindow = window;

            // Track current window state for persistence (captured before close to avoid "destroyed" errors)
            WindowState? currentWindowState = null;
            
            // Helper to capture state safely
            async Task CaptureWindowStateAsync()
            {
                try
                {
                    var bounds = await window.GetBoundsAsync();
                    var isMax = await window.IsMaximizedAsync();
                    var isFs = await window.IsFullScreenAsync();
                    currentWindowState = new WindowState
                    {
                        X = bounds.X,
                        Y = bounds.Y,
                        Width = bounds.Width,
                        Height = bounds.Height,
                        IsMaximized = isMax,
                        IsFullScreen = isFs
                    };
                }
                catch { /* window may be closing */ }
            }

            // Capture window state on move/resize/maximize events
            window.OnMoved += () => { _ = CaptureWindowStateAsync(); };
            window.OnResize += () => { _ = CaptureWindowStateAsync(); };
            window.OnMaximize += () => { _ = CaptureWindowStateAsync(); };
            window.OnUnmaximize += () => { _ = CaptureWindowStateAsync(); };
            window.OnEnterFullScreen += () => { _ = CaptureWindowStateAsync(); };
            window.OnLeaveFullScreen += () => { _ = CaptureWindowStateAsync(); };

            // Track if we're in a shutdown sequence to prevent recursive closes
            var isShuttingDown = false;

            // Handle window close (Alt+F4, X button) - trigger clean async shutdown
            window.OnClose += () =>
            {
                if (isShuttingDown) return;
                isShuttingDown = true;
                
                _logger?.LogInformation("Window close requested - starting async shutdown");
                
                // Don't return from OnClose—let window think it can close
                // But we'll exit the process before that happens
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Signal shutdown to suppress dialogs and background work
                        TextEdit.Infrastructure.Lifecycle.AppShutdown.Begin();
                        
                        // Always persist session
                        PersistSession();

                        // Persist window state (use cached state to avoid accessing destroyed window)
                        if (currentWindowState != null)
                        {
                            try
                            {
                                using var scope = _app!.Services.CreateScope();
                                var repo = scope.ServiceProvider.GetRequiredService<TextEdit.Infrastructure.Persistence.WindowStateRepository>();
                                await repo.SaveAsync(currentWindowState);
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogWarning(ex, "Failed to persist window state");
                            }
                        }
                        
                        // Check how long window has been open
                        var startupTime = swStartup.Elapsed;
                        
                        if (startupTime.TotalSeconds < 2)
                        {
                            // Very early close - SignalR not fully connected yet
                            // Exit immediately after persistence to avoid socket errors
                            _logger?.LogInformation("Early close detected (<2s), exiting immediately after persistence");
                            Electron.App.Exit(0);
                            return;
                        }
                        
                        // Normal close - give SignalR brief time to disconnect gracefully
                        _logger?.LogInformation("Normal close, waiting for SignalR disconnect");
                        await Task.Delay(150);
                        
                        // Exit cleanly
                        Electron.App.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error during shutdown sequence");
                        Electron.App.Exit(1);
                    }
                });
            };

            // Application menu will be configured in Phase 6 (US3)
            ConfigureMenus();

            swStartup.Stop();
            _logger?.LogInformation("Main window created in {ElapsedMs}ms", swStartup.ElapsedMilliseconds);

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
                    _logger?.LogError(ex, "Failed to process initial CLI arguments");
                }
            });

            // Initialize auto-updater (US6)
            _ = Task.Run(async () =>
            {
                try
                {
                    await InitializeAutoUpdaterAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to initialize auto-updater");
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "Failed to create main Electron window");
            throw;
        }
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
                // Find commands (US1)
                new MenuItem { Label = "Find…", Accelerator = "CmdOrCtrl+F", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.FindRequested); } },
                new MenuItem { Label = "Find Next", Accelerator = "F3", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.FindNextRequested); } },
                new MenuItem { Label = "Find Previous", Accelerator = "Shift+F3", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.FindPreviousRequested); } },
                // Replace commands (US2)
                new MenuItem { Label = "Replace…", Accelerator = "CmdOrCtrl+H", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.ReplaceRequested); } },
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Label = OperatingSystem.IsMacOS() ? "Preferences…" : "Options…", Accelerator = OperatingSystem.IsMacOS() ? "Cmd+," : "Ctrl+,", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.OptionsRequested); } },
            }
        };

        var formatMenu = new MenuItem
        {
            Label = "Format",
            Submenu = new MenuItem[]
            {
                new MenuItem { Label = "Heading 1", Accelerator = "CmdOrCtrl+1", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.FormatHeading1Requested); } },
                new MenuItem { Label = "Heading 2", Accelerator = "CmdOrCtrl+2", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.FormatHeading2Requested); } },
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Label = "Bold", Accelerator = "CmdOrCtrl+B", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.FormatBoldRequested); } },
                new MenuItem { Label = "Italic", Accelerator = "CmdOrCtrl+I", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.FormatItalicRequested); } },
                new MenuItem { Label = "Inline Code", Accelerator = "CmdOrCtrl+`", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.FormatCodeRequested); } },
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Label = "Bullet List", Accelerator = "CmdOrCtrl+Shift+8", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.FormatBulletListRequested); } },
                new MenuItem { Label = "Numbered List", Accelerator = "CmdOrCtrl+Shift+7", Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.FormatNumberedListRequested); } },
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
                new MenuItem { Label = "Toggle Toolbar", Accelerator = "Alt+T", Type = MenuType.checkbox, Checked = _appState?.Preferences.ToolbarVisible ?? true, Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.ToggleToolbarRequested); } },
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

        Electron.Menu.SetApplicationMenu(new[] { fileMenu, editMenu, formatMenu, viewMenu, windowMenu, helpMenu });
    }

    /// <summary>
    /// Persist session - called by BeforeQuit event for all quit methods
    /// </summary>
    private static void PersistSession()
    {
        var swQuit = Stopwatch.StartNew();
        if (_app != null)
        {
            try
            {
                _logger?.LogInformation("Persisting session on application quit");
                
                using var scope = _app.Services.CreateScope();
                var appState = scope.ServiceProvider.GetService<AppState>();
                if (appState != null)
                {
                    try
                    {
                        appState.PersistSessionAsync().GetAwaiter().GetResult();
                        appState.PersistEditorPreferences();
                        // Stop background activities to avoid late events during shutdown
                        _logger?.LogInformation("Disposing AppState to stop autosave and file watchers");
                        appState.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error during session persistence");
                    }
                }
                
                // Remove IPC listeners to prevent sends to a closing window
                try
                {
                    Electron.IpcMain.RemoveAllListeners("openFileDialog.request");
                    Electron.IpcMain.RemoveAllListeners("saveFileDialog.request");
                    Electron.IpcMain.RemoveAllListeners("persistUnsaved.request");
                    Electron.IpcMain.RemoveAllListeners("restoreSession.request");
                    Electron.IpcMain.RemoveAllListeners("shell:openExternal");
                    Electron.IpcMain.RemoveAllListeners("theme:setThemeSource");
                    _logger?.LogInformation("IPC listeners removed during shutdown");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to remove one or more IPC listeners during shutdown");
                }
                
                swQuit.Stop();
                _logger?.LogInformation("Session persisted in {ElapsedMs}ms", swQuit.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to persist session");
            }
        }
    }

    /// <summary>
    /// Explicitly quit application - used by menu items only.
    /// Session persistence handled by BeforeQuit event.
    /// </summary>
    private static void PersistAndQuit()
    {
        try
        {
            _logger?.LogInformation("Quitting application via menu/keyboard command");
            Electron.App.Quit();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error calling Electron.App.Quit()");
        }
    }

    /// <summary>
    /// Register IPC message handlers for file dialogs - Phase 2
    /// </summary>
    private static void RegisterIpcHandlers()
    {
        _logger?.LogInformation("Registering IPC handlers");
        
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
                        _logger?.LogWarning("openFileDialog.request received but app is null");
                        return;
                    }

                    using var scope = _app.Services.CreateScope();
                    var ipc = scope.ServiceProvider.GetRequiredService<IpcBridge>();
                    var selectedPath = await ipc.ShowOpenFileDialogAsync();

                    var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
                    if (window is null)
                    {
                        _logger?.LogWarning("openFileDialog.request: No browser window found");
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
                    _logger?.LogError(ex, "Error handling openFileDialog.request");
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
                        _logger?.LogWarning("saveFileDialog.request received but app is null");
                        return;
                    }

                    using var scope = _app.Services.CreateScope();
                    var ipc = scope.ServiceProvider.GetRequiredService<IpcBridge>();
                    var selectedPath = await ipc.ShowSaveFileDialogAsync();

                    var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
                    if (window is null)
                    {
                        _logger?.LogWarning("saveFileDialog.request: No browser window found");
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
                    _logger?.LogError(ex, "Error handling saveFileDialog.request");
                }
            });

            // Placeholders for future Phase 10 tasks (T071c–T071d)
            Electron.IpcMain.RemoveAllListeners("persistUnsaved.request");
            Electron.IpcMain.On("persistUnsaved.request", _ =>
            {
                // For now, AppState handles autosave/session; this channel can be wired in T071c.
            });

            Electron.IpcMain.RemoveAllListeners("restoreSession.request");
            Electron.IpcMain.On("restoreSession.request", _ =>
            {
                // In Phase 10 T071d, respond with records per contracts/ipc.restoreSession.response.schema.json
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
                        _logger?.LogInformation("Opening external URL: {Url}", url);
                        
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
                            _logger?.LogDebug("Opened URL using Process.Start");
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Process.Start failed, trying Electron.Shell.OpenExternalAsync");
                            // Fallback to Electron.NET API
                            var options = new OpenExternalOptions { Activate = true };
                            await Electron.Shell.OpenExternalAsync(url, options);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to open external URL");
                }
            });

            // Channel: theme:setThemeSource (Renderer -> Main)
            // Sets Electron nativeTheme.themeSource to align native menus with app theme
            Electron.IpcMain.RemoveAllListeners("theme:setThemeSource");
            Electron.IpcMain.On("theme:setThemeSource", (args) =>
            {
                try
                {
                    if (args is string source && !string.IsNullOrWhiteSpace(source))
                    {
                        _logger?.LogInformation("Setting theme source to: {Source}", source);
                        
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
                            _logger?.LogDebug("Linux platform detected, flipped theme mode to: {Mode}", mode);
                        }
                        Electron.NativeTheme.SetThemeSource(mode);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to set theme source");
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
    /// <summary>
    /// Initialize auto-updater with GitHub Releases feed and start periodic checks (US6)
    /// </summary>
    private static async Task InitializeAutoUpdaterAsync()
    {
        if (_app is null || _appState is null) return;
        
        try
        {
            _logger?.LogInformation("Initializing auto-updater");
            
            using var scope = _app.Services.CreateScope();
            var autoUpdateService = scope.ServiceProvider.GetRequiredService<TextEdit.Infrastructure.Updates.AutoUpdateService>();
            
            // Initialize with GitHub Releases feed URL
            // TODO: Update owner/repo when repository is created
            autoUpdateService.Initialize("https://github.com/CaptainCodeUK/textedit/releases");
            
            // Check for updates on startup if enabled
            if (_appState.Preferences.Updates.CheckOnStartup)
            {
                _logger?.LogInformation("Checking for updates on startup");
                await autoUpdateService.CheckForUpdatesAsync(_appState.Preferences.Updates.AutoDownload);
                
                // Update last check time
                _appState.Preferences.Updates.LastCheckTime = DateTimeOffset.UtcNow;
                await _appState.SavePreferencesAsync();
            }
            
            // Start periodic check timer (every 15 minutes, check if interval elapsed)
            _ = Task.Run(async () =>
            {
                while (!TextEdit.Infrastructure.Lifecycle.AppShutdown.IsShuttingDown)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(15));
                        
                        if (TextEdit.Infrastructure.Lifecycle.AppShutdown.IsShuttingDown) break;
                        
                        var elapsed = DateTimeOffset.UtcNow - _appState.Preferences.Updates.LastCheckTime;
                        var interval = TimeSpan.FromHours(_appState.Preferences.Updates.CheckIntervalHours);
                        
                        if (elapsed >= interval)
                        {
                            _logger?.LogInformation("Periodic update check triggered (interval: {Hours}h)", 
                                _appState.Preferences.Updates.CheckIntervalHours);
                            
                            await autoUpdateService.CheckForUpdatesAsync(_appState.Preferences.Updates.AutoDownload);
                            
                            _appState.Preferences.Updates.LastCheckTime = DateTimeOffset.UtcNow;
                            await _appState.SavePreferencesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error in periodic update check");
                    }
                }
            });
            
            _logger?.LogInformation("Auto-updater initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize auto-updater");
        }
    }

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
            _logger?.LogInformation("Processing {ValidCount} valid and {InvalidCount} invalid CLI arguments", 
                valid.Count, invalid.Count);
                
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
            _logger?.LogError(ex, "Failed to send CLI args via IPC");
        }
        
        return Task.CompletedTask;
    }
}

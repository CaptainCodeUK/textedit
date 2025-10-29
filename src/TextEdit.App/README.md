# TextEdit.App

The main application entry point that hosts the Blazor Server UI within an Electron.NET desktop shell.

## Responsibilities

- **Electron Host** - Creates native desktop window and manages application lifecycle
- **Native Menus** - File, Edit, and View menus with keyboard accelerators
- **IPC Handlers** - Processes native dialog requests from the Blazor UI
- **Startup/Shutdown** - Initializes services and handles graceful application close
- **Performance Monitoring** - Tracks startup and shutdown times

## Key Components

### `Program.cs`
ASP.NET Core application entry point that:
- Configures the web host
- Sets up dependency injection
- Launches Electron.NET
- Registers all services (Core, Infrastructure, UI)

### `Startup.cs`
Service registration and middleware configuration:
- Registers `DocumentService`, `TabService`, `UndoRedoService`
- Registers `FileSystemService`, `PersistenceService`, `AutosaveService`, `IpcBridge`
- Registers `MarkdownRenderer`, `AppState`, `DialogService`, `PerformanceLogger`
- Configures Blazor Server with SignalR
- Sets up static file serving

### `ElectronHost.cs`
Electron window and menu management:
- Creates main BrowserWindow (1200x800, dark theme)
- Configures File menu (New, Open, Save, Save As, Close Tab, Quit)
- Configures Edit menu (Undo, Redo, Cut, Copy, Paste, Select All)
- Configures Tabs menu (Next, Previous, Close Others, Close Right)
- Configures View menu (Word Wrap, Markdown Preview)
- Implements IPC handlers for `openFileDialog` and `saveFileDialog`
- Handles quit event with session persistence

## Native Integration

### Menu Commands
All menu items route through `EditorCommandHub` to invoke Blazor component methods:

```csharp
new MenuItem { 
    Label = "Save", 
    Accelerator = "CmdOrCtrl+S", 
    Click = () => EditorCommandHub.InvokeSafe(EditorCommandHub.SaveRequested) 
}
```

### IPC Contracts
Implements native dialog contracts (see `specs/001-text-editor/contracts/`):

- **openFileDialog** - Shows native file open picker
  - Request: `{ filters?: { name, extensions }[] }`
  - Response: `{ filePath: string | null }`

- **saveFileDialog** - Shows native file save picker
  - Request: `{ defaultPath?: string, filters?: { name, extensions }[] }`
  - Response: `{ filePath: string | null }`

## Performance

Startup and shutdown times are tracked with `PerformanceLogger`:

```
[Perf] Startup: main window created and menus configured in <X> ms
[Perf] Quit: session persisted in <X> ms
```

Target: <2s for both operations.

## Configuration

### `appsettings.json`
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### `electron.manifest.json`
Electron.NET configuration:
- BrowserWindow options (width, height, theme)
- Build targets (win, osx, linux)
- Application metadata

## Dependencies

- **TextEdit.UI** - Blazor components and AppState
- **TextEdit.Core** - Domain models and services
- **TextEdit.Infrastructure** - File I/O and persistence
- **TextEdit.Markdown** - Markdown rendering
- **ElectronNET.API** - Electron integration
- **ASP.NET Core 8** - Web host

## Running

```bash
# Development mode (from solution root)
./scripts/dev.fish run

# Or with VS Code
# Press F5 to start debugging

# Or manually
cd src/TextEdit.App
electronize start
```

## Building

```bash
# Build for your platform
cd src/TextEdit.App
electronize build /target win      # Windows
electronize build /target osx      # macOS  
electronize build /target linux    # Linux

# Output: bin/Desktop/
```

## Notes

- The app uses Blazor Server (not WebAssembly) for full .NET runtime access
- SignalR provides real-time UI updates between Electron and Blazor
- Native menus are platform-specific (CmdOrCtrl handles Cmd on macOS, Ctrl elsewhere)
- IPC uses Electron's `ipcMain`/`ipcRenderer` pattern

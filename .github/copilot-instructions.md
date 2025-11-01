# TextEdit AI Development Guide

A cross-platform desktop text editor built with .NET 8, Blazor Server hosted in Electron.NET. Features multi-document tabs, markdown preview with aggressive caching, session persistence, and crash-recovery autosave.

## Architecture Overview

**Clean Architecture with Blazor-in-Electron**: Core domain logic has zero external dependencies. Infrastructure implements abstractions. UI orchestrates through `AppState`. Electron menus route through `EditorCommandHub` static delegates.

```
┌─────────────────────────────────────────────────────────────┐
│  TextEdit.App (Electron.NET Host)                           │
│  - ElectronHost: native menus → EditorCommandHub           │
│  - IPC handlers: file dialogs, session persistence         │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│  TextEdit.UI (Blazor Server Components)                     │
│  - AppState: orchestrates all business operations           │
│  - Components: TextEditor, TabStrip, StatusBar, Dialogs     │
│  - EditorCommandHub: static delegates for menu commands     │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────┬────────────────────┬─────────────────────┐
│ TextEdit.Core    │ TextEdit.Markdown  │ TextEdit.Infrastructure │
│ (Pure Domain)    │ (Caching)          │ (File I/O, IPC)      │
└──────────────────┴────────────────────┴─────────────────────┘
```

**Key Pattern**: `AppState` in UI layer coordinates Core services (DocumentService, TabService, UndoRedoService) and Infrastructure services (PersistenceService, FileSystemService, IpcBridge). Components subscribe to `AppState.Changed` event and use `StateVersion` to optimize re-renders.

## Project Structure

- **`src/TextEdit.Core/`** - Pure C# domain models (`Document`, `Tab`, `EditorState`), business logic (`DocumentService`, `TabService`, `UndoRedoService`), and abstractions (`IFileSystem`). **Zero external dependencies**.
- **`src/TextEdit.Infrastructure/`** - File I/O (`FileSystemService`), external change detection (`FileWatcher`), session persistence (`PersistenceService`), crash-recovery autosave (`AutosaveService`), native dialogs (`IpcBridge`).
- **`src/TextEdit.Markdown/`** - Markdown rendering with SHA256-based result caching (`MarkdownRenderer`). 165-330x speedup on cache hits. Uses Markdig for GitHub Flavored Markdown.
- **`src/TextEdit.UI/`** - Blazor components, `AppState` orchestrator, `DialogService`, `EditorCommandHub`. Components use `@key` directives and `ShouldRender` optimizations.
- **`src/TextEdit.App/`** - ASP.NET Core host, Electron window/menu setup, IPC handlers. Entry point: `Program.cs` → `Startup.cs` (DI) → `ElectronHost.cs` (native shell).
- **`tests/unit/`** - xUnit tests with NSubstitute/FluentAssertions. **Per-project coverage thresholds**: Core.Tests enforces 45% (covers Core 78% + Infrastructure 35.7%). See individual `.csproj` files for threshold configuration.
- **`tests/integration/`** - Accessibility tests, end-to-end scenarios.
- **`tests/contract/`** - IPC message validation against JSON schemas in `specs/001-text-editor/contracts/`.
- **`tests/benchmarks/`** - BenchmarkDotNet for DocumentService, MarkdownRenderer. See `BenchmarkDotNet.Artifacts/results/`.

## Essential Development Workflows

### Running & Debugging
```bash
# Development mode (Fish or Bash)
./scripts/dev.fish run          # Kills lingering processes, runs Electron
./scripts/dev.sh run            # Bash equivalent

# Manual cleanup if needed
./scripts/kill-textedit.fish
```

**VS Code Debug**: Use "Launch Electron (Auto PID)" config. Run `./scripts/find-dotnet-pid.fish` to get PID when prompted.

### Testing
```bash
./scripts/dev.fish test          # All tests
./scripts/dev.fish test:unit     # Unit tests only
./scripts/dev.fish test:coverage # With coverage report and per-project threshold enforcement
```

**Coverage Enforcement**: Per-project thresholds defined in individual test `.csproj` files (e.g., Core.Tests: 45% line coverage). No global threshold in `Directory.Build.props`. Prevents coverage regression on a per-project basis.

### Building for Distribution
```bash
cd src/TextEdit.App
electronize build /target osx    # macOS
electronize build /target win    # Windows
electronize build /target linux  # Linux
```
Output: `src/TextEdit.App/bin/Desktop/`

## Critical Patterns & Conventions

### 1. Electron Menu → Blazor Command Flow
Native menus in `ElectronHost.cs` invoke static delegates on `EditorCommandHub`. Blazor `TextEditor` component assigns these delegates in `OnInitialized`:
```csharp
// ElectronHost.cs
new MenuItem { 
    Label = "Save", 
    Accelerator = "CmdOrCtrl+S", 
    Click = () => { _ = EditorCommandHub.InvokeSafe(EditorCommandHub.SaveRequested); } 
}

// TextEditor.razor.cs (UI component)
protected override void OnInitialized()
{
    EditorCommandHub.SaveRequested = async () => await State.SaveActiveAsync();
    // ... other commands
}
```
**Why**: Electron menus run in native context; cannot directly invoke Blazor component methods. Static delegates bridge the gap.

### 2. Session Persistence (No "Save on Close" Dialogs)
When app closes, `ElectronHost` persists **all unsaved work** without prompting:
- **New unsaved documents** → saved to `%AppData%/TextEdit/Session/{documentId}.json`
- **Existing files with changes** → dirty content saved to session file, original path preserved
- On next launch, `AppState.RestoreSessionAsync()` restores tabs and marks documents dirty

**Location**: 
- Windows: `%AppData%\TextEdit\Session`
- macOS: `~/Library/Application Support/TextEdit/Session`
- Linux: `~/.config/TextEdit/Session`

**Cleanup**: Session files deleted after save/discard via `PersistenceService.DeleteSessionFile(Guid)`.

### 3. Markdown Rendering with Aggressive Caching
`MarkdownRenderer` uses SHA256 content hashing for cache keys. **165-330x speedup** on repeated renders. LRU eviction at 100 entries (configurable).
```csharp
// MarkdownRenderer.cs
public string RenderToHtml(string markdownText)
{
    string hash = ComputeHash(markdownText); // SHA256
    if (_cache.TryGetValue(hash, out var cached))
        return cached.Html; // Cache hit
    // ... render with Markdig, cache result
}
```
**Manual refresh** for documents >100KB to avoid performance issues.

### 4. Large File Handling
- **<10MB**: Normal editing with streaming I/O (`FileSystemService.ReadLargeFileAsync`)
- **>10MB**: Read-only mode, no editing allowed
- **Progress reporting**: `IProgress<int>` for long operations (see `DocumentService.OpenAsync`)

### 5. External File Modification Detection
`FileWatcher` monitors open files. On external change, `AppState` shows dialog: **Reload** (discard local), **Keep Mine** (ignore), or **Save As** (resolve conflict).
```csharp
// AppState.cs
private void OnFileChangedExternally(string path)
{
    // ... find document, show dialog, handle decision
}
```
**Debouncing**: 1-second window to avoid multiple events.

### 6. Dependency Injection in `Startup.cs`
All services registered as **singletons** (stateful desktop app, single window):
```csharp
services.AddSingleton<DocumentService>();
services.AddSingleton<TabService>();
services.AddSingleton<IFileSystem, FileSystemService>();
services.AddSingleton<PersistenceService>();
services.AddSingleton<AutosaveService>(sp => new AutosaveService(intervalMs: 5000));
services.AddSingleton<MarkdownRenderer>();
services.AddSingleton<AppState>();
// ... IpcBridge, DialogService, etc.
```

### 7. Component Re-render Optimization
Use `StateVersion` counter to avoid unnecessary re-renders:
```csharp
// TextEditor.razor.cs
protected override bool ShouldRender()
{
    if (_lastRenderedVersion == State.StateVersion)
        return false;
    _lastRenderedVersion = State.StateVersion;
    return true;
}
```
`AppState` increments `StateVersion` on every change and fires `Changed` event.

### 8. IPC Contracts (Electron ↔ Blazor)
JSON schemas in `specs/001-text-editor/contracts/` define request/response shapes. Contract tests in `tests/contract/TextEdit.IPC.Tests/` validate messages.
```csharp
// IpcBridge.cs
public virtual async Task<string?> ShowOpenFileDialogAsync()
{
    var options = new OpenDialogOptions { /* ... */ };
    var result = await Electron.Dialog.ShowOpenDialogAsync(window, options);
    return result?[0];
}
```

## Common Tasks

### Adding a New Menu Command
1. Add delegate to `EditorCommandHub.cs`: `public static Func<Task>? MyCommand { get; set; }`
2. Register in `ElectronHost.cs`: `new MenuItem { Label = "...", Click = () => EditorCommandHub.InvokeSafe(EditorCommandHub.MyCommand) }`
3. Assign in component: `EditorCommandHub.MyCommand = async () => await MyMethod();`

### Adding a New Core Service
1. Define in `TextEdit.Core/` with zero external dependencies
2. Use abstractions (e.g., `IFileSystem`) for external concerns
3. Register as singleton in `Startup.ConfigureServices`
4. Inject into `AppState` or components as needed
5. Write unit tests with mocks (xUnit + Moq)

### Modifying Persistence Format
1. Update `PersistenceService.cs` read/write methods
2. Update session file structure in `specs/001-text-editor/data-model.md`
3. Add migration logic if needed (version field in JSON)
4. Test restore with old session files

## Code Style & Standards
- **C# 12**, **.NET 8**, nullable reference types enabled
- **One class per file**, XML comments for public APIs
- **YAGNI, KISS**: Avoid over-engineering, prefer simplicity
- **SRP**: Keep classes focused (e.g., `DocumentService` only orchestrates documents)
- **Test coverage**: 65% minimum (Core: 92%+, Infrastructure: 52%+)
- **Naming**: Standard .NET conventions (PascalCase public, camelCase private)

## Performance Targets
- **Startup**: <2s (main window + menus + session restore)
- **Shutdown**: <2s (session persistence)
- **Markdown render**: <5ms (cold), <50μs (cached)
- **File open**: Streaming I/O for >10MB, progress reporting

See `BenchmarkDotNet.Artifacts/results/` for current benchmarks.

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->

## Active Technologies
- C# 12 / .NET 8.0 + Electron.NET 23.6.2, ASP.NET Core (Blazor Server), Markdig (markdown rendering) (002-v1-1-enhancements)
- File system (JSON for preferences in OS app data directories, existing session persistence) (002-v1-1-enhancements)

## Recent Changes
- 002-v1-1-enhancements: Added C# 12 / .NET 8.0 + Electron.NET 23.6.2, ASP.NET Core (Blazor Server), Markdig (markdown rendering)

# TextEdit.Infrastructure

Infrastructure services for file system operations, session persistence, autosave, IPC, and telemetry.

## Responsibilities

- **File System** - Implements `IFileSystem` abstraction from Core
- **File Watching** - Detects external file modifications
- **Session Persistence** - Saves/restores unsaved work on app close/open
- **Autosave** - Periodic crash recovery snapshots
- **IPC Bridge** - Native dialog integration with Electron
- **Telemetry** - Performance logging and metrics

## Key Components

### FileSystem

#### `FileSystemService.cs`
Implements `IFileSystem` interface:

```csharp
public class FileSystemService : IFileSystem
{
    public bool FileExists(string path)
    public long GetFileSize(string path)
    public Task<string> ReadAllTextAsync(string path, Encoding encoding)
    public Task WriteAllTextAsync(string path, string contents, Encoding encoding)
    public Task<string> ReadLargeFileAsync(string path, Encoding encoding, 
        IProgress<int>? progress, CancellationToken ct)
    public Task WriteLargeFileAsync(string path, string contents, Encoding encoding, 
        IProgress<int>? progress, CancellationToken ct)
}
```

**Features:**
- Standard file operations using `System.IO.File`
- Large file support with chunked reads/writes (10MB threshold)
- Progress reporting for long operations
- Proper encoding support (UTF-8 by default)

#### `FileWatcher.cs`
Detects external file modifications:

```csharp
public class FileWatcher : IDisposable
{
    public event Action<string>? ChangedExternally;
    public void Watch(string filePath)
    public void Stop()
    public void Dispose()
}
```

**Implementation:**
- Uses `FileSystemWatcher` for change detection
- Debounces multiple events (1-second window)
- Fires `ChangedExternally` event with file path
- Thread-safe event invocation

### Persistence

#### `PersistenceService.cs`
Session and preference persistence:

```csharp
public class PersistenceService
{
    public void PersistSession(IEnumerable<Document> documents, IEnumerable<Tab> tabs, 
        Guid? activeTabId)
    public (List<Document> documents, List<Tab> tabs, Guid? activeTabId) RestoreSession()
    public void DeleteSessionFile(Guid documentId)
    public void ClearAllSessions()
    public void PersistEditorPreferences(bool wordWrap, bool showPreview)
    public (bool wordWrap, bool showPreview) RestoreEditorPreferences()
}
```

**Session Files:**
- Location: `/tmp/TextEdit/` or `%TEMP%\TextEdit\`
- Format: `session-{documentId}.json`
- Content: Document content, file path, dirty state, encoding, EOL, tab order

**Preferences File:**
- Location: `/tmp/TextEdit/preferences.json`
- Content: `{ "WordWrap": bool, "ShowPreview": bool }`

**Features:**
- Persists unsaved new documents
- Persists unsaved changes to existing files
- Restores original file path for dirty documents
- Falls back to "Untitled" if original file missing
- Cleans up session files after save/discard

### Autosave

#### `AutosaveService.cs`
Crash recovery autosave:

```csharp
public class AutosaveService
{
    public event Action<Exception>? AutosaveFailed;
    public void Start(Func<Task> autosaveAction, int intervalSeconds = 30)
    public void Stop()
}
```

**Features:**
- Runs on background timer (default 30 seconds)
- Calls provided `autosaveAction` delegate (typically `AppState.PersistSessionAsync`)
- Fires `AutosaveFailed` event on errors (non-blocking)
- Graceful shutdown on `Stop()`

**Usage:**
```csharp
autosave.Start(async () => await appState.PersistSessionAsync(), intervalSeconds: 30);
```

### IPC

#### `IpcBridge.cs`
Native dialog integration:

```csharp
public class IpcBridge
{
    public Task<string?> ShowOpenFileDialogAsync()
    public Task<string?> ShowSaveFileDialogAsync()
    public Task<CloseDecision> ConfirmCloseDirtyAsync(string fileName)
    public Task<ExternalChangeDecision> ConfirmReloadExternalAsync(string fileName)
}
```

**Native Dialogs (via Electron):**
- `ShowOpenFileDialogAsync()` - File open picker
- `ShowSaveFileDialogAsync()` - File save picker

**Blazor Dialogs (via DialogService):**
- `ConfirmCloseDirtyAsync()` - Save/Discard/Cancel on tab close
- `ConfirmReloadExternalAsync()` - Reload/Keep Mine on external change

**IPC Protocol:**
1. Blazor sends request: `ipc.openFileDialog.request`
2. Electron shows native dialog via `Electron.Dialog.ShowOpenDialogAsync()`
3. Electron sends response: `ipc.openFileDialog.response`
4. Blazor resolves Promise with file path or null

See `specs/001-text-editor/contracts/` for JSON schemas.

### Telemetry

#### `PerformanceLogger.cs`
Structured performance logging:

```csharp
public class PerformanceLogger
{
    public IDisposable BeginOperation(string operationName)
    public void LogOperation(string operationName, long durationMs, bool success = true)
    public void LogMetric(string metricName, long value, string? unit = null)
    public OperationStats? GetStats(string operationName)
    public void PrintAllStats()
}
```

**Output Format:**
```
[PERF] OpenFile: 45ms (success=true)
[METRIC] FileSize: 524288 bytes
```

**Statistics:**
```csharp
public class OperationStats
{
    public string OperationName { get; }
    public long Count { get; }
    public long SuccessCount { get; }
    public double SuccessRate { get; }
    public double AverageDurationMs { get; }
    public long MinDurationMs { get; }
    public long MaxDurationMs { get; }
}
```

**Usage:**
```csharp
using (perfLogger.BeginOperation("OpenFile"))
{
    // Operation tracked automatically
}

// Or manual
var sw = Stopwatch.StartNew();
// ... operation ...
perfLogger.LogOperation("SaveFile", sw.ElapsedMilliseconds);
```

## Configuration

### Session Directory
```csharp
// Default: /tmp/TextEdit/ or %TEMP%\TextEdit\
var sessionDir = Path.Combine(Path.GetTempPath(), "TextEdit");
```

Override by passing different path to `PersistenceService` constructor.

### Autosave Interval
```csharp
// Default: 30 seconds
autosaveService.Start(autosaveAction, intervalSeconds: 30);
```

## Dependencies

- **TextEdit.Core** - Domain models and abstractions
- **System.IO.FileSystem** - File operations
- **System.Text.Json** - Session serialization

## Testing

Unit tests in `tests/unit/TextEdit.Infrastructure.Tests/`:
- `AutosaveServiceTests.cs` - Timer logic, failure handling
- `PersistenceServiceTests.cs` - Session save/restore, cleanup
- `FileWatcherTests.cs` - External modification detection
- `IpcBridgeTests.cs` - Dialog integration
- `PerformanceLoggerTests.cs` - Telemetry and statistics

Coverage: 53%+ (harder to test due to file I/O and timers)

## Design Decisions

### Why JSON for Session Files?

- Human-readable for debugging
- Easy to inspect in crash scenarios
- System.Text.Json is fast and built-in
- No need for binary serialization performance

### Why Temp Directory for Sessions?

- Survives app crashes (unlike in-memory)
- OS cleans up on reboot (no permanent storage needed)
- No user configuration required
- Works across platforms

### Why Separate Autosave and Persistence?

- `PersistenceService` - Manual save/restore operations
- `AutosaveService` - Periodic background execution
- Clear separation of concerns
- Testable in isolation

## Known Issues

- FileSystemWatcher can miss rapid changes (debounced to 1s)
- Session files not encrypted (plain text)
- No migration strategy for session format changes

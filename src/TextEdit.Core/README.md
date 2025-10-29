# TextEdit.Core

Domain models and business logic for the text editor. This project has **no external dependencies** and contains pure C# domain entities.

## Responsibilities

- **Domain Models** - Document, Tab entities
- **Business Logic** - Document operations, file I/O orchestration
- **Editor State** - Word wrap, preview, caret position
- **Undo/Redo** - Per-document history management
- **Abstractions** - IFileSystem for testability

## Architecture

Follows **Clean Architecture** principles:
- No dependencies on Infrastructure, UI, or external frameworks
- Pure C# domain logic
- Abstractions for external concerns (IFileSystem)

## Key Components

### Documents

#### `Document.cs`
Represents a text document being edited:

```csharp
public class Document
{
    public Guid Id { get; init; }
    public string? FilePath { get; private set; }
    public string Name => FilePath is null ? "Untitled" : Path.GetFileName(FilePath);
    public string Content { get; private set; }
    public bool IsDirty { get; private set; }
    public bool IsReadOnly { get; private set; }
    public bool HasExternalModification { get; private set; }
    public Encoding Encoding { get; set; }
    public string Eol { get; set; }  // "\n" by default
}
```

**Methods:**
- `SetContent(string)` - Updates content, marks dirty (throws if read-only)
- `SetContentInternal(string)` - Internal use for loading without dirty check
- `MarkDirtyInternal()` - Manually mark as dirty (for restoration)
- `MarkReadOnly(bool)` - Enable/disable read-only mode
- `MarkSaved(string?)` - Clear dirty flag, optionally update file path
- `MarkExternalModification(bool)` - Track external file changes

#### `DocumentService.cs`
Orchestrates document operations:

```csharp
public class DocumentService
{
    public Document NewDocument()
    public Task<Document> OpenAsync(string path, Encoding? encoding = null, 
        IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    public void UpdateContent(Document doc, string content)
    public Task SaveAsync(Document doc, string? path = null, 
        IProgress<int>? progress = null, CancellationToken cancellationToken = default)
}
```

**Features:**
- Large file support (>10MB) with streaming I/O
- Read-only mode for files >10MB
- Progress reporting for long operations
- External modification detection (timestamp comparison)
- UTF-8 encoding by default

#### `Tab.cs`
UI tab metadata:

```csharp
public class Tab
{
    public Guid Id { get; init; }
    public Guid DocumentId { get; set; }
    public bool IsActive { get; set; }
    public string Title { get; set; }
}
```

#### `TabService.cs`
Tab collection management:

```csharp
public class TabService
{
    public IReadOnlyList<Tab> Tabs { get; }
    public Tab AddTab(Document doc)
    public void CloseTab(Guid tabId)
    public void ActivateTab(Guid tabId)
}
```

### Editing

#### `EditorState.cs`
Global editor settings and caret state:

```csharp
public class EditorState
{
    public bool WordWrap { get; set; }
    public bool ShowPreview { get; set; }
    public int CaretLine { get; set; }
    public int CaretColumn { get; set; }
    public int CharacterCount { get; set; }
    public Dictionary<Guid, int> CaretIndexByDocument { get; }
    
    public event Action? Changed;
    public void NotifyChanged()
}
```

**Note:** `Changed` event is for local UI updates (e.g., StatusBar) that shouldn't trigger full AppState re-renders.

#### `UndoRedoService.cs`
Per-document undo/redo stacks:

```csharp
public interface IUndoRedoService
{
    void Attach(Document doc, string initialContent = "");
    void Push(Document doc, string content);
    bool CanUndo(Guid documentId);
    bool CanRedo(Guid documentId);
    string? Undo(Guid documentId);
    string? Redo(Guid documentId);
    void Clear(Guid documentId);
}
```

**Implementation:**
- Simple stack-based history
- Stores full-text snapshots (not diffs)
- Independent history per document
- Clears redo stack on new edit

### Abstractions

#### `IFileSystem`
File system abstraction for testability:

```csharp
public interface IFileSystem
{
    bool FileExists(string path);
    long GetFileSize(string path);
    Task<string> ReadAllTextAsync(string path, Encoding encoding);
    Task WriteAllTextAsync(string path, string contents, Encoding encoding);
    Task<string> ReadLargeFileAsync(string path, Encoding encoding, 
        IProgress<int>? progress = null, CancellationToken cancellationToken = default);
    Task WriteLargeFileAsync(string path, string contents, Encoding encoding, 
        IProgress<int>? progress = null, CancellationToken cancellationToken = default);
}
```

Implemented by `TextEdit.Infrastructure.FileSystem.FileSystemService`.

## Design Decisions

### Why No Interfaces for Domain Services?

`DocumentService` and `TabService` are **not** behind interfaces because:
1. They are domain services, not external concerns
2. They have no external dependencies (except IFileSystem abstraction)
3. Unit tests can mock IFileSystem instead
4. Avoids interface proliferation (YAGNI)

### Why Full-Text Undo Snapshots?

Simple and fast for typical text documents (<10MB):
- Average text file: <100KB → ~100KB per snapshot
- 50 undo levels → ~5MB memory
- Instant undo/redo (no diff calculation)

For large files (>10MB), read-only mode prevents excessive memory use.

### Why Separate EditorState.Changed Event?

`EditorState.Changed` fires frequently (every keystroke for caret updates) while `AppState.Changed` fires only for document/tab changes. This allows StatusBar to update without re-rendering TabStrip.

## Testing

Unit tests in `tests/unit/TextEdit.Core.Tests/`:
- `DocumentServiceTests.cs` - File operations with mocked IFileSystem
- `UndoRedoServiceTests.cs` - Undo/redo logic

Coverage: 85%+ line, 80%+ branch

## Dependencies

**None** - Core is dependency-free by design.

## Usage Example

```csharp
var fs = new FileSystemService();
var undo = new UndoRedoService();
var docs = new DocumentService(fs, undo);

// Create new document
var doc = docs.NewDocument();

// Open existing file
var doc = await docs.OpenAsync("/path/to/file.txt");

// Edit content
docs.UpdateContent(doc, "New content");
undo.Push(doc, doc.Content);

// Save
await docs.SaveAsync(doc);

// Undo
if (undo.CanUndo(doc.Id))
{
    var previousContent = undo.Undo(doc.Id);
    docs.UpdateContent(doc, previousContent);
}
```

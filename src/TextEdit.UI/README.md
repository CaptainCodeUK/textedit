# TextEdit.UI

Blazor Server UI components, pages, and application state orchestration.

## Responsibilities

- **UI Components** - Editor, TabStrip, StatusBar, PreviewPanel, Dialogs
- **AppState** - Application state management and business logic orchestration
- **DialogService** - User confirmation and error dialogs
- **EditorCommandHub** - Central command routing for menu actions
- **Performance** - Selective re-renders to minimize unnecessary updates

## Architecture

### State Management

```
┌─────────────────────────────────────────────┐
│           AppState (Orchestrator)           │
│  - Coordinates Core + Infrastructure        │
│  - Manages document/tab lifecycle           │
│  - Notifies components via Changed event    │
└──────────────┬──────────────────────────────┘
               │
       ┌───────┼───────┐
       │       │       │
  ┌────▼─┐ ┌──▼───┐ ┌─▼────────┐
  │Editor│ │Tabs  │ │StatusBar │
  └──────┘ └──────┘ └──────────┘
```

### Key Components

## App State

### `AppState.cs`
Central orchestrator for all business operations:

```csharp
public class AppState
{
    // Current state
    public Tab? ActiveTab { get; }
    public Document? ActiveDocument { get; }
    public IEnumerable<Tab> Tabs { get; }
    public IEnumerable<Document> AllDocuments { get; }
    public EditorState EditorState { get; }
    public int StateVersion { get; }  // Increments on every change
    
    // Document operations
    public Document CreateNew()
    public Task<Document?> OpenAsync()
    public Task SaveActiveAsync()
    public Task<bool> SaveAsActiveAsync()
    public Task<bool> CloseTabAsync(Guid tabId)
    public Task CloseOthersAsync(Guid keepTabId)
    public Task CloseRightAsync(Guid fromTabId)
    
    // Tab operations
    public void ActivateTab(Guid tabId)
    public void ActivateNextTab()
    public void ActivatePreviousTab()
    
    // Session management
    public Task RestoreSessionAsync()
    public Task PersistSessionAsync()
    
    // Change notification
    public event Action? Changed;
}
```

**Responsibilities:**
- Coordinates `DocumentService`, `TabService`, `PersistenceService`, `IpcBridge`
- Manages file watchers for external modification detection
- Handles error scenarios with user-friendly dialogs
- Tracks state version for performance optimizations
- Fires `Changed` event to trigger UI updates

## UI Components

### Editor

#### `TextEditor.razor` / `TextEditor.razor.cs`
Main text editing area:

```csharp
[Parameter] public AppState State { get; set; }
[Parameter] public EditorState EditorState { get; set; }
```

**Features:**
- Textarea with two-way binding to `Content`
- Word wrap toggle
- Undo/redo with 400ms debounce
- Caret position tracking
- Character count updates
- Focus management with JS interop

**Performance:**
- Direct `@bind="Content"` with `@bind:event="oninput"`
- Debounced undo snapshots (400ms) to reduce memory churn
- No input debouncing (removed in T073e after causing regressions)

#### `EditorCommandHub.cs`
Static command hub for menu actions:

```csharp
public static class EditorCommandHub
{
    public static Func<Task>? NewRequested { get; set; }
    public static Func<Task>? OpenRequested { get; set; }
    public static Func<Task>? SaveRequested { get; set; }
    public static Func<Task>? SaveAsRequested { get; set; }
    public static Func<Task>? UndoRequested { get; set; }
    public static Func<Task>? RedoRequested { get; set; }
    public static Func<Task>? NextTabRequested { get; set; }
    public static Func<Task>? PrevTabRequested { get; set; }
    public static Func<Task>? CloseTabRequested { get; set; }
    public static Func<Task>? CloseOthersRequested { get; set; }
    public static Func<Task>? CloseRightRequested { get; set; }
    public static Func<Task>? ToggleWordWrapRequested { get; set; }
    public static Func<Task>? TogglePreviewRequested { get; set; }
    
    public static Task InvokeSafe(Func<Task>? action)
}
```

**Usage:** Electron menus → `EditorCommandHub` → TextEditor methods

### Tabs

#### `TabStrip.razor`
Tab bar with close buttons:

**Performance Optimization:**
```csharp
protected override bool ShouldRender()
{
    var currentTabCount = State.Tabs.Count;
    var currentActiveTabId = State.ActiveDocument?.Id;
    var currentStateVersion = State.StateVersion;

    var shouldRender = _lastTabCount != currentTabCount
        || _lastActiveTabId != currentActiveTabId
        || _lastStateVersion != currentStateVersion;

    // Update tracking and return
}
```

**Result:** Skips re-renders when only EditorState changes (caret position, character count).

**Features:**
- Keyboard navigation (Left/Right arrows)
- Middle-click to close
- Screen reader announcements
- ARIA labels and roles
- Dirty indicator (asterisk)

#### `TabItem.razor`
Individual tab button (subcomponent).

### Status Bar

#### `StatusBar.razor`
Bottom status bar with document info:

**Performance Optimization:**
```csharp
protected override bool ShouldRender()
{
    var currentDocId = State.ActiveDocument?.Id;
    var currentDirty = State.ActiveDocument?.IsDirty ?? false;
    var currentExternalMod = State.ActiveDocument?.HasExternalModification ?? false;
    var currentCaretLine = EditorState.CaretLine;
    var currentCaretColumn = EditorState.CaretColumn;
    var currentCharCount = EditorState.CharacterCount;
    var currentAutosaveTime = State.AutosaveService.LastAutosaveTime;
    var currentFileName = State.ActiveDocument?.Name ?? "";

    var shouldRender = _lastDocId != currentDocId
        || _lastDirtyState != currentDirty
        || _lastExternalModState != currentExternalMod
        || _lastCaretLine != currentCaretLine
        || _lastCaretColumn != currentCaretColumn
        || _lastCharacterCount != currentCharCount
        || _lastAutosaveTime != currentAutosaveTime
        || _lastFileName != currentFileName;

    // Update tracking and return
}
```

**Features:**
- Line and column position
- Character count
- Autosave indicator (last save time)
- Filename display
- External modification indicator
- Screen reader announcements

### Preview

#### `PreviewPanel.razor`
Markdown preview pane:

```csharp
[Parameter] public string Content { get; set; }
[Parameter] public bool IsLargeFile { get; set; }
```

**Features:**
- Automatic refresh for files <100KB
- Manual refresh button for large files (>100KB)
- Markdown rendering with caching (165-330x speedup)
- Scrollable HTML preview
- TailwindCSS typography styling

**Performance:**
```csharp
protected override void OnParametersSet()
{
    if (!IsLargeFile && Content != _lastRenderedContent)
    {
        _renderedHtml = _renderer.RenderToHtml(Content);
        _lastRenderedContent = Content;
    }
}
```

### Dialogs

#### `ErrorDialog.razor`
User-friendly error messages:

```csharp
[Parameter] public string Title { get; set; }
[Parameter] public string Message { get; set; }
[Parameter] public ErrorAction Action { get; set; }
[Parameter] public EventCallback OnOk { get; set; }
[Parameter] public EventCallback OnSaveAs { get; set; }
```

**Error Types:**
- File not found
- Permission denied
- Disk full
- I/O error

**Accessibility:**
- Auto-focus on primary button
- Tab trapping
- Escape to close
- ARIA alertdialog role

#### `ConfirmDialog.razor`
Yes/No confirmation dialogs:

```csharp
[Parameter] public string Title { get; set; }
[Parameter] public string Message { get; set; }
[Parameter] public EventCallback<bool> OnResult { get; set; }
```

**Used For:**
- Close dirty tab (Save/Don't Save/Cancel)
- External file change (Reload/Keep Mine)

**Accessibility:**
- Same as ErrorDialog

#### `DialogService.cs`
Dialog state management:

```csharp
public class DialogService
{
    public ErrorDialogState? CurrentError { get; private set; }
    public ConfirmDialogState? CurrentConfirm { get; private set; }
    
    public event Action? OnStateChanged;
    
    public void ShowError(string title, string message, ErrorAction action, 
        Func<Task>? onOk, Func<Task>? onSaveAs)
    public Task<bool> ShowConfirm(string title, string message)
    public void CloseError()
    public void CloseConfirm()
}
```

## Pages

### `App.razor`
Root application component:

**Responsibilities:**
- Restores session on mount
- Renders main layout (Editor + TabStrip + StatusBar + Preview)
- Hosts ErrorDialog and ConfirmDialog
- Subscribes to AppState.Changed
- CSS Grid layout

**Layout:**
```css
.app-container {
    display: grid;
    grid-template-rows: auto 1fr auto;
    grid-template-columns: 1fr auto;
    height: 100vh;
}
```

## Styles

### TailwindCSS
- `tailwind.config.cjs` - Custom configuration
- `Styles/app.css` - Main stylesheet with Tailwind directives
- `typography` plugin for markdown preview

### Component Styles
- `TextEditor.razor.css` - Editor-specific styles
- `StatusBar.razor.css` - Status bar layout
- etc.

## JavaScript Interop

### `wwwroot/editorFocus.js`
```javascript
window.editorFocus = {
    focus: function(elementId) {
        const el = document.getElementById(elementId);
        if (el) el.focus();
    }
};
```

**Usage:** Focus textarea after file operations.

## Performance Summary

### Optimizations
1. **Selective Re-renders** - StatusBar and TabStrip skip unnecessary updates
2. **Markdown Caching** - 165-330x speedup on cache hits
3. **Debounced Undo** - 400ms debounce reduces memory allocations
4. **StateVersion Tracking** - Fast change detection without deep comparisons

### Avoided Anti-Patterns
- ❌ Input debouncing (T073e) - Caused regressions, rolled back
- ✅ Direct binding with selective component updates instead

## Dependencies

- **TextEdit.Core** - Domain models
- **TextEdit.Infrastructure** - Services
- **TextEdit.Markdown** - Rendering
- **Microsoft.AspNetCore.Components.Web** - Blazor Server
- **TailwindCSS** - Styling

## Testing

Unit tests in `tests/unit/TextEdit.UI.Tests/` (if exists).

Integration tests in `tests/integration/TextEdit.App.Tests/`:
- `AccessibilityTests.cs` - Playwright-based UI tests
- `SaveAsTests.cs` - Save As workflow tests

## Design Decisions

### Why Blazor Server Instead of WASM?

**Pros:**
- Full .NET runtime access (file I/O, native dialogs via Electron)
- Smaller download size
- Better performance for CPU-intensive operations (markdown rendering)

**Cons:**
- Requires SignalR connection
- Not suitable for web deployment (desktop-only)

**Decision:** Blazor Server is ideal for desktop apps with Electron.NET.

### Why AppState Instead of Redux/Flux?

**Simple state management** is sufficient:
- Single source of truth (`AppState`)
- Event-based notifications (`Changed` event)
- No complex reducers or actions needed
- Easy to test

### Why Static EditorCommandHub?

**Pros:**
- Electron menus need static entry point
- Simple to wire up from ElectronHost
- No dependency injection complexity for menus

**Cons:**
- Global mutable state (delegates)

**Decision:** Acceptable trade-off for menu integration. Delegates are assigned once on TextEditor mount.

## Known Limitations

- No virtual scrolling (not needed for text documents <10MB)
- No syntax highlighting (out of scope for v1)
- No search/replace UI (planned for v2)

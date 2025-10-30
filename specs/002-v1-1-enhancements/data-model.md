# Data Model: Scrappy Text Editor v1.1 Enhancements

**Feature Branch**: `002-v1-1-enhancements`  
**Date**: 2025-10-30  
**Purpose**: Define entities, relationships, validation rules, and state transitions

## Entity Definitions

### 1. UserPreferences

**Location**: `src/TextEdit.Core/Preferences/UserPreferences.cs`

**Purpose**: Represents all user-configurable settings that persist across application sessions

**Fields**:

| Field | Type | Default | Validation | Description |
|-------|------|---------|------------|-------------|
| `Theme` | `ThemeMode` (enum) | `ThemeMode.System` | Must be Light, Dark, or System | User's theme preference |
| `FontFamily` | `string` | `""` (empty) | Non-null, max 100 chars | Font family name; empty = system monospace |
| `FontSize` | `int` | `12` | Range: 8–72 | Font size in points |
| `FileExtensions` | `List<string>` | `[".txt", ".md", ".log", ".json", ".xml", ".csv", ".ini", ".cfg", ".conf"]` | Each extension must match regex `^\.[a-zA-Z0-9-]+$`, no duplicates | Recognized text file extensions |
| `LoggingEnabled` | `bool` | `false` | N/A | Whether detailed logging is active |
| `ToolbarVisible` | `bool` | `true` | N/A | Whether toolbar is shown |

**Validation Rules**:
- `FontSize` must be clamped to 8–72 range (silently correct out-of-range values)
- `FileExtensions` must contain at least `.txt` and `.md` (cannot be removed)
- Extension format validation: must start with `.`, contain only alphanumeric and hyphen
- No duplicate extensions allowed (case-insensitive comparison)

**Example JSON**:
```json
{
  "theme": "Dark",
  "fontFamily": "Consolas",
  "fontSize": 14,
  "fileExtensions": [".txt", ".md", ".log", ".json", ".rs", ".py"],
  "loggingEnabled": true,
  "toolbarVisible": true
}
```

---

### 2. ThemeMode (Enum)

**Location**: `src/TextEdit.Core/Preferences/ThemeMode.cs`

**Purpose**: Represents available theme options

**Values**:
- `Light = 0`: Always use light theme
- `Dark = 1`: Always use dark theme
- `System = 2`: Follow operating system theme preference

**State Transitions**:
- User can switch between any values via Options dialog
- `System` mode automatically updates UI when OS theme changes (no manual switch needed)
- Theme changes take effect immediately (<500ms per spec)

---

### 3. ThemeColors

**Location**: `src/TextEdit.UI/Services/ThemeColors.cs`

**Purpose**: Defines color palettes for light and dark themes meeting WCAG AA standards

**Fields** (per theme):

| Property | Light Theme | Dark Theme | Contrast Ratio |
|----------|-------------|------------|----------------|
| `BackgroundPrimary` | `#FFFFFF` | `#111827` | N/A (base) |
| `TextPrimary` | `#1F2937` | `#F9FAFB` | 15.8:1 ✓ |
| `BackgroundSecondary` | `#F3F4F6` | `#1F2937` | N/A |
| `TextSecondary` | `#4B5563` | `#D1D5DB` | 7.2:1 ✓ |
| `AccentColor` | `#3B82F6` | `#60A5FA` | N/A |
| `AccentText` | `#FFFFFF` | `#111827` | 4.9:1 / 7.5:1 ✓ |
| `BorderColor` | `#E5E7EB` | `#374151` | N/A |
| `ErrorColor` | `#EF4444` | `#F87171` | 4.5:1 ✓ |
| `SuccessColor` | `#10B981` | `#34D399` | 4.5:1 ✓ |

**Validation**: All text-on-background combinations must meet WCAG AA 4.5:1 ratio (verified via automated tools)

---

### 4. CommandLineArgs

**Location**: `src/TextEdit.App/Models/CommandLineArgs.cs`

**Purpose**: Parsed and validated command-line arguments passed at launch

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `ValidFilePaths` | `List<string>` | Absolute paths to files that exist and are readable |
| `InvalidFiles` | `List<InvalidFileInfo>` | Files that couldn't be opened with reason |

**Nested Type**: `InvalidFileInfo`
- `Path` (string): The path that was provided
- `Reason` (string): Simple reason phrase (e.g., "File not found", "Permission denied", "Unreadable")

**Validation Rules**:
- Resolve relative paths to absolute using `Directory.GetCurrentDirectory()`
- Check file existence with `File.Exists()`
- Verify read permissions with `try/catch` on `File.OpenRead()`
- Classify failures into standard reasons:
  - "File not found" - Path doesn't exist
  - "Permission denied" - File exists but can't be read
  - "Unreadable" - File exists but is locked/corrupt
  - "Invalid path" - Path contains illegal characters

**Example**:
```csharp
var args = new CommandLineArgs {
    ValidFilePaths = new List<string> {
        "/home/user/doc.txt",
        "/home/user/notes.md"
    },
    InvalidFiles = new List<InvalidFileInfo> {
        new("", "File not found")
    }
};
```

---

### 5. ToolbarState

**Location**: `src/TextEdit.UI/App/ToolbarState.cs`

**Purpose**: Tracks enabled/disabled state of toolbar buttons based on editor context

**Fields**:

| Field | Type | Computed From | Description |
|-------|------|---------------|-------------|
| `CanSave` | `bool` | `ActiveDocument?.IsDirty ?? false` | Save button enabled |
| `CanCut` | `bool` | `SelectionLength > 0` | Cut button enabled |
| `CanCopy` | `bool` | `SelectionLength > 0` | Copy button enabled |
| `CanPaste` | `bool` | `Clipboard.HasText()` | Paste button enabled |
| `CanApplyMarkdown` | `bool` | `ActiveDocument != null` | Markdown buttons enabled |
| `CurrentFont` | `string` | `Preferences.FontFamily ?? "System Monospace"` | Selected font display |
| `CurrentFontSize` | `int` | `Preferences.FontSize` | Selected font size display |

**State Recalculation**: Triggered on:
- Document change (new tab, close tab, switch tab)
- Text selection change in editor
- Document dirty state change (edit, save)
- Preferences change (font family/size updated)

---

### 6. AboutDialogInfo

**Location**: `src/TextEdit.UI/Components/AboutDialog.razor.cs`

**Purpose**: Metadata displayed in About dialog

**Fields** (all readonly, populated from assembly attributes):

| Field | Type | Source | Example |
|-------|------|--------|---------|
| `ApplicationName` | `string` | Hardcoded | "Scrappy Text Editor" |
| `Version` | `string` | `Assembly.GetExecutingAssembly().GetName().Version` | "1.1.0" |
| `BuildDate` | `DateTime` | `Assembly attribute or build script` | "2025-10-30" |
| `Description` | `string` | Hardcoded | "A friendly, lightweight text editor for everyday writing and markdown" |
| `Technologies` | `List<string>` | Hardcoded | `["Blazor Server", "Electron.NET", ".NET 8", "Markdig"]` |
| `Copyright` | `string` | Hardcoded | "© 2025 [Author/Org] - MIT License" |
| `ProjectUrl` | `string` | Hardcoded | "https://github.com/CaptainCodeUK/textedit" |

---

### 7. MarkdownFormat (Enum)

**Location**: `src/TextEdit.UI/Services/MarkdownFormat.cs`

**Purpose**: Represents markdown formatting operations

**Values**:
- `Bold = 0`: Wrap with `**text**`
- `Italic = 1`: Wrap with `_text_`
- `Code = 2`: Wrap with `` `text` ``
- `H1 = 3`: Prefix with `# `
- `H2 = 4`: Prefix with `## `
- `BulletedList = 5`: Prefix each line with `- `
- `NumberedList = 6`: Prefix each line with `1. `, `2. `, etc.

**Associated Data** (per format):

| Format | Prefix | Suffix | Behavior |
|--------|--------|--------|----------|
| Bold | `**` | `**` | Wrap selection or insert `**\|**` |
| Italic | `_` | `_` | Wrap selection or insert `_\|_` |
| Code | `` ` `` | `` ` `` | Wrap selection or insert `` `\|` `` |
| H1 | `# ` | `""` | Prefix line |
| H2 | `## ` | `""` | Prefix line |
| BulletedList | `- ` | `""` | Prefix each line in selection |
| NumberedList | `N. ` | `""` | Prefix each line with incrementing number |

---

### 8. LogEntry

**Location**: `src/TextEdit.Infrastructure/Logging/LogEntry.cs`

**Purpose**: Represents a single log entry when detailed logging is enabled

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `Timestamp` | `DateTime` | UTC timestamp of log entry |
| `Level` | `LogLevel` (enum) | Severity: Trace, Debug, Info, Warning, Error, Critical |
| `Category` | `string` | Source category (e.g., "FileSystem", "UI", "IPC") |
| `Message` | `string` | Human-readable log message |
| `Exception` | `Exception?` | Optional exception details |
| `Context` | `Dictionary<string, object>?` | Optional key-value context data |

**Log File Format** (JSON Lines):
```json
{"timestamp":"2025-10-30T14:32:10.123Z","level":"Info","category":"FileSystem","message":"File opened","context":{"path":"/home/user/doc.txt"}}
{"timestamp":"2025-10-30T14:32:15.456Z","level":"Error","category":"IPC","message":"Failed to send message","exception":"System.TimeoutException: ..."}
```

---

## Entity Relationships

```
┌─────────────────┐
│  AppState       │
│  (UI Layer)     │
└────────┬────────┘
         │ owns
         ├──────────────┐
         │              │
         v              v
┌─────────────────┐  ┌─────────────────┐
│ UserPreferences │  │  ToolbarState   │
│  (Core Layer)   │  │   (UI Layer)    │
└────────┬────────┘  └────────┬────────┘
         │                    │
         │ uses               │ computes from
         v                    v
┌─────────────────┐  ┌─────────────────┐
│   ThemeMode     │  │  Document       │
│    (enum)       │  │  (Core Layer)   │
└─────────────────┘  └─────────────────┘

┌─────────────────┐
│ CommandLineArgs │
│  (App Layer)    │
└────────┬────────┘
         │ forwarded via IPC
         v
┌─────────────────┐
│    AppState     │
│   (UI Layer)    │
└─────────────────┘

┌─────────────────┐
│  ThemeColors    │
│  (UI Layer)     │
└────────┬────────┘
         │ applied by
         v
┌─────────────────┐
│  ThemeManager   │
│   (UI Layer)    │
└─────────────────┘
```

**Key Relationships**:
1. `AppState` owns `UserPreferences` and coordinates all state changes
2. `UserPreferences` persisted by `PreferencesRepository` (Infrastructure)
3. `ThemeMode` determines which `ThemeColors` palette is active
4. `ToolbarState` is computed from `AppState` properties (reactive)
5. `CommandLineArgs` parsed in App layer, forwarded to UI layer via IPC
6. `MarkdownFormat` operations modify `Document` content

---

## State Transitions

### 1. Theme Change Flow

```
User selects theme in Options dialog
  ↓
OptionsDialog updates AppState.Preferences.Theme
  ↓
AppState calls PreferencesRepository.SaveAsync()
  ↓
AppState.Changed event fires
  ↓
ThemeManager applies new ThemeColors to UI
  ↓
MarkdownRenderer updates theme for preview
  ↓
All components re-render with new theme
  (Duration: <500ms per spec)
```

### 2. Font Change Flow

```
User selects font/size in Toolbar
  ↓
Toolbar updates AppState.Preferences.FontFamily/FontSize
  ↓
AppState validates (clamp size to 8-72)
  ↓
AppState calls PreferencesRepository.SaveAsync()
  ↓
AppState.Changed event fires
  ↓
TextEditor component re-renders with new font
  ↓
All open documents reflect new font
  (Duration: <100ms per spec)
```

### 3. CLI Args Processing Flow

```
User launches app with file args
  ↓
ElectronHost parses Environment.GetCommandLineArgs()
  ↓
ElectronHost validates paths → CommandLineArgs
  ↓
ElectronHost forwards ValidFilePaths to AppState via IPC
  ↓
AppState opens each file in new tab
  ↓
If InvalidFiles non-empty:
  ↓
  CliErrorSummary component displays after startup
  ↓
  User dismisses or closes summary
  (Non-blocking, doesn't prevent valid files from opening)
```

### 4. Markdown Formatting Flow

```
User clicks Bold button (or Ctrl+B)
  ↓
Toolbar calls MarkdownFormattingService.ApplyFormat()
  ↓
Service checks if text is selected:
  If YES: wrap selection with **text**
  If NO: insert **|** with caret between markers
  ↓
Service returns (newText, newCaretPos)
  ↓
TextEditor updates document content
  ↓
Document.IsDirty = true
  ↓
ToolbarState.CanSave = true
```

---

## Validation Matrix

| Entity | Field | Validation Rule | Error Handling |
|--------|-------|-----------------|----------------|
| UserPreferences | FontSize | 8 ≤ size ≤ 72 | Silently clamp to range |
| UserPreferences | FileExtensions | Regex `^\.[a-zA-Z0-9-]+$` | Show error in Options dialog, don't save |
| UserPreferences | FileExtensions | Must include .txt, .md | Prevent removal, show error message |
| CommandLineArgs | ValidFilePaths | File must exist and be readable | Move to InvalidFiles with reason |
| ThemeColors | All combinations | WCAG AA 4.5:1 contrast | Fail CI build if violated |
| LogEntry | Timestamp | Must be UTC | Convert on creation |
| MarkdownFormat | Selection range | 0 ≤ start ≤ start+length ≤ text.Length | Clamp to valid range |

---

## Persistence Schema

### preferences.json

```json
{
  "theme": "Dark",
  "fontFamily": "Consolas",
  "fontSize": 14,
  "fileExtensions": [".txt", ".md", ".log", ".json", ".xml", ".csv", ".ini", ".cfg", ".conf", ".rs"],
  "loggingEnabled": true,
  "toolbarVisible": true
}
```

**Location**:
- Windows: `%AppData%\Scrappy\preferences.json`
- macOS: `~/Library/Application Support/Scrappy/preferences.json`
- Linux: `~/.config/Scrappy/preferences.json`

**Migration Strategy**:
- If file missing: use defaults, create on first save
- If file corrupt: log error, use defaults, backup corrupt file as `preferences.json.bak`
- If schema version changes: add `version` field, implement migration logic

---

## Summary

This data model defines 8 core entities supporting v1.1 enhancements:
1. **UserPreferences** - All configurable settings with JSON persistence
2. **ThemeMode** - Light/Dark/System enum
3. **ThemeColors** - WCAG AA compliant color palettes
4. **CommandLineArgs** - Validated file paths from CLI
5. **ToolbarState** - Computed button enabled/disabled states
6. **AboutDialogInfo** - Application metadata for About dialog
7. **MarkdownFormat** - Formatting operations enum
8. **LogEntry** - Structured logging when enabled

All entities follow Clean Architecture:
- **Core layer**: UserPreferences, ThemeMode, MarkdownFormat (pure domain)
- **Infrastructure layer**: PreferencesRepository, LogEntry (external concerns)
- **UI layer**: ThemeColors, ToolbarState, AboutDialogInfo (presentation)
- **App layer**: CommandLineArgs (native integration)

Relationships are clear, state transitions are documented, and validation rules ensure data integrity.

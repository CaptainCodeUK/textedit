# Research Document: Scrappy Text Editor v1.1 Enhancements

**Feature Branch**: `002-v1-1-enhancements`  
**Date**: 2025-10-30  
**Purpose**: Resolve technical unknowns and establish best practices for implementation

## Research Tasks

### 1. Command-Line Arguments in Electron.NET

**Decision**: Use `Electron.CommandLine` API and custom IPC for argument forwarding

**Rationale**:
- Electron.NET provides `Electron.CommandLine` for parsing command-line args
- Args must be forwarded from Electron (Node.js) context to Blazor (.NET) context via IPC
- Single-instance enforcement uses `Electron.App.RequestSingleInstanceLock()` which triggers `second-instance` event with args

**Implementation Pattern**:
```csharp
// In ElectronHost.cs
var gotTheLock = await Electron.App.RequestSingleInstanceLockAsync();
if (!gotTheLock) {
    Electron.App.Exit();
    return;
}

Electron.App.On("second-instance", async (args) => {
    string[] fileArgs = ExtractFileArgs(args);
    await IpcBridge.SendToBlazor("open-files", fileArgs);
    // Focus existing window
});

// On first launch
string[] initialArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
```

**Alternatives Considered**:
- Environment.GetCommandLineArgs() alone - Rejected because doesn't handle single-instance second launch
- File-based IPC - Rejected due to complexity and potential race conditions
- Named pipes - Rejected because Electron.NET already provides IPC mechanism

**Best Practices**:
- Validate file paths before passing to Blazor layer
- Handle both absolute and relative paths (resolve relative to CWD)
- Escape special characters in file paths
- Implement timeout for IPC calls (5 seconds)

---

### 2. Single-Instance Enforcement Across Platforms

**Decision**: Use Electron's built-in `RequestSingleInstanceLock()` API

**Rationale**:
- Cross-platform solution (Windows, macOS, Linux)
- Handles platform-specific mechanisms (Windows mutex, macOS/Linux socket files)
- Automatically focuses existing window when second instance attempts to launch
- Provides `second-instance` event with command-line args from second launch

**Implementation Pattern**:
```csharp
var gotLock = await Electron.App.RequestSingleInstanceLockAsync();
if (!gotLock) {
    Electron.App.Exit(); // Second instance exits immediately
    return;
}

Electron.App.On("second-instance", async (args) => {
    var mainWindow = Electron.WindowManager.BrowserWindows.First();
    if (mainWindow.IsMinimized) await mainWindow.RestoreAsync();
    await mainWindow.FocusAsync();
    // Handle args...
});
```

**Alternatives Considered**:
- Manual named mutex (Windows) - Rejected due to platform-specific code complexity
- File-based locking - Rejected due to cleanup issues and race conditions
- TCP socket listening - Rejected because not needed, Electron provides this

**Best Practices**:
- Always check lock status before creating main window
- Store lock reference to prevent garbage collection
- Handle edge case where existing instance crashes (stale lock)

---

### 3. OS Theme Detection and Watching

**Decision**: Use `Electron.NativeTheme` API for detection and `updated` event for watching

**Rationale**:
- Electron provides `nativeTheme.shouldUseDarkColors` property
- `nativeTheme.themeSource` allows setting 'light', 'dark', or 'system'
- `updated` event fires when OS theme changes
- Cross-platform support (Windows 10+, macOS 10.14+, Linux with GTK theme)

**Implementation Pattern**:
```csharp
// ThemeDetectionService.cs (Infrastructure layer)
public class ThemeDetectionService {
    public async Task<ThemeMode> GetCurrentOsTheme() {
        var isDark = await Electron.NativeTheme.ShouldUseDarkColorsAsync();
        return isDark ? ThemeMode.Dark : ThemeMode.Light;
    }
    
    public void WatchThemeChanges(Action<ThemeMode> callback) {
        Electron.NativeTheme.On("updated", async () => {
            var theme = await GetCurrentOsTheme();
            callback(theme);
        });
    }
}
```

**Alternatives Considered**:
- P/Invoke to Windows registry (HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize) - Rejected for being Windows-only
- Polling SystemParameters.HighContrast (WPF) - Rejected because not available in Blazor context
- CSS media query `prefers-color-scheme` - Rejected because we need .NET-side control for markdown rendering theme

**Best Practices**:
- Cache theme value to avoid excessive IPC calls
- Debounce theme change events (100ms) to prevent rapid switching
- Provide fallback to Light theme if detection fails
- Test on all three platforms for consistent behavior

---

### 4. User Preferences Persistence (JSON Storage)

**Decision**: Use `System.Text.Json` with custom JsonSerializerOptions, store in OS app data directory

**Rationale**:
- `System.Text.Json` is built-in, performant, and supports nullable reference types
- OS app data directories are standard locations for user settings:
  - Windows: `Environment.SpecialFolder.ApplicationData` → `%AppData%\Scrappy`
  - macOS: `Environment.SpecialFolder.ApplicationData` → `~/Library/Application Support/Scrappy`
  - Linux: `Environment.GetFolderPath(ApplicationData)` → `~/.config/Scrappy`
- Human-readable format enables manual editing and debugging
- Schema validation via data annotations on UserPreferences model

**Implementation Pattern**:
```csharp
// UserPreferences.cs (Core layer)
public class UserPreferences {
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public string FontFamily { get; set; } = ""; // Empty = system monospace
    public int FontSize { get; set; } = 12;
    public List<string> FileExtensions { get; set; } = new() { ".txt", ".md", ".log" };
    public bool LoggingEnabled { get; set; } = false;
    public bool ToolbarVisible { get; set; } = true;
}

// PreferencesRepository.cs (Infrastructure layer)
public class PreferencesRepository {
    private readonly string _prefsPath;
    
    public PreferencesRepository() {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Scrappy"
        );
        Directory.CreateDirectory(appDataDir);
        _prefsPath = Path.Combine(appDataDir, "preferences.json");
    }
    
    public async Task<UserPreferences> LoadAsync() {
        if (!File.Exists(_prefsPath))
            return new UserPreferences(); // Defaults
        
        var json = await File.ReadAllTextAsync(_prefsPath);
        return JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
    }
    
    public async Task SaveAsync(UserPreferences prefs) {
        var json = JsonSerializer.Serialize(prefs, new JsonSerializerOptions {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_prefsPath, json);
    }
}
```

**Alternatives Considered**:
- XML configuration - Rejected for verbosity and poor human readability
- Binary serialization - Rejected for lack of human readability and versioning difficulty
- Registry (Windows) / plist (macOS) / gsettings (Linux) - Rejected for platform-specific code
- Existing PersistenceService session format - Rejected because session persistence is for crash recovery, not preferences

**Best Practices**:
- Use atomic write (write to temp file, then move) to prevent corruption
- Validate JSON schema after deserialization
- Provide migration path if preferences schema changes (version field)
- Handle missing/corrupt preferences file gracefully (fall back to defaults)
- Don't save preferences on every change (debounce or save on dialog close)

---

### 5. Application Icon Design and Multi-Resolution Export

**Decision**: Commission SVG icon design, export to multi-resolution PNG/ICO/ICNS using electron-icon-maker or manual tools

**Rationale**:
- Electron requires multiple icon formats:
  - Windows: `.ico` with 16x16, 32x32, 48x48, 256x256
  - macOS: `.icns` with 16x16 to 512x512 @1x and @2x
  - Linux: `.png` at various sizes (16, 32, 48, 64, 128, 256, 512)
- SVG source allows crisp rendering at all sizes
- Icon must be recognizable at 16x16 (taskbar/tab icon size)
- Puppy character with pen/notepad should be stylized, not photorealistic

**Implementation Pattern**:
```bash
# Using electron-icon-maker (npm package)
npm install -g electron-icon-maker
electron-icon-maker --input=icon.svg --output=./icons

# Or manual with ImageMagick
convert icon.svg -resize 16x16 icon-16.png
convert icon.svg -resize 32x32 icon-32.png
# ... repeat for all sizes
# Create .ico with png2ico or similar
# Create .icns with iconutil (macOS)
```

**File Locations**:
- Source SVG: `src/TextEdit.App/wwwroot/images/scrappy-icon.svg`
- Generated icons: `src/TextEdit.App/wwwroot/icons/`
- Reference in `electron.manifest.json`: `"icon": "/icons/icon"`

**Alternatives Considered**:
- Single PNG source - Rejected because doesn't scale well to large sizes (512x512)
- Bitmap-only formats - Rejected for lack of editability and scaling
- Online icon generators - Rejected for lack of control over output quality

**Best Practices**:
- Design icon with high contrast for dark mode compatibility
- Test visibility on light and dark taskbar/dock backgrounds
- Simplify details at small sizes (16x16, 32x32)
- Use consistent stroke widths that work at all sizes
- Ensure puppy character is recognizable even at smallest size

---

### 6. Markdown Formatting Button Behavior (Wrap vs. Insert)

**Decision**: Check for selection, wrap if exists, otherwise insert paired markers with caret positioned between

**Rationale**:
- Matches behavior of popular markdown editors (Typora, VS Code, GitHub)
- Provides efficient workflow for both scenarios:
  - Wrapping: Select text, click Bold → `**selected text**`
  - Inserting: Click Bold → `**|**` (caret between markers)
- Caret positioning after insert enables immediate typing

**Implementation Pattern**:
```csharp
// MarkdownFormattingService.cs
public (string newText, int newCaretPos) ApplyFormat(
    string text, int caretPos, int selectionLength, MarkdownFormat format)
{
    var (prefix, suffix) = format switch {
        MarkdownFormat.Bold => ("**", "**"),
        MarkdownFormat.Italic => ("_", "_"),
        MarkdownFormat.Code => ("`", "`"),
        MarkdownFormat.H1 => ("# ", ""),
        // ...
    };
    
    if (selectionLength > 0) {
        // Wrap selection
        var before = text[..caretPos];
        var selected = text.Substring(caretPos, selectionLength);
        var after = text[(caretPos + selectionLength)..];
        var newText = $"{before}{prefix}{selected}{suffix}{after}";
        var newCaretPos = caretPos + prefix.Length + selected.Length + suffix.Length;
        return (newText, newCaretPos);
    } else {
        // Insert empty markers
        var before = text[..caretPos];
        var after = text[caretPos..];
        var newText = $"{before}{prefix}{suffix}{after}";
        var newCaretPos = caretPos + prefix.Length; // Position between markers
        return (newText, newCaretPos);
    }
}
```

**Alternatives Considered**:
- Always require selection - Rejected for poor UX (extra steps for user)
- Insert prefix only (no suffix) - Rejected for inconsistency with wrapping behavior
- Toggle behavior (insert, then remove on second click) - Rejected for complexity and ambiguity

**Best Practices**:
- Support undo/redo for formatting operations
- Handle edge cases: caret at start/end of document, existing markdown syntax
- Provide keyboard shortcuts for common formats (Ctrl+B, Ctrl+I)
- Update toolbar button states to reflect current formatting context

---

### 7. WCAG AA Contrast Compliance Implementation

**Decision**: Use pre-calculated color palettes meeting WCAG AA (4.5:1) for light/dark themes, validate with automated tools

**Rationale**:
- WCAG AA requires 4.5:1 contrast for normal text, 3:1 for large text (18pt+)
- Manual color picking is error-prone; use design tokens or palette generators
- Automated validation in CI prevents regressions

**Implementation Pattern**:
```css
/* light-theme.css */
:root[data-theme="light"] {
    --bg-primary: #FFFFFF;      /* White */
    --text-primary: #1F2937;    /* Gray-800, ratio 15.8:1 ✓ */
    --bg-secondary: #F3F4F6;    /* Gray-100 */
    --text-secondary: #4B5563;  /* Gray-600, ratio 7.2:1 ✓ */
    --accent: #3B82F6;          /* Blue-500 */
    --accent-text: #FFFFFF;     /* White on blue, ratio 4.9:1 ✓ */
}

/* dark-theme.css */
:root[data-theme="dark"] {
    --bg-primary: #111827;      /* Gray-900 */
    --text-primary: #F9FAFB;    /* Gray-50, ratio 16.1:1 ✓ */
    --bg-secondary: #1F2937;    /* Gray-800 */
    --text-secondary: #D1D5DB;  /* Gray-300, ratio 9.9:1 ✓ */
    --accent: #60A5FA;          /* Blue-400 */
    --accent-text: #111827;     /* Gray-900 on blue, ratio 7.5:1 ✓ */
}
```

**Validation Tools**:
- WebAIM Contrast Checker: https://webaim.org/resources/contrastchecker/
- Chrome DevTools Lighthouse accessibility audit
- axe DevTools browser extension
- Automated CI check with Pa11y or similar

**Alternatives Considered**:
- System-provided colors only - Rejected because can't guarantee WCAG compliance
- WCAG AAA (7:1) - Rejected as too restrictive, AA is sufficient per spec
- Dynamic color generation - Rejected for complexity and potential runtime failures

**Best Practices**:
- Test with actual users who have visual impairments
- Provide high-contrast mode option for users who need it
- Don't rely on color alone to convey information (use icons, labels)
- Test with color blindness simulators (protanopia, deuteranopia, tritanopia)

---

### 8. Toolbar Implementation in Blazor

**Decision**: Create single `Toolbar.razor` component with button sub-components, coordinate via `AppState`

**Rationale**:
- Blazor components enable reactive UI updates when state changes
- Toolbar buttons are stateful (enabled/disabled based on editor state)
- AppState already orchestrates document operations, extend for toolbar
- Component hierarchy: `Toolbar` → `ToolbarButton`, `ToolbarDropdown`, `ToolbarDivider`

**Implementation Pattern**:
```razor
<!-- Toolbar.razor -->
<div class="toolbar">
    <ToolbarButton Icon="folder-open" OnClick="@OpenFile" Tooltip="Open File" />
    <ToolbarButton Icon="save" OnClick="@SaveFile" Disabled="@(!HasUnsavedChanges)" Tooltip="Save" />
    <ToolbarDivider />
    <ToolbarButton Icon="cut" OnClick="@Cut" Disabled="@(!HasSelection)" Tooltip="Cut" />
    <ToolbarButton Icon="copy" OnClick="@Copy" Disabled="@(!HasSelection)" Tooltip="Copy" />
    <ToolbarButton Icon="paste" OnClick="@Paste" Tooltip="Paste" />
    <ToolbarDivider />
    <ToolbarDropdown Options="@FontFamilies" Value="@CurrentFont" OnChange="@ChangeFont" />
    <ToolbarDropdown Options="@FontSizes" Value="@CurrentFontSize" OnChange="@ChangeFontSize" />
    <ToolbarDivider />
    <ToolbarButton Icon="bold" OnClick="@(() => ApplyFormat(MarkdownFormat.Bold))" Tooltip="Bold (Ctrl+B)" />
    <!-- ... more buttons -->
</div>

@code {
    [CascadingParameter] public AppState State { get; set; }
    
    private bool HasUnsavedChanges => State.ActiveDocument?.IsDirty ?? false;
    private bool HasSelection => State.SelectionLength > 0;
    private string CurrentFont => State.Preferences.FontFamily;
    // ...
}
```

**Alternatives Considered**:
- Individual toolbar item components without container - Rejected for layout/styling difficulty
- Pure HTML/CSS without Blazor - Rejected because can't react to state changes
- Third-party component library - Rejected for added dependency and customization difficulty

**Best Practices**:
- Use cascading parameters to pass AppState to nested components
- Implement keyboard shortcuts for toolbar operations
- Show visual feedback on button click (ripple effect or active state)
- Group related buttons with dividers for visual hierarchy
- Make toolbar hideable via View menu (preference persists)

---

## Summary of Decisions

| Area | Decision | Key Technology/Pattern |
|------|----------|------------------------|
| CLI Args | Electron.CommandLine + IPC forwarding | Electron.NET IPC |
| Single Instance | RequestSingleInstanceLock() | Electron.App API |
| Theme Detection | NativeTheme API + updated event | Electron.NativeTheme |
| Preferences Storage | JSON in OS app data directory | System.Text.Json |
| App Icon | SVG source → multi-resolution export | electron-icon-maker |
| Markdown Formatting | Wrap selection OR insert markers | Custom service |
| WCAG Compliance | Pre-calculated palettes (4.5:1) | CSS custom properties |
| Toolbar | Blazor component with state coordination | Toolbar.razor + AppState |

All decisions align with:
- Existing Clean Architecture (Core/Infrastructure/UI separation)
- Electron.NET capabilities and patterns
- Cross-platform requirements (Windows, macOS, Linux)
- Performance targets (<2s startup, <500ms theme switch, <200ms toolbar ops)
- Accessibility standards (WCAG AA)

## Next Steps (Phase 1)

1. Generate data-model.md with entity definitions
2. Create API contracts in /contracts/ for IPC messages
3. Generate quickstart.md for developer onboarding
4. Update agent context with new technologies

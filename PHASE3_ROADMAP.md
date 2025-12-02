# Phase 3: Context Menu & Custom Dictionary - Implementation Roadmap

**Status**: Ready to Start  
**Estimated Duration**: 1-2 weeks  
**Prerequisite**: Phase 2 (Decorations Integration) ✅ COMPLETE

---

## Overview

Phase 3 adds user interaction capabilities:
1. Right-click context menu with spell check suggestions
2. "Add to Dictionary" functionality
3. Options dialog for spell check preferences
4. Custom dictionary persistence

---

## Phase 3a: Context Menu Suggestions

### Tasks

#### 1. Register Context Menu Handler in MonacoEditor
**File**: `src/TextEdit.UI/Components/MonacoEditor.razor.cs`

```csharp
// In OnAfterRenderAsync, after editor creation:
editor.onContextMenu((e) => {
    // Check if click is on misspelled word (check decorations)
    // Retrieve suggestions from decoration
    // Show custom context menu
});
```

#### 2. Create SpellCheckContextMenu Service
**File**: `src/TextEdit.Infrastructure/SpellChecking/SpellCheckContextMenuService.cs`

```csharp
public class SpellCheckContextMenuService
{
    /// <summary>
    /// Gets context menu items for a misspelled word at position
    /// </summary>
    public IReadOnlyList<ContextMenuItem> GetSuggestionsMenu(
        SpellCheckResult result,
        Func<string, Task> onSuggestionClick,
        Func<Task> onAddToDictionary);
}

public class ContextMenuItem
{
    public string Label { get; set; }
    public Func<Task> OnClick { get; set; }
    public bool IsSeparator { get; set; }
}
```

#### 3. Create Blazor ContextMenu Component
**File**: `src/TextEdit.UI/Components/Dialogs/SpellCheckContextMenu.razor`

```razor
@if (IsVisible)
{
    <div class="context-menu" style="left: @PositionX; top: @PositionY">
        @foreach (var item in Items)
        {
            @if (item.IsSeparator)
            {
                <div class="context-menu-separator"></div>
            }
            else
            {
                <button class="context-menu-item" @onclick="() => OnItemClick(item)">
                    @item.Label
                </button>
            }
        }
    </div>
}
```

#### 4. JavaScript Interop Enhancement
**File**: `src/TextEdit.App/wwwroot/js/monacoInterop.js`

```javascript
// Get decoration at cursor position
getDecorationAtPosition: function(elementId, position) {
    const entry = window.textEditMonaco.editors[elementId];
    if (!entry) return null;
    
    const decorations = entry.editor.getLineDecorations(position.lineNumber);
    return decorations.find(d => 
        d.range.startColumn <= position.column &&
        d.range.endColumn >= position.column
    );
}
```

### Testing
- Unit tests for SpellCheckContextMenuService
- Integration test: Click on misspelled word, verify menu appears
- Test "Add to Dictionary" click handling

---

## Phase 3b: Options Dialog Integration

### Tasks

#### 1. Create SpellCheckPreferencesDialog Component
**File**: `src/TextEdit.UI/Components/Dialogs/SpellCheckPreferencesDialog.razor`

```razor
<div class="spell-check-options">
    <label>
        <input type="checkbox" @bind="Preferences.IsEnabled" />
        Enable Spell Checking
    </label>
    
    <label>
        <input type="checkbox" @bind="Preferences.CheckCodeBlocks" />
        Check Code Blocks
    </label>
    
    <label>
        Max Suggestions:
        <input type="number" @bind="Preferences.MaxSuggestions" min="1" max="10" />
    </label>
    
    <label>
        Debounce Interval (ms):
        <input type="number" @bind="Preferences.DebounceIntervalMs" min="100" max="2000" />
    </label>
</div>
```

#### 2. Integrate with OptionsDialog
**File**: `src/TextEdit.UI/Components/OptionsDialog.razor`

- Add "Spell Check" tab or section
- Include SpellCheckPreferencesDialog
- Save/load preferences from AppState

#### 3. Update AppState to Persist Preferences
**File**: `src/TextEdit.UI/App/AppState.cs`

```csharp
public async Task SaveSpellCheckPreferencesAsync(SpellCheckPreferences prefs)
{
    Preferences.SpellCheckPreferences = prefs;
    await _persistenceService.SaveAsync();
    StateVersion++;
    Changed?.Invoke();
}
```

#### 4. Update PersistenceService
**File**: `src/TextEdit.Infrastructure/PersistenceService.cs`

- Include `SpellCheckPreferences` in session/preferences serialization
- Load on app startup
- Inject into SpellCheckingService

### Testing
- Unit tests for preferences dialog
- Integration test: Change preferences, verify spell checking behavior changes
- Persistence test: Save preferences, restart app, verify preferences restored

---

## Phase 3c: Custom Dictionary Persistence

### Tasks

#### 1. Enhance HunspellSpellChecker
**File**: `src/TextEdit.Infrastructure/SpellChecking/HunspellSpellChecker.cs`

```csharp
// Add custom dictionary file path
public async Task LoadCustomDictionaryAsync(string customDictPath)
{
    if (File.Exists(customDictPath))
    {
        var lines = await File.ReadAllLinesAsync(customDictPath);
        foreach (var word in lines)
        {
            if (!string.IsNullOrWhiteSpace(word))
                _customWords.Add(word.Trim().ToLower());
        }
    }
}

public async Task SaveCustomDictionaryAsync(string customDictPath)
{
    await File.WriteAllLinesAsync(customDictPath, _customWords.OrderBy(w => w));
}
```

#### 2. Create CustomDictionaryService
**File**: `src/TextEdit.Infrastructure/SpellChecking/CustomDictionaryService.cs`

```csharp
public class CustomDictionaryService
{
    private readonly string _customDictPath;
    private readonly IFileSystem _fileSystem;
    
    public CustomDictionaryService(IFileSystem fileSystem)
    {
        // Get path: %AppData%/TextEdit/CustomDictionary/default.dic
        _customDictPath = Path.Combine(
            GetAppDataPath(),
            "TextEdit",
            "CustomDictionary",
            "default.dic"
        );
    }
    
    public async Task AddWordAsync(string word)
    public async Task RemoveWordAsync(string word)
    public async Task<IReadOnlyList<string>> GetWordsAsync()
    public async Task SaveAsync()
}
```

#### 3. Integrate with SpellCheckingService
- Load custom dictionary on initialization
- Add words when "Add to Dictionary" clicked
- Persist changes automatically

#### 4. Options Dialog: Manage Custom Dictionary
**Feature**: List of custom words with Remove button
- View all custom dictionary words
- Remove words individually
- Clear all custom words

### Testing
- Unit tests for CustomDictionaryService
- Integration test: Add word, restart app, verify word is in custom dictionary
- Test remove/clear operations
- Test cross-platform path handling (Windows/Mac/Linux)

---

## Phase 3d: Performance Benchmarks

### Tasks

#### 1. Create Spell Check Benchmark
**File**: `tests/benchmarks/TextEdit.Benchmarks/SpellCheckBenchmarks.cs`

```csharp
[Benchmark]
public async Task CheckSpelling_10000Words()
{
    // Create 10,000 word document with 10% misspellings
    var text = GenerateTestDocument(10000);
    var results = await _spellChecker.CheckSpellingAsync(text);
}

[Benchmark]
public async Task CheckSpelling_100000Words()
{
    // Performance for very large documents
}
```

#### 2. Update BenchmarkDotNet Reports
- Add to `BenchmarkDotNet.Artifacts/results/`
- Target: <3s for 10,000 words
- Include memory allocation metrics

#### 3. Performance Optimization (if needed)
- Implement progressive spell checking (only visible viewport)
- Cache results for unchanged text regions
- Use worker threads for very large documents

---

## Implementation Order Recommendation

```
Phase 3a (Context Menu): Days 1-3
├─ SpellCheckContextMenuService
├─ ContextMenu Blazor component
├─ JavaScript interop enhancement
└─ Testing & validation

Phase 3b (Options Dialog): Days 4-5
├─ SpellCheckPreferencesDialog component
├─ OptionsDialog integration
├─ AppState preferences persistence
└─ Testing & validation

Phase 3c (Custom Dictionary): Days 6-7
├─ HunspellSpellChecker enhancement
├─ CustomDictionaryService
├─ "Add to Dictionary" flow
├─ Options dialog UI for management
└─ Testing & validation

Phase 3d (Benchmarks): Day 8+
├─ BenchmarkDotNet tests
├─ Performance profiling
└─ Optimization (if needed)
```

---

## Key Integration Points

### From Decoration to Context Menu
```
User right-clicks on misspelled word
    ↓
Monaco.onContextMenu(e) event
    ↓
Get cursor position from event
    ↓
Find decoration at position via JS
    ↓
Extract suggestions from decoration.options.suggestions
    ↓
SpellCheckContextMenuService.GetSuggestionsMenu()
    ↓
Show Blazor ContextMenu component
    ↓
User clicks suggestion
    ↓
JS: replaceSpellingError() or DotNet: AddWordToDictionary()
```

### Persistence Flow
```
App starts
    ↓
PersistenceService.LoadAsync()
    ↓
Load SpellCheckPreferences
    ↓
CustomDictionaryService.LoadAsync()
    ↓
HunspellSpellChecker.LoadCustomDictionaryAsync()
    ↓
Ready for spell checking ✅
```

---

## Browser & API Compatibility

- **Monaco Context Menu API**: 0.38.0+ ✅
- **File I/O**: .NET async file APIs ✅
- **Path Handling**: Cross-platform support ✅
- **JSON Serialization**: System.Text.Json ✅

---

## Quality Gates

- [ ] All new tests passing
- [ ] No regression in existing tests (should remain 337+ passing)
- [ ] Build succeeds with no warnings
- [ ] Code review approved
- [ ] Accessibility testing (WCAG 2.1 AA)
- [ ] Cross-platform testing (Windows/Mac/Linux)
- [ ] Performance benchmark < 3s

---

## Documentation Updates

- [ ] Update SPELL_CHECK_PROGRESS.md with Phase 3 completion
- [ ] Update tasks.md to 100% complete
- [ ] Update FEATURE_STATUS_REPORT.md to "✅ COMPLETE"
- [ ] Create Phase 3 completion summary

---

## Merge & Release

**After Phase 3 completion**:
1. Merge `003-v1-2-spell-checker` → `main`
2. Create GitHub release for v1.2
3. Update CHANGELOG.md with spell checking feature
4. Begin v1.3 planning

---

**Ready to Start Phase 3** ✅  
**All prerequisites complete** ✅  
**Expected completion**: 1-2 weeks ⏱️

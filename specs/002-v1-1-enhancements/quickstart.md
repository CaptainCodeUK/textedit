# Quickstart Guide: Scrappy Text Editor v1.1 Development

**Feature Branch**: `002-v1-1-enhancements`  
**Date**: 2025-10-30  
**Audience**: Developers implementing v1.1 features

## Overview

This guide helps developers quickly onboard to v1.1 implementation. Read this FIRST before starting any tasks.

## Prerequisites

- .NET 8 SDK installed
- Node.js 18+ installed (for Electron.NET)
- Git configured
- VS Code or Visual Studio 2022+
- Familiarity with Blazor Server and Clean Architecture

## Key Documents (Read in Order)

1. **[spec.md](./spec.md)** - Complete feature specification with user stories, FRs, and success criteria
2. **[plan.md](./plan.md)** - Implementation plan with architecture decisions and project structure
3. **[research.md](./research.md)** - Technical research decisions and best practices
4. **[data-model.md](./data-model.md)** - Entity definitions, relationships, and state transitions
5. **[contracts/](./contracts/)** - IPC and JSON schema contracts

## Quick Architecture Overview

```
Electron.NET Shell (Native)
    ↓ IPC
Blazor Server UI (Components)
    ↓ Uses
AppState Orchestrator (Coordination)
    ↓ Calls
Core Services (Business Logic)
    ↓ Uses
Infrastructure (File I/O, Persistence)
```

**Key Pattern**: All state flows through `AppState`. Components subscribe to `AppState.Changed` event.

## Project Structure (Where Things Go)

| Layer | Location | What Goes Here |
|-------|----------|----------------|
| **Native Shell** | `src/TextEdit.App/` | Electron menus, CLI args parsing, IPC setup |
| **UI Components** | `src/TextEdit.UI/Components/` | Blazor components (Toolbar, Options, About, etc.) |
| **State Management** | `src/TextEdit.UI/App/AppState.cs` | Orchestrate all operations, fire change events |
| **Services** | `src/TextEdit.UI/Services/` | Theme management, markdown formatting |
| **Core Domain** | `src/TextEdit.Core/` | Pure business logic, entities, abstractions |
| **Infrastructure** | `src/TextEdit.Infrastructure/` | File I/O, preferences persistence, OS integration |
| **Unit Tests** | `tests/unit/` | Test individual classes in isolation |
| **Integration Tests** | `tests/integration/` | Test cross-layer interactions |
| **Contract Tests** | `tests/contract/` | Validate IPC messages and JSON schemas |

## Development Workflow

### 1. Setup Development Environment

```bash
# Clone and checkout feature branch
git checkout 002-v1-1-enhancements

# Restore dependencies
dotnet restore textedit.sln

# Run existing tests to ensure baseline works
./scripts/dev.fish test

# Start app in development mode
./scripts/dev.fish run
```

### 2. Implementing a New Feature

**Example**: Adding the Options Dialog

1. **Read the spec** - Find relevant user stories (e.g., User Story 3 - Theme Customization)
2. **Check data model** - Review `UserPreferences` entity and validation rules
3. **Review contracts** - Check if IPC contracts needed (theme-changed.md)
4. **Write tests first** (TDD):
   ```bash
   # Create test file
   touch tests/unit/TextEdit.UI.Tests/Components/OptionsDialogTests.cs
   
   # Write failing tests based on acceptance scenarios
   # Run tests - should fail
   dotnet test
   ```
5. **Implement component**:
   ```bash
   # Create component files
   touch src/TextEdit.UI/Components/OptionsDialog.razor
   touch src/TextEdit.UI/Components/OptionsDialog.razor.cs
   
   # Implement according to spec
   # Run tests - should pass
   dotnet test
   ```
6. **Integrate with AppState**:
   - Add methods to `AppState` for showing dialog
   - Wire up events for preference changes
   - Test integration
7. **Manual testing**:
   ```bash
   ./scripts/dev.fish run
   # Test all acceptance scenarios from spec
   ```

### 3. Testing Strategy

**Test Pyramid** (bottom to top):
1. **Unit Tests** (70%) - Fast, isolated, mock dependencies
2. **Integration Tests** (25%) - Cross-layer, real dependencies
3. **Contract Tests** (5%) - IPC/JSON validation

**Coverage Requirements**:
- Overall: 65% minimum (enforced by Directory.Build.props)
- Core layer: 92%+ target
- Infrastructure: 52%+ target

**Running Tests**:
```bash
# All tests
./scripts/dev.fish test

# Unit tests only
./scripts/dev.fish test:unit

# With coverage
./scripts/dev.fish test:coverage
```

## Common Patterns

### 1. Adding a New Preference

```csharp
// 1. Add to UserPreferences (Core)
public class UserPreferences {
    public bool MyNewSetting { get; set; } = false;
}

// 2. Update JSON schema in contracts/preferences-schema.md

// 3. Add UI control in OptionsDialog.razor
<label>
    <input type="checkbox" @bind="Preferences.MyNewSetting" />
    My New Setting
</label>

// 4. Handle change in AppState
public async Task SavePreferencesAsync() {
    await _preferencesRepo.SaveAsync(Preferences);
    await NotifyChanged();
}

// 5. Test persistence
[Fact]
public async Task Preference_Persists_Across_Sessions() {
    // Arrange
    var prefs = new UserPreferences { MyNewSetting = true };
    await _repo.SaveAsync(prefs);
    
    // Act
    var loaded = await _repo.LoadAsync();
    
    // Assert
    Assert.True(loaded.MyNewSetting);
}
```

### 2. Adding a New IPC Message

```csharp
// 1. Define contract in contracts/my-message.md

// 2. Sender (Electron side - ElectronHost.cs)
var message = new { data = "value" };
await Electron.IpcMain.Send("my-channel", message);

// 3. Receiver (Blazor side - IpcBridge.cs)
public void RegisterHandlers() {
    Electron.IpcMain.On("my-channel", async (args) => {
        var message = JsonSerializer.Deserialize<MyMessage>(args);
        await _appState.HandleMessage(message);
    });
}

// 4. Contract test
[Fact]
public void Message_Serializes_Correctly() {
    var msg = new MyMessage { Data = "value" };
    var json = JsonSerializer.Serialize(msg);
    var deserialized = JsonSerializer.Deserialize<MyMessage>(json);
    Assert.Equal(msg.Data, deserialized.Data);
}
```

### 3. Adding a Toolbar Button

```razor
<!-- Toolbar.razor -->
<ToolbarButton 
    Icon="my-icon" 
    OnClick="@MyAction" 
    Disabled="@(!CanPerformAction)"
    Tooltip="Do My Action (Ctrl+M)" />

@code {
    [CascadingParameter] 
    public AppState State { get; set; }
    
    private bool CanPerformAction => State.ActiveDocument != null;
    
    private async Task MyAction() {
        await State.PerformMyActionAsync();
    }
    
    protected override void OnInitialized() {
        State.Changed += OnStateChanged;
        EditorCommandHub.MyAction = MyAction;
    }
}
```

## Debugging Tips

### Electron Process Debugging

```bash
# Find .NET process ID
./scripts/find-dotnet-pid.fish

# Use VS Code "Launch Electron (Auto PID)" config
# Enter PID when prompted
```

### Blazor Component Debugging

- Use Chrome DevTools (F12 in Electron window)
- Set breakpoints in .razor.cs files
- Use `@bind-value:event="oninput"` for live updates
- Check `AppState.StateVersion` to verify change events firing

### Preference File Debugging

```bash
# View current preferences (Linux/macOS)
cat ~/.config/Scrappy/preferences.json

# Windows
type %AppData%\Scrappy\preferences.json

# Reset to defaults (delete file)
rm ~/.config/Scrappy/preferences.json  # Linux/macOS
del %AppData%\Scrappy\preferences.json  # Windows
```

## Performance Profiling

### BenchmarkDotNet

```bash
cd tests/benchmarks/TextEdit.Benchmarks
dotnet run -c Release

# Results in BenchmarkDotNet.Artifacts/results/
```

### Theme Switch Performance

```csharp
// Add timing in ThemeManager
var sw = Stopwatch.StartNew();
ApplyTheme(newTheme);
sw.Stop();
if (sw.ElapsedMilliseconds > 500) {
    _logger.LogWarning("Theme switch took {Ms}ms", sw.ElapsedMilliseconds);
}
```

## Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| Electron window doesn't open | Check `electronize start` output for errors; ensure Node.js 18+ |
| Tests fail with "cannot find assembly" | Run `dotnet restore` and `dotnet build` |
| Preferences don't persist | Check file permissions in app data directory |
| IPC messages not received | Verify channel name matches in sender and receiver |
| Theme doesn't update | Ensure `AppState.Changed` event fires and components subscribe |
| Coverage below 65% | Add unit tests for new code; check `Directory.Build.props` threshold |

## Resources

- **Electron.NET Docs**: https://github.com/ElectronNET/Electron.NET/wiki
- **Blazor Docs**: https://learn.microsoft.com/en-us/aspnet/core/blazor/
- **Markdig**: https://github.com/xoofx/markdig
- **WCAG Contrast Checker**: https://webaim.org/resources/contrastchecker/
- **JSON Schema Validator**: https://www.jsonschemavalidator.net/

## Getting Help

1. **Check spec first** - Most questions answered in spec.md
2. **Review research** - Technical decisions documented in research.md
3. **Check existing code** - Similar patterns already implemented
4. **Ask team** - Post question with context (what you tried, error messages)

## Next Steps

1. ✅ Read this quickstart
2. ✅ Review spec.md user stories and FRs
3. ✅ Scan research.md decisions
4. ✅ Familiarize with data-model.md entities
5. ⏭️ Check tasks.md for assigned work (created by `/speckit.tasks` command)
6. ⏭️ Start implementing with test-first approach

**Remember**: 
- Test-first development (TDD)
- 65% coverage minimum
- Constitution compliance (code quality, accessibility, performance)
- All changes flow through AppState
- IPC contracts for Electron ↔ Blazor communication

Happy coding! 🐶📝

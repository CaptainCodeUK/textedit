````markdown
# Implementation Plan: Scrappy Text Editor v1.1 Enhancements

**Branch**: `002-v1-1-enhancements` | **Date**: 2025-10-30 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-v1-1-enhancements/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

This feature implements a comprehensive set of user-facing enhancements to transform "TextEdit" into "Scrappy Text Editor" v1.1, including: command-line file opening with single-instance enforcement, complete rebranding with puppy-themed application icon, dark/light/system theme support, user preferences management (file extensions, logging, font settings), rich toolbar with file operations and markdown formatting, menu icons, enhanced visual styling with WCAG AA compliance, and improved title bar with filename display and dirty indicators. The technical approach leverages existing Electron.NET infrastructure for native integration, extends the current Blazor Server UI with new components and theming system, implements JSON-based preferences persistence in OS application data directories, and maintains the established Clean Architecture pattern across Core/Infrastructure/UI layers.

## Technical Context

**Language/Version**: C# 12 / .NET 8.0  
**Primary Dependencies**: Electron.NET 23.6.2, ASP.NET Core (Blazor Server), Markdig (markdown rendering)  
**Storage**: File system (JSON for preferences in OS app data directories, existing session persistence)  
**Testing**: xUnit, Moq, Coverlet (65% line coverage minimum, 92%+ for Core layer)  
**Target Platform**: Cross-platform desktop (Windows, macOS, Linux via Electron.NET)  
**Project Type**: Desktop application (Blazor Server hosted in Electron shell)  
**Performance Goals**: <2s startup, <500ms theme switching, <200ms toolbar operations, <100ms font changes  
**Constraints**: WCAG AA contrast (4.5:1), single-instance enforcement, no UI blocking during CLI error handling  
**Scale/Scope**: 8 user stories, 68 functional requirements, 17 success criteria, 4 new services, 10+ new UI components

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Verify alignment with TextEdit Constitution principles:

- [x] **Code Quality Standards**: Linting/static analysis tools identified for language stack (C# analyzer, nullable reference types enabled)
- [x] **Code Quality Standards**: Code review process defined with clear acceptance criteria (PR review required, spec/task references)
- [x] **Code Quality Standards**: Documentation requirements specified for public APIs (XML comments for public interfaces/services)
- [x] **Testing Standards**: Test coverage targets defined (65% minimum overall, 92%+ for Core layer per Directory.Build.props)
- [x] **Testing Standards**: Unit, integration, and contract test requirements identified (xUnit tests for all new services/components)
- [x] **Testing Standards**: Test-first development workflow confirmed for new features (acceptance scenarios guide test creation)
- [x] **Testing Standards**: CI pipeline configuration includes all required tests (existing test task infrastructure)
- [x] **UX Consistency**: Design system compliance verified (extends existing Blazor component patterns, uses Tailwind CSS)
- [x] **UX Consistency**: Accessibility requirements (WCAG 2.1 AA) confirmed (FR-025, FR-026, FR-061 specify 4.5:1 contrast, keyboard nav, screen reader support)
- [x] **UX Consistency**: Responsive design targets defined (desktop application, single window, no responsive breakpoints needed)
- [x] **UX Consistency**: Error handling and loading states specified (FR-004a non-blocking CLI errors, FR-053 disabled toolbar states, loading indicators in spec)
- [x] **Performance Requirements**: Response time targets defined (FR startup <2s, theme switch <500ms, toolbar <200ms, font <100ms per spec)
- [x] **Performance Requirements**: Resource efficiency constraints specified (no specific memory limits but maintains existing autosave/session behavior)
- [x] **Performance Requirements**: Load testing requirements identified (N/A - single-user desktop app, no concurrent users)
- [x] **Performance Requirements**: Monitoring and alerting strategy defined (N/A - desktop app, relies on local logging per FR-037 to FR-044)
- [x] **Performance Requirements**: Performance budget verified (N/A - desktop app, not web, no transfer size limits)

*All gates passed. No violations requiring justification.*

## Project Structure

### Documentation (this feature)

```text
specs/002-v1-1-enhancements/
├── spec.md              # Feature specification (✅ complete)
├── plan.md              # This file (/speckit.plan command output, ✅ complete)
├── research.md          # Phase 0 output (✅ complete - 8 technical decisions)
├── data-model.md        # Phase 1 output (✅ complete - 8 entities, relationships, state transitions)
├── quickstart.md        # Phase 1 output (✅ complete - developer onboarding guide)
├── contracts/           # Phase 1 output (✅ complete - 3 IPC/schema contracts)
│   ├── cli-file-args.md        # IPC contract for command-line file arguments
│   ├── theme-changed.md        # IPC contract for OS theme change notifications
│   └── preferences-schema.md   # JSON schema for user preferences
└── tasks.md             # Phase 2 output (/speckit.tasks command - pending)
```

### Source Code (repository root)

```text
src/
├── TextEdit.App/           # Electron.NET host, native integration
│   ├── ElectronHost.cs    # (MODIFY) Add CLI args parsing, single-instance, menu icons
│   ├── Program.cs         # (MODIFY) Pass CLI args to Blazor
│   ├── Startup.cs         # (MODIFY) Register new services (PreferencesService, ThemeService, etc.)
│   └── wwwroot/           # (ADD) New app icons (puppy theme, multi-resolution)
│
├── TextEdit.Core/          # Pure domain logic, zero dependencies
│   ├── Documents/         # (EXISTING) Document model
│   ├── Editing/           # (EXISTING) Undo/redo
│   └── Preferences/       # (NEW) UserPreferences model, PreferencesService interface
│
├── TextEdit.Infrastructure/ # External concerns (file I/O, IPC)
│   ├── FileSystem/        # (EXISTING) File operations
│   ├── Persistence/       # (MODIFY) Add PreferencesRepository for JSON storage
│   ├── Ipc/               # (MODIFY) Extend IpcBridge for CLI args, theme detection
│   └── Themes/            # (NEW) ThemeDetectionService for OS theme watching
│
├── TextEdit.Markdown/      # (MODIFY) Extend for theme-aware rendering
│   └── MarkdownRenderer.cs
│
└── TextEdit.UI/            # Blazor components, AppState orchestrator
    ├── App/               # (EXISTING) AppState
    │   └── AppState.cs   # (MODIFY) Add preferences, theme, toolbar state management
    ├── Components/        # (EXISTING) UI components
    │   ├── TextEditor.razor       # (MODIFY) Apply theme, font preferences
    │   ├── TabStrip.razor         # (EXISTING) Tab display
    │   ├── StatusBar.razor        # (EXISTING) Status display
    │   ├── AboutDialog.razor      # (NEW) About box with version/tech info
    │   ├── OptionsDialog.razor    # (NEW) Theme/extensions/font/logging options
    │   ├── Toolbar.razor          # (NEW) File/edit/markdown operations
    │   └── CliErrorSummary.razor  # (NEW) Non-blocking post-startup error summary
    ├── Services/          # (EXISTING) DialogService, EditorCommandHub
    │   └── ThemeManager.cs # (NEW) Apply theme to UI components
    └── wwwroot/           # (MODIFY) Add theme CSS, icon assets

tests/
├── unit/
│   ├── TextEdit.Core.Tests/           # (ADD) Tests for PreferencesService, UserPreferences model
│   ├── TextEdit.Infrastructure.Tests/ # (ADD) Tests for PreferencesRepository, ThemeDetectionService
│   └── TextEdit.UI.Tests/             # (ADD) Tests for ThemeManager, new dialogs/toolbar
├── integration/
│   └── TextEdit.Integration.Tests/    # (ADD) CLI args parsing, preferences persistence, theme switching
└── contract/
    └── TextEdit.IPC.Tests/            # (ADD) CLI args IPC contract, preferences JSON schema
```

**Structure Decision**: This is a single desktop application project using Clean Architecture. The existing 5-project structure (App, Core, Infrastructure, Markdown, UI) is maintained. New functionality is added through:
1. **Core layer** - New `Preferences/` folder for domain models and abstractions
2. **Infrastructure layer** - New `Themes/` folder for OS integration, extended `Persistence/` for JSON preferences
3. **UI layer** - New components for Options, About, Toolbar, CLI error summary; extended `AppState` for preferences/theme coordination
4. **App layer** - Enhanced `ElectronHost` for CLI args and menu icons, updated `Startup` for DI registration

No new projects needed. All enhancements fit within existing architectural boundaries.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

*No violations. All constitution gates passed.*

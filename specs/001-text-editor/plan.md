# Implementation Plan: Text Editor Application

**Branch**: `001-text-editor` | **Date**: 2025-10-24 | **Spec**: [/specs/001-text-editor/spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-text-editor/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Build a cross-platform desktop text editor with tabs, standard menus (File/Edit/View),
word-wrap toggle, status bar, per-tab dirty tracking and undo/redo, markdown preview,
and robust session persistence for unsaved work. Implemented with .NET 8 (C# 12),
Blazor (component UI), TailwindCSS for styling, hosted in an Electron shell via
Electron.NET to ship Windows/macOS/Linux binaries. Autosave and recovery protect user
data; file-watch detects external edits; performance budgets ensure responsive UX.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: .NET 8 (C# 12), ASP.NET Core 8, Blazor (Server-in-Electron)  
**Primary Dependencies**: ElectronNET.API, Markdig (Markdown), TailwindCSS, 
Microsoft.Extensions.* (DI/Logging/Options), System.IO (FileSystemWatcher), 
System.Text.Json; Tests: xUnit, bUnit (Blazor), Microsoft.Playwright, coverlet, FluentAssertions  
**Storage**: Local filesystem; session autosave + crash-recovery in OS temp/app data dir  
**Testing**: xUnit (unit), bUnit (component), Playwright (.NET) for UI/E2E; contract tests for IPC schemas  
**Target Platform**: Windows 10+, macOS 12+, Linux (Ubuntu 22.04+), x64/arm64  
**Project Type**: Desktop app (Electron shell + ASP.NET Core host + Blazor UI)  
**Performance Goals**: 
- UI interactions (typing, undo/redo) p95 < 100ms
- Open files ≤1MB in < 1s; markdown render ≤500ms for files ≤100KB
- App close with ≤10 unsaved tabs < 2s with no blocking save dialogs
- Memory budget ≤ 200MB per session (per constitution)
**Constraints**: Offline-capable; cross-platform file system behaviors; no silent data loss;
p95 UX latency targets above; performance budget for assets (Tailwind purged CSS)  
**Scale/Scope**: 10–20 concurrent tabs; files up to 10MB editable; 1 window, <10 screens

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Verify alignment with TextEdit Constitution principles:

- [x] **Code Quality Standards**: dotnet analyzers + StyleCop/EditorConfig; PR review required
- [x] **Code Quality Standards**: Public services/components documented; complexity justified
- [x] **Testing Standards**: Coverage targets: 80% overall, 95% critical (autosave, persistence)
- [x] **Testing Standards**: Unit, integration, contract (IPC schemas) identified
- [x] **Testing Standards**: Test-first for new features; CI enforces all tests
- [x] **UX Consistency**: Tailwind design tokens/components; design review checklist
- [x] **UX Consistency**: WCAG 2.1 AA (keyboard nav, focus states, contrast) verified in E2E
- [x] **UX Consistency**: Responsive at 320/768/1920 where applicable
- [x] **UX Consistency**: Error and loading states defined (status bar/banners/modals)
- [x] **Performance Requirements**: UI latency, render, close-time targets defined
- [x] **Performance Requirements**: Memory budget ≤200MB; large file strategy defined
- [x] **Performance Requirements**: Not a multi-user server—load testing N/A; micro-bench UI
- [x] **Performance Requirements**: Local telemetry + logs; perf probes in critical flows
- [x] **Performance Requirements**: Purged Tailwind; JS bundle minimized (Blazor static assets)

*If any gates cannot be met, document justification in Complexity Tracking section*

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
textedit.sln
src/
├── TextEdit.App/                 # ASP.NET Core host + Electron.NET bootstrap
│   ├── Program.cs
│   ├── Startup.cs
│   ├── ElectronHost.cs           # Electron window lifecycle
│   └── wwwroot/                  # Static assets (built Tailwind CSS, icons)
├── TextEdit.UI/                  # Blazor components (pages, layouts)
│   ├── Components/
│   ├── Pages/
│   ├── Styles/                   # Tailwind input CSS
│   └── tailwind.config.cjs
├── TextEdit.Core/                # Domain models & services
│   ├── Documents/
│   ├── Persistence/
│   └── Editing/
├── TextEdit.Infrastructure/      # File IO, autosave, IPC bridges, watchers
│   ├── FileSystem/
│   ├── Autosave/
│   └── Ipc/
└── TextEdit.Markdown/           # Markdown rendering (Markdig-based)

tests/
├── unit/
│   └── TextEdit.Core.Tests/
├── integration/
│   └── TextEdit.App.Tests/       # Playwright-driven Electron UI flows
└── contract/
  └── TextEdit.IPC.Tests/      # IPC schema compatibility tests
```

**Structure Decision**: Multi-project .NET solution separates UI, domain, infra, and
renderer concerns. Tests organized by unit/integration/contract. Electron.NET hosts the
ASP.NET Core + Blazor app; Tailwind builds into `wwwroot`.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Additional projects (Markdown, Infra) | Clear separation of concerns and testability | Monolith increases coupling; harder to test and swap implementations |
| Electron + Blazor stack | Cross-platform desktop with web UI productivity | MAUI/Avalonia viable; Electron chosen for Tailwind+Blazor synergy and packaging |

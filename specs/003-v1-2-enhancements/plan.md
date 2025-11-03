# Implementation Plan: Scrappy Text Editor v1.2 Enhancements

**Branch**: `003-v1-2-enhancements` | **Date**: 3 November 2025 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-v1-2-enhancements/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

This plan covers the implementation of v1.2 enhancements for Scrappy Text Editor, including:
- Find/find and replace in a tab
- Simple spell checking with built-in and custom dictionaries
- Window size, position, and state persistence
- Automatic updates (Squirrel or similar)
- Automated GitHub Actions for release builds

Technical approach leverages the existing .NET 8, Blazor Server, Electron.NET architecture, extending core and UI services, and integrating with CI/CD for packaging and updates.

## Technical Context

**Language/Version**: C# 12, .NET 8.0
**Primary Dependencies**: Electron.NET 23.6.2, Blazor Server, Markdig, Squirrel (for auto-update), GitHub Actions
**Storage**: JSON files in OS app data directories (preferences, session, custom dictionary)
**Testing**: xUnit, NSubstitute, FluentAssertions, BenchmarkDotNet
**Target Platform**: Windows, macOS, Linux (desktop, Electron shell)
**Project Type**: Desktop (Electron.NET host, Blazor Server UI)
**Performance Goals**: Startup <2s, shutdown <2s, markdown render <5ms cold/<50μs cached, file open <2s for <10MB, spell check <3s for 10k words
**Constraints**: <200MB RAM per session, <5MB added to app size, <200ms added to startup, accessibility (WCAG 2.1 AA)
**Scale/Scope**: 10k+ users, 100k+ word documents, 100+ concurrent update downloads

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Verify alignment with TextEdit Constitution principles:

- [x] **Code Quality Standards**: Linting/static analysis via .NET analyzers, code review via PRs, XML docs for public APIs
- [x] **Testing Standards**: xUnit tests, 80%+ coverage, test-first for new features, CI runs all tests
- [x] **UX Consistency**: Blazor components use design system, accessibility (WCAG 2.1 AA), responsive layouts, error/loading states
- [x] **Performance Requirements**: Benchmarks for core services, startup/shutdown/markdown/file open targets, resource constraints, monitoring via logs/metrics


## Project Structure

### Documentation (this feature)

```text
specs/003-v1-2-enhancements/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── TextEdit.Core/           # Domain models, business logic, abstractions
├── TextEdit.Infrastructure/ # File I/O, persistence, autosave, IPC
├── TextEdit.Markdown/       # Markdown rendering, caching
├── TextEdit.UI/             # Blazor components, AppState, Dialogs, CommandHub
├── TextEdit.App/            # Electron.NET host, menus, IPC handlers

directories captured above]
├── unit/
├── integration/
├── contract/
├── benchmarks/
```

**Structure Decision**: Use existing Clean Architecture layout, extending Core, Infrastructure, UI, and App projects as needed for v1.2 features. All new contracts and data models will be documented in the feature spec directory.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |

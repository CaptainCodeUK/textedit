# v1.1 Implementation Plan — Scrappy Text Editor

This plan turns the approved v1.1 specification into concrete delivery steps across Identity/Branding, CLI file opening, Options, Toolbar, Styling/Accessibility, and Title Bar updates. It follows the repo’s Clean Architecture and existing patterns (AppState orchestration, EditorCommandHub delegates, IPC contracts, session persistence).

## Scope and success
- In scope: Everything defined in `spec.md` for v1.1, including clarifications for markdown formatting (wrap/insert) and CLI invalid path summary (non‑blocking, simple messages).
- Out of scope: New editor engines, multi‑window support, plugin systems, or markdown preview feature changes.
- Success: All acceptance scenarios pass; 65%+ total coverage; no regressions to session persistence or startup/shutdown SLAs.

## Assumptions
- Single-window, single-instance app remains.
- Preferences persist under existing user prefs store (Infrastructure.PersistenceService).
- Electron.NET remains the host; Electron menus → EditorCommandHub → AppState remains the command flow.

## Architecture impact (by area)
- Identity/Branding
  - App name: Update UI strings, window title sources, and package name imagery; do not change assembly names unless required.
  - App icon: Provide multi-res assets; wire in `electron.manifest.json` and platform-specific fields.
  - About dialog: New Blazor dialog with version, author/org, license, technologies; open via menu/toolbar.
- CLI open + invalid-path summary
  - Ensure single-instance enforcement; marshal args from subsequent invocations to primary via IPC.
  - On startup (or when receiving args), try open-valid paths; collect invalids with simple reasons.
  - After startup, show a non-blocking summary UI listing skipped items and reasons.
- Options (Theme/Extensions/Logging)
  - Theme: Light/Dark/System (follow system); respect High Contrast; persist choice.
  - Extensions: Manage list and default associations; persist.
  - Logging toggle: Enable/disable Serilog rolling sink with rotation; reflect in UI; persist.
- Toolbar
  - Buttons: Open/Save, Cut/Copy/Paste; Font name/size (global, persisted); Markdown (H1, H2, Bold, Italic).
  - Hook all to existing AppState operations or small additions; disable appropriately for read-only/large files.
  - Markdown behaviors: wrap selection; when no selection, insert paired markers and place caret between.
- Styling/Accessibility
  - Introduce colorful yet accessible theme tokens; use system colors where appropriate; ensure WCAG AA.
  - Menu icons and toolbar icons added; ensure contrast and theming.
- Title bar
  - Set window title to current tab’s filename + dirty indicator; update on changes.

## Phases and milestones
1) Identity/Branding (rename, icon, About)
2) CLI file open + invalid-path summary UI
3) Options UI (Theme System/Light/Dark; Extensions; Logging toggle)
4) Toolbar (file/clipboard, font global + persistence, markdown behaviors)
5) Menu icons and overall styling pass (AA contrast)
6) Title bar filename + dirty state
7) Tests (unit, contract, integration) and coverage hardening
8) Performance/QA sweep (startup/shutdown, markdown cache sanity)

## Detailed design notes
- Single-instance + CLI
  - Primary instance detection via Electron (app.requestSingleInstanceLock). On second-instance, forward args to first via Electron.NET bridge; AppState opens valid files and accumulates invalids.
  - Summary surface: Blazor non-modal panel/toast listing “skipped” with simple reasons (e.g., “File not found”, “Permission denied”, “Unreadable”).
  - Contract tests: Add cases for valid-only, invalid-only, and mixed.
- About dialog
  - Component under UI/DialogService; content from assembly metadata + appsettings/license; include current year.
- App icon + menu icons
  - Place icon assets under `src/TextEdit.App/wwwroot/icons/` (multi-res). Update `electron.manifest.json` and platform fields. Use icons in Electron menu items.
- Options UI
  - Preferences page/sheet bound to AppState.UserPreferences with immediate apply + persist.
  - Theme: integrate with Electron nativeTheme for “Follow System”; fallback to CSS variables; respect High Contrast.
  - Logging: Toggle changes Serilog configuration at runtime or short restart prompt if needed; ensure low overhead when disabled.
- Toolbar
  - New Blazor component + lightweight service for toolbar state; wire actions to AppState and EditorCommandHub.
  - Font name/size: applies globally to editor component; persist in preferences; changes re-render via StateVersion.
  - Markdown commands: operate on EditorState selection; if no selection, insert paired markers and place caret between.
- Title bar
  - AppState emits title updates; ElectronHost sets BrowserWindow title. “•” or similar marker for dirty.

## Risks and mitigations
- Runtime theming regressions → Add visual regression snapshots for key states; stick to tokenized CSS vars.
- Logging toggle runtime reconfig → If hot-reload is unstable, require confirmation and perform quick restart.
- CLI on Linux packaging nuances → Validate `electronize start/build` command-line delivery early.
- Toolbar caret/selection edge cases → Add unit tests for empty selection, multi-line, and nested markers.

## Definition of Done
- All spec acceptance scenarios pass (including clarified markdown and CLI summary behaviors).
- Unit/contract/integration tests added; total coverage ≥ 65%.
- Startup < 2s, Shutdown < 2s validated.
- Accessibility checks: keyboard nav, screen reader labels, AA contrast.
- Packaging: icon present, name “Scrappy Text Editor” displayed, menu icons load, title bar behavior verified.

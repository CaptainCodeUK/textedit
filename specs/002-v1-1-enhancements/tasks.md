# v1.1 Tasks — Scrappy Text Editor

Status legend: [ ] not started, [~] in progress, [x] done

## Phase 1 — Identity/Branding
- [ ] T-001: Update app display name to “Scrappy Text Editor” in UI strings and window title sources (no assembly rename)
- [ ] T-002: Add multi-resolution app icon assets under `src/TextEdit.App/wwwroot/icons/`
- [ ] T-003: Wire icons into `electron.manifest.json` and platform-specific icon fields
- [ ] T-004: Implement About dialog (Blazor) with version, author/org, license, technologies; wire to menu and toolbar

## Phase 2 — CLI open + invalid-path summary
- [ ] T-010: Enforce single-instance; marshal args from second instances to primary via Electron bridge
- [ ] T-011: Parse CLI args and attempt to open valid files into tabs in given order
- [ ] T-012: Collect invalid paths and map to simple reasons (File not found, Permission denied, Unreadable)
- [ ] T-013: Implement non-blocking post-startup summary UI listing skipped files + reasons
- [ ] T-014: Contract tests for valid-only, invalid-only, and mixed scenarios

## Phase 3 — Options UI (Theme, Extensions, Logging)
- [ ] T-020: Create Options page/sheet bound to `AppState.UserPreferences`
- [ ] T-021: Theme selection (Light/Dark/System [follow system]); respect High Contrast; persist
- [ ] T-022: File extension associations management UI; persist defaults
- [ ] T-023: Logging toggle in options; enable/disable Serilog rolling sink with rotation; persist
- [ ] T-024: Unit tests for preferences persistence and theme switching

## Phase 4 — Toolbar
- [ ] T-030: Add Toolbar component with Open/Save and Cut/Copy/Paste actions
- [ ] T-031: Font name and size controls (global, persisted); bind to editor; re-render via StateVersion
- [ ] T-032: Markdown buttons (H1, H2, Bold, Italic); wrap selection or insert paired markers at caret with caret placement between
- [ ] T-033: Disable/guard commands in read-only/large-file scenarios
- [ ] T-034: Unit tests for markdown behaviors (selection/no-selection, multi-line)

## Phase 5 — Menu icons + Styling
- [ ] T-040: Add icon assets for menus/toolbar; integrate with Electron menu items
- [ ] T-041: Theming tokens using system colors; ensure WCAG AA across Light/Dark/System
- [ ] T-042: Accessibility checks (keyboard nav, screen reader, focus states)

## Phase 6 — Title bar filename + dirty
- [ ] T-050: AppState emits title updates reflecting current tab filename and dirty marker
- [ ] T-051: ElectronHost updates BrowserWindow title on state changes
- [ ] T-052: Integration tests for title updates on open/save/dirty/close

## Phase 7 — Tests and coverage
- [ ] T-060: Unit tests for CLI argument handling and error mapping
- [ ] T-061: Contract tests for IPC/menu/toolbar commands
- [ ] T-062: Integration tests for Options interactions and theme switching
- [ ] T-063: Ensure 65%+ total coverage; fix or add tests as needed

## Phase 8 — Performance/QA
- [ ] T-070: Validate startup < 2s, shutdown < 2s with v1.1 features enabled
- [ ] T-071: Sanity-check MarkdownRenderer caching with toolbar usage
- [ ] T-072: Manual UX sweep; verify non-blocking CLI summary and About dialog content

## Cross-cutting
- [ ] T-080: Update docs/README and specs contracts if any new IPC surfaces are introduced
- [ ] T-081: Verify packaging via `electronize start/build` on Linux with new icons and name

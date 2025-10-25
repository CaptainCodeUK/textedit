# Implementation Tasks: Text Editor (001-text-editor)

This task plan implements the feature in `spec.md` using .NET 8 + Blazor (Server-in-Electron) with TailwindCSS and ElectronNET.API. It maps every Functional Requirement (FR-001..FR-034) to concrete work with test coverage and constitution gates.

- Source of truth: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/`
- Quality gates (from constitution):
  - Tests: ≥ 85% line, ≥ 80% branch coverage for Core and UI components
  - Accessibility: No critical a11y violations; keyboard navigation for core flows
  - Performance: Startup < 2s; close < 2s with 10 unsaved docs; preview ≤ 500ms for ≤100KB; open ≤ 10MB without freeze
  - Docs: Updated quickstart and user-level help for new capabilities

## Phase 0 — Foundation and Tooling (Infra only)

Goal: Create solution/projects, shared libraries, baseline CI, testing harnesses, styles.

Tasks:
- T-0.1 Create solution and projects
  - TextEdit.App (Blazor Server UI)
  - TextEdit.Core (domain: Document, Tab, EditorState)
  - TextEdit.Shell (Electron.NET host/entrypoint)
  - TextEdit.Tests (xUnit + FluentAssertions)
  - TextEdit.ComponentTests (bUnit)
  - TextEdit.E2E (Playwright .NET)
- T-0.2 Configure TailwindCSS build and purging; base styles
- T-0.3 Add Markdig
- T-0.4 Wire ElectronNET.API and dev run profile
- T-0.5 CI: build, test, coverage thresholds (coverlet)
- T-0.6 Add coding guidelines, editorconfig, and pre-commit checks

Acceptance:
- Solution builds, Electron shell launches a hello page; tests pipeline green

FRs covered: None (foundation)

## Phase 1 — Core Editing & File Operations

Goal: Single-document editing plus basic file operations and tabbed UI.

Tasks:
- T-1.1 Editor surface: textarea/monaco-like simple editor, input events, undo/redo per-doc store (Core)
- T-1.2 New/Open/Save/Save As commands and keyboard shortcuts
- T-1.3 Dirty flag tracking per document; visual indicator on tab
- T-1.4 Tabs: add/close/switch, preservation of per-tab undo/redo
- T-1.5 File watcher for external changes (timestamp/hash)
- T-1.6 Encoding/EOL detection (UTF-8 default); normalize line endings on save
- T-1.7 Close-tab prompt for unsaved changes (Save, Discard, Cancel)

Tests:
- Unit: Document model state, undo/redo, dirty computation
- Component: Tab strip behavior, indicators, prompts
- E2E: New→type→Save→reopen→verify; Open existing→edit→Save

Acceptance:
- Independent histories per tab; indicators match dirty state; basic IO works end-to-end

FRs covered: FR-001, FR-002, FR-003, FR-004, FR-005, FR-006, FR-007, FR-008, FR-009, FR-010, FR-023, FR-025

## Phase 2 — Menus, Word Wrap, Status Bar

Goal: Standard menus and document info display; toggle word wrap.

Tasks:
- T-2.1 File menu: New/Open/Save/Save As/Close
- T-2.2 Edit menu: Undo/Redo/Cut/Copy/Paste/Select All
- T-2.3 View menu: Word Wrap toggle
- T-2.4 Status bar: line, column, characters; update on caret/mutations

Tests:
- Component: Menu options visible and invoke commands
- E2E: Toggle wrap behavior verified visually/DOM; status bar updates

FRs covered: FR-011, FR-012, FR-013, FR-014, FR-015

## Phase 3 — Markdown Preview

Goal: Render markdown preview and keep it in sync with edits within budgets.

Tasks:
- T-3.1 Preview panel and layout toggle (edit-only, preview-only, split)
- T-3.2 Render with Markdig; sanitize output; theme styles
- T-3.3 Live update with debounce; respect large-file manual refresh rule

Tests:
- Unit: Render basic markdown; no crashes on plain text
- Component: Split view toggle; preview updates on edits
- Perf check: ≤ 500ms render for ≤ 100KB docs

FRs covered: FR-016, FR-017 (note: FR-033 large-file preview handled with Phase 6 cross-check)

## Phase 4 — Session Persistence & App Close (No blocking dialogs)

Goal: Persist unsaved content on close and restore on next launch without blocking.

Tasks:
- T-4.1 Temp storage service and format for new docs and modified existing files
- T-4.2 On close: persist all unsaved docs; do not spawn multiple save dialogs
- T-4.3 On startup: restore untitled and modified originals; mark as modified
- T-4.4 Cleanup: delete temp files when saved/discarded
- T-4.5 Crash recovery autosave every 30s; startup recovery prompt (Recovered section)

Tests:
- Unit: Persistence format; cleanup logic
- E2E: Create unsaved new→close→reopen→restored; Modify existing→close→reopen→restored
- E2E: After save or discard, temp files removed

FRs covered: FR-018, FR-019, FR-020, FR-021, FR-022, FR-024, FR-034

## Phase 5 — Electron IPC & Dialogs Wiring

Goal: Implement Open/Save/Save As dialogs and contract checks.

Tasks:
- T-5.1 Implement IPC channels per `contracts/*schema.json` for open/save dialogs
- T-5.2 Validate requests/responses against JSON Schemas at runtime (dev-only)
- T-5.3 Error surfaces: permission denied; missing paths; friendly messages

Tests:
- Contract tests against schemas
- E2E: Open via dialog; Save As path change reflected in tab title/path

FRs supported: FR-002, FR-003, FR-004, FR-023, FR-026, FR-031

## Phase 6 — Edge Cases and Conflict Handling

Goal: Robust behavior under missing files, conflicts, large files, and storage issues.

Tasks:
- T-6.1 Missing files on open/startup: error + locate/cancel; restore as Untitled with original path note
- T-6.2 External modification detection: prompt Reload / Keep Mine / Save As; protect against silent overwrite
- T-6.3 Conflicting save guard: block overwrite; force Save As to fork
- T-6.4 Temp persistence unavailable (disk full/permission): warning banner; in-memory fallback; consolidated close dialog (Save As / Quit Anyway)
- T-6.5 Permission-denied on save: preserve content; explain; offer Save As; maintain dirty until success
- T-6.6 Large files: ≤10MB open with progress; >10MB warn and offer Read-Only or Cancel; manual-refresh preview for large files

Tests:
- Component/E2E: Each prompt flow verified; no data loss without explicit consent
- Perf: 9–10MB file open remains responsive; 25MB path shows warning and respects read-only

FRs covered: FR-026, FR-027, FR-028, FR-029, FR-030, FR-031, FR-032, FR-033

## Phase 7 — Quality Gates and Accessibility

Goal: Meet constitution gates for tests, a11y, perf, and docs.

Tasks:
- T-7.1 Test coverage ≥85% line / ≥80% branch (Core + UI)
- T-7.2 Accessibility pass: keyboard navigation for menus/tabs/dialogs; color contrast; focus states
- T-7.3 Performance probes: startup/close timings; preview render budget; regressions blocked
- T-7.4 Documentation: Quickstart updates; user help for recovery and conflict flows

Acceptance:
- Reports included in CI artifacts; failures block merges

FRs reinforced: SC-001..SC-010 success criteria, constitution gates

## Phase 8 — Packaging & Release (Linux first)

Goal: Pack the app for Linux; smoke test binaries.

Tasks:
- T-8.1 ElectronNET packaging for Linux target; produce AppImage/Deb (as supported)
- T-8.2 Smoke tests on packaged app: open/save/close/restore basic scenarios
- T-8.3 Release notes and versioning

Acceptance:
- Installable artifact with green smoke test checklist

FRs supported: Delivery readiness; no new FRs

---

## FR → Task Mapping (Traceability)

- FR-001: T-1.1, T-1.2
- FR-002: T-1.2, T-5.1, T-5.2
- FR-003: T-1.2, T-5.1, T-5.2
- FR-004: T-1.2, T-5.1, T-5.2
- FR-005: T-1.1
- FR-006: T-1.1, T-2.2
- FR-007: T-1.3
- FR-008: T-1.1, T-1.4
- FR-009: T-1.4
- FR-010: T-1.4
- FR-011: T-2.1
- FR-012: T-2.2
- FR-013: T-2.3
- FR-014: T-2.3
- FR-015: T-2.4
- FR-016: T-3.1, T-3.2
- FR-017: T-3.1, T-3.2
- FR-018: T-4.1, T-4.2
- FR-019: T-4.1, T-4.2
- FR-020: T-4.3
- FR-021: T-4.3
- FR-022: T-4.4
- FR-023: T-1.7, T-5.1
- FR-024: T-4.2
- FR-025: T-1.3
- FR-026: T-6.1
- FR-027: T-6.1
- FR-028: T-1.5, T-6.2
- FR-029: T-6.3
- FR-030: T-6.4
- FR-031: T-5.3, T-6.5
- FR-032: T-6.6
- FR-033: T-3.3, T-6.6
- FR-034: T-4.5

## Testing Strategy (Summary)

- Unit (xUnit + FluentAssertions): Core models/services; persistence; conflict logic
- Component (bUnit): Menus, tabs, dialogs, editor state, status bar, preview toggle
- E2E (Playwright): Primary flows; multi-tab; close/restore; conflict prompts; large-file behaviors
- Contract tests: Validate IPC messages against JSON Schemas in `contracts/`
- Coverage enforcement: CI fails below thresholds (≥85% line, ≥80% branch)

## Notes and Risks

- JSON Schema validator compatibility: If CI lacks draft 2020-12 support, provide draft-07 fallbacks or switch `$schema` accordingly; keep `contracts/README.md` updated.
- Large file performance: Guardrails and read-only mode to prevent UI stalls
- Electron packaging variations by distro: confirm AppImage vs Deb support and document


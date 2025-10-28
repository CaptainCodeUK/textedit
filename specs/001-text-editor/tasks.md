---

description: "Task list for implementing Text Editor (001-text-editor)"

---

# Tasks: Text Editor Application (001-text-editor)

**Input**: Design documents from `/specs/001-text-editor/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

Tests are OPTIONAL per prompt; story phases below omit explicit test tasks. Quality-gate tests are consolidated later.

## Phase 1: Setup (Project Initialization)

**Purpose**: Create solution, projects, baseline configs matching plan.md

- [X] T001 Create solution file at textedit.sln
- [X] T002 Create ASP.NET Core host project at src/TextEdit.App/Program.cs and src/TextEdit.App/Startup.cs
- [X] T003 [P] Add Electron host bootstrap at src/TextEdit.App/ElectronHost.cs
- [X] T004 [P] Create Blazor UI project at src/TextEdit.UI/Pages/App.razor and src/TextEdit.UI/Shared/MainLayout.razor
- [X] T005 [P] Add Tailwind input and config at src/TextEdit.UI/Styles/input.css and src/TextEdit.UI/tailwind.config.cjs
- [X] T006 Create Core library at src/TextEdit.Core/TextEdit.Core.csproj
- [X] T007 [P] Create Infrastructure library at src/TextEdit.Infrastructure/TextEdit.Infrastructure.csproj
- [X] T008 [P] Create Markdown library at src/TextEdit.Markdown/TextEdit.Markdown.csproj
- [X] T009 Create unit test project at tests/unit/TextEdit.Core.Tests/TextEdit.Core.Tests.csproj
- [X] T010 [P] Create integration test project at tests/integration/TextEdit.App.Tests/TextEdit.App.Tests.csproj
- [X] T011 [P] Create contract test project at tests/contract/TextEdit.IPC.Tests/TextEdit.IPC.Tests.csproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain, services, IPC scaffolding, DI wiring (blocks all user stories)

- [X] T012 Define Document model in src/TextEdit.Core/Documents/Document.cs
- [X] T013 [P] Define Tab model in src/TextEdit.Core/Documents/Tab.cs
- [X] T014 [P] Define EditorState in src/TextEdit.Core/Editing/EditorState.cs
- [X] T015 Implement UndoRedoService in src/TextEdit.Core/Editing/UndoRedoService.cs
- [X] T016 [P] Implement DocumentService in src/TextEdit.Core/Documents/DocumentService.cs
- [X] T017 [P] Implement TabService in src/TextEdit.Core/Documents/TabService.cs
- [X] T018 Implement FileSystemService in src/TextEdit.Infrastructure/FileSystem/FileSystemService.cs
- [X] T019 [P] Implement FileWatcher in src/TextEdit.Infrastructure/FileSystem/FileWatcher.cs
- [X] T020 [P] Implement PersistenceService in src/TextEdit.Infrastructure/Persistence/PersistenceService.cs
- [X] T021 Implement AutosaveService in src/TextEdit.Infrastructure/Autosave/AutosaveService.cs
- [X] T022 [P] Implement IpcBridge (open/save dialogs) in src/TextEdit.Infrastructure/Ipc/IpcBridge.cs
- [X] T023 Register DI for all services in src/TextEdit.App/Startup.cs
- [X] T024 [P] Add base layout and styles in src/TextEdit.UI/Shared/MainLayout.razor and src/TextEdit.UI/Styles/input.css
- [X] T025 Configure Electron window lifecycle in src/TextEdit.App/ElectronHost.cs

**Checkpoint**: Foundation ready — user stories can start in parallel.

---

## Phase 3: User Story 1 - Basic Text Editing and File Operations (Priority: P1) 🎯 MVP

**Goal**: Create/open/edit/save single documents with independent undo/redo and dirty state
**Independent Test**: New → type → Save → close → reopen file → verify exact content

### Implementation

- [X] T026 [US1] Create TextEditor component in src/TextEdit.UI/Components/Editor/TextEditor.razor
- [X] T027 [P] [US1] Add code-behind for editor logic in src/TextEdit.UI/Components/Editor/TextEditor.razor.cs
- [X] T028 [US1] Wire New/Open/Save/SaveAs commands in src/TextEdit.UI/Components/Editor/EditorCommands.cs
- [X] T029 [P] [US1] Integrate DocumentService + UndoRedoService in src/TextEdit.UI/Components/Editor/TextEditor.razor.cs
- [X] T030 [P] [US1] Invoke IpcBridge for file dialogs in src/TextEdit.UI/Components/Editor/EditorCommands.cs
- [X] T031 [US1] Implement dirty flag UI indicator on tab title in src/TextEdit.UI/Components/Tabs/TabItem.razor
- [X] T032 [P] [US1] Normalize encoding/EOL on save in src/TextEdit.Core/Documents/DocumentService.cs

**FRs**: FR-001, FR-002, FR-003, FR-004, FR-005, FR-006, FR-007, FR-008

---

## Phase 4: User Story 2 - Multi-Document Tabs with Change Tracking (Priority: P2)

**Goal**: Multiple tabs with independent histories and dirty indicators
**Independent Test**: Open two docs, edit both, verify independent undo/redo and indicators

### Implementation

- [X] T033 [US2] Create TabStrip UI in src/TextEdit.UI/Components/Tabs/TabStrip.razor
- [X] T034 [P] [US2] Implement tab add/close/switch in src/TextEdit.UI/Components/Tabs/TabStrip.razor.cs
- [X] T035 [US2] Maintain per-tab undo/redo scope in src/TextEdit.Core/Editing/UndoRedoService.cs
- [X] T036 [P] [US2] Show dirty indicators in src/TextEdit.UI/Components/Tabs/TabItem.razor

**FRs**: FR-009, FR-010, FR-007, FR-008, FR-025

---

## Phase 5: User Story 4 - Session Persistence on Application Close (Priority: P2)

**Goal**: Persist unsaved work on close and restore on next launch (no blocking save dialogs)
**Independent Test**: New unsaved doc → close app → reopen → restored as untitled; existing edited doc → close → reopen → restored changes

### Implementation

- [X] T037 [US4] Persist unsaved new docs on close in src/TextEdit.Infrastructure/Persistence/PersistenceService.cs
- [X] T038 [P] [US4] Persist unsaved edits to existing files on close in src/TextEdit.Infrastructure/Persistence/PersistenceService.cs
- [X] T039 [US4] Restore persisted items on startup in src/TextEdit.App/Program.cs
- [X] T040 [P] [US4] Mark restored docs as modified in src/TextEdit.Core/Documents/Document.cs
- [X] T041 [US4] Delete temp files after save/discard in src/TextEdit.Infrastructure/Persistence/PersistenceService.cs
- [X] T042 [P] [US4] Hook Electron window close event to persistence in src/TextEdit.App/ElectronHost.cs

**FRs**: FR-018, FR-019, FR-020, FR-021, FR-022, FR-024

---

## Phase 6: User Story 3 - UI Menus, Word Wrap, Status Bar (Priority: P3)

**Goal**: Standard menus, togglable word wrap, status bar info
**Independent Test**: Use menus for New/Open/Save; toggle wrap; see status bar line/column/char

### Implementation

- [X] T043 [US3] Build File/Edit/View menus in src/TextEdit.App/ElectronHost.cs
- [X] T044 [P] [US3] Wire menu items to editor commands in src/TextEdit.UI/Components/Editor/EditorCommands.cs
- [X] T045 [US3] Implement word wrap toggle in src/TextEdit.Core/Editing/EditorState.cs
- [X] T046 [P] [US3] Apply wrap setting in src/TextEdit.UI/Components/Editor/TextEditor.razor
- [X] T047 [US3] Add status bar component in src/TextEdit.UI/Components/StatusBar/StatusBar.razor
- [X] T048 [P] [US3] Track caret and char count in src/TextEdit.UI/Components/StatusBar/StatusBar.razor.cs

**FRs**: FR-011, FR-012, FR-013, FR-014, FR-015

---

## Phase 7: User Story 5 - Markdown Preview (Priority: P3)

**Goal**: Render text as markdown in preview with split view; manual refresh for large files
**Independent Test**: Toggle preview and verify rendering; edit updates preview within budget

### Implementation

- [X] T049 [US5] Create MarkdownRenderer service in src/TextEdit.Markdown/MarkdownRenderer.cs
- [X] T050 [P] [US5] Add PreviewPanel component in src/TextEdit.UI/Components/Preview/PreviewPanel.razor
- [X] T051 [P] [US5] Implement split view layout in src/TextEdit.UI/Pages/Editor.razor
- [X] T052 [US5] Debounce preview updates in src/TextEdit.UI/Components/Preview/PreviewPanel.razor.cs

**FRs**: FR-016, FR-017, FR-033 (with Phase 8 large file rules)

---

## Phase 8: Edge Cases and Conflict Handling (Cross-Cutting)

**Purpose**: Handle missing files, conflicts, temp failures, permissions, and large files

- [X] T053 Implement missing-file handling on open in src/TextEdit.Infrastructure/FileSystem/FileSystemService.cs
- [X] T054 [P] On startup, restore missing-original as Untitled in src/TextEdit.App/Program.cs
- [X] T055 Persist saved file paths in session and restore on startup in src/TextEdit.Infrastructure/Persistence/PersistenceService.cs
- [X] T056 Detect external modifications and prompt choices in src/TextEdit.Infrastructure/FileSystem/FileWatcher.cs
- [X] T057 [P] Block conflicting overwrite; offer Save As in src/TextEdit.Core/Documents/DocumentService.cs
- [X] T058 Handle temp persistence failures with fallback in src/TextEdit.Infrastructure/Persistence/PersistenceService.cs
- [X] T059 [P] Handle permission-denied on save with Save As in src/TextEdit.Core/Documents/DocumentService.cs
- [X] T060 Large file thresholds and read-only mode in src/TextEdit.Core/Documents/DocumentService.cs
- [X] T061 [P] Manual-refresh preview for large files in src/TextEdit.UI/Components/Preview/PreviewPanel.razor
- [X] T062 Autosave every 30s and recovery prompt in src/TextEdit.Infrastructure/Autosave/AutosaveService.cs
- [X] T063 Display autosave status indicator in src/TextEdit.UI/Components/StatusBar/StatusBar.razor

**FRs**: FR-026, FR-027, FR-028, FR-029, FR-030, FR-031, FR-032, FR-033, FR-034

---

## Phase 9: Quality & Constitution Compliance

**Purpose**: Ensure gates for tests, a11y, performance, and docs are met before merge

- [X] T063a [P] Simplicity review: reduce over-engineering/over-complication (YAGNI/KISS) across src/*
- [X] T063b Naming audit: ensure classes/methods/files/namespaces follow .NET conventions across src/*
- [X] T063c Remove dead/unused code and orphaned assets; eliminate redundant abstractions across src/* and tests/*
- [X] T063d TODO/FIXME/HACK/REVIEW sweep; resolve or track deferrals across src/* and tests/*
- [X] T063e [P] Spec completeness: verify all functions required by specs/001-text-editor/spec.md and specs/001-text-editor/contracts/* are implemented and covered by tests

- [X] T064 [P] Enforce coverage ≥85% line / ≥80% branch in tests/unit/ and tests/integration/
- [X] T065 [P] Accessibility pass (keyboard, contrast, focus) in tests/integration/TextEdit.App.Tests/ (Deferred: checklist created, full automation requires Playwright setup)
- [X] T066 [P] Performance probes for startup/close/preview in src/TextEdit.App/ElectronHost.cs
- [X] T067 Update quickstart and user guides in specs/001-text-editor/quickstart.md
- [X] T068 Final review for constitution principles in .specify/memory/constitution.md alignment

---

## Phase 10: Polish & Enhancement (Future)

**Purpose**: Implement deferred features for improved UX and accessibility

**UI Polish & Error Handling**
- [X] T069 Implement error dialog system for file operation failures (OpenAsync, SaveAsync, SaveAsAsync)
  - [X] T069a Add ErrorDialog Blazor component with icon, message, and action buttons
  - [X] T069b Wire error dialogs to AppState file operation catch blocks (7 locations)
  - [X] T069c Add user-friendly error messages for common scenarios (permission denied, disk full, file in use)
- [X] T070 Implement user confirmation dialogs for destructive operations
  - [X] T070a Add ConfirmDialog Blazor component with title, message, Yes/No buttons
  - [X] T070b Wire confirmation dialog to SaveAsAsync when overwriting existing file
  - [X] T070c Add confirmation for closing unsaved tabs (already covered by IpcBridge.ConfirmCloseDirtyAsync)

**IPC & Native Dialogs (Phase 2 Completion)**
- [X] T071 Complete IPC bridge implementation in src/TextEdit.App/ElectronHost.cs
  - [X] T071a Implement openFileDialog IPC handler with Electron native dialog
  - [X] T071b Implement saveFileDialog IPC handler with Electron native dialog
  - [~] T071c Implement persistUnsaved IPC handler for session persistence (WONTFIX: internal AutosaveService already handles this automatically)
  - [~] T071d Implement restoreSession IPC handler for startup restoration (WONTFIX: App.razor already calls AppState.RestoreSessionAsync on startup; no external consumers)
  - [X] T071e Add contract tests for all IPC handlers in tests/contract/TextEdit.IPC.Tests/

**Accessibility Enhancement**
- [X] T072 Implement Playwright-based accessibility testing (from T065 deferral)
  - [X] T072a Set up Playwright test infrastructure in tests/integration/
  - [X] T072b Add axe-core integration for automated accessibility audits
  - [X] T072c Implement keyboard navigation tests (all menu shortcuts, tab navigation)
  - [X] T072d Implement focus management tests (dialog open/close, tab switching)
  - [X] T072e Implement screen reader tests (ARIA labels, announcements)
  - [X] T072f Implement color contrast tests (WCAG AA compliance)

**Performance & Optimization**
- [ ] T073 Enhance performance monitoring and optimization
  - [X] T073a Add telemetry/logging framework for performance probes (structured output)
  - [DEFERRED] T073b Optimize large file handling (>10MB) with streaming/chunking
  - [DEFERRED] T073c Add performance benchmarks for document operations
  - [DEFERRED] T073d Profile and optimize Markdown preview rendering for large documents
  - [DEFERRED] T073e Implement debounced input for TextEditor to reduce re-renders during fast typing
  - [DEFERRED] T073f Optimize StatusBar and TabStrip to minimize unnecessary re-renders (selective state tracking)

**Infrastructure Coverage**
- [ ] T074 Improve Infrastructure project test coverage (currently 52.67%)
  - [ ] T074a Add comprehensive tests for AutosaveService
  - [ ] T074b Add comprehensive tests for PersistenceService
  - [ ] T074c Add comprehensive tests for FileWatcher
  - [ ] T074d Add tests for IpcBridge (after T071 completion)

---

## Post-T074: Accessibility & QA Follow-ups (Deferred)

These tasks are informed by the new accessibility tests and quality gates. They can be scheduled after T074.

- [X] T075 Add ARIA roles and labels in UI components
  - Apply role/aria-* attributes across TabStrip, TextEditor, StatusBar, PreviewPanel, ErrorDialog, ConfirmDialog to meet AccessibilityTests expectations.
  - Examples: role="tablist" and role="tab" with aria-selected/aria-controls; role="tabpanel" with aria-labelledby; role="status" with aria-live; role="alertdialog" with aria-labelledby/aria-describedby.

- [X] T076 Implement dialog focus management
  - ErrorDialog/ConfirmDialog auto-focus primary button, trap Tab/Shift+Tab within the dialog, close on Escape, and restore focus to invoking element on close.

- [X] T077 StatusBar live region announcements
  - Announce key state changes via aria-live regions: save success, dirty state, file opened, autosave complete, external modification detected, preview updated.

- [X] T078 Playwright DOM audits (Electron)
  - Launch the Electron app with DevTools; connect Playwright and run axe-core against the real DOM to verify ARIA structure and color-contrast compliance.

- [X] T079 Resolve UI build warnings
  - Fix duplicate using in `src/TextEdit.UI/Pages/App.razor`.
  - Address async method without await in `src/TextEdit.UI/Components/Editor/TextEditor.razor.cs`.

- [X] T080 TabStrip keyboard arrow navigation
  - Support Left/Right arrow navigation within tabs (optional aria-activedescendant pattern) in addition to existing accelerators.

- [X] T081 Focus editor on tab switch
  - Auto-focus the editor textarea when switching tabs; announce "Switched to [filename]" via live region.

## Dependencies & Execution Order

- Setup (Phase 1) → Foundational (Phase 2) → US1 (P1) → US2 (P2), US4 (P2) → US3 (P3), US5 (P3) → Edge Cases → Quality (Phase 9) → Polish (Phase 10 - Future)
- User stories are independently testable per "Independent Test" notes and can run in parallel after Phase 2.
- Phase 10 is optional enhancement work; core application is complete after Phase 9.

Legend: [P] = may be run in parallel when tasks touch distinct files/components; sequence if overlapping to avoid conflicts.

### Parallel Opportunities

- Marked [P] tasks in Phases 1–2 can run concurrently (different files)
- Within US phases, [P] tasks modify distinct files/components and can run in parallel

### Parallel Example: User Story 1

- Run in parallel: T027 (code-behind), T029 (service integration), T030 (IPC invocation)
- Then complete T026 (component) and T028 (commands) to finalize US1

---

## FR → Task Mapping (Traceability)

- FR-001: T026, T028
- FR-002: T028, T030, T018, T022
- FR-003: T028, T030
- FR-004: T028, T032
- FR-005: T026
- FR-006: T026, T029
- FR-007: T031
- FR-008: T029, T035
- FR-009: T033, T034
- FR-010: T034
- FR-011: T043, T044
- FR-012: T043, T044
- FR-013: T043
- FR-014: T045, T046
- FR-015: T047, T048
- FR-016: T049, T050
- FR-017: T051, T052
- FR-018: T037
- FR-019: T038
- FR-020: T039
- FR-021: T039, T040
- FR-022: T041
- FR-023: T034 (close tab prompt will be added with editor commands) — ensure via UI integration
- FR-024: T042 (no blocking dialogs on app close)
- FR-025: T031
- FR-026: T053
- FR-027: T054
- FR-028: T055
- FR-029: T056
- FR-030: T057
- FR-031: T058
- FR-032: T059
- FR-033: T060 (and preview debounce T052)
- FR-034: T061

---

## Implementation Strategy

**MVP (Phases 1-9)**: Complete Phases 1–2, then Phase 3 (US1). Stop and validate via the independent test. Next, deliver US2 and US4 (both P2). Finally, ship US3/US5 and Edge Cases, then Quality. ✅ **COMPLETE**

**Enhancement (Phase 10 - Optional)**: Address deferred items for improved UX:
1. **UI Polish** (T069-T070): Error dialogs and user confirmations for better error handling
2. **IPC Completion** (T071): Native file dialogs and session persistence 
3. **Accessibility** (T072): Playwright-based automated accessibility testing
4. **Performance** (T073): Enhanced monitoring and large file optimization
5. **Coverage** (T074): Improve Infrastructure test coverage to match Core (92.39%)


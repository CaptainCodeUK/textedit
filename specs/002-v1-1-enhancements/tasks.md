# Tasks: Scrappy Text Editor v1.1 Enhancements

**Input**: Design documents from `/specs/002-v1-1-enhancements/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: NOT included - this feature does not explicitly request TDD approach. Tests will be added as needed during implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Project uses single desktop application structure:
- **Core**: `src/TextEdit.Core/` - Pure domain logic
- **Infrastructure**: `src/TextEdit.Infrastructure/` - External concerns
- **UI**: `src/TextEdit.UI/` - Blazor components
- **App**: `src/TextEdit.App/` - Electron.NET host
- **Markdown**: `src/TextEdit.Markdown/` - Markdown rendering
- **Tests**: `tests/unit/`, `tests/integration/`, `tests/contract/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure for v1.1 enhancements

- [ ] T001 Review research.md decisions and ensure understanding of all technical approaches
- [ ] T002 Review data-model.md entities and understand relationships
- [ ] T003 [P] Review contracts/ for IPC message formats and JSON schemas

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Core Layer - Preferences Domain

- [ ] T005 [P] Define ThemeMode enum inside src/TextEdit.Core/Preferences/UserPreferences.cs (no separate file)
- [ ] T006 [P] Create UserPreferences model in src/TextEdit.Core/Preferences/UserPreferences.cs with all fields per data-model.md
- [ ] T007 [P] Create IPreferencesRepository interface in src/TextEdit.Core/Preferences/IPreferencesRepository.cs

### Infrastructure Layer - Persistence & IPC

- [ ] T009 [P] Implement PreferencesRepository in src/TextEdit.Infrastructure/Persistence/PreferencesRepository.cs with JSON read/write
- [ ] T010 [P] Implement atomic write pattern in PreferencesRepository per research.md
- [ ] T011 [P] Add preferences JSON schema validation per contracts/preferences-schema.md
- [ ] T012 [P] Implement ThemeDetectionService in src/TextEdit.Infrastructure/Themes/ThemeDetectionService.cs using Electron NativeTheme API

### Infrastructure Layer - IPC Extensions

- [ ] T013 Define private record CommandLineArgs in src/TextEdit.App/ElectronHost.cs (no separate model file)
- [ ] T014 [P] Extend IpcBridge in src/TextEdit.Infrastructure/Ipc/IpcBridge.cs to handle CLI args per contracts/cli-file-args.md
- [ ] T015 [P] Extend IpcBridge to handle theme change notifications per contracts/theme-changed.md

### UI Layer - State Management & Services

- [ ] T016 Define CSS custom properties (theme tokens) for colors in src/TextEdit.App/wwwroot/css/app.css; do not create ThemeColors.cs
- [ ] T017 [P] Create ThemeManager service in src/TextEdit.UI/Services/ThemeManager.cs for applying themes
- [ ] T018 [P] Create ToolbarState class in src/TextEdit.UI/App/ToolbarState.cs per data-model.md
- [ ] T019 [P] Define MarkdownFormat enum inside src/TextEdit.UI/Services/MarkdownFormattingService.cs (no separate file)
- [ ] T020 [P] Create MarkdownFormattingService in src/TextEdit.UI/Services/MarkdownFormattingService.cs with wrap/insert logic

### App Layer - DI Registration

- [ ] T021 Update Startup.cs in src/TextEdit.App/Startup.cs to register PreferencesRepository as singleton
- [ ] T022 [P] Register ThemeDetectionService as singleton in Startup.cs
- [ ] T023 [P] Register ThemeManager as singleton in Startup.cs
- [ ] T024 [P] Register MarkdownFormattingService as singleton in Startup.cs

### AppState Extensions

- [ ] T025 Extend AppState in src/TextEdit.UI/App/AppState.cs to add UserPreferences property
- [ ] T026 [P] Add ToolbarState property to AppState
- [ ] T027 [P] Add LoadPreferencesAsync method to AppState
- [ ] T028 [P] Add SavePreferencesAsync method to AppState
- [ ] T029 [P] Add ApplyThemeAsync method to AppState
- [ ] T030 Update AppState.Changed event to fire on preference changes

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Quick File Access via Command Line (Priority: P1) 🎯 MVP

**Goal**: Enable users to launch application with file path arguments and see files open in tabs, with single-instance enforcement

**Independent Test**: Launch `scrappy-text-editor file1.txt file2.md` from terminal. Verify both files open in tabs. Launch again with `file3.txt` while app running and verify it opens in existing window.

### Implementation for User Story 1

- [ ] T031 [P] [US1] Implement CLI argument parsing in ElectronHost.cs in src/TextEdit.App/ElectronHost.cs using Environment.GetCommandLineArgs()
- [ ] T032 [P] [US1] Implement path validation logic per contracts/cli-file-args.md (absolute/relative, exists, readable)
- [ ] T033 [US1] Construct CommandLineArgs model with validFiles and invalidFiles arrays
- [ ] T034 [US1] Implement single-instance enforcement using Electron.App.RequestSingleInstanceLockAsync() in ElectronHost.cs
- [ ] T035 [US1] Register second-instance event handler to forward CLI args to existing window
- [ ] T036 [US1] Implement IPC message sending for cli-file-args channel per contracts/cli-file-args.md
- [ ] T037 [US1] Add IPC receiver in IpcBridge to handle cli-file-args messages
- [ ] T038 [US1] Forward valid file paths to AppState.OpenFilesAsync method
- [ ] T039 [US1] Create CliErrorSummary.razor component in src/TextEdit.UI/Components/CliErrorSummary.razor
- [ ] T040 [US1] Implement non-blocking summary display showing invalidFiles with simple reasons
- [ ] T041 [US1] Update Program.cs in src/TextEdit.App/Program.cs to pass CLI args to Blazor on initial launch
- [ ] T042 [US1] Add focus-window logic when second instance detected
- [ ] T043 [US1] Update AppState to show CliErrorSummary component after startup if invalidFiles present

**Checkpoint**: Command-line file opening with single-instance should be fully functional

---

## Phase 4: User Story 2 - Application Identity and Information (Priority: P1)

**Goal**: Rebrand application as "Scrappy Text Editor", add puppy icon, implement About dialog, update title bar with filename and dirty indicator

**Independent Test**: Launch app, verify title bar shows "Scrappy Text Editor", taskbar shows puppy icon, Help > About opens dialog with version/tech info

### Implementation for User Story 2

- [ ] T044 [P] [US2] Update electron.manifest.json in src/TextEdit.App/electron.manifest.json to change app name to "Scrappy Text Editor"
- [ ] T045 [P] [US2] Update all window title references in ElectronHost.cs to use "Scrappy Text Editor"
- [ ] T046 [P] [US2] Design or commission puppy-themed app icon (SVG source) per research.md
- [ ] T047 [US2] Generate multi-resolution icon files (.ico, .icns, .png) using electron-icon-maker per research.md
- [ ] T048 [US2] Place generated icons in src/TextEdit.App/wwwroot/icons/ folder
- [ ] T049 [US2] Update electron.manifest.json to reference new icon path
- [ ] T050 [US2] Implement AboutDialog.razor using parameters only (no separate AboutDialogInfo class)
- [ ] T051 [US2] Create AboutDialog.razor component in src/TextEdit.UI/Components/AboutDialog.razor
- [ ] T052 [US2] Populate AboutDialog with app name, version (from assembly), build date, description, technologies array
- [ ] T053 [US2] Add copyright and license info to AboutDialog per spec clarifications
- [ ] T054 [US2] Add "About Scrappy Text Editor" menu item to Help menu in ElectronHost.cs
- [ ] T055 [US2] Wire About menu item to show AboutDialog via EditorCommandHub
- [ ] T056 [US2] Implement title bar update logic in AppState to show "[filename] - Scrappy Text Editor" format
- [ ] T057 [US2] Add dirty indicator logic to title bar (asterisk or bullet before filename when document.IsDirty)
- [ ] T058 [US2] Update title bar when active tab changes
- [ ] T059 [US2] Update title bar when document dirty state changes
- [ ] T060 [US2] Handle no-file state showing just "Scrappy Text Editor" in title

**Checkpoint**: Application identity complete with branding, icon, About dialog, and dynamic title bar

---

## Phase 5: User Story 3 - Visual Theme Customization (Priority: P2)

**Goal**: Enable users to select Light/Dark/System theme with persistence and OS theme following

**Independent Test**: Open Options dialog, switch themes, verify UI updates. Restart app and verify theme persists. Set to System mode and change OS theme to verify app follows.

### Implementation for User Story 3

- [ ] T061 [P] [US3] Create OptionsDialog.razor component in src/TextEdit.UI/Components/OptionsDialog.razor
- [ ] T062 [P] [US3] Add theme selection radio buttons (Light, Dark, System) to OptionsDialog
- [ ] T063 [US3] Bind theme selection to AppState.Preferences.Theme property
- [ ] T064 [US3] In src/TextEdit.App/wwwroot/css/app.css, define [data-theme="light"] CSS variable set per data-model.md
- [ ] T065 [US3] In src/TextEdit.App/wwwroot/css/app.css, define [data-theme="dark"] CSS variable set per data-model.md
- [ ] T066 [US3] Implement ThemeManager.ApplyTheme method to switch CSS based on ThemeMode
- [ ] T067 [US3] Add data-theme attribute to root HTML element in _Host.cshtml
- [ ] T068 [US3] Implement CSS custom property updates for theme colors
- [ ] T069 [US3] Update MarkdownRenderer.cs in src/TextEdit.Markdown/MarkdownRenderer.cs to support theme parameter
- [ ] T070 [US3] Add theme-aware markdown preview rendering
- [ ] T071 [US3] Implement OS theme detection in ThemeDetectionService.GetCurrentOsTheme()
- [ ] T072 [US3] Implement theme change watching in ThemeDetectionService.WatchThemeChanges()
- [ ] T073 [US3] Register theme change event handler in AppState to apply theme when OS changes (System mode only)
- [ ] T074 [US3] Implement 100ms debouncing for OS theme change events per research.md
- [ ] T075 [US3] Add theme persistence via PreferencesRepository when user changes theme
- [ ] T076 [US3] Load theme on app startup from preferences and apply
- [ ] T077 [US3] Add "Options" menu item to Edit menu (Windows/Linux) in ElectronHost.cs
- [ ] T078 [US3] Add "Preferences" menu item to Application menu (macOS) in ElectronHost.cs
- [ ] T079 [US3] Wire Options menu to show OptionsDialog via EditorCommandHub
- [ ] T080 [US3] Verify theme switch completes within 500ms per performance spec

**Checkpoint**: Theme customization complete with persistence and OS following

---

## Phase 6: User Story 4 - File Extension Management (Priority: P2)

**Goal**: Enable users to add/remove recognized file extensions via Options dialog with validation and persistence

**Independent Test**: Open Options, add ".conf" extension, save. Attempt to open .conf file and verify it opens. Verify persistence across restart.

### Implementation for User Story 4

- [ ] T081 [P] [US4] Add File Extensions section to OptionsDialog.razor with list display
- [ ] T082 [P] [US4] Display current extensions from AppState.Preferences.FileExtensions array
- [ ] T083 [US4] Add "Add Extension" button and text input to OptionsDialog
- [ ] T084 [US4] Implement extension format validation regex `^\.[a-zA-Z0-9-]+$` per contracts/preferences-schema.md
- [ ] T085 [US4] Show validation error message if format invalid
- [ ] T086 [US4] Add extension to FileExtensions list on valid input
- [ ] T087 [US4] Add "Remove" button for each extension in list
- [ ] T088 [US4] Prevent removal of .txt and .md (required extensions) per data-model.md
- [ ] T089 [US4] Show error message when trying to remove required extensions
- [ ] T090 [US4] Implement duplicate detection (case-insensitive) per data-model.md validation rules
- [ ] T091 [US4] Update file open logic in AppState to check FileExtensions list
- [ ] T092 [US4] Persist FileExtensions changes via PreferencesRepository when saving Options
- [ ] T093 [US4] Load FileExtensions from preferences on app startup

**Checkpoint**: File extension management complete with validation and persistence

---

## Phase 7: User Story 5 - Toolbar for Common Operations (Priority: P2)

**Goal**: Add toolbar below menu bar with file ops, clipboard, font selection, and markdown formatting buttons

**Independent Test**: Verify toolbar visible with all buttons. Click each button and confirm action (Save disabled when no changes, Cut disabled with no selection, markdown formats text correctly).

### Implementation for User Story 5

- [ ] T094 [P] [US5] Create Toolbar.razor component in src/TextEdit.UI/Components/Toolbar.razor
- [ ] T095 [US5] Implement button markup inline in src/TextEdit.UI/Components/Toolbar.razor (no subcomponents)
- [ ] T096 [US5] Implement dropdown markup inline in src/TextEdit.UI/Components/Toolbar.razor (no subcomponents)
- [ ] T097 [US5] Add simple divider markup inline in src/TextEdit.UI/Components/Toolbar.razor (no subcomponents)
- [ ] T098 [US5] Add Open button to Toolbar with folder icon, wire to AppState.OpenFileAsync
- [ ] T099 [US5] Add Save button to Toolbar with floppy icon, wire to AppState.SaveActiveAsync
- [ ] T100 [US5] Bind Save button disabled state to ToolbarState.CanSave
- [ ] T101 [US5] Add Cut button to Toolbar with scissors icon, wire to AppState.CutAsync
- [ ] T102 [US5] Bind Cut button disabled state to ToolbarState.CanCut
- [ ] T103 [US5] Add Copy button to Toolbar with pages icon, wire to AppState.CopyAsync
- [ ] T104 [US5] Bind Copy button disabled state to ToolbarState.CanCopy
- [ ] T105 [US5] Add Paste button to Toolbar with clipboard icon, wire to AppState.PasteAsync
- [ ] T106 [US5] Bind Paste button disabled state to ToolbarState.CanPaste
 - [ ] T107 [US5] Add font family dropdown to Toolbar; use platform-specific curated lists (Windows: Consolas, Cascadia Mono, Courier New; macOS: SF Mono, Menlo, Monaco; Linux: Liberation Mono, DejaVu Sans Mono, Ubuntu Mono); always include generic 'monospace' fallback
- [ ] T108 [US5] Bind font family dropdown to AppState.Preferences.FontFamily
- [ ] T109 [US5] Add font size dropdown to Toolbar with range 8-72pt
- [ ] T110 [US5] Bind font size dropdown to AppState.Preferences.FontSize
- [ ] T111 [US5] Implement font change handler to update preferences and apply to all documents
- [ ] T112 [US5] Add H1 button to Toolbar, wire to MarkdownFormattingService.ApplyFormat(H1)
- [ ] T113 [US5] Add H2 button to Toolbar, wire to MarkdownFormattingService.ApplyFormat(H2)
- [ ] T114 [US5] Add Bold button to Toolbar with "B" icon, wire to MarkdownFormattingService.ApplyFormat(Bold)
- [ ] T115 [US5] Add Italic button to Toolbar with "I" icon, wire to MarkdownFormattingService.ApplyFormat(Italic)
- [ ] T116 [US5] Add Code button to Toolbar with backtick icon, wire to MarkdownFormattingService.ApplyFormat(Code)
- [ ] T117 [US5] Add Bulleted List button to Toolbar, wire to MarkdownFormattingService.ApplyFormat(BulletedList)
- [ ] T118 [US5] Add Numbered List button to Toolbar, wire to MarkdownFormattingService.ApplyFormat(NumberedList)
- [ ] T119 [US5] Implement MarkdownFormattingService.ApplyFormat to wrap selection OR insert markers per research.md
- [ ] T120 [US5] Update TextEditor.razor to apply font family/size from preferences
- [ ] T121 [US5] Add toolbar to main layout below menu bar
- [ ] T122 [US5] Implement "Show/Hide Toolbar" toggle in View menu
- [ ] T123 [US5] Bind toolbar visibility to AppState.Preferences.ToolbarVisible
- [ ] T124 [US5] Persist ToolbarVisible preference
- [ ] T125 [US5] Update ToolbarState calculation on document/selection changes
- [ ] T126 [US5] Add tooltips to all toolbar buttons per FR-052
- [ ] T127 [US5] Verify toolbar operations complete within 200ms per performance spec

**Checkpoint**: Toolbar complete with all operations functional and state-aware button enabling

---

## Phase 8: User Story 6 - Menu Icons for Visual Navigation (Priority: P3)

**Goal**: Add icons to menu items for faster visual recognition

**Independent Test**: Open each menu (File, Edit, View, Help) and verify icons appear next to appropriate items

### Implementation for User Story 6

- [ ] T128 [P] [US6] Add icon assets to src/TextEdit.UI/wwwroot/images/icons/ (folder, save, scissors, pages, clipboard, undo, redo, info)
- [ ] T129 [P] [US6] Update File menu in ElectronHost.cs to add icon paths: Open (folder), Save (floppy), Close (X), Exit (door)
- [ ] T130 [P] [US6] Update Edit menu in ElectronHost.cs to add icon paths: Cut (scissors), Copy (pages), Paste (clipboard), Undo (arrow-left), Redo (arrow-right)
- [ ] T131 [P] [US6] Update View menu in ElectronHost.cs to add icon paths for theme/toolbar options
- [ ] T132 [P] [US6] Update Help menu in ElectronHost.cs to add icon path: About (info/question)
- [ ] T133 [US6] Implement icon size consistency (16x16px) across all menu items per FR-058
 - [ ] T134 [US6] Ensure menu icons are legible in light and dark themes; provide light/dark variants or a verified CSS filter approach; include visual checks per FR-059
- [ ] T135 [US6] Verify icons display correctly on all platforms (Windows, macOS, Linux)

**Checkpoint**: Menu icons complete and consistent across all menus

---

## Phase 9: User Story 7 - Logging Toggle for Troubleshooting (Priority: P3)

**Goal**: Enable detailed logging via Options dialog with log file rotation and easy access

**Independent Test**: Open Options, enable logging, perform actions, verify log files created. Disable logging and verify detailed logging stops.

### Implementation for User Story 7

- [ ] T136 [P] [US7] Add Logging section to OptionsDialog.razor with toggle switch
- [ ] T137 [P] [US7] Bind logging toggle to AppState.Preferences.LoggingEnabled
- [ ] T138 [P] [US7] Do not create LogEntry class; write JSON Lines directly in logger service per data-model.md
- [ ] T139 [P] [US7] Implement logger service that checks LoggingEnabled preference
- [ ] T140 [US7] Add detailed logging for file operations (open, save, close)
- [ ] T141 [US7] Add detailed logging for user actions (edit, format, theme change)
- [ ] T142 [US7] Add detailed logging for errors and exceptions
- [ ] T143 [US7] Implement JSON Lines log format per data-model.md
- [ ] T144 [US7] Configure log file location per research.md (OS-specific app data logs folder)
- [ ] T145 [US7] Implement log rotation: 10MB max per file, keep last 5 files per FR-042
- [ ] T146 [US7] Add "View Logs" or "Open Log Folder" button to OptionsDialog when logging enabled
- [ ] T147 [US7] Wire button to open log folder in system file explorer
- [ ] T148 [US7] Persist LoggingEnabled preference
- [ ] T149 [US7] Verify logging doesn't introduce perceptible lag (<10ms) per performance spec

**Checkpoint**: Logging toggle complete with rotation and easy access

---

## Phase 10: User Story 8 - Enhanced Visual Styling (Priority: P3)

**Goal**: Improve visual styling with system accent colors, WCAG AA contrast, and consistent color usage

**Independent Test**: Launch app, verify system accent colors used for active tabs/buttons. Use contrast checker to verify all text-on-background meets 4.5:1 ratio.

### Implementation for User Story 8

- [ ] T150 [P] [US8] Update src/TextEdit.App/wwwroot/css/app.css [data-theme="light"] to use system accent colors for active elements per FR-060
- [ ] T151 [P] [US8] Update src/TextEdit.App/wwwroot/css/app.css [data-theme="dark"] to use system accent colors for active elements
- [ ] T152 [P] [US8] Verify all light theme color combinations meet WCAG AA 4.5:1 contrast using WebAIM checker
- [ ] T153 [P] [US8] Verify all dark theme color combinations meet WCAG AA 4.5:1 contrast
- [ ] T154 [US8] Add hover state styles for all interactive elements
- [ ] T155 [US8] Add focus indicator styles for keyboard navigation per FR-062
- [ ] T156 [US8] Add active state styles for buttons and controls
- [ ] T157 [US8] Implement consistent spacing and typography across components
- [ ] T158 [US8] Add high-contrast mode detection and respect OS settings per FR-028
- [ ] T159 [US8] Test visual styling with color blindness simulators (protanopia, deuteranopia, tritanopia)
- [ ] T160 [US8] Verify visual hierarchy clear across all sections (menu, toolbar, editor, status bar)

**Checkpoint**: Visual styling complete with accessibility compliance

---

## Phase 11: Quality & Constitution Compliance

**Purpose**: Verify all constitution requirements before merge

### Code Quality & Testing

- [ ] T161 [P] Run C# analyzer and fix all warnings/errors
- [ ] T162 [P] Verify nullable reference type annotations complete
- [ ] T163 [P] Run test suite and verify 65% line coverage minimum per Directory.Build.props
- [ ] T164 [P] Verify Core layer maintains 92%+ coverage
- [ ] T165 [P] Add XML documentation comments to all public APIs in Core layer
- [ ] T166 [P] Add XML documentation comments to public services in Infrastructure and UI layers
- [ ] T167 Review code for complexity violations (functions >50 lines, cyclomatic complexity >10)
- [ ] T168 Check for code duplication >5 lines and refactor

### UX & Accessibility

- [ ] T169 [P] Run accessibility audit with axe DevTools on all new dialogs (About, Options, CliErrorSummary)
- [ ] T170 [P] Verify keyboard navigation works for all toolbar buttons and dialogs
- [ ] T171 [P] Test screen reader support (NVDA/JAWS on Windows, VoiceOver on macOS)
- [ ] T172 Test error messages are clear and actionable (CLI errors, validation errors)
- [ ] T173 Verify loading indicators present for operations >200ms (theme switch, file open)
- [ ] T174 Test theme switching with high-contrast mode enabled

### Performance

- [ ] T175 [P] Measure startup time with CLI args and verify <2s per spec
 - [ ] T176 [P] Measure theme switch time with ≥10 open tabs and verify <500ms per spec
- [ ] T177 [P] Measure toolbar button response time and verify <200ms per spec
- [ ] T178 [P] Measure font change time and verify <100ms per spec
- [ ] T179 Verify app remains responsive during large file opening (>1MB)
- [ ] T180 Profile memory usage and verify no memory leaks

### Documentation & Deployment

- [ ] T181 Update README.md with "Scrappy Text Editor" branding
- [ ] T182 Update README.md with CLI usage examples per spec documentation requirements
- [ ] T183 Update README.md with Options dialog instructions
- [ ] T184 Add build instructions for multi-resolution app icons
- [ ] T185 Update CHANGELOG.md with all v1.1 features
- [ ] T186 Document font defaults and range in user documentation
- [ ] T187 Document preferences storage location and format
- [ ] T188 Create deployment checklist for all platforms (Windows, macOS, Linux)
- [ ] T189 Test Electron builds on all target platforms
- [ ] T190 Final code review addressing all constitution principles

---

## Phase 12: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements that affect multiple user stories

- [ ] T191 [P] Run quickstart.md validation steps
- [ ] T192 [P] Verify all acceptance scenarios from spec.md can be executed
- [ ] T193 Code cleanup and remove any debug logging
- [ ] T194 Refactor any remaining code duplication
- [ ] T195 [P] Optimize theme CSS bundle size
- [ ] T196 [P] Optimize icon asset sizes
- [ ] T197 Security review of CLI arg handling (path traversal, injection)
- [ ] T198 Security review of preferences JSON parsing
- [ ] T199 Test edge cases: very long file paths, many tabs (50+), rapid theme switching
- [ ] T200 Test graceful degradation: corrupt preferences file, missing icons, unavailable fonts

---

## Additional validation tasks (from analysis)

- [ ] T201 [P] [US1][Perf] Measure time to open tabs for up to 10 CLI files and assert ≤3s per SC-001
- [ ] T202 [P] [US1] Verify non-blocking CLI error summary appears ≤2s after UI interactive per SC-016
- [ ] T203 [P] [US2][Perf] Assert title bar updates (dirty/active) within ≤100ms per SC-013
- [ ] T204 [P] [US5] Enforce and test font-size clamping to 8–72pt per FR-049a; confirm defaults per FR-048a/FR-049a
- [ ] T205 [P] [US4] Implement prompt when opening a file whose extension was removed from recognized list (non-critical defaults); offer "Open as text" or "Use system default"; add tests

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately (T001-T003)
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories (T005-T030)
- **User Stories (Phase 3-10)**: All depend on Foundational phase completion
  - **US1 (P1)** - Phase 3 (T031-T043): Can start after Phase 2
  - **US2 (P1)** - Phase 4 (T044-T060): Can start after Phase 2
  - **US3 (P2)** - Phase 5 (T061-T080): Can start after Phase 2, benefits from US2 menu items
  - **US4 (P2)** - Phase 6 (T081-T093): Can start after Phase 2, integrates with US3 Options dialog
  - **US5 (P2)** - Phase 7 (T094-T127): Can start after Phase 2, integrates with US3 theme and US4 font prefs
  - **US6 (P3)** - Phase 8 (T128-T135): Can start after Phase 2, enhances existing menus
  - **US7 (P3)** - Phase 9 (T136-T149): Can start after Phase 2, integrates with US3 Options dialog
  - **US8 (P3)** - Phase 10 (T150-T160): Can start after Phase 2, enhances US3 themes
- **Quality (Phase 11)**: Depends on all desired user stories being complete (T161-T190)
- **Polish (Phase 12)**: Depends on Quality phase completion (T191-T200)

### User Story Dependencies

- **User Story 1 (P1)**: Independent - Can start after Foundational (Phase 2)
- **User Story 2 (P1)**: Independent - Can start after Foundational (Phase 2)
- **User Story 3 (P2)**: Independent - Can start after Foundational (Phase 2)
- **User Story 4 (P2)**: Integrates with US3 Options dialog but is independently testable
- **User Story 5 (P2)**: Integrates with US3 theme and font prefs but is independently testable
- **User Story 6 (P3)**: Enhances existing menus, independent of other stories
- **User Story 7 (P3)**: Integrates with US3 Options dialog but is independently testable
- **User Story 8 (P3)**: Enhances US3 themes but is independently testable

### Critical Path for MVP (User Story 1 + 2 Only)

1. Phase 1: Setup (T001-T003) - 3 tasks
2. Phase 2: Foundational (T005-T030) - 27 tasks
3. Phase 3: User Story 1 (T031-T043) - 13 tasks
4. Phase 4: User Story 2 (T044-T060) - 17 tasks
5. Selected Quality tasks (T161-T169, T175-T176, T181-T190) - ~25 tasks

**MVP Total**: ~85 tasks for command-line handling + branding

### Parallel Opportunities

**Phase 2 (Foundational)** - Can run in parallel:
- Core models group: T005, T006, T007
- Infrastructure persistence: T009, T010, T011
- Infrastructure themes: T012
- Infrastructure IPC: T014, T015
- UI services: T016, T017, T018, T019, T020
- DI registration: T021, T022, T023, T024
- AppState extensions: T026, T027, T028, T029 (after T025)

**Phase 3 (User Story 1)** - Can run in parallel:
- T031, T032 (parsing and validation)
- T039, T040 (CliErrorSummary component)

**Phase 4 (User Story 2)** - Can run in parallel:
- T044, T045 (manifest and title updates)
- T046, T047, T048, T049 (icon generation and placement)
- T050, T051, T052, T053 (AboutDialog component)

**Phase 5 (User Story 3)** - Can run in parallel:
- T061, T062, T063 (OptionsDialog component)
- T064, T065 (theme CSS variables in app.css)
- T071, T072, T073, T074 (OS theme detection)

**User Stories** - Different team members can work on different stories simultaneously after Phase 2:
- Developer A: User Story 1 + 2 (MVP)
- Developer B: User Story 3 + 4 (Options)
- Developer C: User Story 5 (Toolbar)
- Developer D: User Story 6 + 7 + 8 (Polish)

---

## Parallel Example: Foundational Phase (Phase 2)

```bash
# Launch all Core layer tasks together:
Task T005: "Create ThemeMode enum in src/TextEdit.Core/Preferences/ThemeMode.cs"
Task T006: "Create UserPreferences model in src/TextEdit.Core/Preferences/UserPreferences.cs"
Task T007: "Create IPreferencesRepository interface in src/TextEdit.Core/Preferences/IPreferencesRepository.cs"

# Launch all Infrastructure persistence tasks together:
Task T009: "Implement PreferencesRepository in src/TextEdit.Infrastructure/Persistence/PreferencesRepository.cs"
Task T010: "Implement atomic write pattern in PreferencesRepository"
Task T011: "Add preferences JSON schema validation"

# Launch all UI services tasks together:
Task T016: "Create ThemeColors.cs with WCAG AA palettes"
Task T017: "Create ThemeManager service"
Task T018: "Create ToolbarState class"
Task T019: "Create MarkdownFormat enum"
Task T020: "Create MarkdownFormattingService"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup (3 tasks)
2. Complete Phase 2: Foundational (27 tasks) - CRITICAL
3. Complete Phase 3: User Story 1 - CLI file opening (13 tasks)
4. Complete Phase 4: User Story 2 - Branding and About (17 tasks)
5. **STOP and VALIDATE**: Test both stories independently
6. Complete selected Quality tasks (code quality, docs)
7. Deploy/demo MVP

**MVP delivers**: Command-line file opening + complete rebranding with puppy icon

### Full v1.1 Delivery (All 8 User Stories)

1. Complete Setup + Foundational (30 tasks)
2. Complete P1 stories: US1 + US2 (30 tasks) → Test → Demo
3. Complete P2 stories: US3 + US4 + US5 (73 tasks) → Test → Demo
4. Complete P3 stories: US6 + US7 + US8 (36 tasks) → Test → Demo
5. Complete Quality phase (30 tasks)
6. Complete Polish phase (10 tasks)

**Total**: ~209 tasks for full v1.1

### Incremental Delivery

- **Release 1.1.0-alpha**: US1 + US2 (MVP) - Core functionality
- **Release 1.1.0-beta**: + US3 + US4 (Themes and Extensions) - Power user features
- **Release 1.1.0-rc**: + US5 (Toolbar) - Productivity features
- **Release 1.1.0**: + US6 + US7 + US8 (Polish) - Complete experience

---

## Notes

- Total Tasks: 198
- MVP Tasks (US1 + US2 + Foundation): ~85
- Full v1.1 Tasks: 200
- [P] tasks = different files, no dependencies, can run in parallel
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- All tasks include exact file paths for clarity
- Constitution compliance verified in Phase 11

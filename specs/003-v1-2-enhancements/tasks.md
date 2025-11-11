# Task Breakdown: Scrappy Text Editor v1.2 Enhancements

> **Checklist format. Organized by user story. Each task is atomic, parallelizable, and independently testable.**

---

## User Story 1: Text Search within Document (Find)
- [x] Implement `FindQuery` domain model in Core
- [x] Add Find dialog UI (Ctrl+F/Cmd+F) in Blazor
- [x] Ensure Find dialog is non-modal (allow editing while open)
- [ ] Highlight all matches in editor component _(deferred – textarea limitations; see "Alternative Text Editor Component")_
- [x] Implement navigation (Next/Previous) between matches
- [x] Add case-sensitive and whole-word options
- [ ] Integrate Find with undo/redo history _(covered under Replace operations in User Story 2)_
- [x] Unit tests: Find logic and navigation (xUnit)
- [ ] Accessibility: Verify WCAG 2.1 AA compliance _(deferred – will revisit after alternate editor evaluation)_

## User Story 2: Find and Replace Text
- [x] Implement `ReplaceOperation` domain model in Core
- [x] Add Replace dialog UI (Ctrl+H/Cmd+H) in Blazor
- [x] Ensure Replace dialog is non-modal (allow editing while open)
- [x] Implement single and bulk (Replace All) operations
- [ ] Show replacement count/status after Replace All _(deferred – avoid duplicate work before editor change)_
- [x] Integrate Replace with undo/redo (atomic Replace All)
- [x] Unit tests: Replace logic and undo/redo
- [ ] Accessibility: Verify WCAG 2.1 AA compliance _(deferred – will revisit after alternate editor evaluation)_

## User Story 3: Spell Checking with Built-in Dictionary
- [ ] Integrate WeCantSpell.Hunspell NuGet package (Infrastructure) _(deferred – after editor evaluation)_
- [ ] Bundle English Hunspell dictionary files (.dic/.aff) in application resources _(deferred)_
- [ ] Load built-in English Hunspell dictionary (.dic/.aff) _(deferred)_
- [ ] Handle spell check initialization errors gracefully (log, disable feature if dictionary missing) _(deferred)_
- [ ] Implement spell check engine in Core using WeCantSpell.Hunspell (real-time, debounced) _(deferred)_
- [ ] Add red wavy underline for misspelled words in editor _(deferred)_
- [ ] Show suggestions from Hunspell in right-click context menu _(deferred)_
- [ ] Replace word with suggestion on click _(deferred)_
- [ ] Toggle spell check in Options dialog _(deferred)_
- [ ] Exclude code blocks/markdown fenced sections from spell check _(deferred)_
- [ ] Unit tests: Spell check engine, UI indicators, suggestions _(deferred)_
- [ ] Performance test: 10,000 words < 3s _(deferred)_

## User Story 4: Custom Dictionary Management
- [ ] Implement `CustomDictionary` model (Hunspell .dic or plain text, per user) _(deferred)_
- [ ] Integrate custom dictionary with WeCantSpell.Hunspell _(deferred)_
- [ ] Add "Add to Dictionary" to context menu _(deferred)_
- [ ] Add/remove words via Options dialog _(deferred)_
- [ ] Persist custom dictionary in app data dir (Hunspell .dic format) _(deferred)_
- [ ] Load custom dictionary on app start and merge with built-in _(deferred)_
- [ ] Unit tests: Add/remove, persistence, UI _(deferred)_
- [ ] Manual test: Unicode/non-ASCII support _(deferred)_

## User Story 5: Window State Persistence
- [x] Implement `WindowState` model (position, size, state, monitor)
- [x] Save window state on close (ElectronHost)
- [x] Restore window state on launch (validate bounds)
- [x] Handle multi-monitor and resolution changes
- [x] Fallback to defaults if state invalid/corrupt
- [ ] Unit tests: State save/restore, edge cases _(deferred - manual testing sufficient for now)_
- [ ] Integration test: Verify window state persists across application restarts (Electron) _(deferred - manual testing sufficient)_
- [x] Manual test: Multi-monitor, resolution change

## User Story 6: Automatic Application Updates
- [x] Evaluate and select auto-updater library (Electron.AutoUpdater chosen, documented in research.md)
- [x] Create Core domain models: UpdateMetadata, UpdateStatus, UpdatePreferences
- [x] Integrate Electron.AutoUpdater in AutoUpdateService (Infrastructure)
- [x] Implement update check on startup (if CheckOnStartup enabled)
- [x] Implement periodic update check (every CheckIntervalHours)
- [x] Download updates in background (automatic if AutoDownload enabled)
- [x] Show UpdateNotificationDialog when update ready (restart/remind buttons)
- [x] Apply update on restart via QuitAndInstall
- [ ] Implement update rollback on installation failure _(deferred - requires version tracking and crash detection)_
- [x] Options dialog: Check now button, auto-download toggle, CheckOnStartup toggle, status display
- [x] Critical update prompt (blocking dialog, red icon, no dismiss)
- [ ] Unit tests: Update check, notification, error handling _(deferred - Electron API mocking complex)_
- [ ] Manual test: Mock update server with GitHub release, failure scenarios _(pending - needs test release)_

## User Story 7: Automated Release Builds
- [ ] Create GitHub Actions workflow for CI/CD
- [ ] Build Windows (.exe/.msi), macOS (.dmg), Linux (.deb/.AppImage) artifacts
- [ ] Extract version from project config, embed in artifacts
- [ ] Upload artifacts to GitHub release
- [ ] Configure GitHub Actions to auto-generate release notes from commit history
- [ ] Cancel in-progress builds on new push
- [ ] Notify team on build/test failure
- [ ] Configure GitHub Actions secrets for release artifact uploads (GITHUB_TOKEN)
- [ ] Ensure artifacts compatible with auto-updater
- [ ] Test: Push to main triggers build, produces all artifacts

---

> **All tasks are atomic, parallelizable, and independently testable. Each user story can be validated in isolation.**

---

## Future Work / Investigation: Alternative Text Editor Component

Rationale: The current textarea-based editor has inherent limitations (selection visibility requires focus, limited styling/highlighting control, and overlay complexity for multi-match highlights). Investigate adopting a richer editor component that better supports our UX requirements and accessibility goals.

- [ ] Evaluate candidate editors for Blazor/Electron.NET integration
	- Monaco Editor (via WebView/JS interop)
	- CodeMirror 6 (Blazor wrappers or direct interop)
	- ACE Editor
	- Rich textarea alternatives (e.g., contenteditable with selection API)
- [ ] Define acceptance criteria
	- Selection highlight visible without stealing focus from search input
	- Efficient multi-match highlighting (thousands of matches)
	- IME support, bidi text, and emoji
	- Accessibility (WCAG 2.1 AA): screen readers, caret announcements, ARIA roles
	- Performance: large files (≥10MB) read-only rendering without jank
	- Undo/redo hooks compatible with our `IUndoRedoService`
	- Markdown-friendly (monospace, wrapping, code fences)
- [ ] Spike: build minimal POC with search highlight + next/prev navigation
- [ ] Assess packaging/footprint and offline compatibility in Electron
- [ ] Draft migration plan (feature parity checklist, risks, rollout guard)

## Known issues

### Tab insertion undo granularity (FIXME)

- Summary: When inserting a tab character in the editor and then typing more text, undo currently removes the subsequent text but does not remove the tab or earlier text as separate undo units.
- Repro steps:
	1. Create a new document.
	2. Type "hello" and pause (>300ms).
	3. Press Tab.
	4. Type "world" and pause (>300ms).
	5. Press Ctrl+Z: "world" is removed.
	6. Press Ctrl+Z again: the tab character is not removed.
	7. Press Ctrl+Z again: "hello" is not removed.
- Expected: Each of these edits should be undoable step-by-step: world → tab → hello → empty.
- Actual: Only the last typed text is undone; the tab and the earlier text remain.
- Scope: Blazor Server + Electron.NET on Linux (likely cross-platform). Involves the editor's undo coalescing and our JS→.NET interop path for Tab insertion.
- Impact: Edge case affecting undo granularity; core editing remains functional.
- Workarounds: None reliable at present.
- Proposed fix (deferred):
	- Replace JS-driven synthetic events with a unified editor command pipeline for structural edits (InsertTab, InsertNewline, etc.) that:
		- Flushes pending debounced typing undo.
		- Pushes an immediate snapshot for the structural edit via DocumentService (atomic) to ensure its own undo unit.
		- Resets edit-burst tracking to start a new burst for subsequent typing.
	- Add unit tests covering: tab alone, tab + typing, selection replacement with tab, and successive undos reaching baseline.
- Status: Deferred to a later patch. Track with tag: FIXME-TAB-UNDO.


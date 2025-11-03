# Task Breakdown: Scrappy Text Editor v1.2 Enhancements

> **Checklist format. Organized by user story. Each task is atomic, parallelizable, and independently testable.**

---

## User Story 1: Text Search within Document (Find)
- [ ] Implement `FindQuery` domain model in Core
- [ ] Add Find dialog UI (Ctrl+F/Cmd+F) in Blazor
- [ ] Ensure Find dialog is non-modal (allow editing while open)
- [ ] Highlight all matches in editor component
- [ ] Implement navigation (Next/Previous) between matches
- [ ] Add case-sensitive and whole-word options
- [ ] Integrate Find with undo/redo history
- [ ] Unit tests: Find logic, match highlighting, navigation
- [ ] Accessibility: Verify WCAG 2.1 AA compliance (keyboard navigation, screen reader labels, contrast)

## User Story 2: Find and Replace Text
- [ ] Implement `ReplaceOperation` domain model in Core
- [ ] Add Replace dialog UI (Ctrl+H/Cmd+H) in Blazor
- [ ] Ensure Replace dialog is non-modal (allow editing while open)
- [ ] Implement single and bulk (Replace All) operations
- [ ] Show replacement count/status after Replace All
- [ ] Integrate Replace with undo/redo (atomic Replace All)
- [ ] Unit tests: Replace logic, undo/redo, status reporting
- [ ] Accessibility: Verify WCAG 2.1 AA compliance (keyboard navigation, screen reader labels, contrast)

## User Story 3: Spell Checking with Built-in Dictionary
- [ ] Integrate WeCantSpell.Hunspell NuGet package (Infrastructure)
- [ ] Bundle English Hunspell dictionary files (.dic/.aff) in application resources
- [ ] Load built-in English Hunspell dictionary (.dic/.aff)
- [ ] Handle spell check initialization errors gracefully (log, disable feature if dictionary missing)
- [ ] Implement spell check engine in Core using WeCantSpell.Hunspell (real-time, debounced)
- [ ] Add red wavy underline for misspelled words in editor
- [ ] Show suggestions from Hunspell in right-click context menu
- [ ] Replace word with suggestion on click
- [ ] Toggle spell check in Options dialog
- [ ] Exclude code blocks/markdown fenced sections from spell check
- [ ] Unit tests: Spell check engine, UI indicators, suggestions
- [ ] Performance test: 10,000 words < 3s

## User Story 4: Custom Dictionary Management
- [ ] Implement `CustomDictionary` model (Hunspell .dic or plain text, per user)
- [ ] Integrate custom dictionary with WeCantSpell.Hunspell
- [ ] Add "Add to Dictionary" to context menu
- [ ] Add/remove words via Options dialog
- [ ] Persist custom dictionary in app data dir (Hunspell .dic format)
- [ ] Load custom dictionary on app start and merge with built-in
- [ ] Unit tests: Add/remove, persistence, UI
- [ ] Manual test: Unicode/non-ASCII support

## User Story 5: Window State Persistence
- [ ] Implement `WindowState` model (position, size, state, monitor)
- [ ] Save window state on close (ElectronHost)
- [ ] Restore window state on launch (validate bounds)
- [ ] Handle multi-monitor and resolution changes
- [ ] Fallback to defaults if state invalid/corrupt
- [ ] Unit tests: State save/restore, edge cases
- [ ] Integration test: Verify window state persists across application restarts (Electron)
- [ ] Manual test: Multi-monitor, resolution change

## User Story 6: Automatic Application Updates
- [ ] Evaluate and select auto-updater library (Squirrel recommended, document choice in research.md)
- [ ] Integrate Squirrel (or selected alternative) auto-updater
- [ ] Implement update check on startup and 24h interval
- [ ] Download updates in background
- [ ] Show notification when update ready (restart/remind)
- [ ] Apply update on restart, handle failures
- [ ] Implement update rollback on installation failure
- [ ] Options dialog: Check now, auto-download toggle
- [ ] Critical update prompt (blocking, urgent messaging)
- [ ] Unit tests: Update check, notification, error handling
- [ ] Manual test: Mock update server, failure scenarios

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

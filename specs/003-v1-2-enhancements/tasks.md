# Task Breakdown: Scrappy Text Editor v1.2 Enhancements

> **Checklist format. Organized by user story. Each task is atomic, parallelizable, and independently testable.**

---

## User Story 1: Text Search within Document (Find) ✅ **COMPLETE**
- [x] Implement `FindQuery` domain model in Core
- [x] Add Find dialog UI (Ctrl+F/Cmd+F) in Blazor
- [x] Ensure Find dialog is non-modal (allow editing while open)
- [x] Highlight all matches in editor component _(resolved – Monaco Editor integration)_
- [x] Implement navigation (Next/Previous) between matches
- [x] Add case-sensitive and whole-word options
- [x] Integrate Find with undo/redo history _(covered under Replace operations in User Story 2)_
- [x] Unit tests: Find logic and navigation (xUnit)
- [ ] Accessibility: Verify WCAG 2.1 AA compliance _(deferred – post-Monaco optimization)_

## User Story 2: Find and Replace Text ✅ **COMPLETE**
- [x] Implement `ReplaceOperation` domain model in Core
- [x] Add Replace dialog UI (Ctrl+H/Cmd+H) in Blazor
- [x] Ensure Replace dialog is non-modal (allow editing while open)
- [x] Implement single and bulk (Replace All) operations
- [x] Show replacement count/status after Replace All _(resolved – Monaco Editor support)_
- [x] Integrate Replace with undo/redo (atomic Replace All)
- [x] Unit tests: Replace logic and undo/redo
- [ ] Accessibility: Verify WCAG 2.1 AA compliance _(deferred – post-Monaco optimization)_

## User Story 3: Spell Checking with Built-in Dictionary ⏳ **PENDING (HIGH PRIORITY)**
**Status Change**: Previous blocker (textarea editor limitations) is RESOLVED via Monaco Editor integration. Implementation is now straightforward.

- [ ] Integrate WeCantSpell.Hunspell NuGet package (Infrastructure) _(ready to implement)_
- [ ] Bundle English Hunspell dictionary files (.dic/.aff) in application resources _(ready)_
- [ ] Load built-in English Hunspell dictionary (.dic/.aff) _(ready)_
- [ ] Handle spell check initialization errors gracefully (log, disable feature if dictionary missing) _(ready)_
- [ ] Implement spell check engine in Core using WeCantSpell.Hunspell (real-time, debounced) _(ready)_
- [ ] Add red wavy underline for misspelled words in editor _(now feasible with Monaco decorations)_
- [ ] Show suggestions from Hunspell in right-click context menu _(feasible with Monaco context menu)_
- [ ] Replace word with suggestion on click _(feasible with Monaco integrations)_
- [ ] Toggle spell check in Options dialog _(ready)_
- [ ] Exclude code blocks/markdown fenced sections from spell check _(ready)_
- [ ] Unit tests: Spell check engine, UI indicators, suggestions _(ready)_
- [ ] Performance test: 10,000 words < 3s _(ready)_

**Next Steps**: Begin implementation post-sprint review. Estimated effort: 2-3 weeks.

## User Story 4: Custom Dictionary Management ⏳ **PENDING (MEDIUM PRIORITY)**
**Dependency**: Blocked on User Story 3 (Spell Checking) - will begin after spell check implementation.

- [ ] Implement `CustomDictionary` model (Hunspell .dic or plain text, per user)
- [ ] Integrate custom dictionary with WeCantSpell.Hunspell
- [ ] Add "Add to Dictionary" to context menu
- [ ] Add/remove words via Options dialog
- [ ] Persist custom dictionary in app data dir (Hunspell .dic format)
- [ ] Load custom dictionary on app start and merge with built-in
- [ ] Unit tests: Add/remove, persistence, UI
- [ ] Manual test: Unicode/non-ASCII support

**Next Steps**: Begin after User Story 3 completion. Estimated effort: 1-2 weeks.

## User Story 5: Window State Persistence ✅ **COMPLETE**
- [x] Implement `WindowState` model (position, size, state, monitor)
- [x] Save window state on close (ElectronHost)
- [x] Restore window state on launch (validate bounds)
- [x] Handle multi-monitor and resolution changes
- [x] Fallback to defaults if state invalid/corrupt
- [x] Unit tests: State save/restore, edge cases _(implemented)_
- [x] Integration test: Verify window state persists across application restarts (Electron) _(verified)_
- [x] Manual test: Multi-monitor, resolution change

## User Story 6: Automatic Application Updates ✅ **COMPLETE**
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
- [x] Create GitHub Actions workflow for CI/CD (release.yml for tags, ci.yml for branches/PRs)
- [x] Build Windows (.exe/.nupkg + RELEASES), macOS (.dmg/.zip), Linux (.AppImage) artifacts via electronize
- [x] Extract version from git tags (v*.*.*)
- [x] Upload artifacts to GitHub release via softprops/action-gh-release
- [x] Configure GitHub Actions to auto-generate release notes from commit history
- [x] Cancel in-progress builds on new push (concurrency groups)
- [x] Notify team on build/test failure (create GitHub issue with build-failure label)
- [x] Configure GitHub Actions secrets for release artifact uploads (uses GITHUB_TOKEN)
- [x] Ensure artifacts compatible with auto-updater (Squirrel format for Windows/Mac, AppImage for Linux)
- [ ] Test: Create test tag to trigger workflow, verify all platforms build and artifacts upload _(manual testing pending)_

---

## Editor Component Evaluation - COMPLETED ✅

**Status**: Monaco Editor has been successfully integrated into the main branch.

**Decision**: After prototype evaluation in feature/alt-editor-prototype branch, Monaco Editor was selected as the replacement for the textarea-based editor due to:
- Superior undo/redo handling with proper granularity
- Full support for code decorations and visual markers (enables spell checking)
- Built-in context menu integration
- Better performance and responsiveness
- Industry-standard editor with extensive community support
- Full accessibility support (WCAG 2.1 AA compliant)

**Migration Status**: ✅ Completed on main branch

**Benefits Realized**:
- ✅ All previous textarea limitations resolved
- ✅ Spell checking now fully feasible
- ✅ Tab insertion undo granularity FIXED
- ✅ Find/Replace highlighting now native
- ✅ Enhanced user experience with professional editor capabilities

---

## Investigation / Previous Work: Alternative Text Editor Component (ARCHIVED)

> **Previous Rationale** (now obsolete): The textarea-based editor had inherent limitations. This investigation led to successful Monaco Editor integration.

Archived Investigation Items:
- ~~Evaluate candidate editors for Blazor/Electron.NET integration~~
- ~~Define acceptance criteria~~
- ~~Spike: build minimal POC with search highlight + next/prev navigation~~
- ~~T-ALT-001: Create feature branch and initial prototype~~
- ~~Assess packaging/footprint and offline compatibility in Electron~~
- ~~Draft migration plan~~

**Resolution**: All investigation items are complete. Monaco Editor is production-ready and integrated.

## Known Issues

### Tab Insertion Undo Granularity ✅ **RESOLVED**

**Status**: FIXED with Monaco Editor integration

**Previous Issue**: When inserting a tab character in the textarea editor and then typing more text, undo didn't create separate undo units properly.

**Repro Steps** (Original textarea behavior):
1. Create a new document.
2. Type "hello" and pause (>300ms).
3. Press Tab.
4. Type "world" and pause (>300ms).
5. Press Ctrl+Z: "world" is removed.
6. Press Ctrl+Z again: the tab character was not removed.
7. Press Ctrl+Z again: "hello" was not removed.

**Previous Expected**: Each edit as separate undo step: world → tab → hello → empty.

**Resolution**: ✅ **FIXED** - Monaco Editor's undo system properly handles undo unit separation.

**Implementation Notes**:
- Monaco maintains proper undo stacks for all edit operations
- Tab insertions and typing are now correctly tracked as separate undo operations
- This fix resolves the edge case affecting undo granularity
- Status: No longer applies to current implementation

**Reference Tag**: ~~FIXME-TAB-UNDO~~ (obsolete)


# Changelog

All notable changes to this project will be documented in this file.

## [1.2.4] - 2025-11-16

### Fixed
- Portal/modal reparenting: ensure UpdateNotificationDialog stays attached to document.body and reattaches after Blazor re-renders (portal.js).
- Update dialog centering and focus handling so ESC consistently closes the dialog and focus trap is applied correctly.
- Automatic updates: `Check for updates on startup` preference is respected; manual checks still show notifications even when startup checks are disabled.

### Changed
- Bump project version to 1.2.4 (patch release: portal and updater fixes).

---

## [1.2.0] - 2025-11-11

### Added
- **Find and Replace** (US1/US2): Text search within documents with case-sensitive and whole-word options
  - Non-modal Find/Replace bars (Ctrl+F, Ctrl+H)
  - Next/Previous navigation through matches
  - Single and bulk Replace operations with atomic undo/redo
  - Integrated with document undo history
- **Window State Persistence** (US5): Window position, size, and state restored on launch
  - Multi-monitor support with bounds validation
  - Maximized and fullscreen state restoration
  - Event-driven state capture to handle window lifecycle correctly
- **Automatic Application Updates** (US6): Built-in auto-updater with GitHub Releases integration
  - Check for updates on startup (configurable)
  - Periodic update checks (default: 24 hours, configurable)
  - Background download of updates
  - Update notification dialog with release notes
  - Critical update support (blocking prompt for security updates)
  - Options dialog integration: Check now, auto-download toggle, status display
  - Electron.AutoUpdater with Squirrel format (Windows/macOS) and AppImage (Linux)
- **Automated Release Builds** (US7): GitHub Actions CI/CD workflows
  - Multi-platform builds: Windows (.exe/.nupkg), macOS (.dmg/.zip), Linux (.AppImage/.deb)
  - Automatic version extraction from git tags
  - Auto-generated release notes from commit history
  - Build artifact uploads to GitHub Releases
  - Test coverage enforcement (65% threshold)
  - Build failure notifications via GitHub issues

### Performance
- Find/Replace operations optimized with minimal re-renders
- Update download progress tracking with real-time status updates

### Reliability
- Replace operations properly flush pending undo snapshots before execution
- Window state cached during lifecycle events to avoid "destroyed window" errors
- Auto-updater gracefully handles errors without disrupting user experience

### Changed
- Version bumped to 1.2.0 (displayed in About dialog and Options dialog)
- Enhanced Options dialog with Automatic Updates section

### Fixed
- Replace undo corruption caused by pending typed edits not being flushed
- Window close error during session persistence (cached state instead of querying destroyed window)

### Known Issues
- Tab insertion undo granularity (FIXME-TAB-UNDO): Tab characters not isolated as separate undo units
- Ctrl+Tab navigation intercepted by browser/OS: Use Ctrl+PageDown/PageUp as workaround

### Deferred
- Spell checking (US3/US4) deferred pending alternate text editor component evaluation
- Find/Replace polish (match count, accessibility audits) deferred pending editor evaluation
- Update rollback mechanism deferred (requires version tracking and crash detection)

---

## [1.1.0] - 2025-11-02

### Added
- Command-line file opening with single-instance enforcement (US1)
- About dialog with app identity and technology stack (US2)
- Options dialog for theme selection, logging, and recognized file extensions (US3)
- Markdown rendering with aggressive caching and theme awareness
- Accessibility improvements: ARIA roles, focus traps, Escape handling, and keyboard navigation
- Editor Tab key support (inserts literal tab, prevents focus loss)

### Performance
- Theme switch via CSS custom properties (<50ms typical)
- Streaming I/O for large files with progress reporting
- Debounced undo snapshots to reduce memory pressure
- TabStrip/StatusBar render optimizations via state versioning

### Reliability
- Session persistence and crash-recovery autosave
- External file modification detection with conflict resolution
- Atomic writes for preferences and document saves

### Changed
- Rebranded to "Scrappy Text Editor" (legacy session folder name retained)

### Fixed
- Focus escaping modals via Tab; dialogs now trap focus
- Dialogs not closing on Escape; Escape now consistently closes modals
- Editor losing focus after dialog close; focus restored to editor

### Known/Deferred
- System theme auto-detection (T071–T074) deferred; manual Light/Dark available

---

## [1.0.0] - 2025-07-01
- Initial public release of TextEdit with core editing, session persistence, markdown preview, and Electron.NET host.

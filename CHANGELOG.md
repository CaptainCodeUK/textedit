# Changelog

All notable changes to this project will be documented in this file.

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

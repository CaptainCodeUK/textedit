# Feature Specification: Scrappy Text Editor v1.1 Enhancements

**Feature Branch**: `002-v1-1-enhancements`  
**Created**: 30 October 2025  
**Status**: Draft  
**Input**: User description: "Command line handling, About box, Styling improvements, UI Options (dark mode, file extensions, logging), Toolbar (file ops, editing, markdown formatting), Menu icons, App icon (puppy theme), Rename to Scrappy Text Editor, Filename in title bar with dirty indicator"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Quick File Access via Command Line (Priority: P1)

Users need to quickly open files from their terminal or file manager by passing file paths as command-line arguments when launching the application. This is essential for developers and power users who work primarily from the command line and expect standard desktop application behavior.

**Why this priority**: This is fundamental desktop application behavior that users expect. Without it, the app feels incomplete and creates friction in common workflows. This delivers immediate value and can be independently verified.

**Independent Test**: Launch application from terminal with one or more file paths as arguments. Verify files open in separate tabs. This can be tested without any other v1.1 features being implemented.

**Acceptance Scenarios**:

1. **Given** the application is not running, **When** user launches with a single file path argument (e.g., `scrappy-text-editor /path/to/file.txt`), **Then** the application opens with that file loaded in a tab
2. **Given** the application is not running, **When** user launches with multiple file path arguments, **Then** the application opens with each file in its own tab, ordered as provided
3. **Given** the application is not running, **When** user launches with a non-existent file path, **Then** the application opens with an error message indicating the file was not found and offering to create it or cancel
4. **Given** the application is not running, **When** user launches with a file path that lacks read permissions, **Then** the application opens with a clear error message about insufficient permissions
5. **Given** the application is already running, **When** user launches with file path arguments, **Then** the files open in the existing window as new tabs (single-instance behavior)
6. **Given** user double-clicks a text file in file manager with Scrappy associated, **When** the file opens, **Then** it loads in the application with proper content display

---

### User Story 2 - Application Identity and Information (Priority: P1)

Users need to easily identify the application as "Scrappy Text Editor" across all touchpoints (title bar, menus, about box, app icon) and access version/technology information for support and troubleshooting purposes.

**Why this priority**: Strong branding and identity are essential for user trust and recognition. The about box provides critical information for support scenarios. This is a complete, independently testable feature.

**Independent Test**: Launch application and verify: (1) title bar shows "Scrappy Text Editor", (2) app icon displays puppy theme, (3) Help > About menu opens dialog showing version, technology stack, and copyright. No other features required.

**Acceptance Scenarios**:

1. **Given** application is open with no files, **When** user views the title bar, **Then** it displays "Scrappy Text Editor"
2. **Given** application has an active file named "Hello.txt", **When** user views the title bar, **Then** it displays "Hello.txt - Scrappy Text Editor"
3. **Given** application has an active file with unsaved changes, **When** user views the title bar, **Then** it displays the dirty indicator (e.g., "* Hello.txt - Scrappy Text Editor" or "Hello.txt • - Scrappy Text Editor")
4. **Given** user accesses Help menu, **When** user clicks "About Scrappy Text Editor", **Then** a dialog opens showing application name, version number, build date, brief description, and technology stack (Blazor, Electron.NET, .NET 8, Markdig)
5. **Given** user views the application icon in taskbar/dock, **When** application is running, **Then** the icon displays a cute puppy with pen/pad overlay matching the app's theme
6. **Given** user views the application window icon, **When** application is open, **Then** the window frame shows the puppy-themed icon

---

### User Story 3 - Visual Theme Customization (Priority: P2)

Users work in varying lighting conditions throughout the day and have personal preferences for interface themes. They need the ability to choose between light mode, dark mode, or system-following theme to reduce eye strain and match their operating system's appearance.

**Why this priority**: Dark mode is a highly requested feature for reducing eye strain during extended editing sessions. This is a complete feature slice that delivers significant user value independently of other enhancements.

**Independent Test**: Open Options dialog, toggle between light/dark/system themes, and verify UI updates accordingly. Restart application and confirm theme persists. This works without any toolbar or other v1.1 features.

**Acceptance Scenarios**:

1. **Given** user opens Options dialog, **When** user selects "Dark Mode" option, **Then** the entire UI switches to dark theme with light text on dark backgrounds using system-appropriate colors
2. **Given** user opens Options dialog, **When** user selects "Light Mode" option, **Then** the entire UI switches to light theme with dark text on light backgrounds
3. **Given** user opens Options dialog, **When** user selects "Follow System" option, **Then** the UI matches the operating system's current theme preference
4. **Given** user has selected "Follow System" and system theme changes, **When** the OS theme switches, **Then** the application updates its theme within 5 seconds without requiring restart
5. **Given** user has selected a theme preference, **When** user closes and restarts the application, **Then** the selected theme persists across sessions
6. **Given** dark mode is active, **When** user views syntax highlighting or markdown preview, **Then** colors are adjusted for dark background visibility

---

### User Story 4 - File Extension Management (Priority: P2)

Users work with various text file types beyond common extensions (e.g., custom config files, log files, specialized formats). They need the ability to configure which file extensions the application recognizes and handles as text files.

**Why this priority**: Power users need flexibility to work with diverse file types. This enables the application to be useful for a broader range of workflows (logs, configs, scripts). It's independently testable and valuable.

**Independent Test**: Open Options dialog, add/remove file extensions from the list, save preferences. Attempt to open files with newly added extensions and verify they open correctly. Verify persistence across application restarts.

**Acceptance Scenarios**:

1. **Given** user opens Options dialog, **When** user views File Extensions section, **Then** a list displays all currently recognized extensions (e.g., .txt, .md, .log, .json)
2. **Given** user is viewing File Extensions list, **When** user clicks "Add Extension" and enters a new extension (e.g., ".conf"), **Then** the extension is added to the list and validated for correct format
3. **Given** user is viewing File Extensions list, **When** user selects an extension and clicks "Remove", **Then** the extension is removed from the recognized list
4. **Given** user has added a custom extension, **When** user attempts to open a file with that extension, **Then** the file opens successfully in the editor
5. **Given** user has removed a default extension, **When** user attempts to open a file with that extension, **Then** the application prompts whether to open it as text or use system default handler
6. **Given** user enters an invalid extension format, **When** user attempts to save, **Then** the application displays a validation error explaining correct format (e.g., "Extensions must start with . and contain only alphanumeric characters")

---

### User Story 5 - Toolbar for Common Operations (Priority: P2)

Users need quick access to frequently used editing operations without navigating through menus. A toolbar with common file operations, clipboard actions, and text formatting provides faster workflow for repetitive tasks.

**Why this priority**: Toolbars significantly improve efficiency for common operations. This is a complete UI feature that works independently and delivers immediate productivity gains to users familiar with standard text editors.

**Independent Test**: Verify toolbar displays correctly with all buttons, click each toolbar button and confirm corresponding action executes (open file, save, cut/copy/paste, font changes, markdown formatting). This is testable without any other v1.1 features.

**Acceptance Scenarios**:

1. **Given** application is open, **When** user views the main window, **Then** a toolbar appears below the menu bar with clearly labeled/icon-based buttons for common operations
2. **Given** user clicks toolbar "Open" button, **When** the file dialog appears, **Then** it behaves identically to File > Open menu command
3. **Given** user clicks toolbar "Save" button, **When** the active document is saved, **Then** it behaves identically to File > Save menu command
4. **Given** user has selected text, **When** user clicks Cut/Copy/Paste toolbar buttons, **Then** they behave identically to Edit menu commands
5. **Given** user clicks a font name dropdown in toolbar, **When** user selects a different font, **Then** the editor's text font changes immediately for the active document
6. **Given** user clicks a font size dropdown in toolbar, **When** user selects a different size, **Then** the editor's text size changes immediately for the active document
7. **Given** user has text selected or cursor positioned, **When** user clicks markdown formatting buttons (H1, H2, Bold, Italic), **Then** the appropriate markdown syntax is inserted or wraps the selection
8. **Given** user has no document open, **When** user views toolbar, **Then** relevant buttons (Save, Cut, Copy, formatting) are disabled

---

### User Story 6 - Menu Icons for Visual Navigation (Priority: P3)

Users navigate menus more quickly when visual icons accompany text labels. Icons provide faster recognition of common commands and make the interface feel more modern and polished.

**Why this priority**: This is a polish feature that improves usability but isn't critical for functionality. It can be implemented independently as a visual enhancement layer over existing menu structure.

**Independent Test**: Open each menu and verify appropriate icons appear next to menu items. Verify icons are consistent with industry standards (e.g., floppy disk for Save, folder for Open). No functional changes required.

**Acceptance Scenarios**:

1. **Given** user opens the File menu, **When** viewing menu items, **Then** standard icons appear: Open (folder), Save (floppy disk), Close (X), Exit (door/arrow)
2. **Given** user opens the Edit menu, **When** viewing menu items, **Then** standard icons appear: Cut (scissors), Copy (two pages), Paste (clipboard), Undo (curved arrow left), Redo (curved arrow right)
3. **Given** user opens the View menu, **When** viewing menu items, **Then** relevant icons appear for theme/view options
4. **Given** user opens the Help menu, **When** viewing menu items, **Then** About icon displays (info or question mark)
5. **Given** icons are displayed, **When** user views them, **Then** they are clear, consistent in size, and follow the active UI theme (light/dark)

---

### User Story 7 - Logging Toggle for Troubleshooting (Priority: P3)

Users and support staff need the ability to enable detailed logging when troubleshooting issues. By default, logging should be minimal for performance, but users should be able to easily enable it when needed.

**Why this priority**: This is a support/diagnostic feature that's valuable but not part of the core editing experience. It can be implemented independently and tested by verifying log file generation based on toggle state.

**Independent Test**: Open Options dialog, toggle logging on/off, perform various actions, verify log files are created (when enabled) or minimal (when disabled). Verify toggle state persists across restarts.

**Acceptance Scenarios**:

1. **Given** user opens Options dialog, **When** user views Logging section, **Then** a toggle switch displays with current state (On/Off)
2. **Given** logging is disabled, **When** user enables logging toggle and saves, **Then** the application begins writing detailed logs to application data directory
3. **Given** logging is enabled, **When** user performs actions (file operations, edits, errors), **Then** timestamped entries appear in log files
4. **Given** logging is enabled, **When** user disables logging toggle and saves, **Then** the application stops writing detailed logs (only critical errors logged)
5. **Given** user has changed logging preference, **When** user restarts application, **Then** the logging preference persists
6. **Given** logging is enabled, **When** user opens Options dialog, **Then** a "View Logs" or "Open Log Folder" button is available for easy access

---

### User Story 8 - Enhanced Visual Styling (Priority: P3)

Users need a more visually appealing and modern interface that uses system colors appropriately. The interface should have better contrast and color differentiation while respecting system accessibility settings.

**Why this priority**: This is a polish feature that improves overall aesthetic and accessibility but doesn't change core functionality. It can be implemented independently as a styling layer over existing UI components.

**Independent Test**: Launch application and verify: (1) UI uses system accent colors where appropriate, (2) contrast ratios meet accessibility standards, (3) colors remain appropriate across light/dark themes. Use contrast checker tools to validate.

**Acceptance Scenarios**:

1. **Given** application is running, **When** user views the interface, **Then** active tabs, buttons, and interactive elements use system accent colors
2. **Given** application is in light mode, **When** user views text on backgrounds, **Then** contrast ratios meet WCAG AA standards (4.5:1 minimum)
3. **Given** application is in dark mode, **When** user views text on backgrounds, **Then** contrast ratios meet WCAG AA standards with appropriate dark-theme colors
4. **Given** user has high-contrast mode enabled in OS, **When** application runs, **Then** it respects system high-contrast settings
5. **Given** user interacts with UI elements, **When** hovering or focusing, **Then** visual feedback is clear with appropriate color changes
6. **Given** user views different sections (menu, toolbar, editor, status bar), **Then** visual hierarchy is clear with consistent color usage

---

### Edge Cases

- **Command-line with invalid paths**: What happens when user provides a mix of valid and invalid file paths? System should open valid files and show a summary notification of any failures.
- **Command-line with very long paths**: How does the title bar handle filenames with extremely long paths (>200 characters)? Title should truncate intelligently (e.g., show filename and truncated path).
- **Theme switching performance**: What happens when switching themes with many tabs open? Theme change should apply within 500ms regardless of tab count.
- **File extension conflicts**: What if user tries to add an extension that's already in the list? System should detect duplicates and show helpful message.
- **Font selection with unavailable fonts**: What if user selects a font that's not installed on their system? System should fall back to default monospace font and show notification.
- **Markdown formatting in non-markdown files**: What happens when user applies markdown formatting in a .txt file? Formatting should still work (insert markdown syntax) but preview may not be relevant.
- **Multiple instances without single-instance enforcement**: If single-instance fails, what happens with multiple instances and command-line args? Each instance should handle its own arguments independently.
- **About dialog data freshness**: How does the about box get accurate version information? Should be populated from assembly metadata at build time.
- **Logging file size limits**: What prevents log files from growing indefinitely? Implement log rotation (e.g., 10MB max per file, keep last 5 files).
- **Title bar with multiple dirty tabs**: How is the dirty indicator shown when multiple tabs have unsaved changes? Dirty indicator applies to active tab only; tab titles themselves show individual dirty states.
- **Icon scaling on high-DPI displays**: Does the app icon maintain quality on 4K/retina displays? Icon should include multiple resolutions (16x16 to 512x512) for proper scaling.

## Requirements *(mandatory)*

### Functional Requirements

**Command-Line Handling:**

- **FR-001**: System MUST accept zero or more file paths as command-line arguments at launch
- **FR-002**: System MUST open each valid file path provided as an argument in a separate tab
- **FR-003**: System MUST maintain argument order when creating tabs (first argument = first tab)
- **FR-004**: System MUST display clear error messages for file paths that cannot be opened (not found, no permissions, unsupported format)
- **FR-005**: System MUST support both absolute and relative file paths in command-line arguments
- **FR-006**: System MUST enforce single-instance behavior: if application is already running, new file arguments open in existing window
- **FR-007**: System MUST handle file paths with spaces and special characters correctly when passed as arguments

**Application Identity (Branding and Title Bar):**

- **FR-008**: Application name MUST be "Scrappy Text Editor" in all user-facing locations (title bar, about box, menus, dialogs)
- **FR-009**: Title bar MUST display format: "[filename] - Scrappy Text Editor" when a file is active
- **FR-010**: Title bar MUST display format: "Scrappy Text Editor" when no file is active or new untitled document
- **FR-011**: Title bar MUST include dirty indicator (asterisk or bullet) before filename when document has unsaved changes
- **FR-012**: Application icon MUST feature a puppy character with pen or notepad visual element
- **FR-013**: Application icon MUST include multiple resolutions (16x16, 32x32, 48x48, 64x64, 128x128, 256x256, 512x512) for different display contexts

**About Dialog:**

- **FR-014**: System MUST provide "About Scrappy Text Editor" menu item in Help menu
- **FR-015**: About dialog MUST display application name ("Scrappy Text Editor")
- **FR-016**: About dialog MUST display version number (semantic versioning format: X.Y.Z)
- **FR-017**: About dialog MUST display build date or release date
- **FR-018**: About dialog MUST list core technologies: Blazor Server, Electron.NET, .NET 8, Markdig
- **FR-019**: About dialog MUST display copyright information and license type
- **FR-020**: About dialog MUST include a tagline or brief description (e.g., "A friendly, lightweight text editor for everyday writing and markdown")

**Theme/Dark Mode:**

- **FR-021**: System MUST provide three theme options: Light Mode, Dark Mode, Follow System
- **FR-022**: System MUST apply theme consistently across all UI components (menus, toolbar, editor, status bar, dialogs)
- **FR-023**: System MUST persist user's theme preference across application sessions
- **FR-024**: When "Follow System" is selected, system MUST detect OS theme changes and update UI accordingly within 5 seconds
- **FR-025**: Dark mode color scheme MUST use light text on dark backgrounds with sufficient contrast (WCAG AA: 4.5:1 minimum)
- **FR-026**: Light mode color scheme MUST use dark text on light backgrounds with sufficient contrast (WCAG AA: 4.5:1 minimum)
- **FR-027**: System MUST adjust markdown preview rendering to match selected theme
- **FR-028**: System MUST respect OS high-contrast mode settings when active

**File Extension Management:**

- **FR-029**: System MUST provide UI for viewing list of recognized text file extensions
- **FR-030**: System MUST allow users to add custom file extensions to the recognized list
- **FR-031**: System MUST validate extension format: must start with period, contain only alphanumeric characters and hyphens
- **FR-032**: System MUST allow users to remove extensions from the recognized list (except critical defaults: .txt, .md)
- **FR-033**: System MUST prevent duplicate extensions in the list
- **FR-034**: System MUST persist file extension preferences across application sessions
- **FR-035**: System MUST use updated extension list when determining how to open files (via open dialog or command line)
- **FR-036**: Default recognized extensions MUST include: .txt, .md, .log, .json, .xml, .csv, .ini, .cfg, .conf

**Logging Toggle:**

- **FR-037**: System MUST provide toggle control for enabling/disabling detailed logging
- **FR-038**: When logging is disabled, system MUST log only critical errors and startup/shutdown events
- **FR-039**: When logging is enabled, system MUST log detailed information: file operations, user actions, errors, performance metrics
- **FR-040**: System MUST persist logging preference across application sessions
- **FR-041**: Log files MUST include timestamps, log level, and descriptive messages
- **FR-042**: System MUST implement log rotation: maximum 10MB per log file, retain last 5 files
- **FR-043**: System MUST store log files in standard application data directory for the OS
- **FR-044**: System MUST provide easy access to log folder via Options dialog or Help menu

**Toolbar:**

- **FR-045**: System MUST display toolbar below menu bar with buttons for common operations
- **FR-046**: Toolbar MUST include file operation buttons: Open, Save
- **FR-047**: Toolbar MUST include clipboard operation buttons: Cut, Copy, Paste
- **FR-048**: Toolbar MUST include font selection dropdown showing available system fonts
- **FR-049**: Toolbar MUST include font size selection dropdown or spinner (range: 8pt to 72pt)
- **FR-050**: Toolbar MUST include markdown formatting buttons: H1, H2, Bold, Italic, Code, Bulleted List, Numbered List
- **FR-051**: Toolbar buttons MUST execute same commands as their menu equivalents
- **FR-052**: Toolbar buttons MUST show tooltips on hover explaining their function
- **FR-053**: Toolbar buttons that don't apply to current state MUST be visually disabled (e.g., Save when no changes, Cut/Copy with no selection)
- **FR-054**: Font and size changes via toolbar MUST apply immediately to active editor pane
- **FR-055**: Markdown formatting buttons MUST insert appropriate syntax: wrap selection or insert at cursor position

**Menu Icons:**

- **FR-056**: System MUST display icons next to menu items where appropriate
- **FR-057**: Menu icons MUST use industry-standard symbols: folder (Open), floppy disk (Save), scissors (Cut), two pages (Copy), clipboard (Paste), curved arrows (Undo/Redo)
- **FR-058**: Menu icons MUST be consistent in size and style across all menus
- **FR-059**: Menu icons MUST adapt to active theme (light/dark) for visibility

**Enhanced Styling:**

- **FR-060**: System MUST use OS-provided accent colors for active UI elements (active tabs, selected items, focused controls)
- **FR-061**: System MUST ensure all text-on-background combinations meet WCAG AA contrast requirements (4.5:1 for normal text, 3:1 for large text)
- **FR-062**: System MUST provide clear visual feedback for interactive elements: hover states, focus indicators, active states
- **FR-063**: System MUST maintain visual consistency across all UI components regarding color palette, spacing, and typography

### Key Entities

- **Application Configuration**: Stores user preferences including theme selection (Light/Dark/System), file extension list, logging enabled flag, font preferences (name, size), window position/size
- **Command-Line Arguments**: Collection of file paths provided at application launch, parsed and validated before opening
- **Theme Definition**: Set of color values and style rules for Light Mode, Dark Mode, and System-Follow mode; includes background, foreground, accent, and semantic colors
- **File Extension Registry**: List of recognized extensions that application treats as text files; includes defaults and user additions
- **About Information**: Application metadata including version, build date, technology stack, copyright; sourced from assembly attributes
- **Log Entry**: Record of application event including timestamp, severity level, message, and optional context data
- **Toolbar Action**: Representation of toolbar button or control linked to underlying command; includes enabled state, icon, tooltip
- **Application Icon Asset**: Multi-resolution image set for app icon (puppy theme) used in taskbar, window frame, about dialog, and OS file associations

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can launch application with file paths from command line and see all valid files open in tabs within 3 seconds
- **SC-002**: Users can identify application as "Scrappy Text Editor" within 2 seconds of opening by viewing title bar or app icon
- **SC-003**: Users can access about dialog and view complete version/technology information in under 5 seconds from application launch
- **SC-004**: Users can switch between light and dark themes with UI update completing within 500 milliseconds
- **SC-005**: Selected theme persists across application restarts 100% of the time
- **SC-006**: Users can add or remove file extensions in under 30 seconds with changes taking effect immediately
- **SC-007**: Users can enable logging and verify log file creation within 10 seconds of performing any action
- **SC-008**: All toolbar operations (file, clipboard, formatting) execute within 200 milliseconds of button click
- **SC-009**: Font changes via toolbar apply immediately (under 100ms) to active editor
- **SC-010**: Markdown formatting buttons correctly insert syntax wrapping selected text or at cursor position 100% of the time
- **SC-011**: All text-on-background combinations achieve minimum 4.5:1 contrast ratio verified by automated accessibility tools
- **SC-012**: Application icon displays clearly at all sizes (16x16 to 512x512) with puppy character recognizable at smallest size
- **SC-013**: Title bar updates filename and dirty indicator within 100ms of file change or save action
- **SC-014**: Menu icons are visible and appropriately styled in both light and dark themes with no color clipping or visibility issues
- **SC-015**: 95% of users can complete primary new workflows (open via command line, change theme, format markdown via toolbar) on first attempt without documentation

### Testing & Quality Requirements

- Unit test coverage MUST maintain minimum 65% line coverage across all new code components
- Integration tests MUST verify command-line argument parsing with various path formats (absolute, relative, with spaces, invalid)
- Integration tests MUST verify theme switching updates all UI components correctly
- Accessibility tests MUST verify WCAG AA compliance for all theme combinations using automated tools
- Visual regression tests SHOULD capture screenshots of toolbar, themed UI, and about dialog for comparison
- Performance tests MUST verify theme switching completes within 500ms threshold with 10+ tabs open
- Manual testing MUST verify app icon appearance on Windows, macOS, and Linux at multiple DPI settings
- Manual testing MUST verify single-instance behavior and command-line file opening across supported platforms

### UX & Accessibility Requirements

- All new UI controls (toolbar buttons, options toggles) MUST be keyboard accessible with visible focus indicators
- Screen readers MUST announce toolbar button purposes and current states (enabled/disabled)
- Tooltips MUST appear within 500ms of hover for all toolbar buttons and icons
- Theme colors MUST support users with color vision deficiencies (avoid red/green only indicators)
- About dialog MUST be keyboard navigable and dismissible via Escape key
- File extension management UI MUST provide clear error messages for invalid input with suggested corrections
- Command-line error messages MUST be specific and actionable (not just "File not found")

### Documentation Requirements

- User-facing documentation MUST include command-line usage examples for single and multiple files
- User-facing documentation MUST include instructions for accessing and using Options dialog
- README MUST be updated with new application name "Scrappy Text Editor"
- Build documentation MUST include instructions for creating multi-resolution app icons
- Changelog MUST document all v1.1 features with clear descriptions for end users

### Performance Requirements

- Application startup time MUST remain under 2 seconds even with command-line arguments (up to 10 files)
- Theme switching MUST complete within 500ms regardless of number of open tabs
- Toolbar button response MUST be under 200ms for all operations
- Log file writing MUST not introduce perceptible lag (under 10ms) during normal editing operations
- Font changes via toolbar MUST apply within 100ms to active editor

### Assumptions

- Application will continue using Electron.NET for cross-platform desktop support, enabling consistent command-line argument handling across Windows, macOS, and Linux
- System theme detection will use OS-provided APIs available through Electron
- Font selection will be limited to monospace fonts installed on user's system
- Application icon design (puppy with pen/pad) will be created as a separate design asset, likely commissioned or sourced from graphics team
- Single-instance enforcement will use file-based locking or named mutex (implementation detail)
- Log file location will follow OS conventions: `%AppData%\ScrappyTextEditor\logs` (Windows), `~/Library/Logs/ScrappyTextEditor` (macOS), `~/.config/scrappy-text-editor/logs` (Linux)
- Markdown formatting buttons will use standard syntax (e.g., `**bold**`, `_italic_`, `# H1`) compatible with CommonMark/GFM
- File extension validation will be case-insensitive (e.g., .TXT and .txt treated identically)
- Options dialog will be modal and accessed via Edit > Options (Windows/Linux) or Application > Preferences (macOS)
- Theme preference, file extensions, and logging toggle will be stored in existing application settings persistence mechanism
- About dialog will include a "Close" button and be dismissible via Escape key or clicking outside (if non-modal)
- Toolbar will be dockable or fixed below menu bar (not customizable in v1.1)
- Contrast ratios will target WCAG AA (4.5:1) rather than AAA (7:1) as reasonable baseline for v1.1
- Application will maintain existing session persistence and autosave behavior alongside new features

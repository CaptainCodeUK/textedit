# Feature Specification: Scrappy Text Editor v1.2 Enhancements

**Feature Branch**: `003-v1-2-enhancements`  
**Created**: 3 November 2025  
**Status**: Draft  
**Input**: User description: "Find/find and replace, spell checking with dictionaries, remember window state, auto-updater with Squirrel, and GitHub actions for release builds"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Text Search within Document (Priority: P1)

Users need to quickly locate specific text within their documents, especially when working with long files. This is a fundamental editor capability that enables efficient navigation and content review. Without search functionality, users must manually scroll through potentially thousands of lines to find what they need.

**Why this priority**: Text search is a fundamental text editor feature that users expect. It's essential for basic productivity and can be developed and tested independently. Without this, the editor feels incomplete for professional use.

**Independent Test**: Open a document with multiple paragraphs. Access Find dialog (Ctrl+F/Cmd+F), enter search term, verify highlighting and navigation through matches. This can be tested without any other v1.2 features.

**Acceptance Scenarios**:

1. **Given** a document with text content is open, **When** user presses Ctrl+F (Windows/Linux) or Cmd+F (macOS), **Then** a Find dialog appears with focus on the search input field
2. **Given** the Find dialog is open with a search term entered, **When** user presses Enter or clicks "Find Next", **Then** the first occurrence after current cursor position is highlighted and scrolled into view
3. **Given** the Find dialog is open with a search term that has multiple matches, **When** user repeatedly clicks "Find Next", **Then** each occurrence is highlighted in sequence, wrapping to document start after the last match
4. **Given** the Find dialog is open with a search term entered, **When** user clicks "Find Previous", **Then** the previous occurrence is highlighted and scrolled into view
5. **Given** the Find dialog is open with a search term that has no matches, **When** user attempts to find, **Then** a message indicates "No matches found"
6. **Given** the Find dialog is open, **When** user enables "Match case" option, **Then** only case-sensitive matches are found
7. **Given** the Find dialog is open, **When** user enables "Match whole word" option, **Then** only complete word matches are found (not partial matches within words)
8. **Given** text matches are highlighted in the document, **When** user closes the Find dialog, **Then** highlights remain visible until cleared or document is edited
9. **Given** the Find dialog is open, **When** user presses Escape, **Then** the dialog closes and focus returns to the editor

---

### User Story 2 - Find and Replace Text (Priority: P1)

Users need to modify multiple instances of the same text efficiently, such as renaming variables, fixing typos, or updating terminology across a document. Manual replacement is error-prone and time-consuming. This extends the find capability to enable bulk content modification.

**Why this priority**: Find and replace is essential for editing workflows. It builds on Find (P1) but can still be tested independently - users can open the replace dialog, perform individual or bulk replacements, and verify results.

**Independent Test**: Open a document with repeated text. Open Find and Replace dialog (Ctrl+H/Cmd+H), enter find and replace terms, use "Replace" and "Replace All" buttons, verify changes. Works independently of other v1.2 features.

**Acceptance Scenarios**:

1. **Given** a document with text content is open, **When** user presses Ctrl+H (Windows/Linux) or Cmd+H (macOS), **Then** a Find and Replace dialog appears with find and replace input fields
2. **Given** the Find and Replace dialog is open with find and replace terms entered, **When** user clicks "Replace", **Then** the current match is replaced and the next match is highlighted
3. **Given** the Find and Replace dialog is open with find and replace terms entered, **When** user clicks "Replace All", **Then** all occurrences in the document are replaced simultaneously
4. **Given** Find and Replace has modified content, **When** replacements are made, **Then** the document is marked as dirty and undo history captures the change
5. **Given** the Find and Replace dialog is open with "Replace All" used, **When** multiple replacements occur, **Then** a status message indicates the count of replacements made (e.g., "Replaced 12 occurrences")
6. **Given** the Find and Replace dialog is open with no matches found, **When** user attempts to replace, **Then** a message indicates "No matches found" and no changes are made
7. **Given** the Find and Replace dialog is open, **When** user uses "Replace All" with case sensitivity enabled, **Then** only case-matching occurrences are replaced
8. **Given** Find and Replace modifies content, **When** user presses Ctrl+Z/Cmd+Z, **Then** all replacements from "Replace All" are undone as a single operation

---

### User Story 3 - Spell Checking with Built-in Dictionary (Priority: P2)


Users need to identify and correct spelling errors in their documents to ensure professional quality content. Manual proofreading is time-consuming and misses errors. A built-in spell checker provides real-time feedback for common English words without requiring additional configuration.

**Implementation Note:**
Spell checking will use the open-source [WeCantSpell.Hunspell](https://github.com/WeCantSpell/Hunspell) library (MIT license), a modern .NET implementation of Hunspell. This provides robust dictionary support, Unicode, and cross-platform compatibility. Custom and built-in dictionaries will be loaded using Hunspell's standard formats. No proprietary or closed-source components will be used.

**Rationale:**
- Hunspell is the industry standard for spell checking (used by LibreOffice, Firefox, Chrome).
- WeCantSpell.Hunspell is actively maintained, .NET-native, and supports custom/user dictionaries.
- Avoids reinventing spell checking logic and leverages proven algorithms.

**Why this priority**: Spell checking significantly improves content quality and is expected in modern text editors. The built-in dictionary provides immediate value without user configuration. This is independently testable - type text with misspellings and verify visual indicators.

**Independent Test**: Type a document with correct and incorrect spellings. Verify misspelled words are underlined with visual indicators. Right-click misspelled word and verify correction suggestions appear. This works without custom dictionaries or other v1.2 features.

**Acceptance Scenarios**:

1. **Given** a document is open, **When** user types a word not found in the built-in dictionary, **Then** the word is underlined with a red wavy line indicating a potential misspelling
2. **Given** a misspelled word is underlined, **When** user right-clicks the word, **Then** a context menu appears showing up to 10 suggested corrections ranked by likelihood
3. **Given** a context menu with spelling suggestions is shown, **When** user clicks a suggestion, **Then** the misspelled word is replaced with the selected suggestion
4. **Given** spell checking is enabled, **When** user types a correctly spelled word from the built-in dictionary, **Then** no underline appears
5. **Given** the Options dialog is open, **When** user toggles "Enable spell checking", **Then** spell checking is immediately enabled or disabled across all open documents
6. **Given** spell checking is enabled, **When** user opens a large document, **Then** spell checking completes within 3 seconds for documents up to 10,000 words
7. **Given** spell checking is active, **When** user types in a code block or fenced section, **Then** spell checking is disabled within code blocks and markdown fenced sections; no misspelling indicators appear inside those regions
8. **Given** a word is underlined as misspelled, **When** user's cursor is positioned within the word, **Then** the underline remains visible and accessible for correction

---

### User Story 4 - Custom Dictionary Management (Priority: P3)

Users work with domain-specific terminology, proper nouns, technical jargon, and brand names that aren't in standard dictionaries. They need to teach the spell checker their vocabulary to avoid false positives and improve editing efficiency. This extends the built-in spell checker with personalization.

**Why this priority**: Custom dictionaries reduce noise from legitimate but non-standard words. This depends on base spell checking (P2) but can be tested independently - add custom words, verify they're no longer flagged, persist across sessions.

**Independent Test**: With spell checking enabled, add a custom word to the dictionary via context menu or Options. Verify it's no longer flagged in any document. Restart application and confirm custom words persist.

**Acceptance Scenarios**:

1. **Given** a word is underlined as misspelled, **When** user right-clicks and selects "Add to Dictionary", **Then** the word is added to the custom dictionary and underline is removed
2. **Given** a word is in the custom dictionary, **When** user types that word in any document, **Then** it is not flagged as misspelled
3. **Given** the Options dialog is open, **When** user navigates to the "Spelling" section, **Then** a list of all custom dictionary words is displayed alphabetically
4. **Given** the custom dictionary list is displayed, **When** user selects a word and clicks "Remove", **Then** the word is removed from the dictionary and will be flagged in future documents
5. **Given** the custom dictionary list is displayed, **When** user clicks "Add Word" and enters a term, **Then** the term is added to the custom dictionary
6. **Given** custom dictionary words have been added, **When** user closes and restarts the application, **Then** all custom dictionary entries persist and remain active
7. **Given** the custom dictionary list is displayed, **When** user clicks "Clear All", **Then** a confirmation prompt appears and, if confirmed, all custom words are removed
8. **Given** a custom dictionary file exists, **When** the file is located in the application data directory, **Then** it is stored in a human-readable format (plain text, one word per line) for manual editing

---

### User Story 5 - Window State Persistence (Priority: P2)

Users position and size their application windows to fit their workflow and multi-monitor setups. They expect the application to remember their preferences and restore the same window configuration on next launch. This provides a seamless, personalized experience without manual adjustment every time.

**Why this priority**: Window state persistence is a standard desktop application behavior that significantly improves user experience. It can be tested independently by positioning/resizing the window, restarting, and verifying restoration. Delivers immediate quality-of-life value.

**Independent Test**: Resize and reposition application window, maximize/minimize it, close application. Restart and verify window opens at exact same position, size, and state. Test across multiple monitors if available.

**Acceptance Scenarios**:

1. **Given** user has positioned the application window at specific screen coordinates, **When** user closes and restarts the application, **Then** the window reopens at the exact same position
2. **Given** user has resized the application window to custom dimensions, **When** user closes and restarts the application, **Then** the window reopens with the exact same width and height
3. **Given** user has maximized the application window, **When** user closes and restarts the application, **Then** the window reopens in maximized state
4. **Given** user has minimized the application window before closing, **When** the application restarts, **Then** the window opens in normal (non-minimized) state at last known position/size
5. **Given** user has positioned the window on a secondary monitor, **When** user closes and restarts with the same monitor configuration, **Then** the window reopens on the secondary monitor
6. **Given** user has positioned the window on a secondary monitor that is no longer connected, **When** user restarts the application, **Then** the window opens on the primary monitor at default position
7. **Given** the window state data is corrupted or invalid, **When** the application starts, **Then** it opens with default centered position and reasonable default size
8. **Given** user is on Windows/Linux, **When** user restores from maximized state before closing, **Then** the non-maximized size and position are correctly restored on next launch

---

### User Story 6 - Automatic Application Updates (Priority: P2)

Users need to stay current with bug fixes, security patches, and new features without manually checking for updates or downloading installers. The application should automatically detect, download, and install updates with minimal user disruption. This ensures users always have the best version with minimal effort.

**Why this priority**: Auto-updates improve security, reduce support burden, and ensure users benefit from improvements. This is a complete feature that can be tested independently by releasing a new version and verifying update detection/installation.

**Independent Test**: With a mock update server, publish a new version. Verify application detects update, downloads in background, prompts user to restart, and successfully installs. This works independently of other features.

**Acceptance Scenarios**:

1. **Given** the application starts or is running, **When** a new version is available on the update server, **Then** the application detects the update within 5 minutes
2. **Given** an update has been detected, **When** the download completes, **Then** a non-intrusive notification appears indicating "Update available - restart to install"
3. **Given** an update notification is shown, **When** user clicks "Restart and Update", **Then** the application closes, installs the update, and relaunches with the new version
4. **Given** an update notification is shown, **When** user clicks "Remind Me Later" or dismisses the notification, **Then** the notification reappears after 24 hours
5. **Given** an update is downloading, **When** user is actively editing documents, **Then** download occurs in the background without impacting editor performance
6. **Given** the application is closed with an update pending, **When** user next launches the application, **Then** the update is installed before the main window appears
7. **Given** an update installation fails, **When** the error occurs, **Then** the application remains on the current version and logs the error for troubleshooting
8. **Given** the Options dialog is open, **When** user navigates to the "Updates" section, **Then** options to "Check for updates now" and "Automatically download updates" are available
9. **Given** automatic updates are disabled, **When** user manually checks for updates, **Then** the application detects and offers to download/install available updates
10. **Given** a critical security update is released, **When** the application detects it, **Then** the user is prompted with a clearly labeled critical update dialog; installation proceeds only after user confirmation and the messaging emphasizes urgency

---

### User Story 7 - Automated Release Builds (Priority: P1)

The development team needs to efficiently create distributable packages (installers, archives) for Windows, macOS, and Linux whenever changes are merged to the main branch. Manual builds are time-consuming, error-prone, and delay releases. Automated builds ensure consistent, reproducible releases with every merge.

**Why this priority**: This is essential infrastructure for delivering value to users. Without it, releasing updates (including other v1.2 features) is bottlenecked by manual processes. This is independently testable - trigger a build via git push and verify artifacts are produced.

**Independent Test**: Push a commit to main branch. Verify GitHub Actions workflow triggers, builds complete for all platforms (Windows .exe/.msi, macOS .dmg, Linux .deb/.AppImage), and artifacts are attached to GitHub release. This works independently of user-facing features.

**Acceptance Scenarios**:

1. **Given** a commit is pushed to the main branch, **When** the push completes, **Then** a GitHub Actions workflow automatically triggers within 1 minute
2. **Given** the build workflow is triggered, **When** the build executes, **Then** separate jobs build Windows (.exe, .msi), macOS (.dmg), and Linux (.deb, .AppImage) artifacts
3. **Given** the build workflow completes successfully, **When** builds finish, **Then** all platform artifacts are uploaded and attached to a draft GitHub release
4. **Given** the build workflow encounters an error, **When** a build fails, **Then** the workflow stops and notifies the team via GitHub notifications
5. **Given** version information is needed, **When** the build executes, **Then** the version number is automatically extracted from project configuration and embedded in artifacts
6. **Given** a GitHub release is created, **When** artifacts are attached, **Then** release notes are auto-generated from commit messages since last release
7. **Given** builds complete successfully, **When** artifacts are published, **Then** the auto-updater mechanism can detect and download them
8. **Given** builds are running, **When** multiple commits are pushed rapidly, **Then** any in-progress or queued builds for earlier commits are canceled and only the latest commit is built; canceled runs are marked accordingly in CI
9. **Given** a build workflow completes, **When** all artifacts are ready, **Then** the draft release is automatically published (or requires manual approval)

---

### Edge Cases

- **Find/Replace with empty replace string**: What happens when user performs "Replace All" with an empty replacement value? (Expected: All matches are deleted, operation is undoable as single action)
- **Find/Replace with regex special characters**: How does the system handle search terms containing special characters like `*`, `?`, `[`, `]` when regex mode is not enabled? (Expected: Treat all characters literally, escape special chars automatically)
- **Spell checking during rapid typing**: How does spell checking perform when user types very quickly or pastes large blocks of text? (Expected: Spell check updates are debounced, do not block typing, complete within 3 seconds for 10,000 words)
- **Custom dictionary with non-ASCII characters**: What happens when user adds words with accented characters, emoji, or non-Latin scripts to the custom dictionary? (Expected: All Unicode characters are supported and stored correctly)
- **Window state on resolution change**: How does the system handle window state restoration when user changes screen resolution or display scaling between sessions? (Expected: Window scales proportionally or resets to safe default if previous position is off-screen)
- **Update during active editing**: What happens if user is actively editing a document with unsaved changes when an update requires restart? (Expected: User is prompted to save or discard changes before update proceeds)
- **Update server unreachable**: How does the application behave when the update server is down or network connection is unavailable? (Expected: Silently fails update check, retries after 24 hours, does not impact normal functionality)
- **Build workflow with failing tests**: What happens when automated tests fail during the build process? (Expected: Build workflow fails, no artifacts are published, team is notified)
- **Find dialog with document changes**: What happens when user has Find dialog open and another user/process modifies the underlying file, triggering an external change dialog? (Expected: Find dialog remains functional, operates on current in-memory content regardless of external changes)
- **Spell checking in very large documents**: How does spell checking perform in documents exceeding 100,000 words? (Expected: Spell checking may be disabled or limited to visible portions in very large documents to maintain performance)

## Requirements *(mandatory)*

### Functional Requirements

**Find and Replace**:

- **FR-001**: System MUST provide a Find dialog accessible via keyboard shortcut (Ctrl+F/Cmd+F) that accepts text input and searches within the active document
- **FR-002**: System MUST highlight all matches of the search term in the active document with distinct visual styling
- **FR-003**: System MUST support navigation between search matches using "Find Next" and "Find Previous" controls, wrapping to document start/end
- **FR-004**: System MUST provide case-sensitive and whole-word matching options that can be toggled independently
- **FR-005**: System MUST provide a Find and Replace dialog accessible via keyboard shortcut (Ctrl+H/Cmd+H) with separate find and replace input fields
- **FR-006**: System MUST support single replacement ("Replace" button) and bulk replacement ("Replace All" button) operations
- **FR-007**: System MUST count and report the number of replacements made during "Replace All" operations
- **FR-008**: System MUST treat all replacement operations as undoable actions in the undo history, with "Replace All" as a single atomic operation
- **FR-009**: System MUST keep Find/Replace dialogs non-modal, allowing document editing while dialog is open


**Spell Checking**:

- **FR-010**: System MUST use WeCantSpell.Hunspell for spell checking, loading a built-in English dictionary in Hunspell format
- **FR-011**: System MUST visually indicate potentially misspelled words with red wavy underlines in real-time as user types
- **FR-012**: System MUST provide spelling suggestions via right-click context menu on misspelled words, showing up to 10 ranked suggestions (from Hunspell)
- **FR-013**: System MUST allow users to replace misspelled words with suggestions via single click in context menu
- **FR-014**: System MUST support adding words to a custom user dictionary (Hunspell .dic/.aff or plain text) via "Add to Dictionary" context menu option
- **FR-015**: System MUST persist custom dictionary entries in user preferences directory across application sessions
- **FR-016**: System MUST provide Options dialog section for managing custom dictionary (view, add, remove words)
- **FR-017**: System MUST store custom dictionary in human-readable plain text or Hunspell .dic format, one word per line
- **FR-018**: System MUST complete spell checking for documents up to 10,000 words within 3 seconds on modern hardware
- **FR-019**: System MUST allow users to enable/disable spell checking globally via Options dialog with immediate effect

**Window State Persistence**:

- **FR-020**: System MUST save window position (X, Y coordinates), size (width, height), and state (normal, maximized) on application close
- **FR-021**: System MUST restore window position, size, and state on application startup if previously saved state exists
- **FR-022**: System MUST validate restored window position is within current screen bounds before applying
- **FR-023**: System MUST reset to default centered position and size if saved state is invalid, corrupted, or positions window off-screen
- **FR-024**: System MUST handle multi-monitor scenarios by restoring window on correct monitor if available, or primary monitor if not
- **FR-025**: System MUST open in normal (non-minimized) state even if previously minimized, using last known normal position/size

**Auto-Updater**:

- **FR-026**: System MUST periodically check for application updates by querying a configured update server endpoint
- **FR-027**: System MUST check for updates on application startup and every 24 hours while running
- **FR-028**: System MUST download updates in background without blocking user interface or editor functionality
- **FR-029**: System MUST display non-intrusive notification when update is ready to install, with options to "Restart and Update" or "Remind Me Later"
- **FR-030**: System MUST apply pending updates on next application launch before main window appears
- **FR-031**: System MUST maintain current application version if update installation fails and log error details
- **FR-032**: System MUST provide Options dialog section for update preferences including "Check for updates now" and "Automatically download updates" toggle
- **FR-033**: System MUST handle update check failures gracefully, logging errors and retrying after 24 hours without user notification

**Automated Release Builds**:

- **FR-034**: System MUST include GitHub Actions workflow that triggers on commits pushed to main branch
- **FR-035**: System MUST build Windows distributable packages (.exe installer, .msi installer) as part of automated workflow
- **FR-036**: System MUST build macOS distributable packages (.dmg disk image) as part of automated workflow
- **FR-037**: System MUST build Linux distributable packages (.deb package, .AppImage) as part of automated workflow
- **FR-038**: System MUST extract version information from project configuration and embed in build artifacts
- **FR-039**: System MUST upload completed build artifacts to GitHub release as attachments
- **FR-040**: System MUST generate release notes from commit messages since previous release
- **FR-041**: System MUST fail workflow and notify team if any platform build fails or tests fail
- **FR-042**: System MUST produce artifacts that are compatible with the auto-updater update detection and installation mechanism

### Key Entities *(include if feature involves data)*

- **FindQuery**: Represents a search operation with search term, options (case-sensitive, whole-word), current match index, and total match count
- **ReplaceOperation**: Extends FindQuery with replacement term and operation type (single, all), captures count of replacements made
- **SpellCheckSuggestion**: Represents a spelling correction suggestion with original word, suggested word, and confidence ranking
- **CustomDictionary**: Collection of user-added words stored persistently, supports add/remove operations, loaded on application start
- **WindowState**: Captures window geometry (x, y, width, height), window state enum (normal, maximized, minimized), monitor identifier
- **UpdateMetadata**: Information about an available update including version number, download URL, release notes, file size, checksum
- **ReleaseArtifact**: Build output for a specific platform including filename, file size, download URL, platform identifier, version number

## Success Criteria *(mandatory)*

### Measurable Outcomes

**Find and Replace**:

- **SC-001**: Users can locate any text in a document within 5 seconds of opening Find dialog
- **SC-002**: Users can perform bulk text replacements (10+ occurrences) in under 10 seconds including dialog interaction
- **SC-003**: Find operations on documents up to 50,000 words complete with visible highlighting within 2 seconds
- **SC-004**: 90% of users successfully use Find and Replace features on first attempt without consulting help documentation

**Spell Checking**:

- **SC-005**: Spell checking identifies at least 95% of common English misspellings with appropriate suggestions
- **SC-006**: Spell checking completes for documents up to 10,000 words within 3 seconds without impacting editor responsiveness
- **SC-007**: Custom dictionary operations (add, remove word) complete instantly with immediate visual feedback
- **SC-008**: Users report 40% reduction in spelling errors in published documents compared to pre-spell-check usage

**Window State Persistence**:

- **SC-009**: Window state restoration success rate exceeds 98% across normal startup scenarios
- **SC-010**: Users report zero manual window resizing/repositioning needed after application restart in 95% of sessions
- **SC-011**: Window state restoration completes within 100ms of application launch, imperceptible to users

**Auto-Updater**:

- **SC-012**: 80% of users install updates within 48 hours of release through auto-update mechanism
- **SC-013**: Update detection occurs within 5 minutes of application startup or every 24 hours
- **SC-014**: Update downloads complete in under 5 minutes on broadband connections (5+ Mbps)
- **SC-015**: Update installation success rate exceeds 95%, with automatic rollback on failure
- **SC-016**: Support tickets related to "how to update" or "outdated version" issues decrease by 70%

**Automated Release Builds**:

- **SC-017**: Build workflow completes all platform builds in under 30 minutes from commit push to artifact availability
- **SC-018**: Build success rate on main branch exceeds 95% over 3-month period
- **SC-019**: Time from feature merge to release availability decreases from hours/days to under 1 hour
- **SC-020**: Zero manual build steps required for standard releases after initial workflow setup

### Quality Criteria

- **SC-021**: All find/replace operations maintain undo/redo history integrity with no data corruption
- **SC-022**: Spell checking produces zero false positives for words in built-in dictionary
- **SC-023**: Window state data survives application crashes and is resilient to corruption
- **SC-024**: Update mechanism fails safely - never results in non-functional application after failed update
- **SC-025**: Build artifacts pass all automated tests before publication to release channel
- **SC-026**: All features maintain accessibility standards including keyboard navigation and screen reader support
- **SC-027**: Features add less than 5MB to application download size and less than 200ms to startup time

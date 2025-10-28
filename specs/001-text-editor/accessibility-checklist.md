# Accessibility Testing Checklist

## Keyboard Navigation
- [ ] All menu items accessible via keyboard shortcuts (Ctrl+N, Ctrl+O, Ctrl+S, etc.)
- [ ] Tab key navigates through UI elements in logical order
- [ ] Focus indicators visible on all interactive elements
- [ ] Escape key closes dialogs and cancels operations
- [ ] Arrow keys navigate between tabs

## Focus Management
- [ ] Focus moves to editor textarea when tab is activated
- [ ] Focus returns to appropriate element after dialogs close
- [ ] No focus traps (user can always navigate out)
- [ ] Focus visible indicators meet WCAG AA standards (3:1 contrast ratio)

## Screen Reader Support
- [ ] Menu items have appropriate ARIA labels
- [ ] Tabs announce their state (active/inactive, dirty/clean)
- [ ] Status bar information announced to screen readers
- [ ] File dialog results announced
- [ ] Error messages announced

## Color Contrast
- [ ] Text meets WCAG AA standards (4.5:1 for normal text, 3:1 for large text)
- [ ] UI controls meet WCAG AA standards (3:1 contrast ratio)
- [ ] Dirty indicators visible to colorblind users (not relying solely on color)
- [ ] Focus indicators meet WCAG AA standards

## Implementation Status

**Completed**: Phase 10 T072 - Playwright + axe-core accessibility testing  
**Coverage**: Keyboard navigation, focus management, ARIA labels, screen reader support, color contrast  
**Test Suite**: `tests/integration/TextEdit.App.Tests/AccessibilityTests.cs` with 8 automated tests

The application follows basic accessibility best practices:
- Native HTML textarea for editing (inherent keyboard support)
- Standard menu system via Electron (native accessibility)
- Semantic HTML structure in Blazor components
- TailwindCSS with reasonable contrast defaults

**Recommendation**: Add Playwright-based integration tests with accessibility audits (axe-core) in a future quality pass.

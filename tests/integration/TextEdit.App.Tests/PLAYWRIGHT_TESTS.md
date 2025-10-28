# Playwright DOM Accessibility Tests

## Overview

The `PlaywrightDomTests` class contains end-to-end accessibility audits using Playwright to connect to the running Electron app and run axe-core checks against the rendered DOM.

**Status**: ✅ All 8 tests passing (verified 28 October 2025)

## Test Results Summary

All Playwright DOM accessibility audits passed with **zero WCAG violations**:

- ✅ `AxeCore_FullPageScan_NoViolations` - No accessibility violations detected
- ✅ `AxeCore_TabListStructure_Valid` - ARIA tablist structure compliant
- ✅ `AxeCore_EditorPanel_AriaCompliant` - Editor panel meets WCAG 2.1 AA
- ✅ `AxeCore_StatusBar_LiveRegion` - Status bar live regions configured correctly
- ✅ `AxeCore_Dialogs_ProperlyStructured` - Dialog ARIA requirements documented
- ✅ `ColorContrast_MeetsWCAGAA` - All text meets WCAG AA contrast ratios
- ✅ `KeyboardNavigation_AllInteractiveElementsReachable` - Full keyboard accessibility
- ✅ `LandmarksAndHeadings_ProperStructure` - Proper semantic HTML landmarks

**Total Test Count**: 182 tests (174 unit/integration + 8 Playwright DOM audits)

## Prerequisites

1. **Install Playwright browsers** (first time only):
   ```bash
   cd tests/integration/TextEdit.App.Tests
   pwsh bin/Debug/net8.0/playwright.ps1 install chromium
   # Or on Linux/Mac:
   ./bin/Debug/net8.0/playwright.sh install chromium
   ```

2. **Build the app**:
   ```bash
   dotnet build
   ```

## Running the Tests

### Option 1: Manual App Launch

1. **Start the app with remote debugging**:
   ```bash
   ./scripts/run-app-debug.fish
   # Or from the app directory:
   cd src/TextEdit.App
   electronize start /args --remote-debugging-port=9222
   ```

2. **Run the tests** (in another terminal):
   ```bash
   dotnet test --filter "FullyQualifiedName~PlaywrightDomTests"
   ```

### Option 2: Automatic Launch

The tests can automatically launch the app if it's not running:

```bash
cd tests/integration/TextEdit.App.Tests
dotnet test --filter "FullyQualifiedName~PlaywrightDomTests"
```

The test setup will:
1. Try to connect to an existing app on port 9222
2. If not found, launch the app with `electronize start /args --remote-debugging-port=9222`
3. Wait for the app to be ready
4. Run the accessibility audits
5. Optionally close the app if it was auto-launched

## Test Coverage

The Playwright DOM tests verify:

1. **Full Page Scan** (`AxeCore_FullPageScan_NoViolations`)
   - Runs axe-core against the entire application
   - Checks for any WCAG 2.1 AA violations

2. **Tab List Structure** (`AxeCore_TabListStructure_Valid`)
   - Verifies ARIA tablist/tab/tabpanel structure
   - Ensures proper tab navigation attributes

3. **Editor Panel** (`AxeCore_EditorPanel_AriaCompliant`)
   - Checks main editor area for ARIA compliance
   - Verifies proper labeling and roles

4. **Status Bar** (`AxeCore_StatusBar_LiveRegion`)
   - Validates status bar has proper role and aria-live
   - Ensures screen reader announcements work correctly

5. **Dialogs** (`AxeCore_Dialogs_ProperlyStructured`)
   - Documents expected dialog ARIA structure
   - Can be expanded for E2E dialog testing

6. **Color Contrast** (`ColorContrast_MeetsWCAGAA`)
   - Validates all text meets WCAG AA contrast ratios
   - Reports specific elements with contrast issues

7. **Keyboard Navigation** (`KeyboardNavigation_AllInteractiveElementsReachable`)
   - Ensures all interactive elements are keyboard accessible
   - Checks for proper focus management

8. **Landmarks** (`LandmarksAndHeadings_ProperStructure`)
   - Verifies proper semantic HTML structure
   - Checks for navigation, main, and aside landmarks

## Troubleshooting

### "Could not connect to browser"
- Ensure the app is running with remote debugging enabled
- Check that port 9222 is not blocked by a firewall
- Try manually launching: `electronize start /args --remote-debugging-port=9222`

### "Playwright browsers not installed"
- Run the install command from prerequisites
- Check that Chromium was downloaded successfully

### "No page found"
- The app may still be starting up
- The tests wait up to 30 seconds for the app to be ready
- Check app logs for errors

### Tests fail with violations
- Review the violation details in the test output
- Each violation includes:
  - Rule ID (e.g., "color-contrast")
  - Description of the issue
  - Help URL with remediation guidance
  - Affected HTML elements
- Fix the violations in the UI components and re-run

## CI/CD Integration

For automated testing in CI:

```bash
# In CI pipeline
./scripts/run-app-debug.fish &
APP_PID=$!
sleep 10  # Wait for app to start

# Run tests
dotnet test --filter "FullyQualifiedName~PlaywrightDomTests" --logger "console;verbosity=detailed"

# Cleanup
kill $APP_PID
```

## Related Documentation

- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [axe-core Rules](https://github.com/dequelabs/axe-core/blob/develop/doc/rule-descriptions.md)
- [Playwright Documentation](https://playwright.dev/)
- [Deque AxeCore Playwright](https://github.com/dequelabs/axe-core-npm/tree/develop/packages/playwright)

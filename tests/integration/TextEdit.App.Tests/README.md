# TextEdit.App.Tests (Integration)

Integration tests for the Electron-hosted Blazor application.

## What’s Covered

- Accessibility (Playwright + axe-core)
- DOM structure and ARIA roles
- Menu actions routed via EditorCommandHub
- Save/Save As flows with dialogs
- Tab navigation and focus management

## Prerequisites

- .NET 8 SDK
- Playwright browsers installed (first run will prompt)

See `PLAYWRIGHT_TESTS.md` in this folder for full setup and running guidance.

## How to Run

```fish
# From repository root
./scripts/dev.fish test:all

# Or directly (just integration tests)
dotnet test tests/integration/TextEdit.App.Tests/
```

## Notes

- Tests assume Electron app is launched by the test harness
- Some tests may be skipped on CI environments without display servers

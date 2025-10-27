# Quickstart: Text Editor Application

**Status**: Fully Implemented (Phase 9 Complete)  
**Coverage**: 65.13% line (Core: 92.39%, Infrastructure: 52.67%)  
**Tests**: 109 unit tests passing

## Prerequisites
- .NET 8 SDK
- Node.js 20+ (for TailwindCSS)
- ElectronNET.CLI (dotnet tool)

## Setup

```fish
# From repo root
# 1. Install ElectronNET CLI (if not already installed)
dotnet tool install --global ElectronNET.CLI

# 2. Restore .NET dependencies
dotnet restore

# 3. Initialize Electron (first time only)
cd src/TextEdit.App
electronize init
cd ../..

# 4. Build solution
dotnet build
```

## Run (Development)

```fish
# Option 1: Via VSCode task (recommended)
# Use "Run Task" → "electronize:start"

# Option 2: Via terminal
cd src/TextEdit.App
electronize start
```

**Note**: The app opens a native desktop window with the Blazor UI. Session state persists to `~/.config/TextEdit/Session/`.

## Test

```fish
# Run all tests (117 total: 109 unit + 8 integration/contract)
dotnet test

# Unit tests only (with coverage)
dotnet test tests/unit/TextEdit.Core.Tests --collect:"XPlat Code Coverage"

# Coverage with threshold enforcement (65%)
dotnet test tests/unit/TextEdit.Core.Tests /p:CollectCoverage=true /p:Threshold=65

# Integration tests (requires app build)
dotnet test tests/integration/TextEdit.App.Tests

# Contract tests (JSON schema validation)
dotnet test tests/contract/TextEdit.IPC.Tests
```

## Package

```fish
# Build platform binaries (from src/TextEdit.App/)
cd src/TextEdit.App

# Windows executable
electronize build /target win

# macOS app bundle
electronize build /target osx

# Linux AppImage/deb
electronize build /target linux

# Output location: src/TextEdit.App/bin/Desktop/
```

## Key Features Implemented

- ✅ Multi-tab text editing with dirty tracking
- ✅ File operations (New, Open, Save, Save As)
- ✅ Undo/Redo per document
- ✅ Session persistence (unsaved content survives app restart)
- ✅ Markdown preview with live updates
- ✅ Word wrap toggle
- ✅ Status bar (line/col/char count)
- ✅ Native menus (File, Edit, View)
- ✅ Autosave every 30s
- ✅ Large file support (read-only mode for 10MB+)
- ✅ External change detection
- ✅ Encoding/EOL normalization

## Project Structure

```
src/
  TextEdit.App/        # ASP.NET Core + Electron host
  TextEdit.Core/       # Domain logic (documents, tabs, undo)
  TextEdit.Infrastructure/  # File I/O, persistence, IPC
  TextEdit.Markdown/   # Markdig-based rendering
  TextEdit.UI/         # Blazor components
tests/
  unit/TextEdit.Core.Tests/       # 109 unit tests
  integration/TextEdit.App.Tests/  # Placeholder for Playwright
  contract/TextEdit.IPC.Tests/     # IPC schema validation
```

## Development Notes

- **Configuration**: `appsettings.json` and `appsettings.Development.json`
- **Session storage**: `~/.config/TextEdit/Session/` (Linux) or equivalent per platform
- **Performance**: Startup/quit/preview durations logged to console
- **Coverage gate**: Build fails if unit test coverage drops below 65%
- **Accessibility**: Checklist created; full automation deferred

## Troubleshooting

**Electronize command not found:**
```fish
dotnet tool install --global ElectronNET.CLI
```

**Build errors:**
```fish
dotnet clean
dotnet restore
dotnet build
```

**Tests failing:**
```fish
# Check for file permission issues in temp directories
# Verify .NET 8 SDK installed: dotnet --version
```

**App won't start:**
```fish
# Rebuild Electron components
cd src/TextEdit.App
electronize init
electronize start
```

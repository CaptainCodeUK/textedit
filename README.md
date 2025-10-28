# TextEdit

A cross-platform desktop text editor with tabs, markdown preview, and session persistence.

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (for Electron)

### First Time Setup

```bash
# Clone and navigate to the repository
cd textedit

# Restore NuGet packages
./scripts/dev.sh restore

# Initialize Electron.NET (installs CLI tools)
./scripts/dev.sh electronize:init

# Build the solution
./scripts/dev.sh build
```

### Running the Application

```bash
# Run in development mode
./scripts/dev.sh run

# Or use VS Code: Press F5
```

### Running Tests

```bash
# Run all tests
./scripts/dev.sh test

# Run only unit tests
./scripts/dev.sh test:unit

# Run with coverage
./scripts/dev.sh test:coverage
```

## Project Structure

```
textedit/
├── src/
│   ├── TextEdit.App/           # ASP.NET Core + Electron host
│   ├── TextEdit.UI/            # Blazor components
│   ├── TextEdit.Core/          # Domain models & services
│   ├── TextEdit.Infrastructure/# File I/O, IPC, autosave
│   └── TextEdit.Markdown/      # Markdown rendering
├── tests/
│   ├── unit/                   # Unit tests
│   ├── integration/            # Integration tests
│   └── contract/               # IPC contract tests
├── specs/                      # Feature specifications
│   └── 001-text-editor/
└── scripts/                    # Development helper scripts
```

## Technology Stack

- **.NET 8** (C# 12) with ASP.NET Core 8
- **Blazor Server** for UI components
- **Electron.NET** for cross-platform desktop packaging
- **TailwindCSS** for styling
- **Markdig** for markdown rendering
- **xUnit** + **bUnit** + **Playwright** for testing

## Development

### VS Code

The project includes launch configurations and tasks:

- **F5** - Start debugging
- **Ctrl+Shift+B** - Build solution
- **Ctrl+Shift+P** → "Tasks: Run Task" - Access all tasks

### Command Line

See [`scripts/README.md`](scripts/README.md) for all available commands.

## Features (Planned)

- ✅ Phase 1: Project Setup (Complete)
- ⬜ Phase 2: Core Services & Models
- ⬜ Phase 3: Basic Text Editing (US1)
- ⬜ Phase 4: Multi-Document Tabs (US2)
- ⬜ Phase 5: Session Persistence (US4)
- ⬜ Phase 6: Menus & Status Bar (US3)
- ⬜ Phase 7: Markdown Preview (US5)
- ⬜ Phase 8: Edge Cases & Error Handling
- ⬜ Phase 9: Quality & Testing

## Documentation

- [Implementation Plan](specs/001-text-editor/plan.md)
- [Feature Specification](specs/001-text-editor/spec.md)
- [Task List](specs/001-text-editor/tasks.md)
- [Development Scripts](scripts/README.md)
- [Quickstart Guide](specs/001-text-editor/quickstart.md) - Build, run, and test instructions

## Building & Testing

```fish
# Build the solution
./scripts/dev.fish build

# Run unit tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Run the Electron app
./scripts/dev.fish start
```

See [quickstart.md](specs/001-text-editor/quickstart.md) for detailed instructions.

## License

TBD

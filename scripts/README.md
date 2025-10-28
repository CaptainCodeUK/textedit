# Development Scripts

Quick helper scripts for common development tasks.

## Available Scripts

### `dev.fish` / `dev.sh`

Main development runner script available in both Fish and Bash versions.

**Usage:**
```bash
# Fish shell
./scripts/dev.fish [command]

# Bash/other shells
./scripts/dev.sh [command]
```

**Commands:**

- **`build`** - Build the entire solution (auto-cleans lingering processes)
- **`clean`** - Clean all build artifacts
- **`restore`** - Restore NuGet packages
- **`cleanup`** - Kill any lingering TextEdit/dotnet processes
- **`run`** - Run the Electron app in development mode (auto-cleans lingering processes)
- **`test`** - Run all tests (auto-cleans lingering processes)
- **`test:unit`** - Run only unit tests
- **`test:coverage`** - Run tests with code coverage reporting
- **`electronize:init`** - First-time Electron.NET setup (installs CLI tool)
- **`electronize:build`** - Build production Electron package for distribution
- **`help`** - Show available commands

### PID & Cleanup Helpers

- `find-dotnet-pid.fish` / `find-dotnet-pid.sh` — prints the PID of the dotnet process hosting `TextEdit.App.dll`.
	Use this if you prefer the "Launch Electron (Auto PID)" debug config without process picking.
	Example:

```fish
./scripts/find-dotnet-pid.fish
```

Copy the printed PID into the VS Code prompt when starting the "Launch Electron (Auto PID)" config.

- `kill-textedit.fish` / `kill-textedit.sh` — kills any lingering TextEdit.App, electronize, or dotnet test/build processes.
	This is automatically run by `build`, `run`, and `test` commands, but you can also run it manually:

```fish
./scripts/kill-textedit.fish
# or use the dev.fish wrapper:
./scripts/dev.fish cleanup
```

**Why auto-cleanup?** Lingering dotnet processes from previous runs can interfere with builds and tests by holding file locks. The scripts now automatically clean up before major operations to prevent these issues.

## Quick Start

### First Time Setup

```bash
# 1. Restore dependencies
./scripts/dev.fish restore

# 2. Initialize Electron.NET (installs electronize CLI tool)
./scripts/dev.fish electronize:init

# 3. Build the solution
./scripts/dev.fish build
```

### Development Workflow

```bash
# Run the app in development mode
./scripts/dev.fish run

# Or start and attach by PID (Fish)
set pid (./scripts/find-dotnet-pid.fish); and echo $pid

# Run tests during development
./scripts/dev.fish test:unit

# Check test coverage
./scripts/dev.fish test:coverage
```

### Building for Production

```bash
# Build production package (will prompt for platform)
./scripts/dev.fish electronize:build
```

## VS Code Integration

The project includes `.vscode/launch.json` and `.vscode/tasks.json` configurations:

### Launch Configurations (F5):

- **Launch Electron (F5)** - Starts Electron and auto-attaches by process name
- **Launch Electron (Auto PID)** - Prompts for PID (use the helper script)
- **Attach to Electron Process** - Attach debugger to running process
- **Run Unit Tests** - Execute unit tests with debugging
- **Run All Tests** - Execute all test projects

### Tasks (Ctrl+Shift+B):

- **build** (default) - Build solution
- **clean** - Clean artifacts
- **restore** - Restore packages
- **electronize:start** - Run Electron app
- **test:unit** (default test) - Run unit tests
- **test:all** - Run all tests
- **test:coverage** - Generate coverage report

### Quick Access:

1. **Press F5** to start debugging
2. **Press Ctrl+Shift+B** to build
3. **Press Ctrl+Shift+P** → "Tasks: Run Task" to see all tasks

## Requirements

- .NET 8 SDK
- Node.js 18+ (for Electron)
- ElectronNET.CLI (installed automatically by `electronize:init`)

## Notes

- The first run of `electronize start` or `electronize build` will download Electron binaries (~100MB)
- Development mode uses hot reload for Blazor components
- Production builds are output to `src/TextEdit.App/bin/Desktop/`

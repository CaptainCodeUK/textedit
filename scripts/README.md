# Development Scripts

Quick helper scripts for common development tasks across **macOS, Linux, and Windows**.

## Platform-Specific Scripts

### macOS & Linux

Use the **Fish shell** (`.fish`) or **Bash** (`.sh`) scripts:

```bash
# Fish shell (recommended on macOS/Linux with Fish installed)
./scripts/dev.fish [command]

# Bash (works on macOS/Linux with standard shells)
./scripts/dev.sh [command]
```

### Windows

Use the **PowerShell** (`.ps1`) scripts with a **CMD wrapper** (`.cmd`) for convenience:

```cmd
REM PowerShell directly
.\scripts\dev.ps1 [command]

REM CMD wrapper (recommended - auto-detects PowerShell)
.\scripts\dev.cmd [command]
```

**Note**: On Windows, you may need to set execution policy for PowerShell scripts:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

## Available Scripts

### Main Development Runner: `dev.*`

Platform-specific versions: `dev.fish` (Fish), `dev.sh` (Bash), `dev.ps1` (PowerShell), `dev.cmd` (CMD wrapper)

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

**Examples:**

```bash
# macOS/Linux (Fish)
./scripts/dev.fish build
./scripts/dev.fish run
./scripts/dev.fish test

# macOS/Linux (Bash)
./scripts/dev.sh build
./scripts/dev.sh run
./scripts/dev.sh test
```

```cmd
REM Windows (CMD)
.\scripts\dev.cmd build
.\scripts\dev.cmd run
.\scripts\dev.cmd test

REM Windows (PowerShell)
.\scripts\dev.ps1 build
.\scripts\dev.ps1 run
.\scripts\dev.ps1 test
```

### PID & Cleanup Helpers

#### Find .NET Process PID

Prints the PID of the dotnet process hosting `TextEdit.App.dll`. Useful for VS Code's "Launch Electron (Auto PID)" debug config.

**Files:** `find-dotnet-pid.fish`, `find-dotnet-pid.sh`, `find-dotnet-pid.ps1`

**Usage:**

```bash
# macOS/Linux (Fish)
./scripts/find-dotnet-pid.fish

# macOS/Linux (Bash)
./scripts/find-dotnet-pid.sh
```

```powershell
# Windows (PowerShell)
.\scripts\find-dotnet-pid.ps1
```

Copy the printed PID into the VS Code prompt when starting the "Launch Electron (Auto PID)" config.

#### Kill Lingering Processes

Kills any lingering TextEdit.App, electronize, or dotnet test/build processes. Safe to run even if nothing is running.

**Files:** `kill-textedit.fish`, `kill-textedit.sh`, `kill-textedit.ps1`

**Usage:**

```bash
# macOS/Linux (Fish)
./scripts/kill-textedit.fish
# or use the dev.fish wrapper:
./scripts/dev.fish cleanup

# macOS/Linux (Bash)
./scripts/kill-textedit.sh
```

```powershell
# Windows (PowerShell)
.\scripts\kill-textedit.ps1
# or use the wrapper:
.\scripts\dev.ps1 cleanup
```

**Why auto-cleanup?** Lingering dotnet processes from previous runs can interfere with builds and tests by holding file locks. The scripts now automatically clean up before major operations to prevent these issues.

## Quick Start

### First Time Setup

**macOS/Linux:**
```bash
# 1. Restore dependencies
./scripts/dev.fish restore

# 2. Initialize Electron.NET (installs electronize CLI tool)
./scripts/dev.fish electronize:init

# 3. Build the solution
./scripts/dev.fish build
```

**Windows:**
```cmd
REM 1. Restore dependencies
.\scripts\dev.cmd restore

REM 2. Initialize Electron.NET (installs electronize CLI tool)
.\scripts\dev.cmd electronize:init

REM 3. Build the solution
.\scripts\dev.cmd build
```

### Development Workflow

**macOS/Linux:**
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

**Windows:**
```cmd
REM Run the app in development mode
.\scripts\dev.cmd run

REM Find PID for debugging (PowerShell)
.\scripts\find-dotnet-pid.ps1

REM Run tests during development
.\scripts\dev.cmd test:unit

REM Check test coverage
.\scripts\dev.cmd test:coverage
```

### Building for Production

**All Platforms:**
```bash
# macOS/Linux
./scripts/dev.fish electronize:build

# Windows
.\scripts\dev.cmd electronize:build
```

The script will prompt you to select the target platform (Windows, macOS, or Linux).

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

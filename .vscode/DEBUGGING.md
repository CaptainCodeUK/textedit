# Debugging TextEdit with VS Code

## ✅ F5 Works!

The debugging setup is complete. You have two F5 workflows to choose from.

## Quick Start

Press **F5** and choose your preferred config:
- **Launch Electron (Auto-Attach)** - Fully automatic, no prompts
- **Launch & Debug Electron App** - Shows process picker (useful for troubleshooting)

## Recommended Workflows

### Option 1: Auto-Attach (Zero-Click)

1. Press **F5**
2. Select "Launch Electron (Auto-Attach)"
3. Wait ~10 seconds for the app to start
4. Debugger attaches automatically ✓

This configuration starts Electron and auto-attaches to the `TextEdit.App` process without prompts.

### Option 2: Manual Process Selection

1. Press **F5**
2. Select "Launch & Debug Electron App"
3. Wait for the Electron window to open (~10 seconds)
4. **Process picker appears** - Select the process with command: `/TextEdit.App --environment`
5. Debugger attaches successfully ✓

Useful when you have multiple instances or want to verify which process to attach to.

### Option 3: Script + Manual Attach

If you prefer to start the app yourself:
```bash
./scripts/dev.fish run
```

The app is now running. To attach the debugger, press **F5** and select any attach-only config (they don't have a preLaunchTask so won't restart the app).

## Available Configurations

### Launch Configurations (with preLaunchTask)
- **Launch Electron (Auto-Attach)** - Starts app and auto-attaches by process name
- **Launch & Debug Electron App** - Starts app and shows process picker

### Test Configurations
- **Run Unit Tests** - Runs unit tests with detailed output
- **Run All Tests** - Runs all test projects in the solution

### Standalone Launch
- **Launch ASP.NET Core (no Electron)** - Runs the web host without Electron (useful for browser testing)

## Available Tasks

Build tasks:
- `build` - Build the solution (default build task)
- `clean` - Clean build artifacts
- `restore` - Restore NuGet packages

Electron tasks:
- `electronize:start` - Start Electron in dev mode (used by F5 configs)
- `electronize:build:win` - Package for Windows
- `electronize:build:mac` - Package for macOS
- `electronize:build:linux` - Package for Linux

Test tasks:
- `test:unit` - Run unit tests (default test task)
- `test:all` - Run all tests
- `test:coverage` - Run tests with coverage report

## Troubleshooting

### "Errors exist after running preLaunchTask"
✅ **Fixed** - The `electronize:start` task now correctly signals readiness without false errors.

### "No process with the specified name is currently running"
- Wait ~10 seconds for the app to fully start
- The auto-attach will succeed once `TextEdit.App` process is detected
- If using process picker, it will appear when the process is ready

### Process picker shows many processes?
Look for the process with these characteristics:
- **Name**: `TextEdit.App`
- **Path**: `.../src/TextEdit.App/obj/Host/bin/TextEdit.App`
- **Args**: `--environment=Production /electronPort=8000 /electronWebPort=8001`

Tip: Use `./scripts/find-dotnet-pid.fish` to identify the exact PID if needed.

### App won't start?
1. Build first: `./scripts/dev.fish build` or `dotnet build textedit.sln`
2. Check for build errors in the terminal output
3. Ensure `electronize` tool is installed: `dotnet tool list -g | grep electronize`

### Electron shows connection errors at startup?
✅ **Fixed** - The app now delays window creation until the ASP.NET Core host is fully started, eliminating transient `ERR_CONNECTION_REFUSED` errors.

### Multiple TextEdit.App instances running?
- Auto-attach may prompt you to choose which one
- Or use the process picker config to select manually
- Close extra instances and try again


## How It Works

### Background Task (electronize:start)

When you press F5 with a launch config that has `preLaunchTask: "electronize:start"`:

1. **Task starts** → Runs `electronize start` in the background
2. **Publishes app** → Compiles and publishes to `obj/Host/bin/`
3. **Starts Electron** → Launches the Electron process
4. **ASP.NET Core starts** → Kestrel begins listening on port 8001
5. **Window created** → After host signals `ApplicationStarted`, Electron creates BrowserWindow
6. **Task signals ready** → ProblemMatcher detects "ASP.NET Core host has fully started"
7. **Debugger attaches** → VS Code attaches to the `TextEdit.App` process

### Process Details

- **Process name**: `TextEdit.App` (not `dotnet`)
- **Why**: ElectronNET creates a self-contained executable during publish
- **Location**: `src/TextEdit.App/obj/Host/bin/TextEdit.App`
- **Command line**: `TextEdit.App --environment=Production /electronPort=8000 /electronWebPort=8001`

### Task Configuration

The `electronize:start` task uses:
- **isBackground**: `true` - Runs without blocking VS Code
- **presentation.reveal**: `silent` - Doesn't steal focus
- **problemMatcher.background.endsPattern**: Watches for "ASP.NET Core host has fully started"
- **problemMatcher.pattern**: Only treats ERROR/FATAL/Build FAILED as errors

This ensures the debugger waits for the app to be fully ready before attempting to attach.

## Scripts Reference

All scripts are in the `scripts/` directory and support both `fish` and `bash`:

```bash
# Build the solution
./scripts/dev.fish build

# Run the Electron app (dev mode)
./scripts/dev.fish run

# Run all tests
./scripts/dev.fish test

# Package for platform
./scripts/dev.fish electronize build win    # or mac, linux

# Find the TextEdit.App PID
./scripts/find-dotnet-pid.fish
```

## Tips

- **Set default launch config**: The first config in `launch.json` becomes the F5 default, or VS Code remembers your last selection
- **Use Auto-Attach for daily work**: Fastest workflow with zero prompts
- **Use Process Picker when debugging attach issues**: Helps verify the correct process
- **Check terminal output**: The electronize:start terminal shows detailed startup logs
- **GPU warnings on Linux**: Vulkan/GL warnings are typically harmless; add `--disable-gpu` flag in ElectronHost if needed


# Debugging TextEdit with VS Code

## ✅ Fixed: F5 Now Works!

The `electronize:start` task problemMatcher has been fixed. You can now use F5 to debug.

## Recommended Workflow

### F5 - Press and Pick (Easiest)

1. Press **F5** (or select "Launch & Debug Electron App")
2. Wait for the Electron window to open (~10 seconds)
3. **Process picker appears** - Select the process with command: `/TextEdit.App --environment`
4. Debugger attaches successfully ✓

### Alternative: Attach to Running App

If the app is already running:
1. Use debug config: "Attach to Running Electron App"
2. Debugger auto-attaches to `TextEdit.App` (no picker needed)

### Manual Start + Attach

If you prefer to start the app yourself:
```bash
./scripts/dev.fish run
```
Then use "Attach to Running Electron App" config.

## Troubleshooting

**"Errors exist after running preLaunchTask"**
- ✅ Fixed - The problemMatcher pattern was invalid, now corrected

**"No process with the specified name is currently running"**
- Wait a bit longer (~10 seconds) for the app to fully start
- The process picker will appear when ready
- Select the process containing `/TextEdit.App --environment`

**Process picker shows many processes?**
- Look for: `/home/dave/Documents/source/demo/textedit/src/TextEdit.App/obj/Host/bin/TextEdit.App`
- Has args: `--environment=Production /electronPort=8000`
- Or use: `./scripts/find-dotnet-pid.fish` to get the exact PID

**App won't start?**
- Build first: `./scripts/dev.fish build`
- Check: `dotnet build textedit.sln`

## How It Works

1. **F5 pressed** → Runs `electronize:start` task in background
2. **Task starts Electron** → Publishes and launches the app
3. **App initializes** → ASP.NET Core starts, listens on port 8001
4. **Task detects ready** → Watches for "Now listening on:" or "ASP.NET Core host has fully started"
5. **Debugger ready** → Process picker appears with `TextEdit.App` in the list
6. **You pick** → Select TextEdit.App, debugger attaches

The process name is `TextEdit.App` (not `dotnet`) because ElectronNET creates a self-contained executable.

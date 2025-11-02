# Deployment Checklist (v1.1)

Use this checklist to package and verify Scrappy Text Editor on each platform.

## Prerequisites
- .NET 8 SDK installed
- Node.js 18+ installed
- ElectronNET.CLI installed: `dotnet tool install ElectronNET.CLI -g` (or update)
- Icons present under `src/TextEdit.App/wwwroot/icons/`

## Common Steps
1. Clean/build solution
   - `dotnet clean`
   - `dotnet build -c Release`
2. Verify tests
   - `dotnet test`
3. Ensure app runs in dev
   - `cd src/TextEdit.App`
   - `electronize start`

## Windows Packaging
- Command: `electronize build /target win`
- Output: `src/TextEdit.App/bin/Desktop`
- Verify:
  - App launches without console window
  - Open/save files; session restore on restart
  - About/Options dialogs
  - Theme switching Light/Dark
  - CLI: launch with files merges into running instance

## macOS Packaging
- Command: `electronize build /target osx`
- Output: `src/TextEdit.App/bin/Desktop`
- Verify:
  - App bundle launches (Gatekeeper may require allow)
  - Menu integration (Preferences under App menu)
  - File open via drag-and-drop and CLI
  - Theme switching and session restore
- Optional: Code signing/notarization (outside scope of this checklist)

## Linux Packaging
- Command: `electronize build /target linux`
- Output: `src/TextEdit.App/bin/Desktop`
- Verify:
  - App launches on target distro
  - Open/save with permissions respected
  - Session restore
  - CLI file args handled

## Smoke Tests
- Open ≥3 files, edit, save, close, restore session
- Markdown preview on/off; large doc manual refresh >100KB
- External file change detection prompt
- Autosave recovery

## Post-build Artifacts
- Ensure README and LICENSE included where applicable
- Version and build date displayed in About dialog

## Troubleshooting
- If packaging fails, run `electronize init` once in `src/TextEdit.App`
- Delete `bin/` and `obj/` if stale assets cause issues
- Verify ElectronNET.CLI version compatible with project

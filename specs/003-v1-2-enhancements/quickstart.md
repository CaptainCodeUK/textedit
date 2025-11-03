# Quickstart: Scrappy Text Editor v1.2 Enhancements

## Prerequisites
- .NET 8.0 SDK
- Node.js (for Electron.NET)
- ElectronNET.CLI (install with `dotnet tool install ElectronNET.CLI -g`)
- GitHub Actions enabled for CI/CD

## Build and Run (Development)
```fish
./scripts/dev.fish run
```

## Run Unit Tests
```fish
./scripts/dev.fish test:unit
```

## Build for Distribution
```fish
cd src/TextEdit.App
# For Linux
fish -c 'electronize build /target linux'
# For Windows
fish -c 'electronize build /target win'
# For macOS
fish -c 'electronize build /target osx'
```

## Where to Find Artifacts
- Release builds: `src/TextEdit.App/bin/Desktop/`
- Test results: `TestResults/`
- Coverage: `TestResults/CoverageReport/`

## Custom Dictionary Location
- Linux: `~/.config/TextEdit/CustomDictionary.txt`
- macOS: `~/Library/Application Support/TextEdit/CustomDictionary.txt`
- Windows: `%AppData%\TextEdit\CustomDictionary.txt`

## Updating/Testing Auto-Updater
- Publish a new release on GitHub with attached artifacts
- App will check for updates on launch and every 24h
- For manual check: use Options > Updates > "Check for updates now"

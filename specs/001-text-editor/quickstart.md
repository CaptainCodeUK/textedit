# Quickstart: Text Editor Application

## Prerequisites
- .NET 8 SDK
- Node.js 20+
- ElectronNET.CLI (dotnet tool)
- TailwindCSS CLI

## Setup

```fish
# from repo root
# install ElectronNET CLI
dotnet tool install -g ElectronNET.CLI

# restore .NET
dotnet restore

# install Node deps (if package.json is added for Tailwind)
# npm install

# build Tailwind (example; adapt to your package.json scripts)
# npx tailwindcss -i src/TextEdit.UI/Styles/input.css -o src/TextEdit.App/wwwroot/app.css --minify
```

## Run (development)

```fish
# Start the ASP.NET Core host + Electron shell
# Electron.NET will launch Chromium window
electronize start
```

## Test

```fish
# Unit + component tests
dotnet test tests/unit/TextEdit.Core.Tests

# Install Playwright browsers (first time)
dotnet tool update --global Microsoft.Playwright.CLI
playwright install

# Integration (Electron UI)
dotnet test tests/integration/TextEdit.App.Tests

# Contract tests (JSON Schemas)
dotnet test tests/contract/TextEdit.IPC.Tests
```

## Package

```fish
# Build platform binaries
# Windows
electronize build /target win
# macOS
electronize build /target osx
# Linux
electronize build /target linux
```

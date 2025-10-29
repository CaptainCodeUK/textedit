# TextEdit Development Runner (PowerShell)
# Quick commands for building, running, and testing on Windows

param(
    [Parameter(Position=0)]
    [string]$Command = "help"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Resolve-Path (Join-Path $ScriptDir "..")

function Show-Help {
    Write-Host "TextEdit Development Scripts (Windows)"
    Write-Host ""
    Write-Host "Usage: .\scripts\dev.ps1 [command]"
    Write-Host ""
    Write-Host "Commands:"
    Write-Host "  build              Build the solution"
    Write-Host "  clean              Clean build artifacts"
    Write-Host "  restore            Restore NuGet packages"
    Write-Host "  cleanup            Kill any lingering TextEdit/dotnet processes"
    Write-Host "  run                Run the Electron app (debug mode)"
    Write-Host "  test               Run all tests"
    Write-Host "  test:unit          Run unit tests only"
    Write-Host "  test:coverage      Run tests with coverage"
    Write-Host "  electronize:init   Initialize Electron.NET (first time setup)"
    Write-Host "  electronize:build  Build production Electron package"
    Write-Host "  help               Show this help message"
}

function Invoke-Build {
    Write-Host "🔨 Building solution..." -ForegroundColor Cyan
    # Kill any lingering processes first
    & "$ScriptDir\kill-textedit.ps1"
    dotnet build "$RootDir\textedit.sln"
}

function Invoke-Clean {
    Write-Host "🧹 Cleaning build artifacts..." -ForegroundColor Cyan
    dotnet clean "$RootDir\textedit.sln"
}

function Invoke-Restore {
    Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Cyan
    dotnet restore "$RootDir\textedit.sln"
}

function Invoke-Run {
    Write-Host "🚀 Starting Electron app..." -ForegroundColor Cyan
    # Kill any lingering processes first
    & "$ScriptDir\kill-textedit.ps1"
    
    # Check if electronize is installed
    $electronizeCmd = Get-Command electronize -ErrorAction SilentlyContinue
    if (-not $electronizeCmd) {
        Write-Host "❌ Error: electronize command not found" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please install ElectronNET.CLI tool first:"
        Write-Host "  .\scripts\dev.ps1 electronize:init"
        Write-Host ""
        Write-Host "Or install manually:"
        Write-Host "  dotnet tool install -g ElectronNET.CLI"
        return
    }
    
    Push-Location "$RootDir\src\TextEdit.App"
    try {
        electronize start
    }
    finally {
        Pop-Location
    }
}

function Invoke-Test {
    Write-Host "🧪 Running all tests..." -ForegroundColor Cyan
    # Kill any lingering processes first
    & "$ScriptDir\kill-textedit.ps1"
    dotnet test "$RootDir\textedit.sln" --logger "console;verbosity=normal"
}

function Invoke-TestUnit {
    Write-Host "🧪 Running unit tests..." -ForegroundColor Cyan
    dotnet test "$RootDir\tests\unit\TextEdit.Core.Tests\TextEdit.Core.Tests.csproj" --logger "console;verbosity=normal"
}

function Invoke-TestCoverage {
    Write-Host "🧪 Running tests with coverage..." -ForegroundColor Cyan
    dotnet test "$RootDir\textedit.sln" `
        /p:CollectCoverage=true `
        /p:CoverletOutputFormat=cobertura `
        /p:CoverletOutput=./TestResults/
}

function Invoke-ElectronizeInit {
    Write-Host "⚡ Initializing Electron.NET..." -ForegroundColor Cyan
    
    # Install electronize tool if not present
    Write-Host "Checking for ElectronNET.CLI tool..."
    $toolList = dotnet tool list -g
    if ($toolList -notmatch "ElectronNET.CLI") {
        Write-Host "Installing ElectronNET.CLI tool globally..."
        dotnet tool install -g ElectronNET.CLI
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Failed to install ElectronNET.CLI" -ForegroundColor Red
            return
        }
        Write-Host "✅ ElectronNET.CLI installed successfully" -ForegroundColor Green
    }
    else {
        Write-Host "✅ ElectronNET.CLI already installed" -ForegroundColor Green
    }
    
    Push-Location "$RootDir\src\TextEdit.App"
    try {
        Write-Host "Running electronize init..."
        electronize init
    }
    finally {
        Pop-Location
    }
}

function Invoke-ElectronizeBuild {
    Write-Host "📦 Building production Electron package..." -ForegroundColor Cyan
    
    # Check if electronize is installed
    $electronizeCmd = Get-Command electronize -ErrorAction SilentlyContinue
    if (-not $electronizeCmd) {
        Write-Host "❌ Error: electronize command not found" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please install ElectronNET.CLI tool first:"
        Write-Host "  .\scripts\dev.ps1 electronize:init"
        Write-Host ""
        Write-Host "Or install manually:"
        Write-Host "  dotnet tool install -g ElectronNET.CLI"
        return
    }
    
    Write-Host "Select target platform:"
    Write-Host "  1) Windows"
    Write-Host "  2) macOS"
    Write-Host "  3) Linux"
    $choice = Read-Host "Enter choice (1-3)"
    
    Push-Location "$RootDir\src\TextEdit.App"
    try {
        switch ($choice) {
            "1" {
                Write-Host "Building for Windows..."
                electronize build /target win
            }
            "2" {
                Write-Host "Building for macOS..."
                electronize build /target osx
            }
            "3" {
                Write-Host "Building for Linux..."
                electronize build /target linux
            }
            default {
                Write-Host "Invalid choice. Defaulting to current platform..."
                electronize build
            }
        }
    }
    finally {
        Pop-Location
    }
}

# Main command dispatcher
switch ($Command.ToLower()) {
    "build" {
        Invoke-Build
    }
    "clean" {
        Invoke-Clean
    }
    "restore" {
        Invoke-Restore
    }
    "cleanup" {
        & "$ScriptDir\kill-textedit.ps1"
    }
    "run" {
        Invoke-Run
    }
    "test" {
        Invoke-Test
    }
    "test:unit" {
        Invoke-TestUnit
    }
    "test:coverage" {
        Invoke-TestCoverage
    }
    "electronize:init" {
        Invoke-ElectronizeInit
    }
    "electronize:build" {
        Invoke-ElectronizeBuild
    }
    "help" {
        Show-Help
    }
    default {
        Write-Host "Unknown command: $Command" -ForegroundColor Red
        Write-Host ""
        Show-Help
        exit 1
    }
}

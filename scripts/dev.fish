#!/usr/bin/env fish

# TextEdit Development Runner
# Quick commands for building, running, and testing

set script_dir (dirname (status --current-filename))
set root_dir (realpath "$script_dir/..")

function show_help
    echo "TextEdit Development Scripts"
    echo ""
    echo "Usage: ./scripts/dev.fish [command]"
    echo ""
    echo "Commands:"
    echo "  build              Build the solution"
    echo "  clean              Clean build artifacts"
    echo "  restore            Restore NuGet packages"
    echo "  cleanup            Kill any lingering TextEdit/dotnet processes"
    echo "  run                Run the Electron app (debug mode)"
    echo "  test               Run all tests"
    echo "  test:unit          Run unit tests only"
    echo "  test:coverage      Run tests with coverage"
    echo "  electronize:init   Initialize Electron.NET (first time setup)"
    echo "  electronize:build  Build production Electron package"
    echo "  help               Show this help message"
end

function cmd_build
    echo "🔨 Building solution..."
    # Kill any lingering processes first
    "$script_dir/kill-textedit.fish"
    dotnet build "$root_dir/textedit.sln"
end

function cmd_clean
    echo "🧹 Cleaning build artifacts..."
    dotnet clean "$root_dir/textedit.sln"
end

function cmd_restore
    echo "📦 Restoring NuGet packages..."
    dotnet restore "$root_dir/textedit.sln"
end

function cmd_run
    echo "🚀 Starting Electron app..."
    # Kill any lingering processes first
    "$script_dir/kill-textedit.fish"
    cd "$root_dir/src/TextEdit.App"
    electronize start
end

function cmd_test
    echo "🧪 Running all tests..."
    # Kill any lingering processes first
    "$script_dir/kill-textedit.fish"
    dotnet test "$root_dir/textedit.sln" --logger "console;verbosity=normal"
end

function cmd_test_unit
    echo "🧪 Running unit tests..."
    dotnet test "$root_dir/tests/unit/TextEdit.Core.Tests/TextEdit.Core.Tests.csproj" --logger "console;verbosity=normal"
end

function cmd_test_coverage
    echo "🧪 Running tests with coverage..."
    dotnet test "$root_dir/textedit.sln" \
        /p:CollectCoverage=true \
        /p:CoverletOutputFormat=cobertura \
        /p:CoverletOutput=./TestResults/
end

function cmd_electronize_init
    echo "⚡ Initializing Electron.NET..."
    
    # Install electronize tool if not present
    echo "Checking for ElectronNET.CLI tool..."
    if not dotnet tool list -g | grep -q ElectronNET.CLI
        echo "Installing ElectronNET.CLI tool globally..."
        dotnet tool install -g ElectronNET.CLI
        if test $status -ne 0
            echo "❌ Failed to install ElectronNET.CLI"
            return 1
        end
        echo "✅ ElectronNET.CLI installed successfully"
    else
        echo "✅ ElectronNET.CLI already installed"
    end
    
    cd "$root_dir/src/TextEdit.App"
    echo "Running electronize init..."
    electronize init
end

function cmd_electronize_build
    echo "📦 Building production Electron package..."
    echo "Select target platform:"
    echo "  1) Windows"
    echo "  2) macOS"
    echo "  3) Linux"
    read -P "Enter choice (1-3): " choice
    
    cd "$root_dir/src/TextEdit.App"
    
    switch $choice
        case 1
            echo "Building for Windows..."
            electronize build /target win
        case 2
            echo "Building for macOS..."
            electronize build /target osx
        case 3
            echo "Building for Linux..."
            electronize build /target linux
        case '*'
            echo "Invalid choice. Defaulting to current platform..."
            electronize build
    end
end

# Main command dispatcher
if test (count $argv) -eq 0
    show_help
    exit 0
end

switch $argv[1]
    case build
        cmd_build
    case clean
        cmd_clean
    case restore
        cmd_restore
    case cleanup
        "$script_dir/kill-textedit.fish"
    case run
        cmd_run
    case test
        cmd_test
    case test:unit
        cmd_test_unit
    case test:coverage
        cmd_test_coverage
    case electronize:init
        cmd_electronize_init
    case electronize:build
        cmd_electronize_build
    case help
        show_help
    case '*'
        echo "Unknown command: $argv[1]"
        echo ""
        show_help
        exit 1
end

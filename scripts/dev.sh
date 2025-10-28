#!/usr/bin/env bash

# TextEdit Development Runner
# Quick commands for building, running, and testing

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

show_help() {
    echo "TextEdit Development Scripts"
    echo ""
    echo "Usage: ./scripts/dev.sh [command]"
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
}

cmd_build() {
    echo "🔨 Building solution..."
    # Kill any lingering processes first
    "$SCRIPT_DIR/kill-textedit.sh"
    dotnet build "$ROOT_DIR/textedit.sln"
}

cmd_clean() {
    echo "🧹 Cleaning build artifacts..."
    dotnet clean "$ROOT_DIR/textedit.sln"
}

cmd_restore() {
    echo "📦 Restoring NuGet packages..."
    dotnet restore "$ROOT_DIR/textedit.sln"
}

cmd_run() {
    echo "🚀 Starting Electron app..."
    # Kill any lingering processes first
    "$SCRIPT_DIR/kill-textedit.sh"
    cd "$ROOT_DIR/src/TextEdit.App"
    electronize start
}

cmd_test() {
    echo "🧪 Running all tests..."
    # Kill any lingering processes first
    "$SCRIPT_DIR/kill-textedit.sh"
    dotnet test "$ROOT_DIR/textedit.sln" --logger "console;verbosity=normal"
}

cmd_test_unit() {
    echo "🧪 Running unit tests..."
    dotnet test "$ROOT_DIR/tests/unit/TextEdit.Core.Tests/TextEdit.Core.Tests.csproj" --logger "console;verbosity=normal"
}

cmd_test_coverage() {
    echo "🧪 Running tests with coverage..."
    dotnet test "$ROOT_DIR/textedit.sln" \
        /p:CollectCoverage=true \
        /p:CoverletOutputFormat=cobertura \
        /p:CoverletOutput=./TestResults/
}

cmd_electronize_init() {
    echo "⚡ Initializing Electron.NET..."
    
    # Install electronize tool if not present
    echo "Checking for ElectronNET.CLI tool..."
    if ! dotnet tool list -g | grep -q ElectronNET.CLI; then
        echo "Installing ElectronNET.CLI tool globally..."
        dotnet tool install -g ElectronNET.CLI
        if [ $? -ne 0 ]; then
            echo "❌ Failed to install ElectronNET.CLI"
            return 1
        fi
        echo "✅ ElectronNET.CLI installed successfully"
    else
        echo "✅ ElectronNET.CLI already installed"
    fi
    
    cd "$ROOT_DIR/src/TextEdit.App"
    echo "Running electronize init..."
    electronize init
}

cmd_electronize_build() {
    echo "📦 Building production Electron package..."
    echo "Select target platform:"
    echo "  1) Windows"
    echo "  2) macOS"
    echo "  3) Linux"
    read -p "Enter choice (1-3): " choice
    
    cd "$ROOT_DIR/src/TextEdit.App"
    
    case $choice in
        1)
            echo "Building for Windows..."
            electronize build /target win
            ;;
        2)
            echo "Building for macOS..."
            electronize build /target osx
            ;;
        3)
            echo "Building for Linux..."
            electronize build /target linux
            ;;
        *)
            echo "Invalid choice. Defaulting to current platform..."
            electronize build
            ;;
    esac
}

# Main command dispatcher
if [ $# -eq 0 ]; then
    show_help
    exit 0
fi

case "$1" in
    build)
        cmd_build
        ;;
    clean)
        cmd_clean
        ;;
    restore)
        cmd_restore
        ;;
    cleanup)
        "$SCRIPT_DIR/kill-textedit.sh"
        ;;
    run)
        cmd_run
        ;;
    test)
        cmd_test
        ;;
    test:unit)
        cmd_test_unit
        ;;
    test:coverage)
        cmd_test_coverage
        ;;
    electronize:init)
        cmd_electronize_init
        ;;
    electronize:build)
        cmd_electronize_build
        ;;
    help)
        show_help
        ;;
    *)
        echo "Unknown command: $1"
        echo ""
        show_help
        exit 1
        ;;
esac

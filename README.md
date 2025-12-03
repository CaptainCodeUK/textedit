# Scrappy Text Editor - A Modern Desktop Text Editor

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
<!-- Electron.NET badge intentionally without version to avoid drift -->
[![Electron.NET](https://img.shields.io/badge/Electron.NET-enabled-47848F)](https://github.com/ElectronNET/Electron.NET)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A cross-platform desktop text editor built with .NET 8, Blazor Server, and Electron.NET. Features multi-document tabs, markdown preview, session persistence, and smart autosave.

Scrappy Text Editor was formerly named “TextEdit” in early internal builds; some internal folder names (e.g., session storage) still reflect that for backward compatibility.

## 🚀 Features

### Core Editing
- **Multi-Document Tabs** - Work with multiple files simultaneously with independent undo/redo history
- **Session Persistence** - Automatically saves your work on close and restores it on next launch
- **Smart Autosave** - Recovers unsaved work after unexpected crashes (30-second intervals)
- **Word Wrap** - Toggle word wrapping with Alt+Z
    
Spell checking: a minimal embedded English dictionary is included for development.
To use a full Hunspell `en_US` dictionary, place `en_US.dic` and `en_US.aff` in:
	- Linux: `~/.config/TextEdit/Dictionaries/`
	- Windows: `%AppData%\TextEdit/Dictionaries/`
	- macOS: `~/Library/Application Support/TextEdit/Dictionaries/`

You can also embed dictionaries into the runtime by adding `.dic`/`.aff` files under `src/TextEdit.Infrastructure/SpellChecking/Resources/Dictionaries/` and they will be loaded at startup.

### Markdown Support
- **Live Preview** - Render markdown with GitHub Flavored Markdown support (Alt+P to toggle)
- **Performance Optimized** - Smart caching for instant re-renders (165-330x speedup)
- **Large File Handling** - Manual refresh mode for documents >100KB

### File Management
- **Conflict Detection** - Detects external file modifications and offers Reload/Keep Mine/Save As
- **Large File Support** - Opens files up to 10MB; read-only mode for files >10MB
- **Missing File Recovery** - Gracefully handles deleted or missing files
- **Permission Handling** - Smart fallback to Save As when permission denied

### User Interface
- **Standard Menus** - File, Edit, and View menus with keyboard shortcuts
- **Status Bar** - Line/column position, character count, autosave indicator, filename
- **Error Dialogs** - User-friendly error messages with actionable choices
- **Accessibility** - Full keyboard navigation, ARIA labels, screen reader support

## 📋 Requirements

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 18+** - [Download](https://nodejs.org/) (for Electron.NET)
- **Operating System** - Windows 10+, macOS 10.13+, or Linux with GTK 3

## 🛠️ Quick Start

### Prerequisites

Before building or running the application, install the Electron.NET CLI tool:

```bash
dotnet tool install ElectronNET.CLI -g
```

If already installed, update to the latest version:

```bash
dotnet tool update ElectronNET.CLI -g
```

### Development

```bash
# Clone the repository
git clone https://github.com/CaptainCodeUK/textedit.git
cd textedit

# Restore dependencies
dotnet restore

# Run in development mode
./scripts/dev.fish run
# or
./scripts/dev.sh run
```

The application will launch with Electron.NET in development mode.

### CLI usage

You can launch Scrappy with files from the command line; they’ll open in tabs. If Scrappy is already running, a second launch will forward the files to the existing window.

```bash
# Open one or more files
scrappy-text-editor notes.txt README.md

# While the app is already running, open another file in the current window
scrappy-text-editor todo.md
```

If some paths are invalid or unreadable, a non-blocking summary dialog lists the files and reasons (e.g., “File not found”, “Permission denied”, “Invalid path”).

### Build for Production

```bash
# Build for your current platform
dotnet build -c Release

# Package for distribution
cd src/TextEdit.App
electronize build /target win      # Windows
electronize build /target osx      # macOS
electronize build /target linux    # Linux
```

Binaries will be in `src/TextEdit.App/bin/Desktop/`.

## 🏗️ Project Structure

```
textedit/
├── src/
│   ├── TextEdit.App/              # Electron.NET host application
│   ├── TextEdit.Core/             # Domain models and business logic
│   ├── TextEdit.Infrastructure/   # File I/O, persistence, IPC
│   ├── TextEdit.Markdown/         # Markdown rendering with caching
│   └── TextEdit.UI/               # Blazor components and pages
├── tests/
│   ├── unit/                      # Unit tests (85%+ coverage)
│   ├── integration/               # Integration and accessibility tests
│   ├── contract/                  # IPC contract tests
│   └── benchmarks/                # Performance benchmarks
├── specs/                         # Feature specifications and design docs
└── scripts/                       # Development scripts
```

See individual project README files for detailed documentation:
- [TextEdit.App](src/TextEdit.App/README.md) - Main application entry point
- [TextEdit.Core](src/TextEdit.Core/README.md) - Core domain models
- [TextEdit.Infrastructure](src/TextEdit.Infrastructure/README.md) - Infrastructure services
- [TextEdit.Markdown](src/TextEdit.Markdown/README.md) - Markdown rendering
- [TextEdit.UI](src/TextEdit.UI/README.md) - Blazor UI components

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage reporting
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Run unit tests only
dotnet test tests/unit/TextEdit.Core.Tests/

# Run benchmarks
dotnet run -c Release --project tests/benchmarks/TextEdit.Benchmarks/
```

**Test Coverage:** Per-project thresholds enforce quality gates:
- **Core.Tests**: 45% line coverage (covers Core 78% + Infrastructure 35.7%)
- Additional test projects have independent coverage thresholds as appropriate

The coverage task (`./scripts/dev.fish test:coverage`) enforces thresholds per test project. Each test project's `.csproj` defines its own `<Threshold>` target based on current coverage levels to prevent regression.

### Accessibility: Contrast checks

We track WCAG 2.1 AA contrast for key UI pairs (light/dark themes, tabs, dialogs).

Run the local check:

```bash
python3 scripts/contrast_check.py
```

The script prints contrast ratios (e.g., 11.78:1). Our CI runs the same check and will fail if any tested pair drops below 4.5:1.

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+N` / `Cmd+N` | New document |
| `Ctrl+O` / `Cmd+O` | Open file |
| `Ctrl+S` / `Cmd+S` | Save |
| `Ctrl+Shift+S` / `Cmd+Shift+S` | Save As |
| `Ctrl+W` / `Cmd+W` | Close tab |
| `Ctrl+Z` / `Cmd+Z` | Undo |
| `Ctrl+Y` / `Cmd+Y` | Redo |
| `Alt+Z` | Toggle word wrap |
| `Alt+P` | Toggle markdown preview |
| `Ctrl+Tab` | Next tab |
| `Ctrl+Shift+Tab` | Previous tab |

## 🏛️ Architecture

TextEdit follows **Clean Architecture** principles with clear separation of concerns:

### Layers

1. **Core** (`TextEdit.Core`) - Domain entities, business logic, abstractions
2. **Infrastructure** (`TextEdit.Infrastructure`) - External concerns (file I/O, persistence, IPC)
3. **Markdown** (`TextEdit.Markdown`) - Specialized rendering with caching
4. **UI** (`TextEdit.UI`) - Blazor Server components and state management
5. **App** (`TextEdit.App`) - Electron.NET host and native integration

### Key Design Patterns

- **Repository Pattern** - `DocumentService` abstracts file operations
- **Service Layer** - `AppState` orchestrates business operations
- **Observer Pattern** - Event-based state change notifications
- **Command Pattern** - `EditorCommandHub` for menu command routing
- **Dependency Injection** - Constructor injection throughout

## 🎯 Performance

### Benchmarks (Release build)

| Operation | Performance |
|-----------|-------------|
| Open 10KB file | ~56μs |
| Open 5MB file | ~45ms (streaming) |
| Save 10KB file | ~1.2ms |
| Markdown render (1KB) | 5.5μs |
| Markdown render (cached) | 7.5μs (330x faster) |
| Undo/Redo | <100ms |
| Startup (cold) | <2s |

### Optimizations

- ✅ **Markdown Result Caching** - 165-330x speedup with SHA256-based cache
- ✅ **Selective Component Re-renders** - StatusBar and TabStrip skip unnecessary updates
- ✅ **Streaming I/O** - Files >10MB use chunked read/write
- ✅ **Debounced Undo Snapshots** - 400ms debounce reduces memory pressure

## 📝 Session Persistence

TextEdit automatically saves your work without intrusive save dialogs:

- **New unsaved documents** → Persisted to temp files, restored as "Untitled" on next launch
- **Modified existing files** → Changes saved to temp files, restored with dirty flag
- **Crash Recovery** → Autosave runs every 30 seconds

Session files are stored in the OS application data directory under `TextEdit/Session` (legacy name retained for compatibility):

- Windows: `%AppData%\TextEdit\Session`
- macOS: `~/Library/Application Support/TextEdit/Session`
- Linux: `~/.config/TextEdit/Session`

### Preferences

User preferences (theme, logging, recognized file extensions, etc.) are stored as JSON:

- Path: `%AppData%/Scrappy/preferences.json` (Windows), `~/Library/Application Support/Scrappy/preferences.json` (macOS), or `~/.config/Scrappy/preferences.json` (Linux)
- Format: camelCase JSON with atomic-save behavior (temp file + rename)

### Options dialog

Open Options from the menu to configure:

- Theme: Light or Dark (System mode deferred)
- Logging: Enable detailed logging and open log folder
- Recognized File Extensions: Add/remove extensions (must match `^\.[a-zA-Z0-9-]+$`; `.txt` and `.md` cannot be removed)

### Fonts

Font controls are available in the toolbar. Defaults are chosen to balance readability and compactness; common monospace fonts are supported. Typical size range: 10–24 px.

## 📚 Documentation

- [Feature Specification](specs/001-text-editor/spec.md) - Full feature requirements
- [Development Plan](specs/001-text-editor/plan.md) - Implementation roadmap
- [IPC Contracts](specs/001-text-editor/contracts/) - Native dialog contracts
- [Quickstart Guide](specs/001-text-editor/quickstart.md) - User documentation
- [Task List](specs/001-text-editor/tasks.md) - Implementation tracking

## 🤝 Contributing

Contributions are welcome! Please ensure:
- All tests pass (`dotnet test`)
- Code coverage meets per-project thresholds (enforced automatically when running `dotnet test` with coverage enabled)
- Follow existing code style and naming conventions

## 📄 License

This project is licensed under the MIT License.

---

**Built with ❤️ using .NET 8, Blazor, and Electron.NET**

---

## 🧩 Icon Build Instructions

To generate multi-resolution app icons for packaging, you can use a Node tool like `electron-icon-maker`:

```bash
# Install (optional, run from any directory)
npm install -g electron-icon-maker

# Generate icons from a square PNG source (at least 1024x1024)
electron-icon-maker --input ./assets/icon.png --output ./src/TextEdit.App/wwwroot/icons
```

Ensure `electron.manifest.json` points to the generated icon assets. The repository already includes icons under `src/TextEdit.App/wwwroot/icons/`.

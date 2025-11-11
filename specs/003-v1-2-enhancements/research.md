# Research: Scrappy Text Editor v1.2 Enhancements

## Decision: Spell checking engine/library
- **Chosen**: Use WeCantSpell.Hunspell (MIT) as the spell checking engine for all platforms.
- **Rationale**: Modern, .NET-native implementation of Hunspell (industry standard, used by LibreOffice/Firefox/Chrome). Supports custom/user dictionaries, Unicode, and is actively maintained. Integrates easily with .NET 8, Blazor, and Electron.NET. Avoids reinventing spell checking logic and leverages proven algorithms.
- **Alternatives considered**: NHunspell (older, less .NET-native), NetSpell (less accurate), custom implementation (higher maintenance, less robust).

## Decision: Spell checking in code blocks
- **Chosen**: Spell checking is disabled within code blocks and markdown fenced sections; no misspelling indicators appear inside those regions.
- **Rationale**: Reduces false positives for technical content, matches user/editor expectations, and improves usability for developers and technical writers.
- **Alternatives considered**: Enable everywhere (would flag code as misspelled), user toggle (adds UI complexity).

## Decision: Critical security update behavior
- **Chosen**: Prompt user with a clearly labeled critical update dialog; installation proceeds only after user confirmation and the messaging emphasizes urgency.
- **Rationale**: Balances user control with security urgency, avoids surprise restarts, and ensures users are aware of critical updates.
- **Alternatives considered**: Auto-install without prompt (could disrupt work), treat as normal update (less urgency).

## Decision: CI build behavior for rapid commits
- **Chosen**: Any in-progress or queued builds for earlier commits are canceled and only the latest commit is built; canceled runs are marked accordingly in CI.
- **Rationale**: Efficient use of CI resources, fast feedback on current state, avoids redundant builds and artifacts.
- **Alternatives considered**: Queue all builds (slow, expensive), debounce with time window (delays feedback).

## Decision: Test assertion library preference
- **Chosen**: Use standard xUnit assertions (`Assert.Equal`, `Assert.True`, `Assert.Throws`, etc.) for all unit tests. Avoid FluentAssertions or other third-party assertion libraries.
- **Rationale**: Reduces dependencies, keeps tests simple and maintainable, avoids learning curve for new contributors, and xUnit's built-in assertions are sufficient for our needs. FluentAssertions adds minimal value for the added complexity and dependency maintenance.
- **Alternatives considered**: FluentAssertions (more expressive but adds dependency), Shouldly (similar tradeoffs).

## Decision: Auto-updater library selection
- **Chosen**: Use Electron.NET's built-in auto-updater wrapper around Electron's native autoUpdater module. For Windows/macOS, leverage Squirrel format (Squirrel.Windows for Windows, Squirrel.Mac for macOS). For Linux, use AppImage with built-in update support via electron-builder.
- **Rationale**: 
  - **Electron.NET alignment**: Electron.NET provides `Electron.AutoUpdater` API that wraps the native Electron autoUpdater, avoiding additional NuGet dependencies
  - **Cross-platform**: Electron's autoUpdater natively supports Squirrel.Windows, Squirrel.Mac, and AppImage update mechanisms
  - **Industry standard**: Squirrel is battle-tested (used by Slack, GitHub Desktop, VS Code in early versions) with robust delta updates and rollback
  - **Simple server requirements**: Update server only needs static file hosting (GitHub Releases, S3, CDN) with version manifest
  - **Integrated with electronize**: `electronize build` can generate Squirrel-compatible installers with proper update metadata
  - **No separate NuGet packages**: Electron's autoUpdater is included in ElectronNET.API (already a dependency)
- **Implementation approach**:
  - Windows: Use Squirrel.Windows format (`.nupkg` + RELEASES file) via `electronize build /target win`
  - macOS: Use Squirrel.Mac format (`.zip` with code signing) via `electronize build /target osx`
  - Linux: Use AppImage with zsync for delta updates via `electronize build /target linux`
  - Update server: GitHub Releases (free, CDN-backed, version tagging)
  - Manifest: Electron autoUpdater reads platform-specific manifest (RELEASES for Windows, latest-mac.yml for macOS, latest-linux.yml for Linux)
- **Alternatives considered**:
  - **Squirrel.Windows NuGet**: Requires separate Windows-specific package, more complex integration, no Linux/macOS support
  - **electron-updater (JS)**: Excellent library but requires Node.js interop from .NET, adds complexity for Blazor Server architecture
  - **Custom update logic**: High maintenance, reinvents wheel, lacks delta updates and rollback
  - **ClickOnce**: Windows-only, dated, poor UX for Electron apps
- **Tradeoffs accepted**:
  - Requires proper code signing for macOS/Windows (notarization, certificate management)
  - GitHub Releases as update server creates dependency on GitHub infrastructure (acceptable for open-source project)
  - AppImage adoption on Linux lower than .deb, but AppImage supports auto-update natively
- **References**:
  - Electron autoUpdater API: https://www.electronjs.org/docs/latest/api/auto-updater
  - Electron.NET docs: https://github.com/ElectronNET/Electron.NET/wiki
  - Squirrel.Windows: https://github.com/Squirrel/Squirrel.Windows
  - Publishing updates: https://www.electron.build/auto-update


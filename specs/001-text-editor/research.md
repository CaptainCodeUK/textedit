# Research: Text Editor Application (001-text-editor)

## Decisions, Rationale, Alternatives

### UI Shell and Framework
- Decision: Electron.NET hosting ASP.NET Core 8 + Blazor (Server) UI
- Rationale: Enables Blazor component model and Tailwind styling with Electron packaging
  for Windows/macOS/Linux; mature ecosystem, straightforward distribution via Electron.
- Alternatives: 
  - .NET MAUI (Pros: native, Cons: desktop maturity varies, Tailwind integration custom)
  - Avalonia UI (Pros: native desktop, Cons: Tailwind/Blazor synergy lost, different skillset)
  - Pure Electron + WASM Blazor (Pros: thin .NET host, Cons: more JS interop complexity)

### Styling
- Decision: TailwindCSS built via Node toolchain into wwwroot (purged)
- Rationale: Small, consistent CSS with design tokens; meets performance budget
- Alternatives: Bootstrap, custom CSS; rejected due to heavier payload or more boilerplate

### Markdown Rendering
- Decision: Markdig (.NET) for Markdown -> HTML
- Rationale: Fast, fully managed, CommonMark compliant, extensible pipeline
- Alternatives: Markdown-it via JS; rejected to avoid extra JS runtime coupling

### File Watching / External Edits
- Decision: System.IO.FileSystemWatcher with debounce; hash+timestamp verification
- Rationale: Detect changes reliably across platforms; prompt Reload/Keep Mine/Save As
- Alternatives: Polling; rejected as less efficient, more CPU intensive

### Session Persistence
- Decision: Autosave unsaved/new docs to OS app data/temp dir every 30s; restore on launch
  - Windows: %LocalAppData%/TextEdit/Autosave
  - macOS: ~/Library/Application Support/TextEdit/Autosave
  - Linux: ~/.local/share/TextEdit/autosave (fallback /tmp if needed)
- Rationale: Avoid blocking on close; robust crash recovery; complies with spec FR-018..FR-022
- Alternatives: Single session file; rejected to keep per-doc isolation and recovery clarity

### Large Files
- Decision: Fully editable up to 10MB; show warning ≥10MB with Read-Only option; 
  disable live auto-preview and enable manual refresh for big files
- Rationale: Keeps UI responsive and within memory budget; matches Success Criteria
- Alternatives: Streamed/virtualized editor; defer until needed

### Testing Stack
- Decision: xUnit (unit), bUnit (Blazor components), Microsoft.Playwright (.NET) for UI;
  JSON Schema validation for IPC contract tests; coverlet for coverage
- Rationale: Modern, widely adopted; good CI integration
- Alternatives: NUnit/MSTest; either acceptable, xUnit chosen for ecosystem

### Packaging / Binaries
- Decision: ElectronNET.CLI (electronize build) for Win/macOS/Linux artifacts
- Rationale: Standard toolchain; predictable outputs
- Alternatives: Custom Electron Forge setup; rejected for added complexity

### Accessibility
- Decision: WCAG 2.1 AA; keyboard navigation; focus styling; ARIA roles in components
- Rationale: Constitution compliance (UX Consistency); improves usability

## Open Questions (Resolved)
- Blazor Server vs WASM in Electron: Chosen Server (simpler, rich .NET access)
- IPC scope: Minimal set (open/save dialogs, autosave notifications, window lifecycle)
- Telemetry/Monitoring: Local structured logs + optional performance probes; no external
  telemetry by default (desktop app)

## Implementation Notes
- Tailwind: add build step in solution to generate purged CSS to wwwroot
- Playwright: configure Electron launch in tests; use data-testid selectors in UI
- Coverage: enforce 80% overall, 95% critical (autosave/persistence/IPC) in CI

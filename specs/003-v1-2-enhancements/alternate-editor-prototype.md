# Alternative Editor — Prototype & Evaluation

This page outlines an initial investigation to prototype and evaluate an alternative text editing engine for TextEdit.

Goals
- Explore alternative editor engines (e.g., Monaco, CodeMirror, ACE) for richer editing features (semantic highlighting, multi-cursor, LSP support).
- Verify integration feasibility with Blazor Server + Electron.NET and our `AppState` architecture.
- Build a minimal proof-of-concept (PoC) with Monaco embed in a new Blazor component and basic editing features (open/save, undo/redo, selection sync).

Success Criteria
- Editor runs inside our Blazor Server environment without breaking existing services.
- Core editing flows (open, edit, save) work with our DocumentService and persistence.
- LSP or language-aware features show signs of integration feasibility.
- Performance impact measured (cold render & warm interactions) and within acceptable thresholds.

Steps
1. Prototype: Create `AltEditor.razor` and `AltEditor.razor.cs` that loads Monaco via local CDN and gives an API for value/selection events.
2. AppState wiring: Hook a minimal DocumentService integration to persist content when saved.
3. Validation: Manual test across Windows/macOS/Linux and run a fast local perf test on a 1M+ char document.
4. Decide: If PoC succeeds, create a follow-up PR with the base component plus a feature toggle in Options.

Risks & Mitigations
- Monaco requires Node build tooling for language servers — the initial PoC will use simple static editor features to reduce complexity.
- Editor size may increase packaged binary — track size changes and add conditional packaging of editor-specific assets.

Timeline
- Week 1: PoC and basic DocumentService integration
- Week 2: Upstream LSP trial and performance checks

Next steps
- Branch from `main` called `feature/alt-editor-prototype` and commit the initial PoC and docs.

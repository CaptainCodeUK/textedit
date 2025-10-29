# TextEdit.Infrastructure.Tests

Unit tests for the Infrastructure layer.

## What’s Covered

- FileSystemService: basic I/O and large file chunked read/write
- FileWatcher: change detection and debounce behavior
- PersistenceService: session save/restore, preference persistence
- AutosaveService: timer behavior, failure handling
- IpcBridge: dialog flow (mocked)
- Telemetry/PerformanceLogger: aggregation and metrics

## How to Run

```fish
# From repository root
./scripts/dev.fish test:unit

# Or directly
dotnet test tests/unit/TextEdit.Infrastructure.Tests/
```

## Notes

- Some tests use temporary directories and clean up after themselves
- File system behavior is platform-dependent; tests avoid brittle path assumptions

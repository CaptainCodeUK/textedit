# TextEdit.Core.Tests

Unit tests for the Core domain layer.

## What’s Covered

- DocumentService: open/save/update, large file rules, read-only mode
- UndoRedoService: per-document stacks, undo/redo behavior
- EditorState: caret/character count, event notifications
- TabService: add/close/activate tab flow
- FileWatcher (via integration with core abstractions)
- PersistenceService (core-facing behaviors)

## How to Run

```fish
# From repository root
./scripts/dev.fish test:unit

# Or directly
dotnet test tests/unit/TextEdit.Core.Tests/
```

## Notes

- File I/O is abstracted via IFileSystem for easy mocking
- Tests are deterministic and avoid touching real disk where possible

# TextEdit.IPC.Tests (Contract)

Contract tests for IPC interactions and native dialog flows.

## What’s Covered

- IPC request/response message formats
- File dialog request/response handling
- Save As and Open flows
- Edge cases (cancel, invalid paths)

## How to Run

```fish
# From repository root
./scripts/dev.fish test:all

# Or directly (just contract tests)
dotnet test tests/contract/TextEdit.IPC.Tests/
```

## Notes

- Contract tests validate shape and behavior against the defined schemas in `specs/001-text-editor/contracts/`
- These tests do not spin up a full Electron instance; they verify the bridge logic and message routing

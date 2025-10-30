# IPC Contract: CLI Arguments

**Purpose**: Define message format for passing command-line file arguments from Electron (native) to Blazor (managed) context

**Direction**: Electron → Blazor (one-way)

**Trigger**: Application launch with file arguments OR second instance launch attempt

## Message Schema

### Channel Name
`"cli-file-args"`

### Message Format (JSON)

```json
{
  "validFiles": [
    "/absolute/path/to/file1.txt",
    "/absolute/path/to/file2.md"
  ],
  "invalidFiles": [
    {
      "path": "/path/to/nonexistent.txt",
      "reason": "File not found"
    },
    {
      "path": "/path/to/noperm.log",
      "reason": "Permission denied"
    }
  ],
  "launchType": "initial"
}
```

### Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `validFiles` | `string[]` | Yes | Array of absolute file paths that exist and are readable |
| `invalidFiles` | `InvalidFileInfo[]` | Yes | Array of files that couldn't be opened (may be empty) |
| `launchType` | `string` | Yes | Either `"initial"` (first launch) or `"second-instance"` (focus existing window) |

### InvalidFileInfo Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `path` | `string` | Yes | The file path that was provided |
| `reason` | `string` | Yes | Simple reason phrase: "File not found", "Permission denied", "Unreadable", or "Invalid path" |

## Validation Rules

**Sender (Electron side)**:
- MUST validate all file paths before sending
- MUST convert relative paths to absolute using `process.cwd()`
- MUST classify failures into one of the 4 standard reasons
- MUST send empty array for `validFiles` if no valid files (not null/undefined)
- MUST send empty array for `invalidFiles` if all files valid
- `launchType` MUST be either `"initial"` or `"second-instance"`

**Receiver (Blazor side)**:
- MUST handle empty `validFiles` array gracefully (don't crash)
- MUST display summary for `invalidFiles` only if non-empty
- MUST NOT block UI while processing files
- SHOULD open files in the order provided in `validFiles` array
- MUST sanitize file paths before display to prevent XSS

## Example Messages

### Successful Launch with Multiple Files

```json
{
  "validFiles": [
    "/home/user/documents/readme.txt",
    "/home/user/projects/notes.md",
    "/home/user/logs/app.log"
  ],
  "invalidFiles": [],
  "launchType": "initial"
}
```

### Launch with Some Invalid Files

```json
{
  "validFiles": [
    "C:\\Users\\user\\document.txt"
  ],
  "invalidFiles": [
    {
      "path": "C:\\Users\\user\\missing.txt",
      "reason": "File not found"
    },
    {
      "path": "C:\\restricted\\secret.log",
      "reason": "Permission denied"
    }
  ],
  "launchType": "initial"
}
```

### Second Instance Launch (Focus Existing)

```json
{
  "validFiles": [
    "/home/user/newfile.md"
  ],
  "invalidFiles": [],
  "launchType": "second-instance"
}
```

### Launch with No Files (Application Icon Click)

```json
{
  "validFiles": [],
  "invalidFiles": [],
  "launchType": "initial"
}
```

## Error Handling

**Sender Errors**:
- If path validation throws exception: add to `invalidFiles` with reason "Unreadable"
- If JSON serialization fails: log error, don't crash Electron process
- If IPC send fails: log error, files won't open (acceptable failure mode)

**Receiver Errors**:
- If JSON deserialization fails: log error, show generic "Failed to open command-line files" message
- If file opening fails after validation: treat as new dirty document (don't crash)
- If `validFiles` contains non-existent path: attempt to open, show error in tab if fails

## Performance Requirements

- Path validation (sender): MUST complete in <100ms for up to 50 files
- IPC transmission: MUST complete in <50ms for payload up to 10KB
- File opening (receiver): SHOULD show progress if >10 files, open within 3 seconds total

## Security Considerations

- Paths MUST be absolute (no `..` traversal after conversion)
- Paths MUST be validated against OS-specific path rules
- No execution of files; read-only access required
- Display paths to user MUST be HTML-encoded to prevent XSS

## Testing Checklist

- [ ] Valid single file opens correctly
- [ ] Multiple valid files open in order
- [ ] Invalid file shows in summary with correct reason
- [ ] Mix of valid/invalid handled correctly
- [ ] Empty arrays (no files) handled gracefully
- [ ] Paths with spaces and special characters work
- [ ] Relative paths converted to absolute
- [ ] Long paths (>256 chars on Windows) handled
- [ ] Second instance focuses window and opens files
- [ ] Non-blocking: UI remains responsive during processing

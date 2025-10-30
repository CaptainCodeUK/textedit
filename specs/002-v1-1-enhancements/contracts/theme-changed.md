# IPC Contract: Theme Change Notification

**Purpose**: Define message format for notifying Blazor UI when OS theme changes (System theme mode only)

**Direction**: Electron → Blazor (one-way notification)

**Trigger**: Electron `nativeTheme.on('updated')` event fires

## Message Schema

### Channel Name
`"theme-changed"`

### Message Format (JSON)

```json
{
  "theme": "dark",
  "timestamp": "2025-10-30T14:32:10.123Z"
}
```

### Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `theme` | `string` | Yes | Either `"light"` or `"dark"` |
| `timestamp` | `string` | Yes | ISO 8601 UTC timestamp of change detection |

## Validation Rules

**Sender (Electron side)**:
- MUST only send when user preference is `ThemeMode.System`
- MUST debounce events (100ms window) to prevent rapid switching
- `theme` MUST be lowercase "light" or "dark" (no other values)
- `timestamp` MUST be ISO 8601 format with UTC timezone

**Receiver (Blazor side)**:
- MUST ignore message if user preference is NOT `ThemeMode.System`
- MUST apply theme change within 500ms of receipt
- MUST update all UI components (editor, toolbar, dialogs, etc.)
- MUST re-render markdown preview with new theme
- SHOULD log theme change for debugging purposes

## Example Messages

### OS Switched to Dark Mode

```json
{
  "theme": "dark",
  "timestamp": "2025-10-30T14:32:10.123Z"
}
```

### OS Switched to Light Mode

```json
{
  "theme": "light",
  "timestamp": "2025-10-30T15:45:33.456Z"
}
```

## State Machine

```
Electron detects OS theme change
  ↓
Check if debounce window active (100ms)
  ↓
  If YES: ignore event
  If NO: proceed
  ↓
Read nativeTheme.shouldUseDarkColors
  ↓
Construct message with current theme + timestamp
  ↓
Send IPC message to Blazor
  ↓
Blazor receives message
  ↓
Check if Preferences.Theme == System
  ↓
  If NO: ignore message
  If YES: apply theme change
```

## Error Handling

**Sender Errors**:
- If `nativeTheme.shouldUseDarkColors` throws: default to "light", log error
- If IPC send fails: log error, no theme change applied (acceptable)

**Receiver Errors**:
- If JSON deserialization fails: log error, ignore message
- If theme value is invalid (not "light" or "dark"): log error, ignore message
- If theme application throws: log error, UI may be in inconsistent state (user can manually change theme in Options)

## Performance Requirements

- Event debouncing: 100ms window
- IPC transmission: <10ms
- Theme application in UI: <500ms (per spec FR-024)

## Security Considerations

- No user input in message (OS-generated only)
- Timestamp is informational only; not used for ordering/synchronization
- No sensitive data in message

## Testing Checklist

- [ ] OS theme change detected and sent
- [ ] Message format validated correctly
- [ ] Debouncing prevents rapid switching
- [ ] Blazor applies theme when preference is System
- [ ] Blazor ignores message when preference is Light/Dark
- [ ] UI updates within 500ms
- [ ] Markdown preview updates with correct theme
- [ ] Invalid theme value handled gracefully
- [ ] Missing timestamp handled gracefully

# JSON Schema: User Preferences

**Purpose**: Define JSON structure and validation rules for preferences.json file

**Location**: OS application data directory + `/Scrappy/preferences.json`

**Format**: JSON with pretty-printing (indented)

## Schema Definition

### JSON Schema (v7)

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "Scrappy Text Editor User Preferences",
  "type": "object",
  "required": ["theme", "fontFamily", "fontSize", "fileExtensions", "loggingEnabled", "toolbarVisible"],
  "properties": {
    "theme": {
      "type": "string",
      "enum": ["Light", "Dark", "System"],
      "default": "System",
      "description": "UI theme preference"
    },
    "fontFamily": {
      "type": "string",
      "maxLength": 100,
      "default": "",
      "description": "Editor font family name; empty string means system monospace"
    },
    "fontSize": {
      "type": "integer",
      "minimum": 8,
      "maximum": 72,
      "default": 12,
      "description": "Editor font size in points"
    },
    "fileExtensions": {
      "type": "array",
      "items": {
        "type": "string",
        "pattern": "^\\.[a-zA-Z0-9-]+$"
      },
      "minItems": 2,
      "uniqueItems": true,
      "contains": {
        "enum": [".txt", ".md"]
      },
      "default": [".txt", ".md", ".log", ".json", ".xml", ".csv", ".ini", ".cfg", ".conf"],
      "description": "Recognized text file extensions; must include .txt and .md"
    },
    "loggingEnabled": {
      "type": "boolean",
      "default": false,
      "description": "Whether detailed logging is active"
    },
    "toolbarVisible": {
      "type": "boolean",
      "default": true,
      "description": "Whether toolbar is shown"
    }
  },
  "additionalProperties": false
}
```

## Example Valid Preferences

### Default Preferences (First Launch)

```json
{
  "theme": "System",
  "fontFamily": "",
  "fontSize": 12,
  "fileExtensions": [".txt", ".md", ".log", ".json", ".xml", ".csv", ".ini", ".cfg", ".conf"],
  "loggingEnabled": false,
  "toolbarVisible": true
}
```

### User-Customized Preferences

```json
{
  "theme": "Dark",
  "fontFamily": "Consolas",
  "fontSize": 14,
  "fileExtensions": [".txt", ".md", ".log", ".json", ".xml", ".csv", ".ini", ".cfg", ".conf", ".rs", ".py", ".js"],
  "loggingEnabled": true,
  "toolbarVisible": true
}
```

### Minimal Valid Preferences

```json
{
  "theme": "Light",
  "fontFamily": "",
  "fontSize": 10,
  "fileExtensions": [".txt", ".md"],
  "loggingEnabled": false,
  "toolbarVisible": false
}
```

## Validation Rules

### Field-Level Validation

| Field | Rules | Error Handling |
|-------|-------|----------------|
| `theme` | Must be "Light", "Dark", or "System" (case-sensitive) | Invalid value → default to "System", log warning |
| `fontFamily` | Max 100 characters, can be empty string | Too long → truncate, log warning |
| `fontSize` | Integer between 8 and 72 inclusive | Out of range → clamp to 8 or 72, log warning |
| `fileExtensions` | Array with ≥2 items, must contain ".txt" and ".md" | Missing required → add them, log warning |
| `fileExtensions` | Each item matches `^\.[a-zA-Z0-9-]+$` | Invalid format → remove item, log warning |
| `fileExtensions` | No duplicates (case-insensitive) | Duplicates → keep first occurrence, log warning |
| `loggingEnabled` | Boolean | Non-boolean → default to false, log warning |
| `toolbarVisible` | Boolean | Non-boolean → default to true, log warning |

### File-Level Validation

| Condition | Handling |
|-----------|----------|
| File missing | Create with defaults on first save |
| File corrupt (invalid JSON) | Backup as `.bak`, use defaults, log error |
| Unknown properties | Ignore them (`additionalProperties: false` in schema) |
| Missing required properties | Fill with defaults, log warning |
| Empty file | Treat as missing, use defaults |

## Migration Strategy

### Version 1.0 (Current)

No version field in JSON (implicit v1.0)

### Future Versions

If schema changes, add `version` field:

```json
{
  "version": "1.1",
  "theme": "Dark",
  ...
}
```

**Migration Logic**:
1. Check for `version` field
2. If missing, assume v1.0
3. Apply migrations sequentially (v1.0 → v1.1 → v1.2, etc.)
4. Update `version` field after migration
5. Save migrated preferences

## Atomic Write Pattern

To prevent corruption during save:

```csharp
var tempPath = _prefsPath + ".tmp";
await File.WriteAllTextAsync(tempPath, json);
File.Move(tempPath, _prefsPath, overwrite: true);
```

This ensures:
- If write fails, original file unchanged
- If move fails, temp file exists for recovery
- No partial writes that corrupt JSON

## Testing Checklist

- [ ] Valid default preferences deserialize correctly
- [ ] User-customized preferences persist correctly
- [ ] Invalid theme value defaults to "System"
- [ ] Out-of-range fontSize clamped to 8-72
- [ ] Missing .txt or .md added to fileExtensions
- [ ] Invalid extension format removed from array
- [ ] Duplicate extensions (case-insensitive) deduplicated
- [ ] Corrupt JSON handled with fallback to defaults
- [ ] Unknown properties ignored without error
- [ ] Atomic write prevents corruption
- [ ] Empty file treated as missing
- [ ] Max length exceeded for fontFamily truncates

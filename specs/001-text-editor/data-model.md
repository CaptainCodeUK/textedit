# Data Model: Text Editor Application

## Entities

### Document
- id: GUID (runtime)
- filePath: string | null (null for new/untitled)
- title: string (file name or "Untitled N")
- content: string
- isDirty: bool
- createdAt: DateTimeOffset
- updatedAt: DateTimeOffset
- undoStack: Stack<EditOp>
- redoStack: Stack<EditOp>
- cursorPosition: (line:int, column:int)
- selectionRange: (start:int, length:int)

### Tab
- id: GUID
- documentId: GUID
- isActive: bool
- dirtyIndicator: bool (mirrors Document.isDirty)

### TemporaryPersistenceRecord
- id: GUID
- kind: enum { NewDocument, ExistingFilePatch }
- originalFilePath: string | null
- content: string
- lastSavedAt: DateTimeOffset
- recoveryPath: string (absolute path of autosave file)

### EditorState
- activeDocumentId: GUID | null
- wordWrapEnabled: bool
- statusBar: { line:int, column:int, charCount:int }

### IPC Messages (Contracts overview)
- openFileDialog.request: { filters?: string[], multi?: bool }
- openFileDialog.response: { canceled: bool, filePaths: string[] }
- saveFileDialog.request: { defaultPath?: string }
- saveFileDialog.response: { canceled: bool, filePath?: string }
- persistUnsaved.request: { records: TemporaryPersistenceRecord[] }
- restoreSession.request: {}
- restoreSession.response: { records: TemporaryPersistenceRecord[] }

## Relationships
- 1 Tab -> 1 Document
- 1 EditorState -> [0..*] Tabs (via activeDocumentId + collection maintained in UI layer)
- 0..* TemporaryPersistenceRecord -> 0..1 Document (by originalFilePath when applicable)

## Validation Rules
- Saving requires either existing filePath (write permissions) or selection of a new path
- Autosave interval default 30s; can be clamped to ≥5s to avoid excessive IO
- Large files ≥10MB open read-only unless user confirms edit intent
- Markdown preview manual-refresh for large files (≥10MB) to avoid stalls

## State Transitions (Document)
- New -> Edited (on first change) -> Saved (on Save) -> Edited ...
- Existing -> Edited (on change) -> Saved (on Save) -> Edited ...
- Edited -> Closed (on Discard) [no save]
- Any -> Recovered (on crash recovery)

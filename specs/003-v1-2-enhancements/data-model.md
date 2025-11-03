# Data Model: Scrappy Text Editor v1.2 Enhancements

## Entities

### FindQuery
- searchTerm: string
- matchCase: bool
- wholeWord: bool
- currentIndex: int
- totalMatches: int

### ReplaceOperation
- findQuery: FindQuery
- replacement: string
- operationType: enum (Single, All)
- replacementsMade: int

### SpellCheckSuggestion
- originalWord: string
- suggestion: string
- confidence: float

### CustomDictionary
- words: List<string>
- filePath: string (location in app data dir)
- format: enum (PlainText, HunspellDic) // Plain text (one word per line) or Hunspell .dic format

### WindowState
- x: int
- y: int
- width: int
- height: int
- state: enum (Normal, Maximized, Minimized)
- monitorId: string

### UpdateMetadata
- version: string
- downloadUrl: string
- releaseNotes: string
- fileSize: int
- checksum: string

### ReleaseArtifact
- filename: string
- fileSize: int
- downloadUrl: string
- platform: enum (Windows, macOS, Linux)
- version: string

## Relationships
- ReplaceOperation references FindQuery
- CustomDictionary is loaded at app start and updated via UI
- WindowState is persisted on close and restored on launch
- ReleaseArtifact is produced by CI and referenced by auto-updater

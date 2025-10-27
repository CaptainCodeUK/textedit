namespace TextEdit.Infrastructure.Persistence;

using System.Text.Json;
using TextEdit.Core.Documents;

/// <summary>
/// Persists session state (unsaved documents) to temp storage for crash recovery and session restore.
/// </summary>
public class PersistenceService
{
    private readonly string _sessionDir;

    public PersistenceService()
    {
        // Use user-scoped application data folder for durability (instead of /tmp)
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _sessionDir = Path.Combine(appData, "TextEdit", "Session");
        Directory.CreateDirectory(_sessionDir);
    }

    public async Task PersistAsync(IEnumerable<Document> documents, IList<Guid>? tabOrder = null)
    {
        try
        {
            Directory.CreateDirectory(_sessionDir);
            
            // Clear old session files
            var existingFiles = Directory.GetFiles(_sessionDir, "*.json");
            foreach (var file in existingFiles)
            {
                try { File.Delete(file); } catch { /* ignore cleanup failures */ }
            }

            // Persist each document that has unsaved changes
            var persistedCount = 0;
            var orderIndex = new Dictionary<Guid, int>();
            if (tabOrder != null)
            {
                for (int i = 0; i < tabOrder.Count; i++)
                {
                    orderIndex[tabOrder[i]] = i;
                }
            }
            foreach (var doc in documents)
            {
                // Persist all documents to restore full session state
                // For saved files, only store metadata (no content to save disk space)
                // For unsaved/dirty files, store full content for recovery
                bool shouldPersist = !string.IsNullOrWhiteSpace(doc.FilePath) || !string.IsNullOrWhiteSpace(doc.Content) || doc.IsDirty;
                
                if (shouldPersist)
                {
                    var sessionFile = Path.Combine(_sessionDir, $"{doc.Id}.json");
                    var metadata = new PersistedDocument
                    {
                        Id = doc.Id,
                        FilePath = doc.FilePath,
                        // Only persist content for unsaved/dirty documents
                        Content = (doc.IsDirty || string.IsNullOrWhiteSpace(doc.FilePath)) ? doc.Content : null,
                        IsDirty = doc.IsDirty,
                        Encoding = doc.Encoding.WebName,
                        Eol = doc.Eol,
                        CreatedAt = doc.CreatedAt,
                        UpdatedAt = doc.UpdatedAt,
                        Order = orderIndex.TryGetValue(doc.Id, out var idx) ? idx : int.MaxValue
                    };
                    
                    var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = false });
                    await File.WriteAllTextAsync(sessionFile, json);
                    persistedCount++;
                }
            }

            Console.WriteLine($"[PersistenceService] Persisted {persistedCount} document(s) to '{_sessionDir}'.");
        }
        catch (Exception ex)
        {
            // Log but don't throw - persistence failures shouldn't block app close
            Console.WriteLine($"[PersistenceService] Failed to persist session: {ex.Message}");
        }
    }

    public async Task<IEnumerable<Document>> RestoreAsync()
    {
        var restored = new List<Document>();
        var entries = new List<(Document Doc, int Order, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)>();
        
        try
        {
            if (!Directory.Exists(_sessionDir))
            {
                return restored;
            }

            var sessionFiles = Directory.GetFiles(_sessionDir, "*.json");
            Console.WriteLine($"[PersistenceService] Found {sessionFiles.Length} session file(s) in '{_sessionDir}'.");
            
            foreach (var file in sessionFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var metadata = JsonSerializer.Deserialize<PersistedDocument>(json);
                    
                    if (metadata != null)
                    {
                        // Skip documents with empty GUID (corruption)
                        if (metadata.Id == Guid.Empty)
                        {
                            Console.WriteLine($"[PersistenceService] Skipping document with empty GUID: {file}");
                            try { File.Delete(file); } catch { /* ignore cleanup failures */ }
                            continue;
                        }
                        
                        var doc = new Document
                        {
                            Id = metadata.Id,
                            Encoding = System.Text.Encoding.GetEncoding(metadata.Encoding ?? "utf-8"),
                            Eol = metadata.Eol ?? "\n"
                        };
                        
                        var content = metadata.Content ?? string.Empty;
                        
                        // Handle different restoration scenarios:
                        // 1. Saved file (has path, no content in session) - reload from disk
                        // 2. Dirty file (has path + content) - check for external changes
                        // 3. Untitled (no path, has content) - restore as untitled
                        
                        if (!string.IsNullOrWhiteSpace(metadata.FilePath))
                        {
                            if (File.Exists(metadata.FilePath))
                            {
                                // File exists on disk
                                if (string.IsNullOrEmpty(content))
                                {
                                    // Saved file - reload from disk
                                    try
                                    {
                                        content = await File.ReadAllTextAsync(metadata.FilePath, doc.Encoding);
                                        doc.SetContentInternal(content);
                                        doc.MarkSaved(metadata.FilePath);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[PersistenceService] Failed to reload saved file {metadata.FilePath}: {ex.Message}");
                                        // Skip this document if we can't read it
                                        continue;
                                    }
                                }
                                else
                                {
                                    // Dirty file with unsaved changes - restore with path and dirty state
                                    doc.SetContentInternal(content);
                                    doc.MarkSaved(metadata.FilePath);
                                    doc.MarkDirtyInternal();
                                    Console.WriteLine($"[PersistenceService] Restored dirty file: {metadata.FilePath}");
                                }
                            }
                            else
                            {
                                // File no longer exists - restore as untitled if we have content
                                if (!string.IsNullOrEmpty(content))
                                {
                                    doc.SetContentInternal(content);
                                    doc.MarkDirtyInternal();
                                    Console.WriteLine($"[PersistenceService] Original file not found, restoring as untitled: {metadata.FilePath}");
                                }
                                else
                                {
                                    // No content and no file - skip
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            // New untitled doc
                            doc.SetContentInternal(content);
                            doc.MarkDirtyInternal();
                        }
                        
                        var order = metadata.Order ?? int.MaxValue;
                        entries.Add((doc, order, metadata.CreatedAt, metadata.UpdatedAt));
                        Console.WriteLine($"[PersistenceService] Restored doc Id={doc.Id}, Path='{doc.FilePath ?? "<untitled>"}', Dirty={doc.IsDirty}, Order={order}.");
                    }
                    
                    // Delete the session file after successful restore
                    try { File.Delete(file); } catch { /* ignore cleanup failures */ }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PersistenceService] Failed to restore {file}: {ex.Message}");
                    // Continue with other files
                }
            }

            // Sort by explicit Order first, then CreatedAt, then UpdatedAt to get stable UI order
            foreach (var e in entries.OrderBy(e => e.Order).ThenBy(e => e.CreatedAt).ThenBy(e => e.UpdatedAt))
            {
                restored.Add(e.Doc);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PersistenceService] Failed to restore session: {ex.Message}");
        }
        
        return restored;
    }

    public void DeleteSessionFile(Guid documentId)
    {
        try
        {
            var sessionFile = Path.Combine(_sessionDir, $"{documentId}.json");
            if (File.Exists(sessionFile))
            {
                File.Delete(sessionFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PersistenceService] Failed to delete session file for {documentId}: {ex.Message}");
        }
    }

    public void ClearAllSessions()
    {
        try
        {
            if (Directory.Exists(_sessionDir))
            {
                var files = Directory.GetFiles(_sessionDir, "*.json");
                foreach (var file in files)
                {
                    try { File.Delete(file); } catch { /* ignore */ }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PersistenceService] Failed to clear sessions: {ex.Message}");
        }
    }

    public void PersistEditorPreferences(bool wordWrap, bool showPreview)
    {
        try
        {
            var prefsFile = Path.Combine(_sessionDir, "editor-prefs.json");
            var prefs = new EditorPreferences
            {
                WordWrap = wordWrap,
                ShowPreview = showPreview
            };
            var json = JsonSerializer.Serialize(prefs);
            File.WriteAllText(prefsFile, json);
            Console.WriteLine($"[PersistenceService] Persisted editor preferences: WordWrap={wordWrap}, ShowPreview={showPreview} to '{prefsFile}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PersistenceService] Failed to persist editor preferences: {ex.Message}");
        }
    }

    public (bool WordWrap, bool ShowPreview) RestoreEditorPreferences()
    {
        try
        {
            var prefsFile = Path.Combine(_sessionDir, "editor-prefs.json");
            if (File.Exists(prefsFile))
            {
                var json = File.ReadAllText(prefsFile);
                var prefs = JsonSerializer.Deserialize<EditorPreferences>(json);
                if (prefs != null)
                {
                    return (prefs.WordWrap, prefs.ShowPreview);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PersistenceService] Failed to restore editor preferences: {ex.Message}");
        }
        // Return defaults
        return (WordWrap: true, ShowPreview: false);
    }

    private class EditorPreferences
    {
        public bool WordWrap { get; set; }
        public bool ShowPreview { get; set; }
    }

    private class PersistedDocument
    {
        public Guid Id { get; set; }
        public string? FilePath { get; set; }
        public string? Content { get; set; }
        public bool IsDirty { get; set; }
        public string? Encoding { get; set; }
        public string? Eol { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public int? Order { get; set; }
    }
}

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
                // Only persist if: (1) new untitled doc with content, or (2) existing file with unsaved changes
                if (doc.IsDirty || (string.IsNullOrWhiteSpace(doc.FilePath) && !string.IsNullOrWhiteSpace(doc.Content)))
                {
                    var sessionFile = Path.Combine(_sessionDir, $"{doc.Id}.json");
                    var metadata = new PersistedDocument
                    {
                        Id = doc.Id,
                        FilePath = doc.FilePath,
                        Content = doc.Content,
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
                        var doc = new Document
                        {
                            Id = metadata.Id,
                            Encoding = System.Text.Encoding.GetEncoding(metadata.Encoding ?? "utf-8"),
                            Eol = metadata.Eol ?? "\n"
                        };
                        
                        var content = metadata.Content ?? string.Empty;
                        
                        // If original file path exists and hasn't been modified externally, restore as that file
                        // Otherwise restore as untitled with dirty state
                        if (!string.IsNullOrWhiteSpace(metadata.FilePath) && File.Exists(metadata.FilePath))
                        {
                            var currentContent = await File.ReadAllTextAsync(metadata.FilePath, doc.Encoding);
                            if (currentContent == content)
                            {
                                // File unchanged - can restore safely with path
                                doc.MarkSaved(metadata.FilePath);
                                if (!string.IsNullOrEmpty(content))
                                {
                                    doc.SetContent(content);
                                    doc.MarkSaved(metadata.FilePath); // Clear dirty flag after setting content
                                }
                            }
                            else
                            {
                                // File changed externally - restore as untitled with original path in content
                                doc.SetContent(content);
                            }
                        }
                        else
                        {
                            // New untitled doc or file no longer exists
                            doc.SetContent(content);
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

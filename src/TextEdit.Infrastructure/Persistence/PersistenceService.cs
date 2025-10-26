namespace TextEdit.Infrastructure.Persistence;

using TextEdit.Core.Documents;

/// <summary>
/// Persists session state (unsaved documents). Placeholder to be fleshed out in Phase 5.
/// </summary>
public class PersistenceService
{
    private readonly string _sessionDir = Path.Combine(Path.GetTempPath(), "textedit", "session");

    public Task PersistAsync(IEnumerable<Document> documents)
    {
        Directory.CreateDirectory(_sessionDir);
        // Placeholder: write minimal marker files per document
        // Real implementation will serialize content and metadata (Phase 5)
        foreach (var (doc, idx) in documents.Select((d, i) => (d, i)))
        {
            var marker = Path.Combine(_sessionDir, $"doc_{idx}.marker");
            File.WriteAllText(marker, doc.FilePath ?? "untitled");
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Document>> RestoreAsync()
    {
        // Placeholder: returns empty set to avoid side effects before Phase 5
        IEnumerable<Document> empty = Array.Empty<Document>();
        return Task.FromResult(empty);
    }
}

namespace TextEdit.Core.Documents;

using System.Text;
using System.IO;

/// <summary>
/// Represents a text document in the editor.
/// </summary>
/// <summary>
/// Represents an in-memory text document and its persisted state.
/// </summary>
public class Document
{
    /// <summary>
    /// Unique identifier for this document instance.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Full filesystem path if the document is associated with a file; otherwise <c>null</c> for Untitled.
    /// </summary>
    public string? FilePath { get; private set; }

    /// <summary>
    /// Display name of the document (file name or "Untitled").
    /// </summary>
    public string Name => FilePath is null ? "Untitled" : Path.GetFileName(FilePath);

    /// <summary>
    /// Current UTF-16 string content of the document (normalized to <c>\n</c> line endings in-memory).
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Indicates whether the document has unsaved changes.
    /// </summary>
    public bool IsDirty { get; private set; }

    /// <summary>
    /// When <c>true</c>, content updates are blocked and the document is treated as read-only (e.g., large files).
    /// </summary>
    public bool IsReadOnly { get; private set; }
    /// <summary>
    /// Set when the backing file was changed on disk while the document is open.
    /// When true and the document also has unsaved edits, the UI should surface a conflict indicator.
    /// </summary>
    public bool HasExternalModification { get; private set; }
    /// <summary>
    /// Timestamp when this document instance was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp of the last update to content or state.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Encoding used when reading or writing the file. Defaults to UTF-8 without BOM.
    /// </summary>
    public Encoding Encoding { get; set; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    /// <summary>
    /// Line ending used for this document ("\n" by default).
    /// </summary>
    public string Eol { get; set; } = "\n";

    /// <summary>
    /// Replace the document content (marks document dirty). Throws if <see cref="IsReadOnly"/> is true.
    /// </summary>
    /// <param name="content">New content (expected with <c>\n</c> line endings).</param>
    public void SetContent(string content)
    {
        if (IsReadOnly) throw new InvalidOperationException("Cannot modify read-only document");
        Content = content;
        IsDirty = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Internal helper to update content without read-only checks (used by services during load).
    /// </summary>
    /// <param name="content">New content.</param>
    public void SetContentInternal(string content)
    {
        // Method for DocumentService/PersistenceService to load content without read-only check
        Content = content;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Mark the document as dirty without modifying content (e.g., during session restoration).
    /// </summary>
    public void MarkDirtyInternal()
    {
        // Method to manually mark document as dirty (for restoration)
        IsDirty = true;
    }

    /// <summary>
    /// Set or clear the read-only flag for this document.
    /// </summary>
    /// <param name="readOnly">When true, disables edits to content.</param>
    public void MarkReadOnly(bool readOnly = true)
    {
        IsReadOnly = readOnly;
    }

    /// <summary>
    /// Mark the document as saved and optionally update its file path.
    /// </summary>
    /// <param name="path">Optional path to associate with the document.</param>
    public void MarkSaved(string? path = null)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            FilePath = path;
        }
        IsDirty = false;
        HasExternalModification = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Set the external modification flag (used by file watcher) and update timestamp.
    /// </summary>
    /// <param name="value">Flag value to apply.</param>
    public void MarkExternalModification(bool value = true)
    {
        HasExternalModification = value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

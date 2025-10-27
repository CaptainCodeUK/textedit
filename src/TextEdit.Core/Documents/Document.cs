namespace TextEdit.Core.Documents;

using System.Text;
using System.IO;

/// <summary>
/// Represents a text document in the editor.
/// </summary>
public class Document
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string? FilePath { get; private set; }
    public string Name => FilePath is null ? "Untitled" : Path.GetFileName(FilePath);
    public string Content { get; private set; } = string.Empty;
    public bool IsDirty { get; private set; }
    public bool IsReadOnly { get; private set; }
    /// <summary>
    /// Set when the backing file was changed on disk while the document is open.
    /// When true and the document also has unsaved edits, the UI should surface a conflict indicator.
    /// </summary>
    public bool HasExternalModification { get; private set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public Encoding Encoding { get; set; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    /// <summary>
    /// Line ending used for this document ("\n" by default).
    /// </summary>
    public string Eol { get; set; } = "\n";

    public void SetContent(string content)
    {
        if (IsReadOnly) throw new InvalidOperationException("Cannot modify read-only document");
        Content = content;
        IsDirty = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetContentInternal(string content)
    {
        // Method for DocumentService/PersistenceService to load content without read-only check
        Content = content;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkDirtyInternal()
    {
        // Method to manually mark document as dirty (for restoration)
        IsDirty = true;
    }

    public void MarkReadOnly(bool readOnly = true)
    {
        IsReadOnly = readOnly;
    }

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

    public void MarkExternalModification(bool value = true)
    {
        HasExternalModification = value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

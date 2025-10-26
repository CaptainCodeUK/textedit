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
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public Encoding Encoding { get; set; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    /// <summary>
    /// Line ending used for this document ("\n" by default).
    /// </summary>
    public string Eol { get; set; } = "\n";

    public void SetContent(string content)
    {
        Content = content;
        IsDirty = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkSaved(string? path = null)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            FilePath = path;
        }
        IsDirty = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

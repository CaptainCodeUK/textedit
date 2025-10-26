namespace TextEdit.Core.Documents;

/// <summary>
/// Represents a UI tab associated with a document.
/// </summary>
public class Tab
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public bool IsActive { get; set; }
    public string Title { get; set; } = "Untitled";
}

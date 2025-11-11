namespace TextEdit.Core.Searching;

/// <summary>
/// Describes a replace operation consisting of a find query and the replacement text.
/// </summary>
public sealed class ReplaceOperation
{
    public ReplaceOperation(FindQuery query, string replacement)
    {
        Query = query ?? new FindQuery(string.Empty, false, false);
        Replacement = replacement ?? string.Empty;
    }

    /// <summary>
    /// The search query describing what to find.
    /// </summary>
    public FindQuery Query { get; }

    /// <summary>
    /// The text that will replace each match.
    /// </summary>
    public string Replacement { get; }
}

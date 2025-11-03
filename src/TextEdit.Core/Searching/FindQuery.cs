namespace TextEdit.Core.Searching
{
    /// <summary>
    /// Parameters for a text search operation.
    /// </summary>
    public sealed class FindQuery
    {
        public string SearchTerm { get; }
        public bool MatchCase { get; }
        public bool WholeWord { get; }

        public FindQuery(string searchTerm, bool matchCase, bool wholeWord)
        {
            SearchTerm = searchTerm ?? string.Empty;
            MatchCase = matchCase;
            WholeWord = wholeWord;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;

namespace TextEdit.Core.Searching
{
    /// <summary>
    /// Provides text search across document content with options for case sensitivity and whole-word matching.
    /// </summary>
    public sealed class FindService
    {
        public FindResult FindAll(string text, FindQuery query)
        {
            text ??= string.Empty;
            if (string.IsNullOrEmpty(query?.SearchTerm))
                return new FindResult(query ?? new FindQuery(string.Empty, false, false), Array.Empty<FindMatch>());

            var matches = new List<FindMatch>();
            var comparison = query.MatchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

            int index = 0;
            while (index <= text.Length - query.SearchTerm.Length)
            {
                int found = text.IndexOf(query.SearchTerm, index, comparison);
                if (found < 0)
                    break;

                if (!query.WholeWord || IsWholeWord(text, found, query.SearchTerm.Length))
                {
                    matches.Add(new FindMatch(found, query.SearchTerm.Length));
                }
                index = found + Math.Max(1, query.SearchTerm.Length);
            }

            return new FindResult(query, matches);
        }

        public int FindNextIndex(FindResult result, int currentIndex)
        {
            var count = result?.Matches?.Count ?? 0;
            if (count == 0) return -1;
            if (currentIndex < 0) return 0;
            return (currentIndex + 1) % count;
        }

        public int FindPreviousIndex(FindResult result, int currentIndex)
        {
            var count = result?.Matches?.Count ?? 0;
            if (count == 0) return -1;
            if (currentIndex < 0) return count - 1;
            return (currentIndex - 1 + count) % count;
        }

        private static bool IsWholeWord(string text, int start, int length)
        {
            bool leftOk = start == 0 || !char.IsLetterOrDigit(text[start - 1]);
            int end = start + length;
            bool rightOk = end >= text.Length || !char.IsLetterOrDigit(text[end]);
            return leftOk && rightOk;
        }
    }
}

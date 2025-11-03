using System.Collections.Generic;

namespace TextEdit.Core.Searching
{
    public sealed class FindMatch
    {
        public int Start { get; }
        public int Length { get; }

        public FindMatch(int start, int length)
        {
            Start = start;
            Length = length;
        }
    }

    public sealed class FindResult
    {
        public FindQuery Query { get; }
        public IReadOnlyList<FindMatch> Matches { get; }

        public FindResult(FindQuery query, IReadOnlyList<FindMatch> matches)
        {
            Query = query;
            Matches = matches;
        }
    }
}

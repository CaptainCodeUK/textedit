using System;
using System.Text;

namespace TextEdit.Core.Searching
{
    /// <summary>
    /// Provides text replacement operations built on top of <see cref="FindService"/>.
    /// Pure functions returning updated content and counts/indices.
    /// </summary>
    public sealed class ReplaceService
    {
        private readonly FindService _find;

        public ReplaceService(FindService find)
        {
            _find = find;
        }

        /// <summary>
        /// Replaces all matches in the given text according to the operation.
        /// </summary>
        /// <param name="text">Source text.</param>
        /// <param name="op">Replace operation.</param>
        /// <returns>Tuple of (newText, replacementsMade).</returns>
        public (string newText, int count) ReplaceAll(string text, ReplaceOperation op)
        {
            text ??= string.Empty;
            if (string.IsNullOrEmpty(op?.Query?.SearchTerm)) return (text, 0);

            var result = _find.FindAll(text, op!.Query);
            if (result.Matches.Count == 0) return (text, 0);

            var sb = new StringBuilder(text.Length);
            int lastIndex = 0;
            int replaced = 0;
            foreach (var m in result.Matches)
            {
                // Append text up to match
                if (m.Start > lastIndex)
                {
                    sb.Append(text, lastIndex, m.Start - lastIndex);
                }
                // Append replacement
                sb.Append(op.Replacement);
                replaced++;
                lastIndex = m.Start + m.Length;
            }
            // Append trailing remainder
            if (lastIndex < text.Length)
            {
                sb.Append(text, lastIndex, text.Length - lastIndex);
            }

            return (sb.ToString(), replaced);
        }

        /// <summary>
        /// Replaces the next match at or after the specified caret position. Wraps to the first match when needed.
        /// </summary>
        /// <param name="text">Source text.</param>
        /// <param name="op">Replace operation.</param>
        /// <param name="caretPosition">Caret index where search should start.</param>
        /// <returns>(newText, replacedIndex, replacedLength). When no match, returns (original text, -1, 0).</returns>
        public (string newText, int replacedIndex, int replacedLength) ReplaceNextAtOrAfter(string text, ReplaceOperation op, int caretPosition)
        {
            text ??= string.Empty;
            if (string.IsNullOrEmpty(op?.Query?.SearchTerm)) return (text, -1, 0);

            var result = _find.FindAll(text, op!.Query);
            if (result.Matches.Count == 0) return (text, -1, 0);

            // Find the first match whose start is >= caret; else wrap to first
            int targetIdx = -1;
            for (int i = 0; i < result.Matches.Count; i++)
            {
                if (result.Matches[i].Start >= Math.Max(0, caretPosition))
                {
                    targetIdx = i;
                    break;
                }
            }
            if (targetIdx == -1) targetIdx = 0; // wrap

            var match = result.Matches[targetIdx];
            var sb = new StringBuilder(text.Length - match.Length + op.Replacement.Length);
            sb.Append(text, 0, match.Start);
            sb.Append(op.Replacement);
            int after = match.Start + match.Length;
            if (after < text.Length) sb.Append(text, after, text.Length - after);

            return (sb.ToString(), match.Start, match.Length);
        }
    }
}

using TextEdit.Core.Searching;
using Xunit;

namespace TextEdit.Core.Tests
{
    public class ReplaceServiceTests
    {
        private readonly FindService _find = new();
        private readonly ReplaceService _replace;

        public ReplaceServiceTests()
        {
            _replace = new ReplaceService(_find);
        }

        [Fact]
        public void ReplaceAll_Should_Replace_All_Instances_CaseInsensitive_ByDefault()
        {
            var text = "Foo foo FOO bar";
            var op = new ReplaceOperation(new FindQuery("foo", matchCase: false, wholeWord: false), "baz");

            var (newText, count) = _replace.ReplaceAll(text, op);

            Assert.Equal("baz baz baz bar", newText);
            Assert.Equal(3, count);
        }

        [Fact]
        public void ReplaceNextAtOrAfter_Should_Replace_Next_From_Caret_And_Wrap()
        {
            var text = "one two one two"; // positions: one(0), two(4), one(8), two(12)
            var op = new ReplaceOperation(new FindQuery("one", matchCase: false, wholeWord: true), "ONE");

            // Caret after first 'one' (at index 4), should replace the second 'one' at index 8
            var (t1, idx, len) = _replace.ReplaceNextAtOrAfter(text, op, caretPosition: 4);
            Assert.Equal(0 + 8, idx);
            Assert.Equal(3, len);
            Assert.Equal("one two ONE two", t1);

            // If caret beyond last match, should wrap to first match at index 0
            var (t2, idx2, _) = _replace.ReplaceNextAtOrAfter(text, op, caretPosition: 100);
            Assert.Equal(0, idx2);
            Assert.Equal("ONE two one two", t2);
        }
    }
}

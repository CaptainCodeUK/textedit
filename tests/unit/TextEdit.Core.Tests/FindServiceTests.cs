using System;
using System.Linq;
using TextEdit.Core.Searching;
using Xunit;

namespace TextEdit.Core.Tests
{
    public class FindServiceTests
    {
        private readonly FindService _svc = new();

        [Fact]
        public void FindAll_Should_Find_CaseInsensitive_ByDefault()
        {
            var text = "Test test TEST testing";
            var query = new FindQuery("test", matchCase: false, wholeWord: false);

            var result = _svc.FindAll(text, query);

            Assert.Equal(4, result.Matches.Count);
            Assert.All(result.Matches, m => 
            {
                var matchText = text.Substring(m.Start, m.Length);
                Assert.Contains("test", matchText, StringComparison.OrdinalIgnoreCase);
            });
        }

        [Fact]
        public void FindAll_Should_Respect_CaseSensitive()
        {
            var text = "Test test TEST";
            var query = new FindQuery("test", matchCase: true, wholeWord: false);

            var result = _svc.FindAll(text, query);

            Assert.Equal(1, result.Matches.Count);
            Assert.Equal("test", text.Substring(result.Matches[0].Start, result.Matches[0].Length));
        }

        [Fact]
        public void FindAll_Should_Respect_WholeWord()
        {
            var text = "test testing contest test";
            var query = new FindQuery("test", matchCase: false, wholeWord: true);

            var result = _svc.FindAll(text, query);

            Assert.Equal(2, result.Matches.Count);
            Assert.Equal("test", text.Substring(result.Matches[0].Start, result.Matches[0].Length));
            Assert.Equal("test", text.Substring(result.Matches[1].Start, result.Matches[1].Length));
        }

        [Fact]
        public void Navigation_Should_Wrap_Next_And_Previous()
        {
            var text = "alpha test beta test gamma";
            var query = new FindQuery("test", matchCase: false, wholeWord: true);
            var result = _svc.FindAll(text, query);

            Assert.Equal(2, result.Matches.Count);

            int idx = -1;
            idx = _svc.FindNextIndex(result, idx); // first
            Assert.Equal(0, idx);
            idx = _svc.FindNextIndex(result, idx);
            Assert.Equal(1, idx);
            idx = _svc.FindNextIndex(result, idx);
            Assert.Equal(0, idx); // wrapped

            idx = _svc.FindPreviousIndex(result, idx);
            Assert.Equal(1, idx); // wrapped backwards
        }

        [Fact]
        public void Empty_SearchTerm_Should_Return_Zero_Matches()
        {
            var text = "anything";
            var query = new FindQuery(string.Empty, matchCase: false, wholeWord: false);

            var result = _svc.FindAll(text, query);

            Assert.Equal(0, result.Matches.Count);
            Assert.Equal(-1, _svc.FindNextIndex(result, -1));
        }
    }
}

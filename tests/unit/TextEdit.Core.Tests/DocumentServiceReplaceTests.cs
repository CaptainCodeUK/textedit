using Moq;
using TextEdit.Core.Abstractions;
using TextEdit.Core.Documents;
using TextEdit.Core.Editing;
using TextEdit.Core.Searching;
using Xunit;

namespace TextEdit.Core.Tests
{
    public class DocumentServiceReplaceTests
    {
        [Fact]
        public void ReplaceAll_Performs_Single_Atomic_Undo_Step()
        {
            // Arrange
            var fs = new Mock<IFileSystem>(MockBehavior.Strict);
            var undo = new Mock<IUndoRedoService>(MockBehavior.Strict);
            var replaceSvc = new ReplaceService(new FindService());
            var svc = new DocumentService(fs.Object, undo.Object, logger: null, replace: replaceSvc);

            var doc = new Document();
            doc.SetContent("foo bar foo");

            // Expect one push with final content only
            undo.Setup(u => u.Push(doc, "baz bar baz"));

            // Act
            var count = svc.ReplaceAll(doc, new ReplaceOperation(new FindQuery("foo", false, true), "baz"));

            // Assert
            Assert.Equal(2, count);
            Assert.Equal("baz bar baz", doc.Content);
            undo.Verify(u => u.Push(doc, "baz bar baz"), Times.Once);
        }

        [Fact]
        public void ReplaceNext_Updates_Content_And_Returns_New_Caret()
        {
            var fs = new Mock<IFileSystem>(MockBehavior.Strict);
            var undo = new Mock<IUndoRedoService>(MockBehavior.Strict);
            var replaceSvc = new ReplaceService(new FindService());
            var svc = new DocumentService(fs.Object, undo.Object, logger: null, replace: replaceSvc);

            var doc = new Document();
            doc.SetContent("alpha beta alpha"); // 'alpha' at 0 and 12

            undo.Setup(u => u.Push(doc, "alpha beta ALPHA"));

            var op = new ReplaceOperation(new FindQuery("alpha", false, true), "ALPHA");

            // caret after first alpha (index 5) should replace second one at 12
            var replaced = svc.ReplaceNext(doc, op, caretPosition: 5, out var newCaret);

            Assert.True(replaced);
            Assert.Equal("alpha beta ALPHA", doc.Content);
            // Second 'alpha' starts at index 11 ("alpha"=0..4, space=5, "beta"=6..9, space=10, 'a'=11)
            Assert.Equal(11 + "ALPHA".Length, newCaret);
            undo.Verify(u => u.Push(doc, "alpha beta ALPHA"), Times.Once);
        }

        [Fact]
        public void Multiple_ReplaceNext_Operations_Undo_Correctly()
        {
            // Real undo/redo service to test actual undo behavior
            var fs = new Mock<IFileSystem>(MockBehavior.Strict);
            var undo = new UndoRedoService();
            var replaceSvc = new ReplaceService(new FindService());
            var svc = new DocumentService(fs.Object, undo, logger: null, replace: replaceSvc);

            var doc = new Document();
            doc.SetContent("hello world");
            undo.Attach(doc, doc.Content);

            var op1 = new ReplaceOperation(new FindQuery("hello", false, false), "goodbye");
            var op2 = new ReplaceOperation(new FindQuery("world", false, false), "universe");

            // First replace: hello → goodbye
            svc.ReplaceNext(doc, op1, 0, out _);
            Assert.Equal("goodbye world", doc.Content);

            // Second replace: world → universe
            svc.ReplaceNext(doc, op2, 0, out _);
            Assert.Equal("goodbye universe", doc.Content);

            // Undo second replace
            var text1 = undo.Undo(doc.Id);
            Assert.NotNull(text1);
            Assert.Equal("goodbye world", text1);

            // Undo first replace - should get back to original
            var text2 = undo.Undo(doc.Id);
            Assert.NotNull(text2);
            Assert.Equal("hello world", text2);

            // Should not be able to undo further (only 1 item left in stack - the baseline)
            Assert.False(undo.CanUndo(doc.Id));
        }
    }
}

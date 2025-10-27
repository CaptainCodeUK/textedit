using FluentAssertions;
using TextEdit.Core.Documents;
using TextEdit.Core.Editing;

namespace TextEdit.Core.Tests;

public class UndoRedoServiceTests
{
    [Fact]
    public void Attach_InitializesStacksForDocument()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc = new Document();

        // Act
        service.Attach(doc);

        // Assert
        service.CanUndo(doc.Id).Should().BeFalse();
        service.CanRedo(doc.Id).Should().BeFalse();
    }

    [Fact]
    public void Attach_WithInitialContent_PushesContent()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc = new Document();

        // Act
        service.Attach(doc, "Initial content");

        // Assert
        service.CanUndo(doc.Id).Should().BeFalse(); // Need at least 2 items to undo
        service.CanRedo(doc.Id).Should().BeFalse();
    }

    [Fact]
    public void Push_AddsContentToUndoStack()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc = new Document();
        service.Attach(doc, "Initial");

        // Act
        service.Push(doc, "First edit");

        // Assert
        service.CanUndo(doc.Id).Should().BeTrue();
        service.CanRedo(doc.Id).Should().BeFalse();
    }

    [Fact]
    public void Push_WithoutAttach_AutomaticallyAttaches()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc = new Document();

        // Act
        service.Push(doc, "First content");
        service.Push(doc, "Second content");

        // Assert
        service.CanUndo(doc.Id).Should().BeTrue(); // Now we have 2 items
        service.CanRedo(doc.Id).Should().BeFalse();
    }

    [Fact]
    public void Push_ClearsRedoStack()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc = new Document();
        service.Attach(doc, "Initial");
        service.Push(doc, "First");
        service.Undo(doc.Id);

        // Act
        service.Push(doc, "New branch");

        // Assert
        service.CanRedo(doc.Id).Should().BeFalse();
    }

    [Fact]
    public void Undo_ReturnsEmptyWhenNothingToUndo()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc = new Document();
        service.Attach(doc, "Only one");

        // Act
        var result = service.Undo(doc.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Undo_ReturnsPreviousContent()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc = new Document();
        service.Attach(doc, "Initial");
        service.Push(doc, "First edit");

        // Act
        var result = service.Undo(doc.Id);

        // Assert
        result.Should().Be("Initial");
        service.CanUndo(doc.Id).Should().BeFalse();
        service.CanRedo(doc.Id).Should().BeTrue();
    }

    [Fact]
    public void Undo_MultipleEdits_ReturnsCorrectHistory()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc = new Document();
        service.Attach(doc, "V1");
        service.Push(doc, "V2");
        service.Push(doc, "V3");

        // Act & Assert
        service.Undo(doc.Id).Should().Be("V2");
        service.Undo(doc.Id).Should().Be("V1");
        service.Undo(doc.Id).Should().BeNull();
    }

    [Fact]
    public void Redo_ReturnsNullWhenNothingToRedo()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc = new Document();
        service.Attach(doc, "Content");

        // Act
        var result = service.Redo(doc.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Redo_ReturnsNextContent()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc = new Document();
        service.Attach(doc, "V1");
        service.Push(doc, "V2");
        service.Undo(doc.Id);

        // Act
        var result = service.Redo(doc.Id);

        // Assert
        result.Should().Be("V2");
        service.CanRedo(doc.Id).Should().BeFalse();
        service.CanUndo(doc.Id).Should().BeTrue();
    }

    [Fact]
    public void Redo_MultipleUndos_ReturnsCorrectHistory()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc = new Document();
        service.Attach(doc, "V1");
        service.Push(doc, "V2");
        service.Push(doc, "V3");
        service.Undo(doc.Id);
        service.Undo(doc.Id);

        // Act & Assert
        service.Redo(doc.Id).Should().Be("V2");
        service.Redo(doc.Id).Should().Be("V3");
        service.Redo(doc.Id).Should().BeNull();
    }

    [Fact]
    public void CanUndo_ReturnsFalseForNonexistentDocument()
    {
        // Arrange
        var service = new UndoRedoService();

        // Act & Assert
        service.CanUndo(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void CanRedo_ReturnsFalseForNonexistentDocument()
    {
        // Arrange
        var service = new UndoRedoService();

        // Act & Assert
        service.CanRedo(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllHistory()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc = new Document();
        service.Attach(doc, "V1");
        service.Push(doc, "V2");
        service.Push(doc, "V3");
        service.Undo(doc.Id);

        // Act
        service.Clear(doc.Id);

        // Assert
        service.CanUndo(doc.Id).Should().BeFalse();
        service.CanRedo(doc.Id).Should().BeFalse();
    }

    [Fact]
    public void Clear_NonexistentDocument_DoesNotThrow()
    {
        // Arrange
        var service = new UndoRedoService();

        // Act
        Action act = () => service.Clear(Guid.NewGuid());

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void UndoRedo_ComplexScenario()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc = new Document();
        service.Attach(doc, "Start");
        service.Push(doc, "Step1");
        service.Push(doc, "Step2");
        service.Push(doc, "Step3");

        // Act & Assert - Undo twice
        service.Undo(doc.Id).Should().Be("Step2");
        service.Undo(doc.Id).Should().Be("Step1");

        // Redo once
        service.Redo(doc.Id).Should().Be("Step2");

        // New edit clears redo
        service.Push(doc, "NewBranch");
        service.CanRedo(doc.Id).Should().BeFalse();

        // Undo should go back to Step2 (not Step1, because NewBranch replaced Step3)
        service.Undo(doc.Id).Should().Be("Step2");
    }

    [Fact]
    public void Attach_CalledTwice_ResetsStacks()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc = new Document();
        service.Attach(doc, "V1");
        service.Push(doc, "V2");

        // Act
        service.Attach(doc, "Fresh");

        // Assert
        service.CanUndo(doc.Id).Should().BeFalse();
        service.CanRedo(doc.Id).Should().BeFalse();
    }

    [Fact]
    public void MultipleDocuments_IndependentHistory()
    {
        // Arrange
        var service = new UndoRedoService();
        var doc1 = new Document();
        var doc2 = new Document();
        
        service.Attach(doc1, "Doc1-V1");
        service.Push(doc1, "Doc1-V2");
        
        service.Attach(doc2, "Doc2-V1");
        service.Push(doc2, "Doc2-V2");

        // Act & Assert
        service.Undo(doc1.Id).Should().Be("Doc1-V1");
        service.CanUndo(doc2.Id).Should().BeTrue();
        service.Undo(doc2.Id).Should().Be("Doc2-V1");
    }
}

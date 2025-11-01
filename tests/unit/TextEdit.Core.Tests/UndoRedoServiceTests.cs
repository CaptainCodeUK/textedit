using Xunit;
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
    Assert.False(service.CanUndo(doc.Id));
    Assert.False(service.CanRedo(doc.Id));
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
    Assert.False(service.CanUndo(doc.Id)); // Need at least 2 items to undo
    Assert.False(service.CanRedo(doc.Id));
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
    Assert.True(service.CanUndo(doc.Id));
    Assert.False(service.CanRedo(doc.Id));
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
    Assert.True(service.CanUndo(doc.Id)); // Now we have 2 items
    Assert.False(service.CanRedo(doc.Id));
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
    Assert.False(service.CanRedo(doc.Id));
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
    Assert.Null(result);
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
    Assert.Equal("Initial", result);
    Assert.False(service.CanUndo(doc.Id));
    Assert.True(service.CanRedo(doc.Id));
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
    Assert.Equal("V2", service.Undo(doc.Id));
    Assert.Equal("V1", service.Undo(doc.Id));
    Assert.Null(service.Undo(doc.Id));
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
    Assert.Null(result);
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
    Assert.Equal("V2", result);
    Assert.False(service.CanRedo(doc.Id));
    Assert.True(service.CanUndo(doc.Id));
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
    Assert.Equal("V2", service.Redo(doc.Id));
    Assert.Equal("V3", service.Redo(doc.Id));
    Assert.Null(service.Redo(doc.Id));
    }

    [Fact]
    public void CanUndo_ReturnsFalseForNonexistentDocument()
    {
        // Arrange
        var service = new UndoRedoService();

        // Act & Assert
    Assert.False(service.CanUndo(Guid.NewGuid()));
    }

    [Fact]
    public void CanRedo_ReturnsFalseForNonexistentDocument()
    {
        // Arrange
        var service = new UndoRedoService();

        // Act & Assert
    Assert.False(service.CanRedo(Guid.NewGuid()));
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
    Assert.False(service.CanUndo(doc.Id));
    Assert.False(service.CanRedo(doc.Id));
    }

    [Fact]
    public void Clear_NonexistentDocument_DoesNotThrow()
    {
        // Arrange
        var service = new UndoRedoService();

        // Act
    var ex = Record.Exception(() => service.Clear(Guid.NewGuid()));
    Assert.Null(ex);
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
    Assert.Equal("Step2", service.Undo(doc.Id));
    Assert.Equal("Step1", service.Undo(doc.Id));

        // Redo once
    Assert.Equal("Step2", service.Redo(doc.Id));

        // New edit clears redo
        service.Push(doc, "NewBranch");
    Assert.False(service.CanRedo(doc.Id));

        // Undo should go back to Step2 (not Step1, because NewBranch replaced Step3)
    Assert.Equal("Step2", service.Undo(doc.Id));
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
    Assert.False(service.CanUndo(doc.Id));
    Assert.False(service.CanRedo(doc.Id));
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
    Assert.Equal("Doc1-V1", service.Undo(doc1.Id));
    Assert.True(service.CanUndo(doc2.Id));
    Assert.Equal("Doc2-V1", service.Undo(doc2.Id));
    }
}

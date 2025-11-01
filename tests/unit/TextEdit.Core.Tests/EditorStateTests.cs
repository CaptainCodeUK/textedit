using Xunit;
using TextEdit.Core.Editing;

namespace TextEdit.Core.Tests;

public class EditorStateTests
{
    [Fact]
    public void EditorState_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var state = new EditorState();

        // Assert
    Assert.True(state.WordWrap);
    Assert.False(state.ShowPreview);
    Assert.Equal(1, state.CaretLine);
    Assert.Equal(1, state.CaretColumn);
    Assert.Equal(0, state.CharacterCount);
    Assert.NotNull(state.CaretIndexByDocument);
    Assert.Empty(state.CaretIndexByDocument);
    }

    [Fact]
    public void EditorState_Properties_CanBeSet()
    {
        // Arrange
        var state = new EditorState();

        // Act
        state.WordWrap = false;
        state.ShowPreview = true;
        state.CaretLine = 42;
        state.CaretColumn = 15;
        state.CharacterCount = 1234;

        // Assert
    Assert.False(state.WordWrap);
    Assert.True(state.ShowPreview);
    Assert.Equal(42, state.CaretLine);
    Assert.Equal(15, state.CaretColumn);
    Assert.Equal(1234, state.CharacterCount);
    }

    [Fact]
    public void CaretIndexByDocument_CanStoreMultipleDocuments()
    {
        // Arrange
        var state = new EditorState();
        var doc1Id = Guid.NewGuid();
        var doc2Id = Guid.NewGuid();

        // Act
        state.CaretIndexByDocument[doc1Id] = 100;
        state.CaretIndexByDocument[doc2Id] = 250;

        // Assert
    Assert.Equal(2, state.CaretIndexByDocument.Count);
    Assert.Equal(100, state.CaretIndexByDocument[doc1Id]);
    Assert.Equal(250, state.CaretIndexByDocument[doc2Id]);
    }

    [Fact]
    public void NotifyChanged_RaisesChangedEvent()
    {
        // Arrange
        var state = new EditorState();
        var eventRaised = false;
        state.Changed += () => eventRaised = true;

        // Act
        state.NotifyChanged();

        // Assert
    Assert.True(eventRaised);
    }

    [Fact]
    public void NotifyChanged_WithNoSubscribers_DoesNotThrow()
    {
        // Arrange
        var state = new EditorState();

        // Act
    var ex = Record.Exception(() => state.NotifyChanged());
    Assert.Null(ex);
    }

    [Fact]
    public void NotifyChanged_WithMultipleSubscribers_InvokesAll()
    {
        // Arrange
        var state = new EditorState();
        var callCount = 0;
        state.Changed += () => callCount++;
        state.Changed += () => callCount++;
        state.Changed += () => callCount++;

        // Act
        state.NotifyChanged();

        // Assert
    Assert.Equal(3, callCount);
    }
}

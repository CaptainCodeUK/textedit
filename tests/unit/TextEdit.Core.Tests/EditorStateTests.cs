using FluentAssertions;
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
        state.WordWrap.Should().BeTrue();
        state.ShowPreview.Should().BeFalse();
        state.CaretLine.Should().Be(1);
        state.CaretColumn.Should().Be(1);
        state.CharacterCount.Should().Be(0);
        state.CaretIndexByDocument.Should().NotBeNull().And.BeEmpty();
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
        state.WordWrap.Should().BeFalse();
        state.ShowPreview.Should().BeTrue();
        state.CaretLine.Should().Be(42);
        state.CaretColumn.Should().Be(15);
        state.CharacterCount.Should().Be(1234);
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
        state.CaretIndexByDocument.Should().HaveCount(2);
        state.CaretIndexByDocument[doc1Id].Should().Be(100);
        state.CaretIndexByDocument[doc2Id].Should().Be(250);
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
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void NotifyChanged_WithNoSubscribers_DoesNotThrow()
    {
        // Arrange
        var state = new EditorState();

        // Act
        var act = () => state.NotifyChanged();

        // Assert
        act.Should().NotThrow();
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
        callCount.Should().Be(3);
    }
}

using FluentAssertions;
using TextEdit.Core.Documents;
using TextEdit.Core.Editing;
using TextEdit.Core.Abstractions;
using NSubstitute;
using System.Text;

namespace TextEdit.Core.Tests;

public class DocumentServiceTests
{
    private readonly IFileSystem _fileSystem;
    private readonly IUndoRedoService _undoRedo;
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        _fileSystem = Substitute.For<IFileSystem>();
        _undoRedo = Substitute.For<IUndoRedoService>();
        _service = new DocumentService(_fileSystem, _undoRedo);
    }

    [Fact]
    public async Task SaveAsync_WhenFileDoesNotExist_CreatesFile()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("New content");
        doc.MarkSaved("/test/newfile.txt");

        _fileSystem.FileExists(doc.FilePath!).Returns(false);
        _fileSystem.WriteAllTextAsync(doc.FilePath!, "New content", Arg.Any<Encoding>())
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveAsync(doc);

        // Assert
        doc.IsDirty.Should().BeFalse();
        await _fileSystem.DidNotReceive().ReadAllTextAsync(Arg.Any<string>(), Arg.Any<Encoding>());
        await _fileSystem.Received(1).WriteAllTextAsync(doc.FilePath!, "New content", Arg.Any<Encoding>());
    }

    [Fact]
    public async Task SaveAsync_WhenDiskContentMatches_SavesSuccessfully()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Current content");
        doc.MarkSaved("/test/file.txt");
        doc.SetContent("Modified content");

        _fileSystem.FileExists(doc.FilePath!).Returns(true);
        _fileSystem.ReadAllTextAsync(doc.FilePath!, Arg.Any<Encoding>()).Returns(Task.FromResult("Current content"));
        _fileSystem.WriteAllTextAsync(doc.FilePath!, "Modified content", Arg.Any<Encoding>())
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveAsync(doc);

        // Assert
        doc.IsDirty.Should().BeFalse();
        await _fileSystem.Received(1).WriteAllTextAsync(doc.FilePath!, "Modified content", Arg.Any<Encoding>());
    }

    [Fact]
    public async Task SaveAsync_WhenDiskContentDiffers_StillSavesButDetectsConflict()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Original content");
        doc.MarkSaved("/test/file.txt");
        doc.SetContent("My changes");

        _fileSystem.FileExists(doc.FilePath!).Returns(true);
        _fileSystem.ReadAllTextAsync(doc.FilePath!, Arg.Any<Encoding>()).Returns(Task.FromResult("Someone else's changes"));
        _fileSystem.WriteAllTextAsync(doc.FilePath!, "My changes", Arg.Any<Encoding>())
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveAsync(doc);

        // Assert - conflict is logged but save proceeds
        doc.IsDirty.Should().BeFalse();
        await _fileSystem.Received(1).ReadAllTextAsync(doc.FilePath!, Arg.Any<Encoding>());
        await _fileSystem.Received(1).WriteAllTextAsync(doc.FilePath!, "My changes", Arg.Any<Encoding>());
    }

    [Fact]
    public async Task SaveAsync_WhenUnauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Content");
        doc.MarkSaved("/test/readonly.txt");
        doc.SetContent("Modified"); // Make it dirty again

        _fileSystem.FileExists(doc.FilePath!).Returns(true);
        _fileSystem.ReadAllTextAsync(doc.FilePath!, Arg.Any<Encoding>()).Returns(Task.FromResult("Content"));
        _fileSystem.WriteAllTextAsync(doc.FilePath!, Arg.Any<string>(), Arg.Any<Encoding>())
            .Returns<Task>(x => throw new UnauthorizedAccessException("Permission denied"));

        // Act
        var act = async () => await _service.SaveAsync(doc);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Permission denied");
    }

    [Fact]
    public async Task SaveAsync_WhenUntitledDocument_ThrowsInvalidOperationException()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Content");

        // Act
        var act = async () => await _service.SaveAsync(doc);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

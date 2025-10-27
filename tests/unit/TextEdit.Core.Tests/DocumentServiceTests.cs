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

    [Fact]
    public void NewDocument_CreatesDocumentWithDefaults()
    {
        // Act
        var doc = _service.NewDocument();

        // Assert
        doc.Should().NotBeNull();
        doc.Content.Should().BeEmpty();
        doc.IsDirty.Should().BeFalse();
        doc.FilePath.Should().BeNull();
        _undoRedo.Received(1).Attach(doc, "");
    }

    [Fact]
    public async Task OpenAsync_WhenFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        _fileSystem.FileExists("/test/missing.txt").Returns(false);

        // Act
        var act = async () => await _service.OpenAsync("/test/missing.txt");

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task OpenAsync_LoadsFileSuccessfully()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var content = "File content\nLine 2";
        
        try
        {
            await File.WriteAllTextAsync(tempFile, content);
            _fileSystem.FileExists(tempFile).Returns(true);
            _fileSystem.ReadAllTextAsync(tempFile, Arg.Any<Encoding>()).Returns(Task.FromResult(content));

            // Act
            var doc = await _service.OpenAsync(tempFile);

            // Assert
            doc.Should().NotBeNull();
            doc.Content.Should().Be(content);
            doc.FilePath.Should().Be(tempFile);
            doc.IsDirty.Should().BeFalse();
            _undoRedo.Received(1).Attach(doc, content);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task OpenAsync_NormalizesLineEndings()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var windowsContent = "Line 1\r\nLine 2\r\nLine 3";
        var normalized = "Line 1\nLine 2\nLine 3";
        
        try
        {
            await File.WriteAllTextAsync(tempFile, windowsContent);
            _fileSystem.FileExists(tempFile).Returns(true);
            _fileSystem.ReadAllTextAsync(tempFile, Arg.Any<Encoding>()).Returns(Task.FromResult(windowsContent));

            // Act
            var doc = await _service.OpenAsync(tempFile);

            // Assert
            doc.Content.Should().Be(normalized);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void UpdateContent_WhenContentChanged_UpdatesDocumentAndPushesUndo()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Original");
        var newContent = "Updated";

        // Act
        _service.UpdateContent(doc, newContent);

        // Assert
        doc.Content.Should().Be(newContent);
        _undoRedo.Received(1).Push(doc, newContent);
    }

    [Fact]
    public void UpdateContent_WhenContentUnchanged_DoesNotPushUndo()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Same content");

        // Act
        _service.UpdateContent(doc, "Same content");

        // Assert
        _undoRedo.DidNotReceive().Push(Arg.Any<Document>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SaveAsync_WithSpecificPath_SavesAndUpdatesFilePath()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Content");
        var newPath = "/test/newlocation.txt";
        _fileSystem.WriteAllTextAsync(newPath, "Content", Arg.Any<Encoding>())
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveAsync(doc, newPath);

        // Assert
        doc.FilePath.Should().Be(newPath);
        doc.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_AppliesCorrectEOL()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Line 1\nLine 2\nLine 3");
        doc.Eol = "\r\n"; // Windows style
        doc.MarkSaved("/test/windows.txt");
        doc.SetContent("Line 1\nLine 2\nLine 3"); // Re-mark dirty

        string? capturedText = null;
        _fileSystem.FileExists("/test/windows.txt").Returns(false);
        _fileSystem.WriteAllTextAsync("/test/windows.txt", Arg.Do<string>(x => capturedText = x), Arg.Any<Encoding>())
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveAsync(doc);

        // Assert
        capturedText.Should().Be("Line 1\r\nLine 2\r\nLine 3");
    }

    [Fact]
    public async Task OpenAsync_LargeFile_MarksReadOnly()
    {
        // Arrange: Create a 15MB file (over the 10MB threshold)
        var tempFile = Path.GetTempFileName();
        var largeContent = new string('x', 15 * 1024 * 1024); // 15MB
        
        try
        {
            await File.WriteAllTextAsync(tempFile, largeContent);
            _fileSystem.FileExists(tempFile).Returns(true);
            _fileSystem.ReadAllTextAsync(tempFile, Arg.Any<Encoding>()).Returns(Task.FromResult(largeContent));

            // Act
            var doc = await _service.OpenAsync(tempFile);

            // Assert
            doc.Should().NotBeNull();
            doc.IsReadOnly.Should().BeTrue();
            doc.FilePath.Should().Be(tempFile);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task OpenAsync_SmallFile_IsNotReadOnly()
    {
        // Arrange: Create a small file (under the 10MB threshold)
        var tempFile = Path.GetTempFileName();
        var smallContent = "Small file content";
        
        try
        {
            await File.WriteAllTextAsync(tempFile, smallContent);
            _fileSystem.FileExists(tempFile).Returns(true);
            _fileSystem.ReadAllTextAsync(tempFile, Arg.Any<Encoding>()).Returns(Task.FromResult(smallContent));

            // Act
            var doc = await _service.OpenAsync(tempFile);

            // Assert
            doc.Should().NotBeNull();
            doc.IsReadOnly.Should().BeFalse();
            doc.Content.Should().Be(smallContent);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}


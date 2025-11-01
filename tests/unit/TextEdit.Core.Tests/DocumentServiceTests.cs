using Xunit;
using TextEdit.Core.Documents;
using TextEdit.Core.Editing;
using TextEdit.Core.Abstractions;
using Moq;
using System.Text;

namespace TextEdit.Core.Tests;

public class DocumentServiceTests
{
    private readonly Mock<IFileSystem> _fileSystem;
    private readonly Mock<IUndoRedoService> _undoRedo;
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        _fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        _undoRedo = new Mock<IUndoRedoService>(MockBehavior.Strict);
        _service = new DocumentService(_fileSystem.Object, _undoRedo.Object);
    }

    [Fact]
    public async Task SaveAsync_WhenFileDoesNotExist_CreatesFile()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("New content");
        doc.MarkSaved("/test/newfile.txt");

        _fileSystem.Setup(fs => fs.FileExists(doc.FilePath!)).Returns(false);
        _fileSystem.Setup(fs => fs.WriteAllTextAsync(doc.FilePath!, "New content", It.IsAny<Encoding>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveAsync(doc);

        // Assert
    Assert.False(doc.IsDirty);
    _fileSystem.Verify(fs => fs.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<Encoding>()), Times.Never);
    _fileSystem.Verify(fs => fs.WriteAllTextAsync(doc.FilePath!, "New content", It.IsAny<Encoding>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WhenDiskContentMatches_SavesSuccessfully()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Current content");
        doc.MarkSaved("/test/file.txt");
        doc.SetContent("Modified content");

        _fileSystem.Setup(fs => fs.FileExists(doc.FilePath!)).Returns(true);
        _fileSystem.Setup(fs => fs.ReadAllTextAsync(doc.FilePath!, It.IsAny<Encoding>())).ReturnsAsync("Current content");
        _fileSystem.Setup(fs => fs.WriteAllTextAsync(doc.FilePath!, "Modified content", It.IsAny<Encoding>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveAsync(doc);

        // Assert
    Assert.False(doc.IsDirty);
    _fileSystem.Verify(fs => fs.WriteAllTextAsync(doc.FilePath!, "Modified content", It.IsAny<Encoding>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WhenDiskContentDiffers_StillSavesButDetectsConflict()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Original content");
        doc.MarkSaved("/test/file.txt");
        doc.SetContent("My changes");

        _fileSystem.Setup(fs => fs.FileExists(doc.FilePath!)).Returns(true);
        _fileSystem.Setup(fs => fs.ReadAllTextAsync(doc.FilePath!, It.IsAny<Encoding>())).ReturnsAsync("Someone else's changes");
        _fileSystem.Setup(fs => fs.WriteAllTextAsync(doc.FilePath!, "My changes", It.IsAny<Encoding>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveAsync(doc);

        // Assert - conflict is logged but save proceeds
    Assert.False(doc.IsDirty);
    _fileSystem.Verify(fs => fs.ReadAllTextAsync(doc.FilePath!, It.IsAny<Encoding>()), Times.Once);
    _fileSystem.Verify(fs => fs.WriteAllTextAsync(doc.FilePath!, "My changes", It.IsAny<Encoding>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WhenUnauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Content");
        doc.MarkSaved("/test/readonly.txt");
        doc.SetContent("Modified"); // Make it dirty again

        _fileSystem.Setup(fs => fs.FileExists(doc.FilePath!)).Returns(true);
        _fileSystem.Setup(fs => fs.ReadAllTextAsync(doc.FilePath!, It.IsAny<Encoding>())).ReturnsAsync("Content");
        _fileSystem.Setup(fs => fs.WriteAllTextAsync(doc.FilePath!, It.IsAny<string>(), It.IsAny<Encoding>()))
            .ThrowsAsync(new UnauthorizedAccessException("Permission denied"));

        // Act
        // Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _service.SaveAsync(doc));
        Assert.Equal("Permission denied", ex.Message);
    }

    [Fact]
    public async Task SaveAsync_WhenUntitledDocument_ThrowsInvalidOperationException()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Content");

        // Act
    // Assert
    await Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.SaveAsync(doc));
    }

    [Fact]
    public void NewDocument_CreatesDocumentWithDefaults()
    {
        // Arrange
        _undoRedo.Setup(u => u.Attach(It.IsAny<Document>(), ""));

        // Act
        var doc = _service.NewDocument();

        // Assert
        Assert.NotNull(doc);
        Assert.Equal(string.Empty, doc.Content);
        Assert.False(doc.IsDirty);
        Assert.Null(doc.FilePath);
        _undoRedo.Verify(u => u.Attach(doc, ""), Times.Once);
    }

    [Fact]
    public async Task OpenAsync_WhenFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
    _fileSystem.Setup(fs => fs.FileExists("/test/missing.txt")).Returns(false);

        // Act
    // Assert
    await Assert.ThrowsAsync<FileNotFoundException>(async () => await _service.OpenAsync("/test/missing.txt"));
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
            _fileSystem.Setup(fs => fs.FileExists(tempFile)).Returns(true);
            _fileSystem.Setup(fs => fs.GetFileSize(tempFile)).Returns(content.Length);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFile, It.IsAny<Encoding>())).ReturnsAsync(content);
            _undoRedo.Setup(u => u.Attach(It.IsAny<Document>(), content));

            // Act
            var doc = await _service.OpenAsync(tempFile);

            // Assert
            Assert.NotNull(doc);
            Assert.Equal(content, doc.Content);
            Assert.Equal(tempFile, doc.FilePath);
            Assert.False(doc.IsDirty);
            _undoRedo.Verify(u => u.Attach(doc, content), Times.Once);
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
            _fileSystem.Setup(fs => fs.FileExists(tempFile)).Returns(true);
            _fileSystem.Setup(fs => fs.GetFileSize(tempFile)).Returns(windowsContent.Length);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFile, It.IsAny<Encoding>())).ReturnsAsync(windowsContent);
            _undoRedo.Setup(u => u.Attach(It.IsAny<Document>(), normalized));

            // Act
            var doc = await _service.OpenAsync(tempFile);

            // Assert
            Assert.Equal(normalized, doc.Content);
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
        _undoRedo.Setup(u => u.Push(doc, newContent));

        // Act
        _service.UpdateContent(doc, newContent);

        // Assert
        Assert.Equal(newContent, doc.Content);
        _undoRedo.Verify(u => u.Push(doc, newContent), Times.Once);
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
    _undoRedo.Verify(u => u.Push(It.IsAny<Document>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WithSpecificPath_SavesAndUpdatesFilePath()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Content");
        var newPath = "/test/newlocation.txt";
        _fileSystem.Setup(fs => fs.WriteAllTextAsync(newPath, "Content", It.IsAny<Encoding>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveAsync(doc, newPath);

        // Assert
    Assert.Equal(newPath, doc.FilePath);
    Assert.False(doc.IsDirty);
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
        _fileSystem.Setup(fs => fs.FileExists("/test/windows.txt")).Returns(false);
        _fileSystem.Setup(fs => fs.WriteAllTextAsync("/test/windows.txt", It.IsAny<string>(), It.IsAny<Encoding>()))
            .Callback<string, string, Encoding>((pathArg, textArg, enc) => capturedText = textArg)
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveAsync(doc);

        // Assert
    Assert.Equal("Line 1\r\nLine 2\r\nLine 3", capturedText);
    }

    [Fact]
    public async Task OpenAsync_LargeFile_MarksReadOnly()
    {
        // Arrange: Create a 15MB file.
        // NOTE: The large-file streaming threshold in production is 10MB
        // (see src/TextEdit.Core/Documents/DocumentService.cs -> LargeFileThreshold).
        // We intentionally use 15MB here to be safely above the threshold and avoid
        // boundary-related flakiness. This ensures the streaming/read-only path is exercised.
        var tempFile = Path.GetTempFileName();
        var largeContent = new string('x', 15 * 1024 * 1024); // 15MB (> 10MB threshold)
        
        try
        {
            await File.WriteAllTextAsync(tempFile, largeContent);
            _fileSystem.Setup(fs => fs.FileExists(tempFile)).Returns(true);
            _fileSystem.Setup(fs => fs.GetFileSize(tempFile)).Returns(15L * 1024 * 1024);
            _fileSystem.Setup(fs => fs.ReadLargeFileAsync(tempFile, It.IsAny<Encoding>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(largeContent);
            _undoRedo.Setup(u => u.Attach(It.IsAny<Document>(), largeContent));

            // Act
            var doc = await _service.OpenAsync(tempFile);

            // Assert
            Assert.NotNull(doc);
            Assert.True(doc.IsReadOnly);
            Assert.Equal(tempFile, doc.FilePath);
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
            _fileSystem.Setup(fs => fs.FileExists(tempFile)).Returns(true);
            _fileSystem.Setup(fs => fs.GetFileSize(tempFile)).Returns(smallContent.Length);
            _fileSystem.Setup(fs => fs.ReadAllTextAsync(tempFile, It.IsAny<Encoding>())).ReturnsAsync(smallContent);
            _undoRedo.Setup(u => u.Attach(It.IsAny<Document>(), smallContent));

            // Act
            var doc = await _service.OpenAsync(tempFile);

            // Assert
            Assert.NotNull(doc);
            Assert.False(doc.IsReadOnly);
            Assert.Equal(smallContent, doc.Content);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}


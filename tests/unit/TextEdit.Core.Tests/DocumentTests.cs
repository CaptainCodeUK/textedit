using Xunit;
using TextEdit.Core.Documents;
using System.Text;

public class DocumentTests
{
    [Fact]
    public void Constructor_CreatesDocumentWithDefaults()
    {
        // Act
        var doc = new Document();

        // Assert
        Assert.NotEqual(Guid.Empty, doc.Id);
        Assert.Null(doc.FilePath);
        Assert.Equal("Untitled", doc.Name);
        Assert.Equal(string.Empty, doc.Content);
        Assert.False(doc.IsDirty);
        Assert.True((DateTimeOffset.UtcNow - doc.CreatedAt) < TimeSpan.FromSeconds(2));
        Assert.True((DateTimeOffset.UtcNow - doc.UpdatedAt) < TimeSpan.FromSeconds(2));
        Assert.IsType<UTF8Encoding>(doc.Encoding);
        Assert.Equal("\n", doc.Eol);
    }

    [Fact]
    public void SetContent_UpdatesContentAndMarksDirty()
    {
        // Arrange
        var doc = new Document();
        var initialUpdatedAt = doc.UpdatedAt;

        // Act
        doc.SetContent("Hello World");

        // Assert
        Assert.Equal("Hello World", doc.Content);
        Assert.True(doc.IsDirty);
        // On some runners, clock resolution can make timestamps equal when operations happen very fast
        Assert.True(doc.UpdatedAt >= initialUpdatedAt);
    }

    [Fact]
    public void SetContent_MultipleUpdates_KeepsUpdatingTimestamp()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("First");
        var firstUpdate = doc.UpdatedAt;

        // Act
        Thread.Sleep(10); // Ensure time difference
        doc.SetContent("Second");

        // Assert
        Assert.Equal("Second", doc.Content);
        Assert.True(doc.IsDirty);
        Assert.True(doc.UpdatedAt > firstUpdate);
    }

    [Fact]
    public void MarkSaved_WithoutPath_ClearsDirtyFlag()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Some content");
        var initialUpdatedAt = doc.UpdatedAt;

        // Act
        Thread.Sleep(10); // Ensure time difference
        doc.MarkSaved();

        // Assert
        Assert.False(doc.IsDirty);
        Assert.Null(doc.FilePath);
        Assert.True(doc.UpdatedAt > initialUpdatedAt);
    }

    [Fact]
    public void MarkSaved_WithPath_SetsFilePathAndClearsDirty()
    {
        // Arrange
        var doc = new Document();
        doc.SetContent("Some content");
        var path = "/path/to/file.txt";

        // Act
        doc.MarkSaved(path);

        // Assert
        Assert.False(doc.IsDirty);
        Assert.Equal(path, doc.FilePath);
        Assert.Equal("file.txt", doc.Name);
    }

    [Theory]
    [InlineData("/home/user/documents/readme.txt", "readme.txt")]
    [InlineData("/var/log/app.log", "app.log")]
    [InlineData("simple.txt", "simple.txt")]
    public void Name_WithFilePath_ReturnsFileName(string filePath, string expectedName)
    {
        // Arrange
        var doc = new Document();

        // Act
        doc.MarkSaved(filePath);

        // Assert
        Assert.Equal(expectedName, doc.Name);
    }

    [Fact]
    public void Name_WithWindowsPath_ReturnsFileName()
    {
        // Arrange - Use actual Path.Combine for cross-platform compatibility
        var doc = new Document();
        var fileName = "test.md";
        // On Windows this will be a Windows path, on Linux it will work with / separators
        var filePath = Path.Combine(Path.GetTempPath(), "Users", "Documents", fileName);

        // Act
        doc.MarkSaved(filePath);

        // Assert - GetFileName works correctly regardless of OS
        Assert.Equal(Path.GetFileName(filePath), doc.Name);
    }


    [Fact]
    public void Encoding_CanBeChanged()
    {
        // Arrange
        var doc = new Document();
        var encoding = Encoding.UTF32;

        // Act
        doc.Encoding = encoding;

        // Assert
        Assert.Equal(encoding, doc.Encoding);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public void Eol_CanBeSet(string eol)
    {
        // Arrange
        var doc = new Document();

        // Act
        doc.Eol = eol;

        // Assert
        Assert.Equal(eol, doc.Eol);
    }

    [Fact]
    public void MarkSaved_WithEmptyPath_DoesNotUpdateFilePath()
    {
        // Arrange
        var doc = new Document();
        doc.MarkSaved("/original/path.txt");

        // Act
        doc.MarkSaved("");

        // Assert
        Assert.Equal("/original/path.txt", doc.FilePath);
        Assert.False(doc.IsDirty);
    }

    [Fact]
    public void MarkSaved_WithWhitespacePath_DoesNotUpdateFilePath()
    {
        // Arrange
        var doc = new Document();
        doc.MarkSaved("/original/path.txt");

        // Act
        doc.MarkSaved("   ");

        // Assert
        Assert.Equal("/original/path.txt", doc.FilePath);
        Assert.False(doc.IsDirty);
    }

    [Fact]
    public void SetContent_WithEmptyString_MarksDirty()
    {
        // Arrange
        var doc = new Document();
        doc.MarkSaved();

        // Act
        doc.SetContent("");

        // Assert
        Assert.True(doc.IsDirty);
        Assert.Equal(string.Empty, doc.Content);
    }

    [Fact]
    public void IsReadOnly_DefaultsToFalse()
    {
        // Arrange & Act
        var doc = new Document();

        // Assert
        Assert.False(doc.IsReadOnly);
    }

    [Fact]
    public void MarkReadOnly_SetsIsReadOnlyFlag()
    {
        // Arrange
        var doc = new Document();

        // Act
        doc.MarkReadOnly(true);

        // Assert
        Assert.True(doc.IsReadOnly);
    }

    [Fact]
    public void MarkReadOnly_CanBeCleared()
    {
        // Arrange
        var doc = new Document();
        doc.MarkReadOnly(true);

        // Act
        doc.MarkReadOnly(false);

        // Assert
        Assert.False(doc.IsReadOnly);
    }

    [Fact]
    public void SetContent_WhenReadOnly_ThrowsInvalidOperationException()
    {
        // Arrange
        var doc = new Document();
        doc.MarkReadOnly(true);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => doc.SetContent("New content"));
        Assert.Equal("Cannot modify read-only document", ex.Message);
    }

    [Fact]
    public void SetContentInternal_WhenReadOnly_AllowsContentChange()
    {
        // Arrange
        var doc = new Document();
        doc.MarkReadOnly(true);

        // Act
        doc.SetContentInternal("Internal content");

        // Assert
        Assert.Equal("Internal content", doc.Content);
        Assert.True(doc.IsReadOnly);
    }

    [Fact]
    public void SetContentInternal_DoesNotMarkDirty()
    {
        // Arrange
        var doc = new Document();

        // Act
        doc.SetContentInternal("Content");

        // Assert
        Assert.Equal("Content", doc.Content);
        Assert.False(doc.IsDirty);
    }

    [Fact]
    public void MarkDirtyInternal_SetsDirtyFlag()
    {
        // Arrange
        var doc = new Document();

        // Act
        doc.MarkDirtyInternal();

        // Assert
        Assert.True(doc.IsDirty);
    }

    [Fact]
    public void MarkDirtyInternal_CanBeUsedAfterSetContentInternal()
    {
        // Arrange
        var doc = new Document();
        doc.SetContentInternal("Content");

        // Act
        doc.MarkDirtyInternal();

        // Assert
        Assert.Equal("Content", doc.Content);
        Assert.True(doc.IsDirty);
    }
}

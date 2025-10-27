using FluentAssertions;
using TextEdit.Core.Documents;
using System.Text;

namespace TextEdit.Core.Tests;

public class DocumentTests
{
    [Fact]
    public void Constructor_CreatesDocumentWithDefaults()
    {
        // Act
        var doc = new Document();

        // Assert
        doc.Id.Should().NotBeEmpty();
        doc.FilePath.Should().BeNull();
        doc.Name.Should().Be("Untitled");
        doc.Content.Should().BeEmpty();
        doc.IsDirty.Should().BeFalse();
        doc.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        doc.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        doc.Encoding.Should().BeOfType<UTF8Encoding>();
        doc.Eol.Should().Be("\n");
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
        doc.Content.Should().Be("Hello World");
        doc.IsDirty.Should().BeTrue();
        doc.UpdatedAt.Should().BeAfter(initialUpdatedAt);
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
        doc.Content.Should().Be("Second");
        doc.IsDirty.Should().BeTrue();
        doc.UpdatedAt.Should().BeAfter(firstUpdate);
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
        doc.IsDirty.Should().BeFalse();
        doc.FilePath.Should().BeNull();
        doc.UpdatedAt.Should().BeAfter(initialUpdatedAt);
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
        doc.IsDirty.Should().BeFalse();
        doc.FilePath.Should().Be(path);
        doc.Name.Should().Be("file.txt");
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
        doc.Name.Should().Be(expectedName);
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
        doc.Name.Should().Be(Path.GetFileName(filePath));
    }

    [Fact]
    public void Name_WithoutFilePath_ReturnsUntitled()
    {
        // Arrange
        var doc = new Document();

        // Act & Assert
        doc.Name.Should().Be("Untitled");
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
        doc.Encoding.Should().Be(encoding);
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
        doc.Eol.Should().Be(eol);
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
        doc.FilePath.Should().Be("/original/path.txt");
        doc.IsDirty.Should().BeFalse();
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
        doc.FilePath.Should().Be("/original/path.txt");
        doc.IsDirty.Should().BeFalse();
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
        doc.IsDirty.Should().BeTrue();
        doc.Content.Should().BeEmpty();
    }
}
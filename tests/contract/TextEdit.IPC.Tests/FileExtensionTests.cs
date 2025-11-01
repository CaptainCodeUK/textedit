using Xunit;

namespace TextEdit.IPC.Tests;

/// <summary>
/// Tests for file extension handling logic.
/// These tests verify the behavior that mimics IpcBridge.ShowSaveFileDialogAsync
/// </summary>
public class FileExtensionTests
{
    [Theory]
    [InlineData("/path/to/file", "/path/to/file.txt")]
    [InlineData("/path/to/myfile", "/path/to/myfile.txt")]
    [InlineData("C:\\Users\\Documents\\readme", "C:\\Users\\Documents\\readme.txt")]
    [InlineData("filename", "filename.txt")]
    [InlineData("/some/path/document", "/some/path/document.txt")]
    public void AppendTxtExtension_WhenNoExtension_AddsExtension(string input, string expected)
    {
        // Arrange & Act
        var result = !Path.HasExtension(input) ? input + ".txt" : input;

        // Assert
    Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("/path/to/file.txt", "/path/to/file.txt")]
    [InlineData("/path/to/myfile.md", "/path/to/myfile.md")]
    [InlineData("C:\\Users\\Documents\\readme.log", "C:\\Users\\Documents\\readme.log")]
    [InlineData("filename.csv", "filename.csv")]
    [InlineData("/some/path/document.json", "/some/path/document.json")]
    [InlineData("archive.tar.gz", "archive.tar.gz")]
    public void AppendTxtExtension_WhenHasExtension_PreservesOriginal(string input, string expected)
    {
        // Arrange & Act
        var result = !Path.HasExtension(input) ? input + ".txt" : input;

        // Assert
    Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("readme.TXT", "readme.TXT")]
    [InlineData("data.Txt", "data.Txt")]
    public void AppendTxtExtension_EdgeCases_PreservesOriginal(string input, string expected)
    {
        // Arrange & Act
        var result = !Path.HasExtension(input) ? input + ".txt" : input;

        // Assert
    Assert.Equal(expected, result);
    }

    [Fact]
    public void AppendTxtExtension_TrailingDot_AddsExtension()
    {
        // Arrange
        var input = "file.";

        // Act
        // Note: Path.HasExtension("file.") behavior differs by platform
        // On Linux/.NET Core, it returns false, so .txt is appended
        var result = !Path.HasExtension(input) ? input + ".txt" : input;

        // Assert
        // The behavior is to add .txt when no extension is detected
    Assert.Equal("file..txt", result);
    }

    [Fact]
    public void PathHasExtension_EmptyString_ReturnsFalse()
    {
        // Arrange
        var input = "";

        // Act
        var hasExtension = Path.HasExtension(input);

        // Assert
    Assert.False(hasExtension);
    }

    [Fact]
    public void PathHasExtension_Null_ReturnsFalse()
    {
        // Arrange
        string? input = null;

        // Act
        var hasExtension = Path.HasExtension(input);

        // Assert
    Assert.False(hasExtension);
    }

    [Theory]
    [InlineData(".gitignore", true)]
    [InlineData(".env", true)]
    [InlineData(".", false)]
    [InlineData("..", false)]
    public void PathHasExtension_DotFiles_HandledCorrectly(string input, bool expectedHasExtension)
    {
        // Act
        var hasExtension = Path.HasExtension(input);

        // Assert
    Assert.Equal(expectedHasExtension, hasExtension);
    }
}

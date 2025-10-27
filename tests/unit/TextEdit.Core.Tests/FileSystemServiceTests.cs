using FluentAssertions;
using System.Text;
using TextEdit.Infrastructure.FileSystem;

namespace TextEdit.Core.Tests;

public class FileSystemServiceTests
{
    private readonly FileSystemService _fileSystem = new();

    [Fact]
    public async Task ReadAllTextAsync_ReadsFileContents()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var expectedContent = "Test content with UTF-8: 你好世界";
        await File.WriteAllTextAsync(tempFile, expectedContent, Encoding.UTF8);

        try
        {
            // Act
            var result = await _fileSystem.ReadAllTextAsync(tempFile, Encoding.UTF8);

            // Assert
            result.Should().Be(expectedContent);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task WriteAllTextAsync_WritesFileContents()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var content = "New content: 测试数据";

        try
        {
            // Act
            await _fileSystem.WriteAllTextAsync(tempFile, content, Encoding.UTF8);

            // Assert
            var written = await File.ReadAllTextAsync(tempFile, Encoding.UTF8);
            written.Should().Be(content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void FileExists_ReturnsTrueForExistingFile()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            var result = _fileSystem.FileExists(tempFile);

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void FileExists_ReturnsFalseForNonExistentFile()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.txt");

        // Act
        var result = _fileSystem.FileExists(nonExistentFile);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task WriteAllTextAsync_CreatesNewFile()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"newfile_{Guid.NewGuid()}.txt");
        var content = "Fresh content";

        try
        {
            // Act
            await _fileSystem.WriteAllTextAsync(tempFile, content, Encoding.UTF8);

            // Assert
            File.Exists(tempFile).Should().BeTrue();
            var written = await File.ReadAllTextAsync(tempFile);
            written.Should().Be(content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAllTextAsync_WithDifferentEncodings_PreservesContent()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var content = "Latin: café, Cyrillic: привет, Chinese: 你好";

        try
        {
            await File.WriteAllTextAsync(tempFile, content, Encoding.Unicode);

            // Act
            var result = await _fileSystem.ReadAllTextAsync(tempFile, Encoding.Unicode);

            // Assert
            result.Should().Be(content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}

namespace TextEdit.Infrastructure.Tests.FileSystem;

using System.Text;
using FluentAssertions;
using TextEdit.Infrastructure.FileSystem;
using Xunit;

public class FileSystemServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly FileSystemService _sut;

    public FileSystemServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"textedit-fs-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _sut = new FileSystemService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void GetFileSize_FileExists_ReturnsCorrectSize()
    {
        // Arrange
        var path = Path.Combine(_testDir, "test.txt");
        var content = "Hello World";
        File.WriteAllText(path, content);

        // Act
        var size = _sut.GetFileSize(path);

        // Assert
        size.Should().Be(new FileInfo(path).Length);
    }

    [Fact]
    public void GetFileSize_FileDoesNotExist_ReturnsZero()
    {
        // Arrange
        var path = Path.Combine(_testDir, "nonexistent.txt");

        // Act
        var size = _sut.GetFileSize(path);

        // Assert
        size.Should().Be(0);
    }

    [Fact]
    public async Task ReadLargeFileAsync_SmallFile_ReadsCorrectly()
    {
        // Arrange
        var path = Path.Combine(_testDir, "small.txt");
        var expected = "Small file content\nLine 2\nLine 3";
        await File.WriteAllTextAsync(path, expected, Encoding.UTF8);

        // Act
        var result = await _sut.ReadLargeFileAsync(path, Encoding.UTF8);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task ReadLargeFileAsync_LargeFile_ReadsCorrectly()
    {
        // Arrange
        var path = Path.Combine(_testDir, "large.txt");
        var sb = new StringBuilder();
        for (int i = 0; i < 100000; i++)
        {
            sb.AppendLine($"Line {i}: This is a test line with some content to make it bigger.");
        }
        var expected = sb.ToString();
        await File.WriteAllTextAsync(path, expected, Encoding.UTF8);

        // Act
        var result = await _sut.ReadLargeFileAsync(path, Encoding.UTF8);

        // Assert
        result.Should().Be(expected);
        result.Length.Should().BeGreaterThan(1_000_000); // Verify it's actually large
    }

    [Fact]
    public async Task ReadLargeFileAsync_WithProgress_ReportsProgress()
    {
        // Arrange
        var path = Path.Combine(_testDir, "progress-test.txt");
        var content = new string('A', 1_000_000); // 1MB of 'A's
        await File.WriteAllTextAsync(path, content, Encoding.UTF8);
        
        var progressValues = new List<int>();
        var progress = new Progress<int>(p => progressValues.Add(p));

        // Act
        var result = await _sut.ReadLargeFileAsync(path, Encoding.UTF8, progress);

        // Assert
        result.Should().Be(content);
        progressValues.Should().NotBeEmpty("progress should be reported during large file read");
        // Progress reporting is async and may arrive out of order; just verify 100% was reported at some point
        progressValues.Should().Contain(100, "progress should eventually report 100%");
        progressValues.Should().Contain(p => p > 0 && p < 100, "progress should report intermediate values");
    }

    [Fact]
    public async Task ReadLargeFileAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var path = Path.Combine(_testDir, "cancel-test.txt");
        var content = new string('A', 10_000_000); // 10MB
        await File.WriteAllTextAsync(path, content, Encoding.UTF8);
        
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _sut.ReadLargeFileAsync(path, Encoding.UTF8, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task WriteLargeFileAsync_SmallContent_WritesCorrectly()
    {
        // Arrange
        var path = Path.Combine(_testDir, "write-small.txt");
        var content = "Small content\nLine 2\nLine 3";

        // Act
        await _sut.WriteLargeFileAsync(path, content, Encoding.UTF8);

        // Assert
        var result = await File.ReadAllTextAsync(path, Encoding.UTF8);
        result.Should().Be(content);
    }

    [Fact]
    public async Task WriteLargeFileAsync_LargeContent_WritesCorrectly()
    {
        // Arrange
        var path = Path.Combine(_testDir, "write-large.txt");
        var sb = new StringBuilder();
        for (int i = 0; i < 100000; i++)
        {
            sb.AppendLine($"Line {i}: This is a test line with some content to make it bigger.");
        }
        var content = sb.ToString();

        // Act
        await _sut.WriteLargeFileAsync(path, content, Encoding.UTF8);

        // Assert
        var result = await File.ReadAllTextAsync(path, Encoding.UTF8);
        result.Should().Be(content);
        result.Length.Should().BeGreaterThan(1_000_000); // Verify it's actually large
    }

    [Fact]
    public async Task WriteLargeFileAsync_WithProgress_ReportsProgress()
    {
        // Arrange
        var path = Path.Combine(_testDir, "write-progress.txt");
        var content = new string('B', 1_000_000); // 1MB of 'B's
        
        var progressValues = new List<int>();
        var progress = new Progress<int>(p => progressValues.Add(p));

        // Act
        await _sut.WriteLargeFileAsync(path, content, Encoding.UTF8, progress);

        // Assert
        File.Exists(path).Should().BeTrue();
        progressValues.Should().NotBeEmpty();
        progressValues.Last().Should().Be(100); // Final progress should be 100%
        progressValues.Should().BeInAscendingOrder(); // Progress should increase
    }

    [Fact]
    public async Task WriteLargeFileAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var path = Path.Combine(_testDir, "write-cancel.txt");
        var content = new string('C', 10_000_000); // 10MB
        
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _sut.WriteLargeFileAsync(path, content, Encoding.UTF8, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ReadLargeFileAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var path = Path.Combine(_testDir, "nonexistent.txt");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _sut.ReadLargeFileAsync(path, Encoding.UTF8));
    }

    [Fact]
    public async Task ReadWriteLargeFile_RoundTrip_PreservesContent()
    {
        // Arrange
        var path = Path.Combine(_testDir, "roundtrip.txt");
        var original = "Content with unicode: 你好世界 🚀\nMultiple\nLines\n";

        // Act
        await _sut.WriteLargeFileAsync(path, original, Encoding.UTF8);
        var result = await _sut.ReadLargeFileAsync(path, Encoding.UTF8);

        // Assert
        result.Should().Be(original);
    }
}

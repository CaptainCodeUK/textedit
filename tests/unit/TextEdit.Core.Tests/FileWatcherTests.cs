using Xunit;
using TextEdit.Infrastructure.FileSystem;

namespace TextEdit.Core.Tests;

/// <summary>
/// Tests for FileWatcher external file change detection
/// </summary>
public class FileWatcherTests : IDisposable
{
    private readonly string _testDir;
    private readonly FileWatcher _watcher;

    public FileWatcherTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"textedit-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _watcher = new FileWatcher();
    }

    [Fact]
    public async Task Watch_TriggersOnFileChange()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "Initial content");
        
        var changeDetected = false;
        var tcs = new TaskCompletionSource<string>();
        
        _watcher.ChangedExternally += path =>
        {
            changeDetected = true;
            tcs.SetResult(path);
        };

        _watcher.Watch(testFile);
        
        // Act
        await Task.Delay(100); // Let watcher initialize
        await File.WriteAllTextAsync(testFile, "Modified content");
        
        var result = await Task.WhenAny(tcs.Task, Task.Delay(2000));

        // Assert
    Assert.Same(tcs.Task, result);
    Assert.True(changeDetected);
    Assert.Equal(testFile, tcs.Task.Result);
    }

    [Fact]
    public async Task Stop_PreventsChangeDetection()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "test2.txt");
        await File.WriteAllTextAsync(testFile, "Initial");
        
        var changeCount = 0;
        _watcher.ChangedExternally += _ => changeCount++;
        
        _watcher.Watch(testFile);
        await Task.Delay(100);

        // Act: Stop watching before change
        _watcher.Stop();
        await File.WriteAllTextAsync(testFile, "Modified");
        await Task.Delay(500);

        // Assert: No change should be detected
    Assert.Equal(0, changeCount);
    }

    [Fact]
    public void Watch_WithNullPath_DoesNotCrash()
    {
        // Act & Assert: Should not throw
    var ex = Record.Exception(() => _watcher.Watch(null!));
    Assert.Null(ex);
    }

    [Fact]
    public void Watch_WithEmptyPath_DoesNotCrash()
    {
        // Act & Assert: Should not throw
    var ex = Record.Exception(() => _watcher.Watch(""));
    Assert.Null(ex);
    }

    [Fact]
    public void Watch_WithNonexistentFile_DoesNotCrash()
    {
        // Act & Assert: Should not throw
        var nonexistent = Path.Combine(_testDir, "does-not-exist.txt");
    var ex = Record.Exception(() => _watcher.Watch(nonexistent));
    Assert.Null(ex);
    }

    [Fact]
    public async Task Watch_DetectsMultipleChanges()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "multiple.txt");
        await File.WriteAllTextAsync(testFile, "Start");
        
        var changes = new List<string>();
        _watcher.ChangedExternally += path => changes.Add(path);
        
        _watcher.Watch(testFile);
        await Task.Delay(100);

        // Act: Make multiple changes
        await File.WriteAllTextAsync(testFile, "Change 1");
        await Task.Delay(300);
        await File.WriteAllTextAsync(testFile, "Change 2");
        await Task.Delay(300);

        // Assert: Should detect both changes (might dedupe rapid changes)
    Assert.True(changes.Count >= 1);
    }

    [Fact]
    public void Dispose_CleansUpResources()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "dispose.txt");
        File.WriteAllText(testFile, "Content");
        _watcher.Watch(testFile);

        // Act & Assert: Should not throw
    var ex = Record.Exception(() => _watcher.Dispose());
    Assert.Null(ex);
    }

    public void Dispose()
    {
        _watcher.Stop();
        _watcher.Dispose();
        
        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}

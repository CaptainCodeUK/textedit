using Xunit;
using System.IO;
using System.Threading.Tasks;
using TextEdit.Infrastructure.Persistence;
using TextEdit.Core.Preferences;

namespace TextEdit.Core.Tests;

public class WindowStateRepositoryTests : IDisposable
{
    private readonly WindowStateRepository _repository;
    private readonly string _testDir;
    private readonly string _originalBaseDir;

    public WindowStateRepositoryTests()
    {
        // Create isolated test directory
        _testDir = Path.Combine(Path.GetTempPath(), $"WindowStateTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        
        _repository = new WindowStateRepository();
    }

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ReturnsDefaultState()
    {
        // Arrange - fresh repository with no saved state

        // Act
        var state = await _repository.LoadAsync();

        // Assert
        Assert.NotNull(state);
        Assert.Equal(800, state.Width); // Default from WindowState constructor
        Assert.Equal(600, state.Height);
        Assert.Equal(100, state.X);
        Assert.Equal(100, state.Y);
        Assert.False(state.IsMaximized);
        Assert.False(state.IsFullscreen);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_PreservesState()
    {
        // Arrange
        var originalState = new WindowState
        {
            Width = 1024,
            Height = 768,
            X = 200,
            Y = 150,
            IsMaximized = true,
            IsFullscreen = false
        };

        // Act
        await _repository.SaveAsync(originalState);
        var loadedState = await _repository.LoadAsync();

        // Assert
        Assert.Equal(1024, loadedState.Width);
        Assert.Equal(768, loadedState.Height);
        Assert.Equal(200, loadedState.X);
        Assert.Equal(150, loadedState.Y);
        Assert.True(loadedState.IsMaximized);
        Assert.False(loadedState.IsFullscreen);
    }

    [Fact]
    public async Task SaveAsync_WithMinimumViolations_ClampsValues()
    {
        // Arrange - state with values below minimums
        var invalidState = new WindowState
        {
            Width = 50,  // Below minimum of 400
            Height = 30, // Below minimum of 300
            X = -1000,
            Y = -1000
        };

        // Act
        await _repository.SaveAsync(invalidState);
        var loadedState = await _repository.LoadAsync();

        // Assert - should be clamped to minimums
        Assert.True(loadedState.Width >= 400);
        Assert.True(loadedState.Height >= 300);
    }

    [Fact]
    public async Task SaveAsync_MultipleTimes_OverwritesPrevious()
    {
        // Arrange
        var state1 = new WindowState { Width = 800, Height = 600 };
        var state2 = new WindowState { Width = 1920, Height = 1080 };

        // Act
        await _repository.SaveAsync(state1);
        await _repository.SaveAsync(state2);
        var loaded = await _repository.LoadAsync();

        // Assert - should have latest values
        Assert.Equal(1920, loaded.Width);
        Assert.Equal(1080, loaded.Height);
    }

    [Fact]
    public async Task LoadAsync_WithCorruptedFile_ReturnsDefaults()
    {
        // Arrange - write invalid JSON to state file
        var statePath = Path.Combine(AppPaths.BaseDir, "window-state.json");
        Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
        await File.WriteAllTextAsync(statePath, "{ invalid json }");

        // Act
        var state = await _repository.LoadAsync();

        // Assert - should return defaults without throwing
        Assert.NotNull(state);
        Assert.Equal(800, state.Width);
    }

    [Fact]
    public async Task SaveAsync_WithFullscreenState_Persists()
    {
        // Arrange
        var state = new WindowState
        {
            Width = 1920,
            Height = 1080,
            IsFullscreen = true,
            IsMaximized = false
        };

        // Act
        await _repository.SaveAsync(state);
        var loaded = await _repository.LoadAsync();

        // Assert
        Assert.True(loaded.IsFullscreen);
        Assert.False(loaded.IsMaximized);
    }

    public void Dispose()
    {
        // Cleanup test files
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
}

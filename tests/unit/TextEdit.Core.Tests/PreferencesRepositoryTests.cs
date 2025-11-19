using Xunit;
using System.IO;
using System.Threading.Tasks;
using TextEdit.Infrastructure.Persistence;
using TextEdit.Core.Preferences;

namespace TextEdit.Core.Tests;

public class PreferencesRepositoryTests : IDisposable
{
    private readonly PreferencesRepository _repository;
    private readonly string _testPrefsPath;
    private readonly string _tempPrefsDir;

    public PreferencesRepositoryTests()
    {
    _tempPrefsDir = Path.Combine(Path.GetTempPath(), "textedit-prefs-tests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(_tempPrefsDir);
    _repository = new PreferencesRepository(_tempPrefsDir);
    _testPrefsPath = Path.Combine(_tempPrefsDir, "preferences.json");
        
        // Clean up any existing test preferences
        if (File.Exists(_testPrefsPath))
        {
            File.Delete(_testPrefsPath);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ReturnsDefaults()
    {
        // Act
        var prefs = await _repository.LoadAsync();

        // Assert
        Assert.NotNull(prefs);
        Assert.NotNull(prefs.FileExtensions);
        Assert.NotNull(prefs.Updates);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_PreservesValues()
    {
        // Arrange
        var originalPrefs = new UserPreferences
        {
            FontFamily = "Consolas",
            FontSize = 14,
            Theme = ThemeMode.Dark,
            ToolbarVisible = false,
            LoggingEnabled = true,
            FileExtensions = new List<string> { ".md", ".txt" }
        };

        // Act
        await _repository.SaveAsync(originalPrefs);
        var loadedPrefs = await _repository.LoadAsync();

        // Assert
        Assert.Equal("Consolas", loadedPrefs.FontFamily);
        Assert.Equal(14, loadedPrefs.FontSize);
        Assert.Equal(ThemeMode.Dark, loadedPrefs.Theme);
        Assert.False(loadedPrefs.ToolbarVisible);
        Assert.True(loadedPrefs.LoggingEnabled);
        Assert.Contains(".md", loadedPrefs.FileExtensions);
    }



    [Fact]
    public async Task SaveAsync_WithLoggingEnabled_Persists()
    {
        var prefs = new UserPreferences { LoggingEnabled = true };
        await _repository.SaveAsync(prefs);
        var loaded = await _repository.LoadAsync();
        Assert.True(loaded.LoggingEnabled);
    }

    [Fact]
    public async Task SaveAsync_WithNullPreferences_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _repository.SaveAsync(null!));
    }

    [Fact]
    public async Task SaveAsync_WithInvalidExtension_ThrowsArgumentException()
    {
        // Arrange - extension containing invalid character '!' to fail validation after normalization
        var invalidPrefs = new UserPreferences
        {
            FileExtensions = new List<string> { ".md!" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.SaveAsync(invalidPrefs));
    }

    [Fact]
    public async Task LoadAsync_WithCorruptedJson_ReturnsDefaults()
    {
        // Arrange - write invalid JSON
        Directory.CreateDirectory(Path.GetDirectoryName(_testPrefsPath)!);
        await File.WriteAllTextAsync(_testPrefsPath, "{ invalid: json }");

        // Act
        var prefs = await _repository.LoadAsync();

        // Assert - should return defaults without throwing
        Assert.NotNull(prefs);
        Assert.NotNull(prefs.FileExtensions);
    }

    [Fact]
    public async Task SaveAsync_CreatesDirectoryIfMissing()
    {
        // Arrange - ensure directory doesn't exist
        var dir = Path.GetDirectoryName(_testPrefsPath)!;
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }

        var prefs = new UserPreferences();

        // Act
        await _repository.SaveAsync(prefs);

        // Assert
        Assert.True(File.Exists(_testPrefsPath));
    }

    [Fact]
    public async Task SaveAsync_NormalizesExtensions()
    {
        // Arrange - extensions with mixed case and spacing
        var prefs = new UserPreferences
        {
            FileExtensions = new List<string> { " .MD ", ".markdown" }
        };

        // Act
        await _repository.SaveAsync(prefs);
        var loaded = await _repository.LoadAsync();

        // Assert - should be normalized to lowercase
        Assert.All(loaded.FileExtensions, ext =>
        {
            Assert.StartsWith(".", ext);
            Assert.Equal(ext.ToLowerInvariant(), ext);
        });
    }

    [Fact]
    public async Task SaveAsync_MultipleTimes_OverwritesCorrectly()
    {
        // Arrange
        var prefs1 = new UserPreferences { FontSize = 12 };
        var prefs2 = new UserPreferences { FontSize = 16 };

        // Act
        await _repository.SaveAsync(prefs1);
        await _repository.SaveAsync(prefs2);
        var loaded = await _repository.LoadAsync();

        // Assert
        Assert.Equal(16, loaded.FontSize);
    }

    [Fact]
    public async Task LoadAsync_WithUpdatePreferences_PreservesSettings()
    {
        // Arrange
        var prefs = new UserPreferences
        {
            Updates = new TextEdit.Core.Updates.UpdatePreferences
            {
                CheckOnStartup = false,
                AutoDownload = true,
                CheckIntervalHours = 48,
                LastCheckTime = DateTimeOffset.UtcNow
            }
        };

        // Act
        await _repository.SaveAsync(prefs);
        var loaded = await _repository.LoadAsync();

        // Assert
        Assert.False(loaded.Updates.CheckOnStartup);
        Assert.True(loaded.Updates.AutoDownload);
        Assert.Equal(48, loaded.Updates.CheckIntervalHours);
    }

    [Fact]
    public async Task SaveAsync_WithEmptyExtensionArray_Succeeds()
    {
        // Arrange
        var prefs = new UserPreferences
        {
            FileExtensions = new List<string>()
        };

        // Act
        await _repository.SaveAsync(prefs);
        var loaded = await _repository.LoadAsync();

        // Assert
        Assert.NotEmpty(loaded.FileExtensions); // NormalizeExtensions adds defaults
    }

    public void Dispose()
    {
        // Cleanup test files
        try
        {
            if (File.Exists(_testPrefsPath)) { File.Delete(_testPrefsPath); }
            var tempPath = _testPrefsPath + ".tmp";
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
}

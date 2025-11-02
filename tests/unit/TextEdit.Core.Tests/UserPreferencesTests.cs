using TextEdit.Core.Preferences;
using Xunit;

namespace TextEdit.Core.Tests;

/// <summary>
/// Tests for UserPreferences validation and normalization logic.
/// Covers font size clamping (FR-049a), extension validation, and default enforcement.
/// </summary>
public class UserPreferencesTests
{
    [Theory]
    [InlineData(8, 8)]    // Min boundary
    [InlineData(12, 12)]  // Normal value
    [InlineData(72, 72)]  // Max boundary
    [InlineData(5, 8)]    // Below min → clamp to 8
    [InlineData(100, 72)] // Above max → clamp to 72
    [InlineData(-10, 8)]  // Negative → clamp to 8
    [InlineData(0, 8)]    // Zero → clamp to 8
    public void FontSize_Clamping_WorksCorrectly(int input, int expected)
    {
        // Arrange
        var prefs = new UserPreferences();

        // Act
        prefs.FontSize = input;

        // Assert
        Assert.Equal(expected, prefs.FontSize);
    }

    [Fact]
    public void FontSize_DefaultValue_Is12()
    {
        // Arrange & Act
        var prefs = new UserPreferences();

        // Assert
        Assert.Equal(12, prefs.FontSize);
    }

    [Fact]
    public void NormalizeExtensions_EnsuresRequiredDefaults()
    {
        // Arrange
        var prefs = new UserPreferences
        {
            FileExtensions = new List<string> { ".log", ".json" }
        };

        // Act
        prefs.NormalizeExtensions();

        // Assert
        Assert.Contains(".txt", prefs.FileExtensions);
        Assert.Contains(".md", prefs.FileExtensions);
    }

    [Fact]
    public void NormalizeExtensions_RemovesDuplicates_CaseInsensitive()
    {
        // Arrange
        var prefs = new UserPreferences
        {
            FileExtensions = new List<string> { ".txt", ".TXT", ".Txt", ".md" }
        };

        // Act
        prefs.NormalizeExtensions();

        // Assert
        Assert.Single(prefs.FileExtensions, e => e == ".txt");
        Assert.Single(prefs.FileExtensions, e => e == ".md");
    }

    [Fact]
    public void NormalizeExtensions_ConvertsToLowercase()
    {
        // Arrange
        var prefs = new UserPreferences
        {
            FileExtensions = new List<string> { ".LOG", ".JSON" }
        };

        // Act
        prefs.NormalizeExtensions();

        // Assert
        Assert.Contains(".log", prefs.FileExtensions);
        Assert.Contains(".json", prefs.FileExtensions);
        Assert.DoesNotContain(".LOG", prefs.FileExtensions);
        Assert.DoesNotContain(".JSON", prefs.FileExtensions);
    }

    [Fact]
    public void NormalizeExtensions_AddsLeadingDot()
    {
        // Arrange
        var prefs = new UserPreferences
        {
            FileExtensions = new List<string> { "txt", "md", "log" }
        };

        // Act
        prefs.NormalizeExtensions();

        // Assert
        Assert.Contains(".txt", prefs.FileExtensions);
        Assert.Contains(".md", prefs.FileExtensions);
        Assert.Contains(".log", prefs.FileExtensions);
    }

    [Fact]
    public void ValidateExtensions_ValidExtensions_ReturnsTrue()
    {
        // Arrange
        var prefs = new UserPreferences();
        prefs.NormalizeExtensions();

        // Act
        var (isValid, invalidEntry) = prefs.ValidateExtensions();

        // Assert
        Assert.True(isValid);
        Assert.Null(invalidEntry);
    }

    [Theory]
    [InlineData(".txt-backup")]  // Valid: contains hyphen
    [InlineData(".cfg2")]         // Valid: contains number
    [InlineData(".myext")]        // Valid: alphanumeric
    public void ValidateExtensions_ValidFormats_ReturnsTrue(string extension)
    {
        // Arrange
        var prefs = new UserPreferences
        {
            FileExtensions = new List<string> { ".txt", ".md", extension }
        };

        // Act
        var (isValid, invalidEntry) = prefs.ValidateExtensions();

        // Assert
        Assert.True(isValid);
        Assert.Null(invalidEntry);
    }

    [Theory]
    [InlineData("txt")]           // Missing leading dot
    [InlineData(".txt.bak")]      // Multiple dots
    [InlineData("..txt")]         // Double dot
    [InlineData(".")]             // Just dot
    [InlineData(".txt!")]         // Invalid character
    [InlineData(".txt space")]    // Contains space
    public void ValidateExtensions_InvalidFormats_ReturnsFalse(string extension)
    {
        // Arrange
        var prefs = new UserPreferences
        {
            FileExtensions = new List<string> { ".txt", ".md", extension }
        };

        // Act
        var (isValid, invalidEntry) = prefs.ValidateExtensions();

        // Assert
        Assert.False(isValid);
        Assert.Equal(extension, invalidEntry);
    }

    [Fact]
    public void Theme_DefaultValue_IsSystem()
    {
        // Arrange & Act
        var prefs = new UserPreferences();

        // Assert
        Assert.Equal(ThemeMode.System, prefs.Theme);
    }

    [Fact]
    public void FontFamily_DefaultValue_IsEmpty()
    {
        // Arrange & Act
        var prefs = new UserPreferences();

        // Assert
        Assert.Equal(string.Empty, prefs.FontFamily);
    }

    [Fact]
    public void LoggingEnabled_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var prefs = new UserPreferences();

        // Assert
        Assert.False(prefs.LoggingEnabled);
    }

    [Fact]
    public void ToolbarVisible_DefaultValue_IsTrue()
    {
        // Arrange & Act
        var prefs = new UserPreferences();

        // Assert
        Assert.True(prefs.ToolbarVisible);
    }
}

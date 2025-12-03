using System;
using TextEdit.Infrastructure.SpellChecking;
using Xunit;

namespace TextEdit.Infrastructure.Tests.SpellChecking;

public class DictionaryServiceTests
{
    [Fact]
    public void LoadEnglishDictionary_EmbeddedResource_IsLoaded()
    {
        // Act
        var checker = DictionaryService.LoadEnglishDictionary();

        // Assert
        Assert.NotNull(checker);
        Assert.True(checker.IsInitialized);
    }

    [Fact]
    public void EnsureEmbeddedDictionaryInstalledToCustomPath_CopiesFiles()
    {
        // Arrange
        var customPath = TextEdit.Infrastructure.SpellChecking.DictionaryService.GetCustomDictionaryPath();
        var dicPath = Path.Combine(customPath, "en_US.dic");
        var affPath = Path.Combine(customPath, "en_US.aff");
        try
        {
            // Act
            TextEdit.Infrastructure.SpellChecking.DictionaryService.EnsureEmbeddedDictionaryInstalledToCustomPath();

            // Assert
            Assert.True(File.Exists(dicPath));
            Assert.True(File.Exists(affPath));
        }
        finally
        {
            // Cleanup
            try { if (File.Exists(dicPath)) File.Delete(dicPath);} catch { }
            try { if (File.Exists(affPath)) File.Delete(affPath);} catch { }
        }
    }
}

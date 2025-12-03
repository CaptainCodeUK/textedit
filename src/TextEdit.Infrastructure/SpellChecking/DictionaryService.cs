using TextEdit.Core.SpellChecking;

namespace TextEdit.Infrastructure.SpellChecking;

/// <summary>
/// Service for loading and managing Hunspell dictionaries.
/// Handles both built-in and custom dictionaries.
/// </summary>
public class DictionaryService
{
    public const string BuiltInDictionaryPath = "Resources/Dictionaries";
    public const string EnglishDicFileName = "en_US.dic";
    public const string EnglishAffFileName = "en_US.aff";

    /// <summary>
    /// Loads the built-in English dictionary.
    /// </summary>
    /// <returns>A configured HunspellSpellChecker with the English dictionary loaded.</returns>
    /// <exception cref="InvalidOperationException">Thrown if dictionary files are not found.</exception>
    public static HunspellSpellChecker LoadEnglishDictionary()
    {
        var spellChecker = new HunspellSpellChecker();

        try
        {
            // Try to load from embedded resources
            var assembly = typeof(DictionaryService).Assembly;
            var dicResourceName = $"{assembly.GetName().Name}.{BuiltInDictionaryPath.Replace("/", ".")}.{EnglishDicFileName}";
            var affResourceName = $"{assembly.GetName().Name}.{BuiltInDictionaryPath.Replace("/", ".")}.{EnglishAffFileName}";

            using (var dicStream = assembly.GetManifestResourceStream(dicResourceName))
            using (var affStream = assembly.GetManifestResourceStream(affResourceName))
            {
                if (dicStream != null && affStream != null)
                {
                    // Loaded from embedded resources
                    spellChecker.LoadDictionary(dicStream, affStream);
                    return spellChecker;
                }
            }

            // If embedded resources are not present, check the custom dictionary path on disk
            var customPath = GetCustomDictionaryPath();
            var dicPath = Path.Combine(customPath, EnglishDicFileName);
            var affPath = Path.Combine(customPath, EnglishAffFileName);
            if (File.Exists(dicPath) && File.Exists(affPath))
            {
                // Load from file system
                return LoadFromFiles(dicPath, affPath);
            }

            throw new InvalidOperationException(
                $"Dictionary resources not found. Expected embedded: {dicResourceName}/{affResourceName} or files: {dicPath}/{affPath}");
        }
        catch (Exception ex)
        {
            spellChecker.Dispose();
            throw new InvalidOperationException("Failed to load English dictionary.", ex);
        }
    }

    /// <summary>
    /// Loads a custom dictionary from a file path.
    /// </summary>
    public static HunspellSpellChecker LoadFromFiles(string dicFilePath, string affFilePath)
    {
        if (!File.Exists(dicFilePath))
            throw new FileNotFoundException($"Dictionary file not found: {dicFilePath}");

        if (!File.Exists(affFilePath))
            throw new FileNotFoundException($"Affix file not found: {affFilePath}");

        var spellChecker = new HunspellSpellChecker();

        try
        {
            using (var dicStream = File.OpenRead(dicFilePath))
            using (var affStream = File.OpenRead(affFilePath))
            {
                spellChecker.LoadDictionary(dicStream, affStream);
            }

            return spellChecker;
        }
        catch (Exception ex)
        {
            spellChecker.Dispose();
            throw new InvalidOperationException("Failed to load dictionary from files.", ex);
        }
    }

    /// <summary>
    /// Gets the path where custom dictionaries should be stored.
    /// </summary>
    public static string GetCustomDictionaryPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var customDictPath = Path.Combine(appDataPath, "TextEdit", "Dictionaries");

        return customDictPath;
    }

    /// <summary>
    /// Ensures the custom dictionary directory exists.
    /// </summary>
    public static void EnsureCustomDictionaryDirectory()
    {
        var dictPath = GetCustomDictionaryPath();
        if (!Directory.Exists(dictPath))
        {
            Directory.CreateDirectory(dictPath);
        }
    }

    /// <summary>
    /// If embedded dictionaries exist, copy them to the user's custom dictionary directory if they are not already present.
    /// This makes it easy for the UI to list and manage dictionary files at runtime.
    /// </summary>
    public static void EnsureEmbeddedDictionaryInstalledToCustomPath()
    {
        var assembly = typeof(DictionaryService).Assembly;
        var dicResourceName = $"{assembly.GetName().Name}.{BuiltInDictionaryPath.Replace("/", ".")}.{EnglishDicFileName}";
        var affResourceName = $"{assembly.GetName().Name}.{BuiltInDictionaryPath.Replace("/", ".")}.{EnglishAffFileName}";

        using var dicStream = assembly.GetManifestResourceStream(dicResourceName);
        using var affStream = assembly.GetManifestResourceStream(affResourceName);
        if (dicStream == null || affStream == null) return;

        EnsureCustomDictionaryDirectory();
        var customDir = GetCustomDictionaryPath();
        var dicPath = Path.Combine(customDir, EnglishDicFileName);
        var affPath = Path.Combine(customDir, EnglishAffFileName);

        if (!File.Exists(dicPath))
        {
            using var outDic = File.OpenWrite(dicPath);
            dicStream.CopyTo(outDic);
        }
        if (!File.Exists(affPath))
        {
            using var outAff = File.OpenWrite(affPath);
            affStream.CopyTo(outAff);
        }
    }
}

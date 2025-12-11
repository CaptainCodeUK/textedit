using TextEdit.Core.SpellChecking;

namespace TextEdit.Infrastructure.SpellChecking;

/// <summary>
/// Service for loading and managing Hunspell dictionaries.
/// Handles both built-in and custom dictionaries.
/// </summary>
public class DictionaryService
{
        public static int LastLoadedDictionaryWordCount { get; private set; } = 0;
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

            // If the direct resource name isn't found (resource may be under a sub-namespace like SpellChecking.Resources), try to locate by suffix
            if (assembly.GetManifestResourceInfo(dicResourceName) == null)
            {
                var found = FindResourceBySuffix(assembly, "." + EnglishDicFileName);
                if (!string.IsNullOrEmpty(found)) dicResourceName = found;
            }
            if (assembly.GetManifestResourceInfo(affResourceName) == null)
            {
                var found = FindResourceBySuffix(assembly, "." + EnglishAffFileName);
                if (!string.IsNullOrEmpty(found)) affResourceName = found;
            }

            using (var dicStreamOrig = assembly.GetManifestResourceStream(dicResourceName))
            using (var affStreamOrig = assembly.GetManifestResourceStream(affResourceName))
            {
                if (dicStreamOrig != null && affStreamOrig != null)
                {
                    // Create copy of the embedded streams so we can inspect the dic file and still pass streams to the spell checker
                    var dicMem = new MemoryStream();
                    var affMem = new MemoryStream();
                    dicStreamOrig.CopyTo(dicMem);
                    affStreamOrig.CopyTo(affMem);
                    dicMem.Position = 0;
                    affMem.Position = 0;

                    // Count dictionary entries - skip potential first-line count header
                    LastLoadedDictionaryWordCount = CountDictionaryWords(dicMem);
                    dicMem.Position = 0;

                    spellChecker.LoadDictionary(dicMem, affMem);
                    System.Diagnostics.Debug.WriteLine($"[DictionaryService] Loaded embedded English dictionary - words: {LastLoadedDictionaryWordCount}");
                    return spellChecker;
                }
            }

            // If embedded resources are not present, check the custom dictionary path on disk
            var customPath = GetCustomDictionaryPath();
            var dicPath = Path.Combine(customPath, EnglishDicFileName);
            var affPath = Path.Combine(customPath, EnglishAffFileName);
            if (File.Exists(dicPath) && File.Exists(affPath))
            {
                // Load from file system (we will compute the size for diagnostics)
                var checker = LoadFromFiles(dicPath, affPath);
                try
                {
                    LastLoadedDictionaryWordCount = CountDictionaryWords(File.OpenRead(dicPath));
                    System.Diagnostics.Debug.WriteLine($"[DictionaryService] Loaded English dictionary from path {dicPath} - words: {LastLoadedDictionaryWordCount}");
                }
                catch { }
                return checker;
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

    private static string? FindResourceBySuffix(System.Reflection.Assembly assembly, string suffix)
    {
        try
        {
            var resources = assembly.GetManifestResourceNames();
            foreach (var r in resources)
            {
                if (r.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return r;
            }
        }
        catch { }
        return null;
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
                // Count words for diagnostics
                LastLoadedDictionaryWordCount = CountDictionaryWords(new MemoryStream(File.ReadAllBytes(dicFilePath)));
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

    private static int CountDictionaryWords(Stream dicStream)
    {
        try
        {
            dicStream.Position = 0;
            using var sr = new StreamReader(dicStream, leaveOpen: true);
            int count = 0;
            bool checkedHeader = false;
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!checkedHeader)
                {
                    // Some dictionaries include a count on the first line
                    var first = line.Trim();
                    if (int.TryParse(first, out _))
                    {
                        checkedHeader = true; continue;
                    }
                    checkedHeader = true;
                }
                count++;
            }
            dicStream.Position = 0;
            return count;
        }
        catch
        {
            try { dicStream.Position = 0; } catch { }
            return 0;
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
        if (assembly.GetManifestResourceInfo(dicResourceName) == null)
        {
            var found = FindResourceBySuffix(assembly, "." + EnglishDicFileName);
            if (!string.IsNullOrEmpty(found)) dicResourceName = found;
        }
        if (assembly.GetManifestResourceInfo(affResourceName) == null)
        {
            var found = FindResourceBySuffix(assembly, "." + EnglishAffFileName);
            if (!string.IsNullOrEmpty(found)) affResourceName = found;
        }

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

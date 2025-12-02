using TextEdit.Core.SpellChecking;

namespace TextEdit.Infrastructure.SpellChecking;

/// <summary>
/// A stub spell checker that returns no suggestions.
/// Used when the dictionary files are not available.
/// </summary>
public class StubSpellChecker : ISpellChecker
{
    public bool IsInitialized => false;

    public bool CheckWord(string word)
    {
        return true; // Consider all words correct
    }

    public IReadOnlyList<string> GetSuggestions(string word, int maxSuggestions = 5)
    {
        return Array.Empty<string>();
    }

    public bool AddWordToDictionary(string word)
    {
        return false; // Can't add - no custom dictionary
    }

    public bool RemoveWordFromDictionary(string word)
    {
        return false; // Can't remove - no custom dictionary
    }

    public IReadOnlyList<string> GetCustomWords()
    {
        return Array.Empty<string>();
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}

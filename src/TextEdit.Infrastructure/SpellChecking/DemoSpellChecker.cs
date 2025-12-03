using TextEdit.Core.SpellChecking;
using System.Collections.Generic;

namespace TextEdit.Infrastructure.SpellChecking;

/// <summary>
/// A small demo spell checker used when Hunspell dictionaries are not available.
/// This is intended for development/demo purposes only - not a replacement for Hunspell.
/// </summary>
public class DemoSpellChecker : ISpellChecker
{
    private readonly HashSet<string> _knownCorrectWords;
    private readonly Dictionary<string, List<string>> _misspellings;

    public DemoSpellChecker()
    {
        // Minimal in-memory word list and common misspellings
        _knownCorrectWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "this", "is", "a", "heading", "markdown", "example", "spell", "checker", "hello", "world"
        };

        _misspellings = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "teh", new List<string>{ "the" } },
            { "recieve", new List<string>{ "receive" } },
            { "langauge", new List<string>{ "language" } },
        };
    }

    public bool IsInitialized => true;

    public bool CheckWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return true;
        if (_knownCorrectWords.Contains(word)) return true;
        if (_misspellings.ContainsKey(word)) return false;
        // If unknown, still consider it correct to reduce noise
        return true;
    }

    public IReadOnlyList<string> GetSuggestions(string word, int maxSuggestions = 5)
    {
        if (_misspellings.TryGetValue(word, out var suggestions))
        {
            return suggestions.Take(maxSuggestions).ToList().AsReadOnly();
        }
        return Array.Empty<string>();
    }

    public bool AddWordToDictionary(string word) => false;
    public bool RemoveWordFromDictionary(string word) => false;
    public IReadOnlyList<string> GetCustomWords() => Array.Empty<string>();
    public void Dispose() { }
}

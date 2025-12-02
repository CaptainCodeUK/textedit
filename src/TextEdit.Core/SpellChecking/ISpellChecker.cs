namespace TextEdit.Core.SpellChecking;

/// <summary>
/// Abstraction for spell checking engine.
/// </summary>
public interface ISpellChecker : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the spell checker is initialized and ready.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Checks if a word is spelled correctly.
    /// </summary>
    /// <param name="word">The word to check.</param>
    /// <returns>True if the word is correctly spelled; otherwise false.</returns>
    bool CheckWord(string word);

    /// <summary>
    /// Gets spelling suggestions for a misspelled word.
    /// </summary>
    /// <param name="word">The misspelled word.</param>
    /// <param name="maxSuggestions">Maximum number of suggestions to return.</param>
    /// <returns>A list of suggestions, ordered by confidence (highest first).</returns>
    IReadOnlyList<string> GetSuggestions(string word, int maxSuggestions = 5);

    /// <summary>
    /// Adds a word to the custom user dictionary.
    /// </summary>
    /// <param name="word">The word to add.</param>
    /// <returns>True if the word was added successfully; false if already present.</returns>
    bool AddWordToDictionary(string word);

    /// <summary>
    /// Removes a word from the custom user dictionary.
    /// </summary>
    /// <param name="word">The word to remove.</param>
    /// <returns>True if the word was removed; false if not found.</returns>
    bool RemoveWordFromDictionary(string word);

    /// <summary>
    /// Gets all words in the custom user dictionary.
    /// </summary>
    /// <returns>A read-only list of custom words.</returns>
    IReadOnlyList<string> GetCustomWords();
}

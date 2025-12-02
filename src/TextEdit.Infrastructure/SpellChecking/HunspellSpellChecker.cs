using WeCantSpell.Hunspell;
using TextEdit.Core.SpellChecking;

namespace TextEdit.Infrastructure.SpellChecking;

/// <summary>
/// WeCantSpell.Hunspell-based spell checker implementation.
/// </summary>
public class HunspellSpellChecker : ISpellChecker
{
    private WordList? _dictionary;
    private readonly HashSet<string> _customWords = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="HunspellSpellChecker"/> class.
    /// </summary>
    public HunspellSpellChecker()
    {
    }

    /// <summary>
    /// Gets a value indicating whether the spell checker is initialized.
    /// </summary>
    public bool IsInitialized => _dictionary != null;

    /// <summary>
    /// Loads a dictionary from a built-in or custom resource.
    /// </summary>
    /// <param name="dicContent">The .dic file content.</param>
    /// <param name="affContent">The .aff file content.</param>
    public void LoadDictionary(Stream dicStream, Stream affStream)
    {
        ThrowIfDisposed();

        try
        {
            _dictionary = WordList.CreateFromStreams(dicStream, affStream);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to load Hunspell dictionary.", ex);
        }
    }

    /// <summary>
    /// Checks if a word is spelled correctly.
    /// </summary>
    public bool CheckWord(string word)
    {
        ThrowIfDisposed();

        if (!IsInitialized || string.IsNullOrWhiteSpace(word))
            return true;

        // Check custom words first
        if (_customWords.Contains(word))
            return true;

        // Check main dictionary
        return _dictionary!.Check(word);
    }

    /// <summary>
    /// Gets spelling suggestions for a misspelled word.
    /// </summary>
    public IReadOnlyList<string> GetSuggestions(string word, int maxSuggestions = 5)
    {
        ThrowIfDisposed();

        if (!IsInitialized || string.IsNullOrWhiteSpace(word))
            return Array.Empty<string>();

        try
        {
            var suggestions = _dictionary!.Suggest(word)
                .Take(maxSuggestions)
                .ToList();

            return suggestions.AsReadOnly();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Adds a word to the custom user dictionary.
    /// </summary>
    public bool AddWordToDictionary(string word)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(word))
            return false;

        return _customWords.Add(word.Trim());
    }

    /// <summary>
    /// Removes a word from the custom user dictionary.
    /// </summary>
    public bool RemoveWordFromDictionary(string word)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(word))
            return false;

        return _customWords.Remove(word.Trim());
    }

    /// <summary>
    /// Gets all words in the custom user dictionary.
    /// </summary>
    public IReadOnlyList<string> GetCustomWords()
    {
        ThrowIfDisposed();
        return _customWords.ToList().AsReadOnly();
    }

    /// <summary>
    /// Disposes the spell checker and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        // WordList doesn't implement IDisposable, so we just clear the reference
        _dictionary = null;
        _customWords.Clear();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HunspellSpellChecker));
    }
}

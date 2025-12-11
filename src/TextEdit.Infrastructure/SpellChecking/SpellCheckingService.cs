using System.Text;
using System.Text.RegularExpressions;
using TextEdit.Core.SpellChecking;

namespace TextEdit.Infrastructure.SpellChecking;

/// <summary>
/// Service for performing spell checking on document content.
/// Handles real-time checking, debouncing, code block exclusion, and word tracking.
/// </summary>
public class SpellCheckingService
{
    private ISpellChecker _spellChecker;
    private SpellCheckPreferences _preferences;
    private CancellationTokenSource? _debounceTokenSource;
    private DateTime _lastCheckTime = DateTime.MinValue;
    private readonly object _lockObject = new();

    // Regex patterns for code block detection
    private static readonly Regex FencedCodeBlockPattern = new(@"```[\s\S]*?```", RegexOptions.Compiled);
    private static readonly Regex InlineCodePattern = new(@"`[^`]+`", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="SpellCheckingService"/> class.
    /// </summary>
    public SpellCheckingService(ISpellChecker spellChecker, SpellCheckPreferences? preferences = null)
    {
        _spellChecker = spellChecker ?? throw new ArgumentNullException(nameof(spellChecker));
        _preferences = preferences ?? new SpellCheckPreferences();
    }

    /// <summary>
    /// Update the runtime preferences used by the spell checking service.
    /// This allows the UI to change preferences (e.g., debounce interval) without restarting the service.
    /// </summary>
    public void UpdatePreferences(SpellCheckPreferences prefs)
    {
        if (prefs == null) throw new ArgumentNullException(nameof(prefs));
        _preferences = prefs;
    }

    /// <summary>
    /// Replace the underlying spell checker implementation at runtime.
    /// This disposes the old checker and swaps the reference atomically.
    /// </summary>
    public void ReplaceSpellChecker(ISpellChecker newChecker)
    {
        if (newChecker == null) throw new ArgumentNullException(nameof(newChecker));
        lock (_lockObject)
        {
            try { _spellChecker?.Dispose(); } catch { }
            _spellChecker = newChecker;
        }
    }

    /// <summary>
    /// Runtime helper indicating if the backing spell checker is initialized.
    /// </summary>
    public bool IsInitialized => _spellChecker?.IsInitialized ?? false;

    /// <summary>
    /// Checks the spelling of words in the provided text.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of spell check results for misspelled words.</returns>
    public async Task<IReadOnlyList<SpellCheckResult>> CheckSpellingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text) || !_spellChecker.IsInitialized)
            return Array.Empty<SpellCheckResult>();

        // Debounce the check
        await DebounceAsync(cancellationToken);

        return await Task.Run(() => CheckSpellingInternal(text), cancellationToken);
    }

    /// <summary>
    /// Checks the spelling of a specific word.
    /// </summary>
    public bool IsWordCorrect(string word)
    {
        if (!_spellChecker.IsInitialized || string.IsNullOrWhiteSpace(word))
            return true;

        return _spellChecker.CheckWord(word);
    }

    /// <summary>
    /// Gets suggestions for a misspelled word.
    /// </summary>
    public IReadOnlyList<SpellCheckSuggestion> GetSuggestions(string word)
    {
        if (!_spellChecker.IsInitialized || string.IsNullOrWhiteSpace(word))
            return Array.Empty<SpellCheckSuggestion>();

        var suggestions = _spellChecker.GetSuggestions(word, _preferences.MaxSuggestions);

        return suggestions
            .Select((s, index) => new SpellCheckSuggestion
            {
                Word = s,
                Confidence = Math.Max(1, 100 - (index * 10)),
                IsPrimary = index == 0
            })
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Adds a word to the custom dictionary.
    /// </summary>
    public bool AddWordToDictionary(string word)
    {
        if (!_spellChecker.IsInitialized || string.IsNullOrWhiteSpace(word))
            return false;

        return _spellChecker.AddWordToDictionary(word);
    }

    /// <summary>
    /// Removes a word from the custom dictionary.
    /// </summary>
    public bool RemoveWordFromDictionary(string word)
    {
        if (!_spellChecker.IsInitialized || string.IsNullOrWhiteSpace(word))
            return false;

        return _spellChecker.RemoveWordFromDictionary(word);
    }

    /// <summary>
    /// Gets all custom dictionary words.
    /// </summary>
    public IReadOnlyList<string> GetCustomWords()
    {
        if (!_spellChecker.IsInitialized)
            return Array.Empty<string>();

        return _spellChecker.GetCustomWords();
    }

    private IReadOnlyList<SpellCheckResult> CheckSpellingInternal(string text)
    {
        var results = new List<SpellCheckResult>();

        // Get code block regions to exclude
        var codeBlockRanges = _preferences.CheckCodeBlocks
            ? Array.Empty<(int, int)>()
            : GetCodeBlockRanges(text);

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        int charPosition = 0;

        for (int lineNum = 0; lineNum < lines.Length; lineNum++)
        {
            var line = lines[lineNum];
            var lineResults = CheckLine(line, charPosition, lineNum + 1, codeBlockRanges);
            results.AddRange(lineResults);

            charPosition += line.Length + 1; // +1 for newline
        }

        return results.AsReadOnly();
    }

    private IReadOnlyList<SpellCheckResult> CheckLine(
        string line,
        int lineStartPosition,
        int lineNumber,
        (int, int)[] excludedRanges)
    {
        var results = new List<SpellCheckResult>();

        // Word pattern: sequence of letters
    // Match Unicode letters (including diacritics), apostrophes and hyphens for contractions and hyphenated words
    var wordPattern = new Regex(@"\b[\p{L}\p{M}'-]+\b", RegexOptions.Compiled);
        var matches = wordPattern.Matches(line);

        foreach (Match match in matches)
        {
            var word = match.Value;
            var startPos = lineStartPosition + match.Index;
            var endPos = startPos + word.Length;

            // Skip if in excluded range
            if (IsPositionInExcludedRange(startPos, endPos, excludedRanges))
                continue;

            // Skip if word length exceeds maximum (if set)
            if (_preferences.MaxWordLengthToCheck > 0 && word.Length > _preferences.MaxWordLengthToCheck)
                continue;

            // Skip session-ignored words
            if (_ignoredWords.Contains(word))
                continue;

            // Check if word is misspelled
            if (!_spellChecker.CheckWord(word))
            {
                var suggestions = GetSuggestions(word);
                results.Add(new SpellCheckResult
                {
                    Word = word,
                    StartPosition = startPos,
                    EndPosition = endPos,
                    LineNumber = lineNumber,
                    // ColumnNumber is 0-based in SpellCheckResult to match tests and easier manipulation
                    ColumnNumber = match.Index,
                    Suggestions = suggestions
                });
            }
        }

        return results;
    }

    private readonly HashSet<string> _ignoredWords = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Ignore a word for the current session (transient until reload).
    /// </summary>
    public void IgnoreWordOnce(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return;
        _ignoredWords.Add(word);
    }

    /// <summary>
    /// Clear session ignored words.
    /// </summary>
    public void ClearIgnoredWords()
    {
        _ignoredWords.Clear();
    }

    private (int, int)[] GetCodeBlockRanges(string text)
    {
        var ranges = new List<(int, int)>();

        // Fenced code blocks
        foreach (Match match in FencedCodeBlockPattern.Matches(text))
        {
            ranges.Add((match.Index, match.Index + match.Length));
        }

        // Inline code
        foreach (Match match in InlineCodePattern.Matches(text))
        {
            ranges.Add((match.Index, match.Index + match.Length));
        }

        return ranges.ToArray();
    }

    private bool IsPositionInExcludedRange(int start, int end, (int, int)[] ranges)
    {
        return ranges.Any(range => start >= range.Item1 && end <= range.Item2);
    }

    private async Task DebounceAsync(CancellationToken cancellationToken)
    {
        lock (_lockObject)
        {
            _debounceTokenSource?.Cancel();
            _debounceTokenSource = new CancellationTokenSource();
        }

        try
        {
            await Task.Delay(_preferences.DebounceIntervalMs, _debounceTokenSource.Token);
            _lastCheckTime = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
            // Debounce was cancelled, which is expected
        }
    }

    /// <summary>
    /// Dispose resources.
    /// </summary>
    public void Dispose()
    {
        _debounceTokenSource?.Dispose();
        _spellChecker?.Dispose();
    }
}

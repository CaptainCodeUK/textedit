using NSubstitute;
using TextEdit.Core.SpellChecking;
using TextEdit.Infrastructure.SpellChecking;
using Xunit;

namespace TextEdit.Infrastructure.Tests.SpellChecking;

public class SpellCheckingServiceTests
{
    private readonly ISpellChecker _mockSpellChecker;
    private readonly SpellCheckingService _service;

    public SpellCheckingServiceTests()
    {
        _mockSpellChecker = Substitute.For<ISpellChecker>();
        _mockSpellChecker.IsInitialized.Returns(true);
        _service = new SpellCheckingService(_mockSpellChecker);
    }

    [Fact]
    public async Task CheckSpellingAsync_WithEmptyText_ReturnsEmptyResults()
    {
        // Arrange
        var text = string.Empty;

        // Act
        var results = await _service.CheckSpellingAsync(text);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task CheckSpellingAsync_WithUninitializedChecker_ReturnsEmptyResults()
    {
        // Arrange
        var uninitializedChecker = Substitute.For<ISpellChecker>();
        uninitializedChecker.IsInitialized.Returns(false);
        var service = new SpellCheckingService(uninitializedChecker);
        var text = "This is a test";

        // Act
        var results = await service.CheckSpellingAsync(text);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task CheckSpellingAsync_WithCorrectWords_ReturnsEmptyResults()
    {
        // Arrange
        var text = "This is correct";
        _mockSpellChecker.CheckWord("This").Returns(true);
        _mockSpellChecker.CheckWord("is").Returns(true);
        _mockSpellChecker.CheckWord("correct").Returns(true);

        // Act
        var results = await _service.CheckSpellingAsync(text);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task CheckSpellingAsync_WithMisspelledWord_ReturnsMisspellingResult()
    {
        // Arrange
        var text = "This is teh test";
        _mockSpellChecker.CheckWord("This").Returns(true);
        _mockSpellChecker.CheckWord("is").Returns(true);
        _mockSpellChecker.CheckWord("teh").Returns(false);
        _mockSpellChecker.CheckWord("test").Returns(true);
        _mockSpellChecker.GetSuggestions("teh", Arg.Any<int>()).Returns(new[] { "the", "tea" });

        // Act
        var results = await _service.CheckSpellingAsync(text);

        // Assert
        Assert.Single(results);
        Assert.Equal("teh", results[0].Word);
        Assert.Equal(1, results[0].LineNumber);
    }

    [Fact]
    public async Task CheckSpellingAsync_WithMultipleMisspelledWords_ReturnsMultipleResults()
    {
        // Arrange
        var text = "Teh qwick brwon fox";
        _mockSpellChecker.CheckWord("Teh").Returns(false);
        _mockSpellChecker.CheckWord("qwick").Returns(false);
        _mockSpellChecker.CheckWord("brwon").Returns(false);
        _mockSpellChecker.CheckWord("fox").Returns(true);
        _mockSpellChecker.GetSuggestions(Arg.Any<string>(), Arg.Any<int>()).Returns(new[] { "suggestion" });

        // Act
        var results = await _service.CheckSpellingAsync(text);

        // Assert
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task CheckSpellingAsync_WithMultilineText_IncludesLineNumbers()
    {
        // Arrange
        var text = "Correct line\nTeh second line\nAnother corect one";
        _mockSpellChecker.CheckWord(Arg.Is<string>(w => w == "Correct" || w == "line" || w == "second" || w == "Another" || w == "one")).Returns(true);
        _mockSpellChecker.CheckWord("Teh").Returns(false);
        _mockSpellChecker.CheckWord("corect").Returns(false);
        _mockSpellChecker.GetSuggestions(Arg.Any<string>(), Arg.Any<int>()).Returns(new[] { "the", "correct" });

        // Act
        var results = await _service.CheckSpellingAsync(text);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.LineNumber == 2); // "Teh"
        Assert.Contains(results, r => r.LineNumber == 3); // "corect"
    }

    [Fact]
    public void IsWordCorrect_WithCorrectWord_ReturnsTrue()
    {
        // Arrange
        _mockSpellChecker.CheckWord("correct").Returns(true);

        // Act
        var result = _service.IsWordCorrect("correct");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWordCorrect_WithMisspelledWord_ReturnsFalse()
    {
        // Arrange
        _mockSpellChecker.CheckWord("mispeled").Returns(false);

        // Act
        var result = _service.IsWordCorrect("mispeled");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetSuggestions_WithMisspelledWord_ReturnsSuggestions()
    {
        // Arrange
        var word = "teh";
        _mockSpellChecker.GetSuggestions(word, Arg.Any<int>()).Returns(new[] { "the", "tea" });

        // Act
        var suggestions = _service.GetSuggestions(word);

        // Assert
        Assert.NotEmpty(suggestions);
        Assert.Equal("the", suggestions[0].Word);
    }

    [Fact]
    public void GetSuggestions_WithEmptyWord_ReturnsEmptySuggestions()
    {
        // Act
        var suggestions = _service.GetSuggestions(string.Empty);

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public void AddWordToDictionary_WithValidWord_ReturnsTrue()
    {
        // Arrange
        const string word = "newword";
        _mockSpellChecker.AddWordToDictionary(word).Returns(true);

        // Act
        var result = _service.AddWordToDictionary(word);

        // Assert
        Assert.True(result);
        _mockSpellChecker.Received().AddWordToDictionary(word);
    }

    [Fact]
    public void RemoveWordFromDictionary_WithValidWord_ReturnsTrue()
    {
        // Arrange
        const string word = "newword";
        _mockSpellChecker.RemoveWordFromDictionary(word).Returns(true);

        // Act
        var result = _service.RemoveWordFromDictionary(word);

        // Assert
        Assert.True(result);
        _mockSpellChecker.Received().RemoveWordFromDictionary(word);
    }

    [Fact]
    public void GetCustomWords_ReturnsCustomWordList()
    {
        // Arrange
        var customWords = new[] { "word1", "word2", "word3" };
        _mockSpellChecker.GetCustomWords().Returns(customWords);

        // Act
        var result = _service.GetCustomWords();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("word1", result);
    }

    [Fact]
    public async Task CheckSpellingAsync_WithCodeBlocks_ExcludesCodeContent()
    {
        // Arrange
        var text = "Check `inline code` and ```\nblock code\n``` should not be checked";
        var preferences = new SpellCheckPreferences { CheckCodeBlocks = false };
        var service = new SpellCheckingService(_mockSpellChecker, preferences);

        _mockSpellChecker.CheckWord("Check").Returns(true);
        _mockSpellChecker.CheckWord("and").Returns(true);
        _mockSpellChecker.CheckWord("should").Returns(true);
        _mockSpellChecker.CheckWord("not").Returns(true);
        _mockSpellChecker.CheckWord("be").Returns(true);
        _mockSpellChecker.CheckWord("checked").Returns(true);

        // Act
        var results = await service.CheckSpellingAsync(text);

        // Assert - no misspellings in code blocks should be reported
        Assert.Empty(results);
    }
}

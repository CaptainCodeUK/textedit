using TextEdit.Core.SpellChecking;
using TextEdit.Infrastructure.SpellChecking;
using Xunit;

namespace TextEdit.Infrastructure.Tests.SpellChecking;

/// <summary>
/// Unit tests for SpellCheckDecorationService.
/// Validates conversion of spell check results to Monaco decoration objects.
/// </summary>
public class SpellCheckDecorationServiceTests
{
    private readonly SpellCheckDecorationService _service = new();

    #region ConvertToDecorations Tests

    [Fact]
    public void ConvertToDecorations_WithEmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var results = new List<SpellCheckResult>();

        // Act
        var decorations = _service.ConvertToDecorations(results);

        // Assert
        Assert.Empty(decorations);
    }

    [Fact]
    public void ConvertToDecorations_WithSingleMisspelling_ReturnsOneDecoration()
    {
        // Arrange
        var result = new SpellCheckResult
        {
            Word = "teh",
            StartPosition = 0,
            EndPosition = 3,
            LineNumber = 1,
            ColumnNumber = 0,
            Suggestions = new List<SpellCheckSuggestion>
            {
                new SpellCheckSuggestion { Word = "the", Confidence = 100, IsPrimary = true }
            }
        };

        var results = new List<SpellCheckResult> { result };

        // Act
        var decorations = _service.ConvertToDecorations(results);

        // Assert
        Assert.Single(decorations);
        Assert.Equal(1, decorations[0].Range.StartLineNumber);
        Assert.Equal(1, decorations[0].Range.StartColumn); // 0-based -> 1-based conversion
        Assert.Equal(1, decorations[0].Range.EndLineNumber);
        Assert.Equal(4, decorations[0].Range.EndColumn); // 0+3 = 3, +1 = 4 (1-based)
    }

    [Fact]
    public void ConvertToDecorations_WithMultipleMisspellings_ReturnsAllDecorations()
    {
        // Arrange
        var results = new List<SpellCheckResult>
        {
            new SpellCheckResult
            {
                Word = "teh",
                StartPosition = 0,
                EndPosition = 3,
                LineNumber = 1,
                ColumnNumber = 0,
                Suggestions = new List<SpellCheckSuggestion>()
            },
            new SpellCheckResult
            {
                Word = "wrld",
                StartPosition = 6,
                EndPosition = 10,
                LineNumber = 1,
                ColumnNumber = 6,
                Suggestions = new List<SpellCheckSuggestion>()
            }
        };

        // Act
        var decorations = _service.ConvertToDecorations(results);

        // Assert
        Assert.Equal(2, decorations.Count);
    }

    [Fact]
    public void ConvertToDecorations_PreservesDecorationOptions()
    {
        // Arrange
        var result = new SpellCheckResult
        {
            Word = "teh",
            StartPosition = 0,
            EndPosition = 3,
            LineNumber = 1,
            ColumnNumber = 0,
            Suggestions = new List<SpellCheckSuggestion>()
        };

        // Act
        var decorations = _service.ConvertToDecorations(new List<SpellCheckResult> { result });

        // Assert
        Assert.NotNull(decorations[0].Options);
        Assert.False(decorations[0].Options.IsWholeLine);
    Assert.Equal("spell-check-error", decorations[0].Options.InlineClassName);
        Assert.Equal("teh", decorations[0].Options.Message);
    }

    [Fact]
    public void ConvertToDecorations_SkipsInvalidRanges()
    {
        // Arrange: invalid column (negative) and invalid end <= start
        var results = new List<SpellCheckResult>
        {
            new SpellCheckResult { Word = "x", StartPosition = 0, EndPosition = 1, LineNumber = 1, ColumnNumber = -2, Suggestions = new List<SpellCheckSuggestion>() },
            new SpellCheckResult { Word = "y", StartPosition = 0, EndPosition = 1, LineNumber = 1, ColumnNumber = 10, Suggestions = new List<SpellCheckSuggestion>() }
        };

        // Act
        var decorations = _service.ConvertToDecorations(results);

        // Assert - the first should be skipped, the second should be converted if its end column is appropriate
        Assert.True(decorations.Count <= results.Count);
        // Specifically ensure none have start column < 1
        Assert.DoesNotContain(decorations.Select(d => d.Range.StartColumn), c => c < 1);
    }

    [Fact]
    public void ConvertToDecorations_IncludesSuggestions()
    {
        // Arrange
        var suggestions = new List<SpellCheckSuggestion>
        {
            new SpellCheckSuggestion { Word = "the", Confidence = 100, IsPrimary = true },
            new SpellCheckSuggestion { Word = "tea", Confidence = 85, IsPrimary = false }
        };

        var result = new SpellCheckResult
        {
            Word = "teh",
            StartPosition = 0,
            EndPosition = 3,
            LineNumber = 1,
            ColumnNumber = 0,
            Suggestions = suggestions
        };

        // Act
        var decorations = _service.ConvertToDecorations(new List<SpellCheckResult> { result });

        // Assert
        Assert.Equal(2, decorations[0].Options.Suggestions.Count);
        Assert.Equal("the", decorations[0].Options.Suggestions[0].Word); // Ordered by confidence
        Assert.Equal("tea", decorations[0].Options.Suggestions[1].Word);
    }

    [Fact]
    public void ConvertToDecorations_SortsSuggestionsByConfidence()
    {
        // Arrange
        var suggestions = new List<SpellCheckSuggestion>
        {
            new SpellCheckSuggestion { Word = "tea", Confidence = 50, IsPrimary = false },
            new SpellCheckSuggestion { Word = "the", Confidence = 100, IsPrimary = true },
            new SpellCheckSuggestion { Word = "ten", Confidence = 75, IsPrimary = false }
        };

        var result = new SpellCheckResult
        {
            Word = "teh",
            StartPosition = 0,
            EndPosition = 3,
            LineNumber = 1,
            ColumnNumber = 0,
            Suggestions = suggestions
        };

        // Act
        var decorations = _service.ConvertToDecorations(new List<SpellCheckResult> { result });

        // Assert
        var orderedSuggestions = decorations[0].Options.Suggestions;
        Assert.Equal(100, orderedSuggestions[0].Confidence);
        Assert.Equal(75, orderedSuggestions[1].Confidence);
        Assert.Equal(50, orderedSuggestions[2].Confidence);
    }

    [Fact]
    public void ConvertToDecorations_HandlesMultilineResults()
    {
        // Arrange
        var results = new List<SpellCheckResult>
        {
            new SpellCheckResult
            {
                Word = "teh",
                StartPosition = 0,
                EndPosition = 3,
                LineNumber = 1,
                ColumnNumber = 0,
                Suggestions = new List<SpellCheckSuggestion>()
            },
            new SpellCheckResult
            {
                Word = "wrld",
                StartPosition = 0,
                EndPosition = 4,
                LineNumber = 2,
                ColumnNumber = 0,
                Suggestions = new List<SpellCheckSuggestion>()
            }
        };

        // Act
        var decorations = _service.ConvertToDecorations(results);

        // Assert
        Assert.Equal(1, decorations[0].Range.StartLineNumber);
        Assert.Equal(2, decorations[1].Range.StartLineNumber);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(10, 15)]
    [InlineData(100, 110)]
    public void ConvertToDecorations_CalculatesCorrectColumnNumbers(int startCol, int wordLength)
    {
        // Arrange
        var result = new SpellCheckResult
        {
            Word = new string('a', wordLength),
            StartPosition = startCol,
            EndPosition = startCol + wordLength,
            LineNumber = 1,
            ColumnNumber = startCol,
            Suggestions = new List<SpellCheckSuggestion>()
        };

        // Act
        var decorations = _service.ConvertToDecorations(new List<SpellCheckResult> { result });

        // Assert
        Assert.Equal(startCol + 1, decorations[0].Range.StartColumn); // 1-based
        Assert.Equal(startCol + wordLength + 1, decorations[0].Range.EndColumn); // 1-based, inclusive
    }

    #endregion

    #region ClearDecorations Tests

    [Fact]
    public void ClearDecorations_ReturnsEmptyList()
    {
        // Act
        var decorations = _service.ClearDecorations();

        // Assert
        Assert.Empty(decorations);
    }

    #endregion

    #region MonacoDecoration Model Tests

    [Fact]
    public void MonacoDecoration_HasRangeProperty()
    {
        // Arrange
        var decoration = new MonacoDecoration();

        // Act
        var range = new MonacoRange
        {
            StartLineNumber = 1,
            StartColumn = 1,
            EndLineNumber = 1,
            EndColumn = 5
        };
        decoration.Range = range;

        // Assert
        Assert.Equal(range, decoration.Range);
    }

    [Fact]
    public void MonacoDecoration_HasOptionsProperty()
    {
        // Arrange
        var decoration = new MonacoDecoration();

        // Act
        var options = new MonacoDecorationOptions
        {
            ClassName = "test-class",
            Message = "test message"
        };
        decoration.Options = options;

        // Assert
        Assert.Equal(options, decoration.Options);
    }

    #endregion

    #region MonacoRange Model Tests

    [Fact]
    public void MonacoRange_StoresLineAndColumnNumbers()
    {
        // Arrange & Act
        var range = new MonacoRange
        {
            StartLineNumber = 5,
            StartColumn = 10,
            EndLineNumber = 5,
            EndColumn = 15
        };

        // Assert
        Assert.Equal(5, range.StartLineNumber);
        Assert.Equal(10, range.StartColumn);
        Assert.Equal(5, range.EndLineNumber);
        Assert.Equal(15, range.EndColumn);
    }

    #endregion

    #region MonacoDecorationOptions Model Tests

    [Fact]
    public void MonacoDecorationOptions_StoresClassName()
    {
        // Arrange & Act
        var options = new MonacoDecorationOptions
        {
            ClassName = "spell-check-error"
        };

        // Assert
        Assert.Equal("spell-check-error", options.ClassName);
    }

    [Fact]
    public void MonacoDecorationOptions_StoresSuggestions()
    {
        // Arrange
        var suggestions = new List<SpellCheckSuggestion>
        {
            new SpellCheckSuggestion { Word = "the", Confidence = 100, IsPrimary = true }
        };

        // Act
        var options = new MonacoDecorationOptions
        {
            Suggestions = suggestions
        };

        // Assert
        Assert.Single(options.Suggestions);
        Assert.Equal("the", options.Suggestions[0].Word);
    }

    [Fact]
    public void MonacoDecorationOptions_DefaultsMessageToNull()
    {
        // Arrange & Act
        var options = new MonacoDecorationOptions();

        // Assert
        Assert.Null(options.Message);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ConvertToDecorations_RoundTrip_PreservesAllData()
    {
        // Arrange
        var suggestions = new List<SpellCheckSuggestion>
        {
            new SpellCheckSuggestion { Word = "hello", Confidence = 95, IsPrimary = true },
            new SpellCheckSuggestion { Word = "hallo", Confidence = 70, IsPrimary = false }
        };

        var result = new SpellCheckResult
        {
            Word = "helo",
            StartPosition = 0,
            EndPosition = 4,
            LineNumber = 1,
            ColumnNumber = 0,
            Suggestions = suggestions
        };

        var results = new List<SpellCheckResult> { result };

        // Act
        var decorations = _service.ConvertToDecorations(results);
        var decoration = decorations[0];

        // Assert
        Assert.Equal(1, decoration.Range.StartLineNumber);
        Assert.Equal(1, decoration.Range.StartColumn);
        Assert.Equal(1, decoration.Range.EndLineNumber);
        Assert.Equal(5, decoration.Range.EndColumn); // 0+4+1
    Assert.Equal("spell-check-error", decoration.Options.InlineClassName);
        Assert.Equal("helo", decoration.Options.Message);
        Assert.Equal(2, decoration.Options.Suggestions.Count);
    }

    #endregion
}

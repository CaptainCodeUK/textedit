using System.Text.Json;
using Xunit;
using FluentAssertions;
using TextEdit.UI.Services;
using TextEdit.Core.SpellChecking;

namespace TextEdit.UI.Tests;

public class SpellCheckSuggestionMapperTests
{
    [Fact]
    public void Parse_StringSuggestion_ReturnsSuggestion()
    {
        var raw = (object)"banana";
        var res = SpellCheckSuggestionMapper.Parse(raw);
        res.Should().NotBeNull();
        res!.Word.Should().Be("banana");
        res.IsPrimary.Should().BeFalse();
        res.Confidence.Should().Be(50);
    }

    [Fact]
    public void Parse_JsonElementString_ReturnsSuggestion()
    {
        using var doc = JsonDocument.Parse("\"apple\"");
        var raw = (object)doc.RootElement;
        var res = SpellCheckSuggestionMapper.Parse(raw);
        res.Should().NotBeNull();
        res!.Word.Should().Be("apple");
    }

    [Fact]
    public void Parse_JsonElementObject_ReturnsSuggestionWithProps()
    {
        using var doc = JsonDocument.Parse("{ \"Word\": \"pear\", \"IsPrimary\": true, \"Confidence\": 80 }");
        var raw = (object)doc.RootElement;
        var res = SpellCheckSuggestionMapper.Parse(raw);
        res.Should().NotBeNull();
        res!.Word.Should().Be("pear");
        res.IsPrimary.Should().BeTrue();
        res.Confidence.Should().Be(80);
    }

    [Fact]
    public void Parse_AnonymousObject_ReturnsSuggestionWithProps()
    {
        var raw = new { Word = "kiwi", IsPrimary = true, Confidence = 90 } as object;
        var res = SpellCheckSuggestionMapper.Parse(raw);
        res.Should().NotBeNull();
        res!.Word.Should().Be("kiwi");
        res.IsPrimary.Should().BeTrue();
        res.Confidence.Should().Be(90);
    }
}

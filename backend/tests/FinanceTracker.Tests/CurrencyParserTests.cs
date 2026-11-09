using System.Text.Json;
using FinanceTracker.Application.Helpers;
using Xunit;

namespace FinanceTracker.Tests;

public class CurrencyParserTests
{
    [Theory]
    [InlineData("R$ 35,90", 35.90)]
    [InlineData("35,90", 35.90)]
    [InlineData("42.50", 42.50)]
    [InlineData("42,50", 42.50)]
    [InlineData("1.250,50", 1250.50)]
    [InlineData("R$ 1.250,50", 1250.50)]
    [InlineData("  R$  100,00  ", 100.00)]
    [InlineData("50", 50.00)]
    public void ParseString_ShouldHandleVariousFormats(string input, decimal expected)
    {
        var result = CurrencyParser.ParseString(input);
        Assert.NotNull(result);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Parse_WithNumericTypes_ShouldParseCorrectly()
    {
        Assert.Equal(35.90m, CurrencyParser.Parse(35.90m));
        Assert.Equal(42.50m, CurrencyParser.Parse(42.5));
        Assert.Equal(50.00m, CurrencyParser.Parse(50));
    }

    [Fact]
    public void Parse_WithJsonElementNumber_ShouldParseCorrectly()
    {
        using var doc = JsonDocument.Parse("{\"amount\": 35.90}");
        var prop = doc.RootElement.GetProperty("amount");
        var result = CurrencyParser.Parse(prop);

        Assert.NotNull(result);
        Assert.Equal(35.90m, result.Value);
    }

    [Fact]
    public void Parse_WithJsonElementString_ShouldParseCorrectly()
    {
        using var doc = JsonDocument.Parse("{\"amount\": \"R$ 1.250,50\"}");
        var prop = doc.RootElement.GetProperty("amount");
        var result = CurrencyParser.Parse(prop);

        Assert.NotNull(result);
        Assert.Equal(1250.50m, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("abc")]
    public void Parse_InvalidInput_ShouldReturnNull(object? input)
    {
        var result = CurrencyParser.Parse(input);
        Assert.Null(result);
    }
}

using DogruTemin.Core.Services;

namespace DogruTemin.Tests;

public class NumberParserTests
{
    [Theory]
    [InlineData("1.234,56", "1234.56")]
    [InlineData("1.234,56 TL", "1234.56")]
    [InlineData("1234,56", "1234.56")]
    public void TurkishNumbersAreParsed(string text, string expectedText)
    {
        Assert.True(NumberParser.TryPositiveDecimal(text, out var value));
        var expected = decimal.Parse(expectedText, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-5")]
    public void NonPositiveValuesAreRejected(string text)
    {
        Assert.False(NumberParser.TryPositiveDecimal(text, out _));
    }
}

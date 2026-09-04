using DogruTemin.Core.Services;

namespace DogruTemin.Tests;

public class NumberParserTests
{
    [Theory]
    [InlineData("1.234,56", "1234.56")]
    [InlineData("1.234,56 TL", "1234.56")]
    [InlineData("1234,56", "1234.56")]
    [InlineData("1,234.56", "1234.56")]
    [InlineData("₺ 1 234,56", "1234.56")]
    [InlineData("1234.56", "1234.56")]
    public void CommonTurkishAndInternationalNumbersAreParsed(string text, string expectedText)
    {
        Assert.True(NumberParser.TryPositiveDecimal(text, out var value));
        var expected = decimal.Parse(expectedText, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("abc")]
    [InlineData("TL")]
    public void InvalidOrNonPositiveValuesAreRejected(string text)
    {
        Assert.False(NumberParser.TryPositiveDecimal(text, out _));
    }

    [Fact]
    public void VeryLargeRealisticAmountRemainsDeterministic()
    {
        const string text = "999.999.999.999,99 TL";

        Assert.True(NumberParser.TryPositiveDecimal(text, out var first));
        Assert.True(NumberParser.TryPositiveDecimal(text, out var second));
        Assert.Equal(999_999_999_999.99m, first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void MoneyFormattingUsesTurkishThousandsAndDecimalSeparators()
    {
        Assert.Equal("1.234,56 TL", NumberParser.Money(1234.56m));
    }
}

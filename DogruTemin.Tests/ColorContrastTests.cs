using DogruTemin.Wpf;

namespace DogruTemin.Tests;

public sealed class ColorContrastTests
{
    [Theory]
    [InlineData("#FFFFFF", "#031C38")]
    [InlineData("#FFB41F", "#031C38")]
    [InlineData("#FFFFFF", "#8A5A12")]
    [InlineData("#FFFFFF", "#0A3154")]
    [InlineData("#DDF6E7", "#092746")]
    [InlineData("#C5D7E7", "#0A3154")]
    public void CriticalNormalTextPairsMeetMinimumContrast(string foreground, string background)
    {
        var ratio = ColorContrastService.ContrastRatio(foreground, background);

        Assert.True(
            ColorContrastService.MeetsNormalText(foreground, background),
            $"{foreground} / {background} kontrastı yetersiz: {ratio:0.00}:1");
    }

    [Fact]
    public void GoldOnNavyHasStrongBrandContrast()
    {
        Assert.True(ColorContrastService.ContrastRatio("#FFB41F", "#031C38") >= 9.0);
    }
}

using DogruTemin.Wpf;

namespace DogruTemin.Tests;

public class ResponsiveLayoutTests
{
    [Fact]
    public void FullHdAt100PercentFitsWithoutUpscaling()
    {
        var profile = ResponsiveLayoutService.CalculateForPixels(1920, 1080, 100);
        Assert.True(profile.ContentScale <= 1.0);
        Assert.True(profile.WindowWidthDip <= 1450.0);
        Assert.True(profile.WindowHeightDip <= 945.0);
    }

    [Fact]
    public void HighDpiDownscalesInsteadOfUpscalingText()
    {
        var profile = ResponsiveLayoutService.CalculateForPixels(1920, 1080, 150);
        Assert.True(profile.RequiresDownscale);
        Assert.True(profile.ContentScale < 1.0);
    }
}

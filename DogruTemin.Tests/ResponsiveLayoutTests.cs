using DogruTemin.Wpf;

namespace DogruTemin.Tests;

public sealed class ResponsiveLayoutTests
{
    [Theory]
    [InlineData(1024, 600, 100)]
    [InlineData(1280, 720, 100)]
    [InlineData(1366, 768, 125)]
    [InlineData(1366, 768, 150)]
    [InlineData(1366, 768, 100)]
    [InlineData(1920, 1080, 100)]
    [InlineData(1920, 1080, 125)]
    [InlineData(1920, 1080, 150)]
    [InlineData(2560, 1440, 175)]
    [InlineData(3840, 2160, 200)]
    public void SupportedScreenProfilesStayInsideWorkArea(int width, int height, int dpiPercent)
    {
        var profile = ResponsiveLayoutService.CalculateForPixels(width, height, dpiPercent);

        Assert.InRange(profile.ContentScale, 0.01, 1.0);
        Assert.True(profile.WindowWidthDip <= profile.WorkWidthDip * 0.96 + 0.2);
        Assert.True(profile.WindowHeightDip <= profile.WorkHeightDip * 0.94 + 0.2);
        Assert.True(profile.WindowWidthDip > 0);
        Assert.True(profile.WindowHeightDip > 0);
    }

    [Fact]
    public void LargeScreenDoesNotBitmapUpscaleDesign()
    {
        var profile = ResponsiveLayoutService.CalculateForPixels(3840, 2160, 100);

        Assert.Equal(1.0, profile.ContentScale, 6);
        Assert.False(profile.RequiresDownscale);
        Assert.Equal(ResponsiveLayoutService.DesignWidth, profile.WindowWidthDip, 1);
        Assert.Equal(ResponsiveLayoutService.DesignHeight, profile.WindowHeightDip, 1);
    }

    [Fact]
    public void FullHdAt150PercentUsesControlledDownscale()
    {
        var profile = ResponsiveLayoutService.CalculateForPixels(1920, 1080, 150);

        Assert.True(profile.RequiresDownscale);
        Assert.InRange(profile.ContentScale, 0.70, 0.73);
    }
}

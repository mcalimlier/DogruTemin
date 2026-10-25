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
    public void FullHdAt150PercentPreservesReadableTextSize()
    {
        var profile = ResponsiveLayoutService.CalculateForPixels(1920, 1080, 150);

        Assert.False(profile.RequiresDownscale);
        Assert.Equal(1, profile.ContentScale);
    }

    [Theory]
    [InlineData("1280x720", 1280, 720)]
    [InlineData("1366x768", 1366, 768)]
    [InlineData("1920x1080", 1920, 1080)]
    public void PreferencesUseIndependentDimensionsAndFitSmallScreens(string preference, int width, int height)
    {
        var large = ResponsiveLayoutService.CalculatePreference(4000, 2200, preference);
        Assert.Equal(width, large.Width);
        Assert.Equal(height, large.Height);
        var small = ResponsiveLayoutService.CalculatePreference(900, 600, preference);
        Assert.InRange(small.Width, 1, 900);
        Assert.InRange(small.Height, 1, 600);
    }

    [Fact]
    public void WindowPreferenceSurvivesSettingsRoundTrip()
    {
        var folder = Path.Combine(Path.GetTempPath(), "DogruTeminWindow_" + Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new DogruTemin.Core.Services.StorageService(folder);
            storage.SaveSettings(new() { WindowSizePreference = "1366x768" });
            Assert.Equal("1366x768", storage.LoadSettings().WindowSizePreference);
            storage.SaveSettings(new() { WindowSizePreference = "invalid" });
            Assert.Equal("Auto", storage.LoadSettings().WindowSizePreference);
        }
        finally { Directory.Delete(folder, true); }
    }
}

using DogruTemin.Wpf;

namespace DogruTemin.Tests;

public class StartupProgressProfileTests
{
    [Fact]
    public void PreloadProgressIsMonotonicAndBounded()
    {
        var previous = StartupProgressProfile.InitialPercent;
        for (var i = 0; i <= 100; i++)
        {
            var current = StartupProgressProfile.CalculatePreloadPercent(i / 100.0);
            Assert.InRange(current, StartupProgressProfile.InitialPercent, StartupProgressProfile.PreloadPercent);
            Assert.True(current >= previous);
            previous = current;
        }

        Assert.Equal(StartupProgressProfile.PreloadPercent, previous);
    }

    [Fact]
    public void AnimationProfileUsesSmoothFrameCadence()
    {
        Assert.InRange(StartupProgressProfile.FrameDelayMs, 16, 40);
        Assert.True(StartupProgressProfile.PreloadDurationMs >= 1500);
        Assert.True(StartupProgressProfile.CompletionDurationMs >= 200);
        Assert.True(StartupProgressProfile.PreloadPercent < StartupProgressProfile.MaximumReportedBeforeComplete);
        Assert.True(StartupProgressProfile.MaximumReportedBeforeComplete < 100);
    }
}

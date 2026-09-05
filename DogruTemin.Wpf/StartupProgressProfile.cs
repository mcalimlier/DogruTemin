namespace DogruTemin.Wpf;

public static class StartupProgressProfile
{
    public const int InitialPercent = 8;
    public const int PreloadPercent = 86;
    public const int MaximumReportedBeforeComplete = 96;
    public const int PreloadDurationMs = 2300;
    public const int FrameDelayMs = 32;
    public const int CompletionDurationMs = 340;

    public static int CalculatePreloadPercent(double progress)
    {
        progress = Math.Clamp(progress, 0.0, 1.0);
        return InitialPercent + (int)Math.Round((PreloadPercent - InitialPercent) * progress);
    }
}

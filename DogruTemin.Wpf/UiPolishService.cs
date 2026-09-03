namespace DogruTemin.Wpf;

/// <summary>
/// DoğruFatura final kalite katmanının DoğruTemin uyarlaması.
/// İş kurallarına dokunmadan responsive yerleşim, görsel standart,
/// metin keskinliği, erişilebilirlik ve liste performansını ortaklaştırır.
/// </summary>
internal static class UiPolishService
{
    public static void Apply(MainWindow window)
    {
        ResponsiveLayoutService.Apply(window);
        TextSharpnessService.Apply(window);
        window.Loaded += (_, _) =>
        {
            UiVisualPolishService.Apply(window);
            UiPerformanceService.Apply(window);
            UiAccessibilityService.Apply(window);
        };
    }
}

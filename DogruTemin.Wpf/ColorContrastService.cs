namespace DogruTemin.Wpf;

public static class ColorContrastService
{
    public static double ContrastRatio(string foregroundHex, string backgroundHex)
    {
        var foreground = RelativeLuminance(ParseRgb(foregroundHex));
        var background = RelativeLuminance(ParseRgb(backgroundHex));
        var lighter = Math.Max(foreground, background);
        var darker = Math.Min(foreground, background);
        return (lighter + 0.05) / (darker + 0.05);
    }

    public static bool MeetsNormalText(string foregroundHex, string backgroundHex) =>
        ContrastRatio(foregroundHex, backgroundHex) >= 4.5;

    public static bool MeetsLargeText(string foregroundHex, string backgroundHex) =>
        ContrastRatio(foregroundHex, backgroundHex) >= 3.0;

    private static (byte R, byte G, byte B) ParseRgb(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Renk boş olamaz.", nameof(value));

        var hex = value.Trim().TrimStart('#');
        if (hex.Length == 8) hex = hex[2..];
        if (hex.Length != 6)
            throw new ArgumentException("Renk #RRGGBB veya #AARRGGBB biçiminde olmalıdır.", nameof(value));

        return (
            Convert.ToByte(hex[0..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16));
    }

    private static double RelativeLuminance((byte R, byte G, byte B) rgb) =>
        (0.2126 * Linear(rgb.R / 255.0))
        + (0.7152 * Linear(rgb.G / 255.0))
        + (0.0722 * Linear(rgb.B / 255.0));

    private static double Linear(double channel) =>
        channel <= 0.04045
            ? channel / 12.92
            : Math.Pow((channel + 0.055) / 1.055, 2.4);
}

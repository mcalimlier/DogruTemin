using System.Text.RegularExpressions;

namespace DogruTemin.Core.Services;

public static class TextNormalizer
{
    public static string Hys(string? value)
    {
        var s = (value ?? string.Empty).Trim();
        while (s.StartsWith("HYS", StringComparison.OrdinalIgnoreCase))
        {
            s = s[3..].Trim();
            while (s.Length > 0 && "-:/".Contains(s[0])) s = s[1..].Trim();
        }
        return s;
    }

    public static string SingleLine(string? value)
    {
        var s = (value ?? string.Empty).Replace("_x000D_", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("_x000A_", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("\r", " ").Replace("\n", " ");
        return Regex.Replace(s, @"\s{2,}", " ").Trim();
    }

    public static string DigitsOnly(string? value) => Regex.Replace(value ?? string.Empty, @"[\s\.\-\(\)\+/\u00A0]", string.Empty);

    public static string NormalizeFirmName(string? value)
    {
        var s = SingleLine(value).ToUpperInvariant();
        return Regex.Replace(s, @"[^\p{L}\p{Nd}]", string.Empty);
    }
}

using System.Globalization;
using System.Text.RegularExpressions;

namespace DogruTemin.Core.Services;

public static class NumberParser
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static bool TryPositiveDecimal(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var s = text.Trim().Replace("₺", "", StringComparison.OrdinalIgnoreCase).Replace("TL", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (decimal.TryParse(s, NumberStyles.Number, Tr, out value) && value > 0) return true;
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value) && value > 0) return true;

        s = Regex.Replace(s, @"\s+", "");
        if (s.Contains(',') && s.Contains('.'))
        {
            var lastComma = s.LastIndexOf(',');
            var lastDot = s.LastIndexOf('.');
            if (lastComma > lastDot) s = s.Replace(".", "").Replace(',', '.');
            else s = s.Replace(",", "");
        }
        else if (s.Contains(',')) s = s.Replace(',', '.');

        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value) && value > 0;
    }

    public static string Money(decimal value) => value.ToString("N2", Tr) + " TL";
}

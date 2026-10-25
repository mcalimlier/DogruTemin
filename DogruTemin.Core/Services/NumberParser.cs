using System.Globalization;
using System.Text.RegularExpressions;

namespace DogruTemin.Core.Services;

public static class NumberParser
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static bool TryPositiveDecimal(string? text, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(text)) return false;
        // Quantities in the calculation loop are usually integers. Avoid regex and
        // separator normalization for that unambiguous, frequent input.
        if (text.All(char.IsAsciiDigit))
            return decimal.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value > 0m;

        // DoğruFatura ile aynı sağlam ayrıştırma yaklaşımı: önce para simgeleri/boşluklar
        // temizlenir, sonra son ayırıcıya göre ondalık ve binlik işaretleri normalize edilir.
        // Böylece hem 1.234,56 hem 1,234.56 hem de 1234.56 doğru yorumlanır.
        var normalized = text.Trim()
            .Replace("₺", "", StringComparison.OrdinalIgnoreCase)
            .Replace("TL", "", StringComparison.OrdinalIgnoreCase);
        normalized = Regex.Replace(normalized, @"\s+", "");

        var comma = normalized.LastIndexOf(',');
        var dot = normalized.LastIndexOf('.');
        if (comma >= 0 && dot >= 0)
        {
            if (comma > dot)
            {
                normalized = normalized
                    .Replace(".", "", StringComparison.Ordinal)
                    .Replace(",", ".", StringComparison.Ordinal);
            }
            else
            {
                normalized = normalized.Replace(",", "", StringComparison.Ordinal);
            }
        }
        else if (comma >= 0)
        {
            normalized = normalized
                .Replace(".", "", StringComparison.Ordinal)
                .Replace(",", ".", StringComparison.Ordinal);
        }
        else if (dot >= 0)
        {
            var dotCount = normalized.Count(c => c == '.');
            var fractionLength = normalized.Length - dot - 1;
            if (dotCount > 1 || (fractionLength == 3 && dot <= 3))
                normalized = normalized.Replace(".", "", StringComparison.Ordinal);
        }

        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value) || value <= 0m) return false;
        var rounded = decimal.Round(value, 2);
        if (rounded != value) return false;
        // Discard redundant trailing zero scale, never significant digits.
        value = rounded;
        return true;
    }

    public static string Money(decimal value) => value.ToString("N2", Tr) + " TL";
}

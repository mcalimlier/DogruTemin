using System.Text.RegularExpressions;

namespace DogruTemin.Core.Services;

/// <summary>
/// Safely reusable primitive input checks. Workflow/document rules remain in ValidationService.
/// </summary>
public static class InputValidationService
{
    public static bool ValidTaxNo(string? text)
    {
        var clean = TextNormalizer.DigitsOnly(text);
        return clean.All(char.IsDigit) && clean.Length is 10 or 11;
    }

    public static bool ValidPhone(string? text)
    {
        var clean = TextNormalizer.DigitsOnly(text);
        return clean.All(char.IsDigit) && clean.Length is >= 10 and <= 15;
    }

    public static bool ValidEmail(string? text)
    {
        var s = (text ?? string.Empty).Trim();
        if (s.Contains(' ', StringComparison.Ordinal)) return false;
        return Regex.IsMatch(s, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}

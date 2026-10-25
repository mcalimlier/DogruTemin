using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

public static class ProcurementLimits
{
    public const int MaxProducts = 10;
    public const int MaxFirms = 5;

    public static void EnsureCapacity(ProcurementData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Products?.Count > MaxProducts || data.Firms?.Count > MaxFirms)
            throw new InvalidDataException("İşlem en fazla 10 ürün ve 5 firma içerebilir. Fazla satırlar silinmedi; kaynak kayıt korunuyor.");
    }

    public static bool HasMoneyPrecision(decimal value) => decimal.Round(value, 2) == value;
}

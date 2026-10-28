namespace DogruTemin.Core.Services;

public static class ProductUnits
{
    public static bool IsSupported(string? unit) => unit is
        "Adet" or "kg" or "Litre" or "Paket" or "Bidon" or "Koli" or "Takım" or "Çift" or "Metre" or "Miktar";
}
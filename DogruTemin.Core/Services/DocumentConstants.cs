namespace DogruTemin.Core.Services;

/// <summary>
/// Resmî belge şablonlarında kullanıcıdan veri alınmadan sabit kullanılan metinler.
/// </summary>
public static class DocumentConstants
{
    // Teklif Verme Son Tarihi artık kullanıcı alanı değildir.
    // Firma teklif formunda yıl sabit 2026, gün/ay boş şablon olarak basılır.
    public const string OfferDeadlinePlaceholder = "...... / ...... / 2026";
}

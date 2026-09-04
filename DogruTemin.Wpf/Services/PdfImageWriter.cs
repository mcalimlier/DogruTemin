using System.Windows.Documents;

namespace DogruTemin.Wpf.Services;

/// <summary>
/// Eski çağrı noktalarını bozmadan yeni metin/vektör PDF motoruna yönlendiren uyumluluk katmanı.
/// </summary>
public static class PdfImageWriter
{
    public static void Save(FixedDocument document, string path, int dpi = 300)
    {
        _ = dpi; // API geriye dönük uyumluluğu; vektör PDF'de sayfa DPI'ı kullanılmaz.
        PdfVectorWriter.Save(document, path);
    }
}

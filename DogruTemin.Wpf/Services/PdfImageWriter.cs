using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DogruTemin.Wpf.Services;

public static class PdfImageWriter
{
    private sealed record RenderedPage(byte[] Jpeg, int PixelWidth, int PixelHeight, double WidthPoints, double HeightPoints);

    public static void Save(FixedDocument document, string path, int dpi = 300)
    {
        if (document.Pages.Count == 0) throw new InvalidOperationException("PDF oluşturmak için sayfa bulunamadı.");
        var pages = new List<RenderedPage>();
        foreach (PageContent content in document.Pages)
        {
            if (content.Child is null)
                throw new InvalidOperationException("PDF sayfası yüklenemedi.");
            pages.Add(Render(content.Child, dpi));
        }
        var folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(folder)) Directory.CreateDirectory(folder);

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        WriteBytes(fs, new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A, 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A });

        var objectCount = 2 + pages.Count * 3;
        var offsets = new long[objectCount + 1];
        var pageObjectIds = Enumerable.Range(0, pages.Count).Select(i => 3 + i * 3).ToArray();

        WriteObject(fs, 1, offsets, $"<< /Type /Catalog /Pages 2 0 R >>");
        WriteObject(fs, 2, offsets, $"<< /Type /Pages /Count {pages.Count} /Kids [ {string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"))} ] >>");

        for (var i = 0; i < pages.Count; i++)
        {
            var page = pages[i];
            var pageId = 3 + i * 3;
            var imageId = pageId + 1;
            var contentId = pageId + 2;
            var imageName = $"Im{i + 1}";
            var w = F(page.WidthPoints);
            var h = F(page.HeightPoints);

            WriteObject(fs, pageId, offsets,
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {w} {h}] /Resources << /XObject << /{imageName} {imageId} 0 R >> >> /Contents {contentId} 0 R >>");

            offsets[imageId] = fs.Position;
            WriteAscii(fs, $"{imageId} 0 obj\n<< /Type /XObject /Subtype /Image /Width {page.PixelWidth} /Height {page.PixelHeight} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {page.Jpeg.Length} >>\nstream\n");
            WriteBytes(fs, page.Jpeg);
            WriteAscii(fs, "\nendstream\nendobj\n");

            var content = Encoding.ASCII.GetBytes($"q\n{w} 0 0 {h} 0 0 cm\n/{imageName} Do\nQ\n");
            offsets[contentId] = fs.Position;
            WriteAscii(fs, $"{contentId} 0 obj\n<< /Length {content.Length} >>\nstream\n");
            WriteBytes(fs, content);
            WriteAscii(fs, "endstream\nendobj\n");
        }

        var xref = fs.Position;
        WriteAscii(fs, $"xref\n0 {objectCount + 1}\n");
        WriteAscii(fs, "0000000000 65535 f \n");
        for (var i = 1; i <= objectCount; i++) WriteAscii(fs, $"{offsets[i]:0000000000} 00000 n \n");
        WriteAscii(fs, $"trailer\n<< /Size {objectCount + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
    }

    private static RenderedPage Render(FixedPage page, int dpi)
    {
        page.Measure(new Size(page.Width, page.Height));
        page.Arrange(new Rect(new Point(0, 0), new Size(page.Width, page.Height)));
        page.UpdateLayout();
        var pxW = Math.Max(1, (int)Math.Round(page.Width / 96.0 * dpi));
        var pxH = Math.Max(1, (int)Math.Round(page.Height / 96.0 * dpi));
        var bmp = new RenderTargetBitmap(pxW, pxH, dpi, dpi, PixelFormats.Pbgra32);
        bmp.Render(page);
        var encoder = new JpegBitmapEncoder { QualityLevel = 100 };
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return new RenderedPage(ms.ToArray(), pxW, pxH, page.Width * 72.0 / 96.0, page.Height * 72.0 / 96.0);
    }

    private static void WriteObject(Stream fs, int id, long[] offsets, string content)
    {
        offsets[id] = fs.Position;
        WriteAscii(fs, $"{id} 0 obj\n{content}\nendobj\n");
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static void WriteAscii(Stream stream, string text) => WriteBytes(stream, Encoding.ASCII.GetBytes(text));
    private static void WriteBytes(Stream stream, byte[] bytes) => stream.Write(bytes, 0, bytes.Length);
}

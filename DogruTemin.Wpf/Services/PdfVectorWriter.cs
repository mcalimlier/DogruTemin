using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfBrush = System.Windows.Media.Brush;
using WpfImage = System.Windows.Controls.Image;
using WpfLine = System.Windows.Shapes.Line;
using WpfPath = System.Windows.Shapes.Path;
using WpfRectangle = System.Windows.Shapes.Rectangle;
using WpfEllipse = System.Windows.Shapes.Ellipse;

namespace DogruTemin.Wpf.Services;

/// <summary>
/// FixedDocument içeriğini tam sayfa ekran görüntüsüne çevirmeden PDF'e aktarır.
/// Metinler gerçek PDF metni, çizgi/kenarlıklar vektör; yalnız gerçek Image öğeleri bitmap kalır.
/// </summary>
public static class PdfVectorWriter
{
    private const double DipToPoint = 72.0 / 96.0;

    public static void Save(FixedDocument document, string path)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Pages.Count == 0)
            throw new InvalidOperationException("PDF oluşturmak için sayfa bulunamadı.");

        var folder = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(folder))
            Directory.CreateDirectory(folder);

        using var pdf = new PdfDocument();
        pdf.Info.Title = "DoğruTemin";
        pdf.Info.Creator = "DoğruTemin";

        foreach (PageContent content in document.Pages)
        {
            if (content.Child is not FixedPage fixedPage)
                throw new InvalidOperationException("PDF sayfası yüklenemedi.");

            PreparePage(fixedPage);

            var page = pdf.AddPage();
            page.Width = XUnit.FromPoint(ToPoint(fixedPage.Width));
            page.Height = XUnit.FromPoint(ToPoint(fixedPage.Height));

            using var gfx = XGraphics.FromPdfPage(page);
            RenderElement(gfx, fixedPage, fixedPage);
        }

        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            pdf.Save(temporary);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static void Validate(FixedDocument document)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DogruTemin_Check_" + Guid.NewGuid().ToString("N") + ".pdf");
        try { Save(document, path); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void PreparePage(FixedPage page)
    {
        if (double.IsNaN(page.Width) || page.Width <= 0 || double.IsNaN(page.Height) || page.Height <= 0)
            throw new InvalidOperationException("PDF sayfa boyutu geçersiz.");

        page.Measure(new Size(page.Width, page.Height));
        page.Arrange(new Rect(new Point(0, 0), new Size(page.Width, page.Height)));
        page.UpdateLayout();
    }

    private static void RenderElement(XGraphics gfx, FixedPage page, DependencyObject current)
    {
        if (current is UIElement ui && ui.Visibility != Visibility.Visible)
            return;

        if (current is TextBlock textBlock)
        {
            DrawTextBlock(gfx, page, textBlock);
            return;
        }

        if (current is WpfImage image)
        {
            DrawImage(gfx, page, image);
            return;
        }

        if (current is Shape shape)
        {
            DrawShape(gfx, page, shape);
            return;
        }

        if (current is Border border)
            DrawBorder(gfx, page, border);
        else if (current is Panel panel)
            DrawPanelBackground(gfx, page, panel);

        var count = VisualTreeHelper.GetChildrenCount(current);
        for (var i = 0; i < count; i++)
            RenderElement(gfx, page, VisualTreeHelper.GetChild(current, i));
    }

    private static void DrawPanelBackground(XGraphics gfx, FixedPage page, Panel panel)
    {
        if (TryBrush(panel.Background, out var brush) && TryBounds(page, panel, out var rect))
            gfx.DrawRectangle(brush, rect);
    }

    private static void DrawBorder(XGraphics gfx, FixedPage page, Border border)
    {
        if (!TryBounds(page, border, out var rect))
            return;

        if (TryBrush(border.Background, out var background))
            gfx.DrawRectangle(background, rect);

        if (!TryColor(border.BorderBrush, out var color))
            return;

        var t = border.BorderThickness;
        DrawBorderLine(gfx, color, t.Left, rect.Left, rect.Top, rect.Left, rect.Bottom);
        DrawBorderLine(gfx, color, t.Top, rect.Left, rect.Top, rect.Right, rect.Top);
        DrawBorderLine(gfx, color, t.Right, rect.Right, rect.Top, rect.Right, rect.Bottom);
        DrawBorderLine(gfx, color, t.Bottom, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
    }

    private static void DrawBorderLine(XGraphics gfx, XColor color, double thicknessDip, double x1, double y1, double x2, double y2)
    {
        if (thicknessDip <= 0)
            return;
        gfx.DrawLine(new XPen(color, Math.Max(0.1, ToPoint(thicknessDip))), x1, y1, x2, y2);
    }

    private static void DrawShape(XGraphics gfx, FixedPage page, Shape shape)
    {
        if (!TryBounds(page, shape, out var rect))
            return;

        var pen = TryColor(shape.Stroke, out var strokeColor)
            ? new XPen(strokeColor, Math.Max(0.1, ToPoint(shape.StrokeThickness)))
            : null;
        var hasFill = TryBrush(shape.Fill, out var fill);

        switch (shape)
        {
            case WpfLine line:
            {
                if (pen is null) return;
                var transform = line.TransformToAncestor(page);
                var p1 = transform.Transform(new Point(line.X1, line.Y1));
                var p2 = transform.Transform(new Point(line.X2, line.Y2));
                gfx.DrawLine(pen, ToPoint(p1.X), ToPoint(p1.Y), ToPoint(p2.X), ToPoint(p2.Y));
                break;
            }
            case WpfRectangle:
                if (hasFill && pen is not null) gfx.DrawRectangle(pen, fill, rect);
                else if (hasFill) gfx.DrawRectangle(fill, rect);
                else if (pen is not null) gfx.DrawRectangle(pen, rect);
                break;
            case WpfEllipse:
                if (hasFill && pen is not null) gfx.DrawEllipse(pen, fill, rect);
                else if (hasFill) gfx.DrawEllipse(fill, rect);
                else if (pen is not null) gfx.DrawEllipse(pen, rect);
                break;
            case WpfPath path:
                DrawFlattenedPath(gfx, page, path, pen, hasFill ? fill : null);
                break;
        }
    }

    private static void DrawFlattenedPath(XGraphics gfx, FixedPage page, WpfPath path, XPen? pen, XBrush? brush)
    {
        if (pen is null && brush is null)
            return;

        var geometry = path.RenderedGeometry?.GetFlattenedPathGeometry();
        if (geometry is null)
            return;

        var transform = path.TransformToAncestor(page);
        foreach (var figure in geometry.Figures)
        {
            var points = new List<XPoint>();
            var start = transform.Transform(figure.StartPoint);
            points.Add(new XPoint(ToPoint(start.X), ToPoint(start.Y)));

            foreach (var segment in figure.Segments)
            {
                if (segment is PolyLineSegment poly)
                {
                    foreach (var point in poly.Points)
                    {
                        var p = transform.Transform(point);
                        points.Add(new XPoint(ToPoint(p.X), ToPoint(p.Y)));
                    }
                }
                else if (segment is LineSegment line)
                {
                    var p = transform.Transform(line.Point);
                    points.Add(new XPoint(ToPoint(p.X), ToPoint(p.Y)));
                }
            }

            if (points.Count < 2)
                continue;

            if (brush is not null && figure.IsClosed)
                gfx.DrawPolygon(pen, brush, points.ToArray(), XFillMode.Winding);
            else if (pen is not null)
                gfx.DrawLines(pen, points.ToArray());
        }
    }

    private static void DrawTextBlock(XGraphics gfx, FixedPage page, TextBlock block)
    {
        var text = block.Text ?? string.Empty;
        if (text.Length == 0) return;
        if (!TryBounds(page, block, out var rect))
            throw TextOverflow(text);

        // Respect both page and cell bounds; a child can measure larger than its cell.
        var bottom = Math.Min(rect.Bottom, ToPoint(page.Height));
        var right = Math.Min(rect.Right, ToPoint(page.Width));
        for (DependencyObject? parent = VisualTreeHelper.GetParent(block); parent is not null && parent != page; parent = VisualTreeHelper.GetParent(parent))
            if (parent is Border cell && TryBounds(page, cell, out var cellRect))
            {
                bottom = Math.Min(bottom, cellRect.Bottom - ToPoint(cell.Padding.Bottom));
                right = Math.Min(right, cellRect.Right - ToPoint(cell.Padding.Right));
            }
        rect = new XRect(rect.Left, rect.Top, Math.Max(0, right - rect.Left), Math.Max(0, bottom - rect.Top));
        // A PDF-only font adjustment must never conceal clipping in the WPF
        // document that will be sent to a physical printer.
        if (ToPoint(block.ActualHeight) > rect.Height + 1.5 || ToPoint(block.ActualWidth) > rect.Width + 1.5)
            throw TextOverflow(text);

        if (TryBrush(block.Background, out var background))
            gfx.DrawRectangle(background, rect);

        if (!TryColor(block.Foreground, out var color))
            color = XColors.Black;

        var family = NormalizeFontFamily(block.FontFamily?.Source);
        var style = FontStyleFor(block);
        var fontSize = Math.Max(1.0, ToPoint(block.FontSize));
        var font = CreateFont(family, fontSize, style);
        var brush = new XSolidBrush(color);
        var minimum = Math.Min(fontSize, ToPoint(7));
        List<string> lines;
        double lineHeight;
        while (true)
        {
            font = CreateFont(family, fontSize, style);
            lineHeight = block.LineHeight > 0 && !double.IsNaN(block.LineHeight)
                ? ToPoint(block.LineHeight) * fontSize / Math.Max(1, ToPoint(block.FontSize))
                : Math.Max(fontSize * 1.18, font.GetHeight());
            lines = block.TextWrapping == TextWrapping.NoWrap
                ? text.Replace("\r", string.Empty).Split('\n').ToList()
                : WrapText(gfx, text, font, Math.Max(1, rect.Width));
            var fits = lines.Count * lineHeight <= rect.Height + 0.5
                && lines.All(line => gfx.MeasureString(line, font).Width <= rect.Width + 0.5)
                && rect.Top >= -0.5 && rect.Left >= -0.5;
            if (fits) break;
            if (fontSize <= minimum + 0.01)
            {
                var error = TextOverflow(text);
                error.Data["Layout"] = $"rect={rect}; lineHeight={lineHeight}; lines={lines.Count}; font={fontSize}";
                throw error;
            }
            fontSize = Math.Max(minimum, fontSize - 0.25);
        }

        var y = rect.Top;
        foreach (var line in lines)
        {
            DrawTextLine(gfx, line, font, brush, new XRect(rect.Left, y, rect.Width, lineHeight), block.TextAlignment);
            y += lineHeight;
        }
    }

    private static InvalidOperationException TextOverflow(string text) => new(
        "Belge alanına metin sığmıyor: '" + (text.Length > 90 ? text[..90] + "…" : text) + "'. Metni kısaltınız veya belge düzenini kontrol ediniz. Eksik PDF oluşturulmadı.");

    private static XFont CreateFont(string family, double size, XFontStyleEx style)
    {
        try
        {
            return new XFont(family, size, style);
        }
        catch
        {
            return new XFont("Arial", size, style);
        }
    }

    private static XFontStyleEx FontStyleFor(TextBlock block)
    {
        var bold = block.FontWeight.ToOpenTypeWeight() >= 600;
        var italic = block.FontStyle == FontStyles.Italic || block.FontStyle == FontStyles.Oblique;
        if (bold && italic) return XFontStyleEx.BoldItalic;
        if (bold) return XFontStyleEx.Bold;
        if (italic) return XFontStyleEx.Italic;
        return XFontStyleEx.Regular;
    }

    private static string NormalizeFontFamily(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)) return "Arial";
        var first = source.Split(',')[0].Trim();
        return first.Length == 0 ? "Arial" : first;
    }

    private static List<string> WrapText(XGraphics gfx, string text, XFont font, double width)
    {
        var result = new List<string>();
        foreach (var paragraph in text.Replace("\r", string.Empty).Split('\n'))
        {
            if (paragraph.Length == 0)
            {
                result.Add(string.Empty);
                continue;
            }

            var words = paragraph.Split(' ', StringSplitOptions.None);
            var line = string.Empty;
            foreach (var word in words)
            {
                if (gfx.MeasureString(word, font).Width > width)
                {
                    if (line.Length > 0) { result.Add(line); line = string.Empty; }
                    var elements = System.Globalization.StringInfo.GetTextElementEnumerator(word);
                    while (elements.MoveNext())
                    {
                        var part = elements.GetTextElement();
                        if (line.Length > 0 && gfx.MeasureString(line + part, font).Width > width)
                        { result.Add(line); line = string.Empty; }
                        line += part;
                    }
                    continue;
                }
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length == 0 || gfx.MeasureString(candidate, font).Width <= width)
                {
                    line = candidate;
                    continue;
                }

                result.Add(line);
                line = word;
            }
            result.Add(line);
        }
        return result;
    }

    private static void DrawTextLine(XGraphics gfx, string line, XFont font, XBrush brush, XRect rect, TextAlignment alignment)
    {
        if (alignment == TextAlignment.Justify)
        {
            DrawJustifiedLine(gfx, line, font, brush, rect);
            return;
        }

        var format = new XStringFormat
        {
            Alignment = alignment switch
            {
                TextAlignment.Center => XStringAlignment.Center,
                TextAlignment.Right => XStringAlignment.Far,
                _ => XStringAlignment.Near
            },
            LineAlignment = XLineAlignment.Near
        };
        gfx.DrawString(line, font, brush, rect, format);
    }

    private static void DrawJustifiedLine(XGraphics gfx, string line, XFont font, XBrush brush, XRect rect)
    {
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2)
        {
            gfx.DrawString(line, font, brush, rect, XStringFormats.TopLeft);
            return;
        }

        var wordsWidth = words.Sum(word => gfx.MeasureString(word, font).Width);
        var gap = (rect.Width - wordsWidth) / (words.Length - 1);
        if (gap <= 0 || gap > font.Size * 2.5)
        {
            gfx.DrawString(line, font, brush, rect, XStringFormats.TopLeft);
            return;
        }

        var x = rect.Left;
        foreach (var word in words)
        {
            var width = gfx.MeasureString(word, font).Width;
            gfx.DrawString(word, font, brush, new XRect(x, rect.Top, width + 1, rect.Height), XStringFormats.TopLeft);
            x += width + gap;
        }
    }

    private static void DrawImage(XGraphics gfx, FixedPage page, WpfImage image)
    {
        if (image.Source is not BitmapSource bitmap || !TryBounds(page, image, out var rect))
            return;

        using var stream = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);
        stream.Position = 0;
        using var xImage = XImage.FromStream(stream);
        gfx.DrawImage(xImage, rect);
    }

    private static bool TryBounds(FixedPage page, FrameworkElement element, out XRect rect)
    {
        rect = default;
        if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
            return false;

        try
        {
            var transform = element.TransformToAncestor(page);
            var width = double.IsNaN(element.Width) ? element.RenderSize.Width : Math.Min(element.Width, element.RenderSize.Width);
            var height = double.IsNaN(element.Height) ? element.RenderSize.Height : Math.Min(element.Height, element.RenderSize.Height);
            var bounds = transform.TransformBounds(new Rect(0, 0, width, height));
            // Centered auto-width text can use its allocated cell, rather than its
            // WPF glyph width (PDF font metrics need not be pixel-identical).
            if (element is TextBlock && double.IsNaN(element.Width) && VisualTreeHelper.GetParent(element) is FrameworkElement parent)
            {
                var slot = System.Windows.Controls.Primitives.LayoutInformation.GetLayoutSlot(element);
                var available = slot.Width - element.Margin.Left - element.Margin.Right;
                if (double.IsFinite(available) && available > width)
                {
                    var cell = parent.TransformToAncestor(page).TransformBounds(new Rect(slot.Left + element.Margin.Left, slot.Top, available, Math.Max(1, height)));
                    bounds = new Rect(cell.Left, bounds.Top, cell.Width, bounds.Height);
                }
            }
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return false;
            rect = new XRect(ToPoint(bounds.Left), ToPoint(bounds.Top), ToPoint(bounds.Width), ToPoint(bounds.Height));
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool TryBrush(WpfBrush? brush, out XBrush xBrush)
    {
        if (TryColor(brush, out var color))
        {
            xBrush = new XSolidBrush(color);
            return true;
        }
        xBrush = XBrushes.Transparent;
        return false;
    }

    private static bool TryColor(WpfBrush? brush, out XColor color)
    {
        if (brush is SolidColorBrush solid)
        {
            var c = solid.Color;
            var alpha = (byte)Math.Clamp((int)Math.Round(c.A * solid.Opacity), 0, 255);
            color = XColor.FromArgb(alpha, c.R, c.G, c.B);
            return true;
        }
        color = XColors.Transparent;
        return false;
    }

    private static double ToPoint(double dip) => dip * DipToPoint;
}

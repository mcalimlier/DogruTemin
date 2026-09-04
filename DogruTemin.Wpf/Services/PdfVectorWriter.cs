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
using WpfColor = System.Windows.Media.Color;
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

        var folder = Path.GetDirectoryName(path);
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

        pdf.Save(path);
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
        if (text.Length == 0 || !TryBounds(page, block, out var rect))
            return;

        if (TryBrush(block.Background, out var background))
            gfx.DrawRectangle(background, rect);

        if (!TryColor(block.Foreground, out var color))
            color = XColors.Black;

        var family = NormalizeFontFamily(block.FontFamily?.Source);
        var style = FontStyleFor(block);
        var fontSize = Math.Max(1.0, ToPoint(block.FontSize));
        var font = CreateFont(family, fontSize, style);
        var brush = new XSolidBrush(color);
        var lineHeight = block.LineHeight > 0 && !double.IsNaN(block.LineHeight)
            ? ToPoint(block.LineHeight)
            : Math.Max(fontSize * 1.18, font.GetHeight());

        var lines = block.TextWrapping == TextWrapping.NoWrap
            ? text.Replace("\r", string.Empty).Split('\n').ToList()
            : WrapText(gfx, text, font, Math.Max(1, rect.Width));

        var y = rect.Top;
        foreach (var line in lines)
        {
            if (y + lineHeight > rect.Bottom + 0.5)
                break;

            DrawTextLine(gfx, line, font, brush, new XRect(rect.Left, y, rect.Width, lineHeight), block.TextAlignment);
            y += lineHeight;
        }
    }

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
            var bounds = transform.TransformBounds(new Rect(new Point(0, 0), element.RenderSize));
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

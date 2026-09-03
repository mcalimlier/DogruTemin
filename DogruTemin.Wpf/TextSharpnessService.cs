using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace DogruTemin.Wpf;

/// <summary>
/// DoğruFatura'da kullanılan metin keskinliği kuralının DoğruTemin karşılığıdır.
/// Metin taşıyan ebeveynlere DropShadow uygulanmasını engeller; Viewbox/DPI altında
/// bitmap yumuşamasını azaltır ve ekran odaklı ClearType/Display metin işleme kullanır.
/// </summary>
internal static class TextSharpnessService
{
    public static void Apply(Window window)
    {
        window.Loaded += (_, _) =>
        {
            NormalizeTree(window);
            window.GotKeyboardFocus += (_, e) =>
            {
                if (e.OriginalSource is DependencyObject source)
                    NormalizeAncestorChain(source, window);
            };
        };
    }

    private static void NormalizeTree(DependencyObject root)
    {
        if (root is FrameworkElement element)
        {
            ApplyTextRendering(element);
            if (element.Effect is DropShadowEffect && IsTextBearing(element))
                element.Effect = null;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
            NormalizeTree(VisualTreeHelper.GetChild(root, index));
    }

    private static void NormalizeAncestorChain(DependencyObject source, DependencyObject stopAt)
    {
        DependencyObject? current = source;
        while (current is not null)
        {
            if (current is FrameworkElement element)
            {
                ApplyTextRendering(element);
                if (element.Effect is DropShadowEffect && IsTextBearing(element))
                    element.Effect = null;
            }

            if (ReferenceEquals(current, stopAt)) break;
            current = ParentOf(current);
        }
    }

    private static void ApplyTextRendering(FrameworkElement element)
    {
        element.SnapsToDevicePixels = true;
        element.UseLayoutRounding = true;
        TextOptions.SetTextFormattingMode(element, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(element, TextRenderingMode.ClearType);
    }

    private static bool IsTextBearing(FrameworkElement element) =>
        element is TextBlock or TextBox or ComboBox or DatePicker or Button or Label or LargeDatePicker
        || ContainsText(element);

    private static bool ContainsText(DependencyObject root)
    {
        if (root is TextBlock or TextBox or ComboBox or DatePicker or Button or Label or LargeDatePicker)
            return true;

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
            if (ContainsText(VisualTreeHelper.GetChild(root, index))) return true;
        return false;
    }

    private static DependencyObject? ParentOf(DependencyObject child)
    {
        if (child is Visual or System.Windows.Media.Media3D.Visual3D)
            return VisualTreeHelper.GetParent(child);
        return LogicalTreeHelper.GetParent(child);
    }
}

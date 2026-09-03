using System.Windows;
using System.Windows.Media;

namespace DogruTemin.Wpf;

internal static class UiTree
{
    public static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    public static T? FindAncestor<T>(DependencyObject child) where T : DependencyObject
    {
        var current = ParentOf(child);
        while (current is not null)
        {
            if (current is T match) return match;
            current = ParentOf(current);
        }
        return null;
    }

    public static bool IsInsideNamedAncestor(DependencyObject child, string name)
    {
        DependencyObject? current = child;
        while (current is not null)
        {
            if (current is FrameworkElement element && element.Name == name) return true;
            current = ParentOf(current);
        }
        return false;
    }

    public static bool IsVisible(Window window, string name) =>
        window.FindName(name) is FrameworkElement element && element.Visibility == Visibility.Visible;

    public static DependencyObject? ParentOf(DependencyObject child)
    {
        if (child is Visual or System.Windows.Media.Media3D.Visual3D)
            return VisualTreeHelper.GetParent(child);
        return LogicalTreeHelper.GetParent(child);
    }
}

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

namespace DogruTemin.Wpf;

public readonly record struct WindowLayoutProfile(double WorkWidthDip, double WorkHeightDip,
    double WindowWidthDip, double WindowHeightDip, double ContentScale, bool RequiresDownscale);

public static class ResponsiveLayoutService
{
    public const double DesignWidth = 1450;
    public const double DesignHeight = 945;

    public static WindowLayoutProfile Calculate(double workWidthDip, double workHeightDip)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workWidthDip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workHeightDip);
        return new(workWidthDip, workHeightDip, Math.Min(DesignWidth, workWidthDip * .96),
            Math.Min(DesignHeight, workHeightDip * .94), 1, false);
    }

    public static WindowLayoutProfile CalculateForPixels(double width, double height, double dpiPercent)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dpiPercent);
        return Calculate(width / (dpiPercent / 100), height / (dpiPercent / 100));
    }

    public static Size CalculatePreference(double workWidth, double workHeight, string preference)
    {
        var automatic = Calculate(workWidth, workHeight);
        var requested = preference switch
        {
            "1280x720" => new Size(1280, 720),
            "1366x768" => new Size(1366, 768),
            "1920x1080" => new Size(1920, 1080),
            _ => new Size(automatic.WindowWidthDip, automatic.WindowHeightDip)
        };
        return new(Math.Min(requested.Width, workWidth * .96), Math.Min(requested.Height, workHeight * .94));
    }

    public static void ApplyPreference(MainWindow window, string preference)
    {
        var work = CurrentWorkArea(window);
        var size = CalculatePreference(work.Width, work.Height, preference);
        window.WindowState = WindowState.Normal;
        window.MinWidth = Math.Min(760, work.Width * .96);
        window.MinHeight = Math.Min(520, work.Height * .94);
        window.Width = size.Width;
        window.Height = size.Height;
        window.Left = work.Left + (work.Width - size.Width) / 2;
        window.Top = work.Top + (work.Height - size.Height) / 2;
        if (preference == "Maximized") window.WindowState = WindowState.Maximized;
        UpdateLayout(window);
    }

    public static void Apply(MainWindow window)
    {
        window.ResizeMode = ResizeMode.CanResizeWithGrip;
        if (window.FindName("WorkspaceScroll") is FrameworkElement viewport)
            viewport.SizeChanged += (_, _) => UpdateWorkspace(window);
        var pairs = new List<AdaptivePair>();
        var cards = new List<(UniformGrid Grid, int Columns)>();
        void Reflow()
        {
            UpdateLayout(window);
            ReflowPairs(window, pairs);
            foreach (var card in cards) card.Grid.Columns = window.ActualWidth < 1250 ? 2 : card.Columns;
        }
        window.SizeChanged += (_, _) => Reflow();
        window.Loaded += (_, _) =>
        {
            if (window.FindName("WorkspaceLayout") is Grid workspace)
            {
                foreach (var grid in LogicalGrids(workspace))
                    if (grid.RowDefinitions.Count == 0 && grid.ColumnDefinitions.Count == 3
                        && grid.ColumnDefinitions[0].Width.IsStar && grid.ColumnDefinitions[2].Width.IsStar
                        && grid.Children.Count is 2 or 3 && grid.Children.Cast<UIElement>().All(c => Grid.GetColumnSpan(c) == 1)
                        && grid.Children.Cast<UIElement>().Count(c => Grid.GetColumn(c) == 0) == 1
                        && grid.Children.Cast<UIElement>().Count(c => Grid.GetColumn(c) == 2) == 1)
                        pairs.Add(new(grid, grid.ColumnDefinitions.Select(c => c.Width).ToArray(),
                            grid.Children.Cast<UIElement>().Single(c => Grid.GetColumn(c) == 2),
                            grid.Children.Cast<UIElement>().SingleOrDefault(c => Grid.GetColumn(c) == 1)));
                foreach (var grid in LogicalNodes(workspace).OfType<UniformGrid>().Where(g => g.Columns >= 4))
                    cards.Add((grid, grid.Columns));
            }
            Reflow();
        };
    }

    private sealed record AdaptivePair(Grid Grid, GridLength[] Columns, UIElement End, UIElement? Separator);

    private static IEnumerable<Grid> LogicalGrids(DependencyObject root)
        => LogicalNodes(root).OfType<Grid>();

    internal static IEnumerable<DependencyObject> LogicalNodes(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var nested in LogicalNodes(child)) yield return nested;
        }
    }

    private static void ReflowPairs(MainWindow window, List<AdaptivePair> pairs)
    {
        var stack = window.ActualWidth < 1250;
        foreach (var pair in pairs)
        {
            var grid = pair.Grid;
            if (stack == (grid.RowDefinitions.Count == 3)) continue;
            grid.RowDefinitions.Clear();
            if (stack)
            {
                grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new() { Height = new GridLength(14) });
                grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
            }
            grid.ColumnDefinitions[0].Width = stack ? new GridLength(1, GridUnitType.Star) : pair.Columns[0];
            grid.ColumnDefinitions[1].Width = stack ? new GridLength(0) : pair.Columns[1];
            grid.ColumnDefinitions[2].Width = stack ? new GridLength(0) : pair.Columns[2];
            Grid.SetColumn(pair.End, stack ? 0 : 2);
            Grid.SetRow(pair.End, stack ? 2 : 0);
            if (pair.Separator is not null) pair.Separator.Visibility = stack ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public static void UpdateWorkspace(MainWindow window)
    {
        if (window.FindName("WorkspaceLayout") is FrameworkElement workspace)
        {
            // Measure the client viewport, not Window.Width: Windows resize borders
            // and DPI rounding can make those differ even for a borderless window.
            var host = window.FindName("WorkspaceScroll") as ScrollViewer;
            var rawWidth = host?.ActualWidth > 0 ? host.ActualWidth : Math.Max(0, window.ActualWidth - 280);
            var rawHeight = host?.ActualHeight > 0 ? host.ActualHeight : Math.Max(0, window.ActualHeight - 136);
            var minimumHeight = UiTree.IsVisible(window, "ArchiveView") ? (window.ActualWidth < 1250 ? 640 : 540)
                : UiTree.IsVisible(window, "ControlView") ? 580 : 0;
            var availableWidth = rawWidth;
            var availableHeight = rawHeight;
            for (var pass = 0; pass < 3; pass++)
            {
                availableWidth = Math.Max(0, rawWidth - (minimumHeight > availableHeight ? SystemParameters.VerticalScrollBarWidth : 0));
                availableHeight = Math.Max(0, rawHeight - (workspace.MinWidth > availableWidth ? SystemParameters.HorizontalScrollBarHeight : 0));
            }
            workspace.Width = Math.Max(workspace.MinWidth, availableWidth);
            workspace.Height = Math.Max(minimumHeight, availableHeight);
            if (host is not null)
            {
                // Form pages own their vertical scrolling. Only the table panels
                // need an outer vertical scrollbar; avoid nested scrollbar feedback.
                host.VerticalScrollBarVisibility = minimumHeight > 0 ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
                host.HorizontalScrollBarVisibility = workspace.MinWidth > availableWidth ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
            }
        }
    }

    private static void UpdateLayout(MainWindow window)
    {
        UpdateWorkspace(window);
        var compact = window.ActualWidth < 1250;
        if (window.FindName("HeaderSaveStatus") is FrameworkElement status)
            status.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        if (window.FindName("HeaderStatusColumn") is ColumnDefinition statusColumn)
            statusColumn.Width = new GridLength(compact ? 0 : 220);
        var narrow = window.ActualWidth < 1000;
        if (window.FindName("HeaderReadiness") is FrameworkElement readiness)
            readiness.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
        if (window.FindName("HeaderReadinessColumn") is ColumnDefinition readinessColumn)
            readinessColumn.Width = new GridLength(narrow ? 0 : 122);
        if (window.FindName("HeaderActiveOperation") is FrameworkElement operation)
            operation.MinWidth = narrow ? 0 : 150;
    }

    private static Rect CurrentWorkArea(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return SystemParameters.WorkArea;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(handle, 2), ref info)) return SystemParameters.WorkArea;
        var dpi = VisualTreeHelper.GetDpi(window);
        return new Rect(info.Work.Left / dpi.DpiScaleX, info.Work.Top / dpi.DpiScaleY,
            (info.Work.Right - info.Work.Left) / dpi.DpiScaleX, (info.Work.Bottom - info.Work.Top) / dpi.DpiScaleY);
    }

    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
}

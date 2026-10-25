using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DogruTemin.Wpf;

namespace DogruTemin.Tests;

internal static class PanelReviewAcceptance
{
    internal static void Review(MainWindow window)
    {
        var folder = Environment.GetEnvironmentVariable("DOGRU_TEMIN_PANEL_AUDIT");
        if (!string.IsNullOrWhiteSpace(folder)) Directory.CreateDirectory(folder);
        var findings = new List<string>();
        Assert.IsType<Grid>(window.FindName("AppScaleViewbox"));
        Assert.Null(window.FindName("NavDataEntry"));
        Assert.Equal(Visibility.Visible, ((FrameworkElement)window.FindName("DataEntrySubmenu")).Visibility);
        foreach (var view in new[] { "Home", "Institution", "General", "Firms", "ApprovalDocument", "Dates", "Output", "Control", "Statistics", "Archive", "Settings", "AboutSafety" })
        foreach (var size in new[] { new Size(1024, 600), new Size(1366, 768) })
        {
            typeof(MainWindow).GetMethod("ShowView", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { view });
            typeof(MainWindow).GetMethod("SynchronizeOutputSubpageWithCurrentView", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
            window.Width = size.Width; window.Height = size.Height;
            window.UpdateLayout();
            window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            if (view is "Settings" or "AboutSafety")
            {
                var host = (ScrollViewer)window.FindName("WorkspaceScroll");
                Assert.True(host.ScrollableWidth < 1,
                    $"{view} {size}: yatay kaydırma={host.ScrollableWidth}; viewport={host.ViewportWidth}; extent={host.ExtentWidth}");
            }
            Assert.Equal(1, ((FrameworkElement)window.FindName("WorkspaceLayout")).LayoutTransform.Value.M11);
            if (view == "Firms")
                Assert.Equal(size.Width < 1250 ? 2 : 0, Grid.GetRow((FrameworkElement)window.FindName("OffersView")));
            if (size.Width == 1366)
            {
                var navigation = (ScrollViewer)window.FindName("NavigationScroll");
                Assert.True(navigation.ScrollableHeight < 1, "Normal pencerede menü tamamen görünmeli.");
                var bottom = (FrameworkElement)window.FindName("BottomNavigation");
                var bounds = bottom.TransformToAncestor(navigation).TransformBounds(new Rect(bottom.RenderSize));
                Assert.InRange(navigation.ActualHeight - bounds.Bottom, -1, 2);
            }
            foreach (var button in UiTree.Descendants<Button>(window).Where(b => b.IsVisible && b.Content is string && b.ActualWidth > 0))
            {
                var text = UiTree.Descendants<TextBlock>(button).FirstOrDefault();
                if (text is null) continue;
                var bounds = text.TransformToAncestor(button).TransformBounds(new Rect(text.RenderSize));
                if (bounds.Left < -1 || bounds.Right > button.ActualWidth + 1 || bounds.Top < -1 || bounds.Bottom > button.ActualHeight + 1)
                    findings.Add($"{view} {size}: {button.Content}; button={button.ActualWidth:0}x{button.ActualHeight:0}; text={bounds}");
            }
            if (view is "General" or "Firms")
            {
                var grid = (DataGrid)window.FindName(view == "General" ? "ProductGrid" : "OfferGrid");
                var last = grid.Items[grid.Items.Count - 1];
                grid.ScrollIntoView(last);
                window.UpdateLayout();
                window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                var row = Assert.IsType<DataGridRow>(grid.ItemContainerGenerator.ContainerFromItem(last));
                Assert.True(row.TransformToAncestor(grid).TransformBounds(new Rect(row.RenderSize)).Bottom <= grid.ActualHeight + 1, "10. satır erişilemiyor: " + view);
            }
            if (string.IsNullOrWhiteSpace(folder)) continue;
            var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            var name = size.Width == 1366 ? view : view + "-1024x600";
            using var stream = File.Create(Path.Combine(folder, name + ".png")); encoder.Save(stream);
        }
        if (!string.IsNullOrWhiteSpace(folder)) File.WriteAllLines(Path.Combine(folder, "button-clipping.txt"), findings.Distinct());
        Assert.Empty(findings);
    }
}

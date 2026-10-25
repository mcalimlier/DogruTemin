using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using DogruTemin.Core.Services;
using DogruTemin.Wpf;

namespace DogruTemin.Tests;

[CollectionDefinition("WPF navigation", DisableParallelization = true)]
public sealed class WpfNavigationGroup;

[Collection("WPF navigation")]
public sealed class OfferNavigationTests
{
    [Fact]
    public void FirmOffersNavigationSelectsExistingFirmAndLoadsProductRows()
    {
        Exception? failure = null;
        var tempRoot = Path.Combine(Path.GetTempPath(), "DogruTemin_OfferNavigation_" + Guid.NewGuid().ToString("N"));
        var thread = new Thread(() =>
        {
            App? app = null;
            MainWindow? window = null;
            try
            {
                app = new App();
                app.InitializeComponent();
                var storage = new StorageService(tempRoot);
                window = new MainWindow(storage);
                window.Show();
                window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                var firm = new DogruTemin.Wpf.Models.Firm { Id = "firm-1", Name = "Örnek Firma" };
                window.Firms.Add(firm);
                window.Products.Add(new DogruTemin.Wpf.Models.ProductItem { Id = "product-1", No = 1, Name = "Örnek Ürün", Quantity = "1", Unit = "Adet" });

                var handler = typeof(MainWindow).GetMethod("NavFirmsOffers_Click", BindingFlags.Instance | BindingFlags.NonPublic)!;
                handler.Invoke(window, new object[] { window, new RoutedEventArgs() });

                var selectorField = typeof(MainWindow).GetField("OfferFirmSelector", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
                var selector = Assert.IsType<ComboBox>(selectorField.GetValue(window));
                Assert.Same(firm, selector.SelectedItem);
                var itemTemplate = Assert.IsType<DataTemplate>(selector.ItemTemplate);
                var itemText = Assert.IsType<TextBlock>(itemTemplate.LoadContent());
                Assert.Equal("Name", System.Windows.Data.BindingOperations.GetBinding(itemText, TextBlock.TextProperty)?.Path.Path);
                Assert.Single(window.OfferRows);
                Assert.Equal("Örnek Ürün", window.OfferRows[0].ProductName);
                VerifyTenProductSaveAndResponsiveArchive(window, storage);
                PanelReviewAcceptance.Review(window);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                if (window is not null)
                {
                    typeof(MainWindow).GetField("_closeApproved", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
                    window.Close();
                }
                app?.Shutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        try
        {
            if (failure is not null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static void VerifyTenProductSaveAndResponsiveArchive(MainWindow window, StorageService storage)
    {
        void Invoke(string name, params object[] arguments) => typeof(MainWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, arguments);
        var data = ProcurementBoundaryTests.Scenario(10);
        foreach (var offer in data.Firms[0].Offers) { offer.ApproximateUnitPrice = 19.53m; offer.FinalUnitPrice = 19.53m; }
        storage.Save(data);
        typeof(MainWindow).GetField("_suppressDirtyTracking", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
        Invoke("ApplyDataToUi", data);
        typeof(MainWindow).GetField("_suppressDirtyTracking", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, false);
        Invoke("SetSavedStatus", data.LastSavedAt!);
        Invoke("NavFirmsOffers_Click", window, new RoutedEventArgs());
        Assert.Equal(10, window.OfferRows.Count);
        Assert.Equal("10 / 10 ürün", ((TextBlock)window.FindName("ProductCountText")).Text);
        Assert.Equal("19,53", window.OfferRows[0].FinalPrice);
        window.OfferRows[0].FinalPrice = "19,5300";
        Invoke("SaveOffer_Click", window, new RoutedEventArgs());
        Assert.Equal(19.53m, storage.LoadRecord(data.RecordId, out _)!.Firms[0].Offers[0].FinalUnitPrice);
        Assert.Equal("19,53", window.OfferRows[0].FinalPrice);
        var status = (TextBlock)window.FindName("OfferEditorStatusTitle");
        var flags = string.Join("; ", new[] { "_hasUnsavedChanges", "_offerEditorDirty", "_firmEditorDirty", "_productEditorDirty" }.Select(name => name + "=" + typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)));
        Assert.True(status.Text == "TEKLİF KALICI KAYDEDİLDİ", status.Text + " " + flags + " status=" + ((TextBlock)window.FindName("SaveStatusText")).Text);
        Invoke("RefreshArchiveRecords");
        Invoke("ShowView", "Archive");
        var grid = (DataGrid)window.FindName("ArchiveGrid");
        Assert.Equal(10, window.ArchiveRecords.Single().ProductCount);
        Assert.Contains(grid.Columns, c => (string)c.Header == "İŞLEMLER");
        var amountColumn = grid.Columns.Single(c => (string)c.Header == "NİHAİ TUTAR");
        var sortingRows = new System.Collections.ObjectModel.ObservableCollection<DogruTemin.Wpf.Models.ArchiveRecordRow>
        {
            new() { RecordId = "ten", FinalTotal = "10,00 TL", FinalTotalValue = 10m },
            new() { RecordId = "two", FinalTotal = "2,00 TL", FinalTotalValue = 2m }
        };
        var sortingView = System.Windows.Data.CollectionViewSource.GetDefaultView(sortingRows);
        sortingView.SortDescriptions.Add(new System.ComponentModel.SortDescription(amountColumn.SortMemberPath, System.ComponentModel.ListSortDirection.Ascending));
        Assert.Equal("two", sortingView.Cast<DogruTemin.Wpf.Models.ArchiveRecordRow>().First().RecordId);
        foreach (var size in new[] { new Size(1024, 600), new Size(1280, 720), new Size(1366, 768), new Size(1920, 1080) })
        {
            window.Width = size.Width;
            window.Height = size.Height;
            window.UpdateLayout();
            window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            Assert.True(grid.ActualHeight > 0, $"Arşiv tablosu görünmüyor: {size}; height={grid.ActualHeight}; window={window.ActualWidth}x{window.ActualHeight}");
            var template = (DataGridTemplateColumn)grid.Columns.Last();
            var buttons = Assert.IsType<StackPanel>(template.CellTemplate.LoadContent()).Children.OfType<Button>().ToList();
            Assert.Equal(3, buttons.Count);
            grid.ScrollIntoView(grid.Items[0], template);
            window.UpdateLayout();
            window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
            var actionContent = template.GetCellContent(grid.Items[0]);
            Assert.NotNull(actionContent);
            var actionPanel = actionContent as StackPanel ?? UiTree.Descendants<StackPanel>(actionContent).First();
            var needed = actionPanel.Children.OfType<Button>().Sum(button => button.ActualWidth + button.Margin.Left + button.Margin.Right);
            Assert.True(needed <= actionContent.ActualWidth + 1, $"Satır işlemleri sığmıyor: {needed} / {actionContent.ActualWidth}; column={template.ActualWidth}; grid={grid.ActualWidth}x{grid.ActualHeight}; size={size}");
            foreach (var button in actionPanel.Children.OfType<Button>())
            {
                var caption = UiTree.Descendants<TextBlock>(button).First();
                var bounds = caption.TransformToAncestor(button).TransformBounds(new Rect(caption.RenderSize));
                Assert.True(bounds.Left >= 0 && bounds.Right <= button.ActualWidth + 1, $"Düğme yazısı kesiliyor: {button.Content}");
            }
            var root = (FrameworkElement)window.Content;
            Assert.True(root.ActualWidth <= window.ActualWidth + 1, $"Kök yerleşim pencereye sığmıyor: {root.ActualWidth} / {window.ActualWidth}");
            var acceptance = Environment.GetEnvironmentVariable("DOGRU_TEMIN_ACCEPTANCE_DIR");
            if (!string.IsNullOrWhiteSpace(acceptance))
            {
                Directory.CreateDirectory(acceptance);
                var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                bitmap.Render(window);
                var png = new System.Windows.Media.Imaging.PngBitmapEncoder();
                png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using var stream = File.Create(Path.Combine(acceptance, $"archive-{size.Width}x{size.Height}.png"));
                png.Save(stream);
                var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(window);
                File.WriteAllText(Path.Combine(acceptance, "native-dpi.txt"), $"Actual WPF scale: {dpi.DpiScaleX * 100:0}% x {dpi.DpiScaleY * 100:0}%; viewport sizes are window tests, not OS DPI changes.");
            }
        }
        var previewFolder = Environment.GetEnvironmentVariable("DOGRU_TEMIN_ACCEPTANCE_DIR");
        if (!string.IsNullOrWhiteSpace(previewFolder))
        {
            Invoke("ShowView", "Home");
            window.Width = 1366; window.Height = 768;
            window.UpdateLayout();
            var image = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            image.Render(window);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
            using var file = File.Create(Path.Combine(previewFolder, "home-cards-1366x768.png"));
            encoder.Save(file);
        }
    }
}

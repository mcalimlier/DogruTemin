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
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                window?.Close();
                app?.Shutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        try
        {
            if (failure is not null)
                throw failure;
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
        }
    }
}

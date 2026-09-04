using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DogruTemin.Wpf;

public partial class SplashWindow : Window
{
    private static readonly TimeSpan MinimumDisplayTime = TimeSpan.FromMilliseconds(3500);

    public SplashWindow()
    {
        InitializeComponent();
        Loaded += SplashWindow_Loaded;
    }

    private async void SplashWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var visibleTime = Stopwatch.StartNew();
        VersionText.Text = AppVersionInfo.DisplayVersion;
        var spin = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1))
        {
            RepeatBehavior = RepeatBehavior.Forever
        };
        SpinnerRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, spin);

        try
        {
            SetProgress(8, "DoğruTemin başlatılıyor...");
            await Dispatcher.Yield(DispatcherPriority.Background);

            // İlerleme yüzdesi MainWindow içindeki gerçek başlangıç kilometre taşlarından gelir.
            var main = new MainWindow(SetProgress);

            // Karşılama ekranı; logo, sürüm ve geliştirici adı rahatça okunabilsin diye
            // hızlı bilgisayarlarda da yaklaşık 3,5 saniye görünür kalır.
            SetProgress(96, "Son kontroller yapılıyor...");
            var remaining = MinimumDisplayTime - visibleTime.Elapsed;
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining);

            SetProgress(100, "Hazır. Program açılıyor...");
            await Task.Delay(180);

            Application.Current.MainWindow = main;
            main.Show();
            Close();
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "DoğruTemin açılırken hata oluştu:\n\n" + ex.Message,
                "DoğruTemin",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Application.Current.Shutdown(-1);
        }
    }

    private void SetProgress(int percent, string stage)
    {
        PercentText.Text = $"%{percent}";
        ProgressFill.Width = 410 * (percent / 100.0);
        StageText.Text = stage;
        Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
    }
}

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DogruTemin.Wpf;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        Loaded += SplashWindow_Loaded;
    }

    private async void SplashWindow_Loaded(object sender, RoutedEventArgs e)
    {
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

            SetProgress(100, "Hazır. Program açılıyor...");
            Application.Current.MainWindow = main;
            main.Show();
            await Task.Delay(140);

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

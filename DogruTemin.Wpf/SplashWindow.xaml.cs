using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DogruTemin.Wpf;

public partial class SplashWindow : Window
{
    private const double ProgressTrackWidth = 410.0;
    private int _reportedPercent;

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
            SetProgress(StartupProgressProfile.InitialPercent, "DoğruTemin başlatılıyor...");
            await Dispatcher.Yield(DispatcherPriority.Render);

            // MainWindow kurulumu UI iş parçacığını kısa süre meşgul edebildiği için önce
            // kullanıcıya gerçekten kesintisiz görünen bir ön yükleme akışı tamamlanır.
            await RunSmoothPreloadAsync();

            SetProgress(StartupProgressProfile.PreloadPercent, "Veriler ve uygulama bileşenleri hazırlanıyor...");
            var main = new MainWindow(SetProgress);

            CompleteProgress("Hazır. Program açılıyor...");
            await Task.Delay(StartupProgressProfile.CompletionDurationMs);

            Application.Current.MainWindow = main;
            main.Show();
            await Task.Delay(100);

            Close();
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
        catch (Exception ex)
        {
            var path = App.WriteCrashLog(ex);
            MessageBox.Show(
                "DoğruTemin başlatılamadı. Teknik ayrıntılar güvenlik amacıyla ekranda gösterilmez.\n\nHata günlüğü:\n" + path,
                "DoğruTemin - Açılış Hatası",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Application.Current.Shutdown(-1);
        }
    }

    private async Task RunSmoothPreloadAsync()
    {
        ProgressFill.BeginAnimation(FrameworkElement.WidthProperty, null);
        ProgressFill.Width = ProgressTrackWidth * (StartupProgressProfile.InitialPercent / 100.0);
        _reportedPercent = StartupProgressProfile.InitialPercent;
        PercentText.Text = $"%{_reportedPercent}";

        var animation = new DoubleAnimation
        {
            From = ProgressFill.Width,
            To = ProgressTrackWidth * (StartupProgressProfile.PreloadPercent / 100.0),
            Duration = TimeSpan.FromMilliseconds(StartupProgressProfile.PreloadDurationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            FillBehavior = FillBehavior.HoldEnd
        };

        ProgressFill.BeginAnimation(
            FrameworkElement.WidthProperty,
            animation,
            HandoffBehavior.SnapshotAndReplace);

        var start = Environment.TickCount64;
        while (Environment.TickCount64 - start < StartupProgressProfile.PreloadDurationMs)
        {
            var elapsed = Math.Clamp(
                (Environment.TickCount64 - start) / (double)StartupProgressProfile.PreloadDurationMs,
                0.0,
                1.0);
            var percent = StartupProgressProfile.CalculatePreloadPercent(elapsed);
            if (percent > _reportedPercent)
            {
                _reportedPercent = percent;
                PercentText.Text = $"%{percent}";
            }
            await Task.Delay(StartupProgressProfile.FrameDelayMs);
        }

        ProgressFill.BeginAnimation(FrameworkElement.WidthProperty, null);
        ProgressFill.Width = ProgressTrackWidth * (StartupProgressProfile.PreloadPercent / 100.0);
        _reportedPercent = StartupProgressProfile.PreloadPercent;
        PercentText.Text = $"%{_reportedPercent}";
    }

    private void SetProgress(int percent, string stage)
    {
        _reportedPercent = Math.Max(
            _reportedPercent,
            Math.Clamp(percent, 0, StartupProgressProfile.MaximumReportedBeforeComplete));
        PercentText.Text = $"%{_reportedPercent}";
        StageText.Text = stage;
        Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
    }

    private void CompleteProgress(string stage)
    {
        ProgressFill.BeginAnimation(FrameworkElement.WidthProperty, null);
        var currentWidth = ProgressFill.Width;
        ProgressFill.BeginAnimation(
            FrameworkElement.WidthProperty,
            new DoubleAnimation(
                currentWidth,
                ProgressTrackWidth,
                TimeSpan.FromMilliseconds(StartupProgressProfile.CompletionDurationMs))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd
            },
            HandoffBehavior.SnapshotAndReplace);

        _reportedPercent = 100;
        PercentText.Text = "%100";
        StageText.Text = stage;
    }
}

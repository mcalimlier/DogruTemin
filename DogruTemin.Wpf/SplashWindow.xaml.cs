using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DogruTemin.Wpf;

public partial class SplashWindow : Window
{
    private const double ProgressTrackWidth = 410.0;
    private static readonly TimeSpan MinimumDisplayTime = TimeSpan.FromMilliseconds(3500);
    private static readonly TimeSpan CompletionAnimationTime = TimeSpan.FromMilliseconds(180);

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
        StartSmoothProgress();

        try
        {
            SetProgress(8, "DoğruTemin başlatılıyor...");
            await Dispatcher.Yield(DispatcherPriority.Background);

            // Ana pencere gerçek başlangıç kilometre taşlarını bildirmeye devam eder;
            // yalnız ilerleme çubuğu bu kilometre taşlarına atlamaz, tek akış halinde ilerler.
            var main = new MainWindow(SetProgress);

            SetProgress(96, "Son kontroller yapılıyor...");
            var remaining = MinimumDisplayTime - visibleTime.Elapsed;
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining);

            SetProgress(100, "Hazır. Program açılıyor...");
            await CompleteProgressAsync();

            Application.Current.MainWindow = main;
            main.Show();
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

    private void StartSmoothProgress()
    {
        ProgressFill.Width = 0;
        PercentText.Text = "%0";
        CompositionTarget.Rendering += UpdateAnimatedPercent;

        var animation = new DoubleAnimation
        {
            From = 0,
            To = ProgressTrackWidth * 0.96,
            Duration = new Duration(MinimumDisplayTime),
            FillBehavior = FillBehavior.HoldEnd
        };
        ProgressFill.BeginAnimation(FrameworkElement.WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private async Task CompleteProgressAsync()
    {
        var currentWidth = Math.Clamp(ProgressFill.Width, 0, ProgressTrackWidth);
        var animation = new DoubleAnimation(currentWidth, ProgressTrackWidth, CompletionAnimationTime)
        {
            FillBehavior = FillBehavior.HoldEnd,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        ProgressFill.BeginAnimation(FrameworkElement.WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
        await Task.Delay(CompletionAnimationTime);
        PercentText.Text = "%100";
    }

    private void UpdateAnimatedPercent(object? sender, EventArgs e)
    {
        var width = Math.Clamp(ProgressFill.Width, 0, ProgressTrackWidth);
        var percent = (int)Math.Round(width / ProgressTrackWidth * 100.0, MidpointRounding.AwayFromZero);
        PercentText.Text = $"%{percent}";
    }

    private void SetProgress(int percent, string stage)
    {
        // Yüzde bilgisi MainWindow ile API uyumluluğu için korunur. Görsel yüzde ve bar,
        // tek bir zaman çizelgesiyle akıcı ilerlediği için burada sıçratılmaz.
        _ = percent;
        StageText.Text = stage;
        Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
    }

    protected override void OnClosed(EventArgs e)
    {
        CompositionTarget.Rendering -= UpdateAnimatedPercent;
        base.OnClosed(e);
    }
}

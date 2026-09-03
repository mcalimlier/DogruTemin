using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DogruTemin.Wpf;

public partial class SplashWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(38) };
    private int _progress;

    public SplashWindow()
    {
        InitializeComponent();
        Loaded += SplashWindow_Loaded;
    }

    private void SplashWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var spin = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1))
        {
            RepeatBehavior = RepeatBehavior.Forever
        };
        SpinnerRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, spin);

        _timer.Tick += (_, _) =>
        {
            _progress++;
            PercentText.Text = $"%{_progress}";
            ProgressFill.Width = 410 * (_progress / 100.0);
            StageText.Text = _progress switch
            {
                < 25 => "Arayüz bileşenleri hazırlanıyor...",
                < 50 => "DoğruTemin modülleri yükleniyor...",
                < 75 => "Görsel bileşenler hazırlanıyor...",
                < 95 => "Son kontroller yapılıyor...",
                _ => "Hazır. Program açılıyor..."
            };

            if (_progress >= 100)
            {
                _timer.Stop();
                var delay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
                delay.Tick += (_, _) =>
                {
                    delay.Stop();
                    var main = new MainWindow();
                    Application.Current.MainWindow = main;
                    main.Show();
                    Close();
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                };
                delay.Start();
            }
        };
        _timer.Start();
    }
}

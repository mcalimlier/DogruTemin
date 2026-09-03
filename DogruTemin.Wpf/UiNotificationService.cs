using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace DogruTemin.Wpf;

internal static class UiNotificationService
{
    public static void Show(Window owner, string message, TimeSpan? duration = null)
    {
        if (!owner.IsLoaded || !owner.IsVisible) return;
        var popup = new Popup
        {
            PlacementTarget = owner,
            Placement = PlacementMode.Center,
            AllowsTransparency = true,
            StaysOpen = true,
            IsHitTestVisible = false
        };
        var messageText = new TextBlock
        {
            Text = message,
            Foreground = Brushes.White,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14.5,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            MaxWidth = 570,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true
        };
        TextOptions.SetTextFormattingMode(messageText, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(messageText, TextRenderingMode.ClearType);
        popup.Child = new Border
        {
            MaxWidth = 620,
            Background = BrushFrom("#0A3154"),
            BorderBrush = BrushFrom("#FFB41F"),
            BorderThickness = new Thickness(1.2),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(18, 13, 18, 13),
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
            Child = messageText
        };
        popup.IsOpen = true;
        var timer = new DispatcherTimer { Interval = duration ?? TimeSpan.FromSeconds(3.2) };
        timer.Tick += (_, _) => { timer.Stop(); popup.IsOpen = false; };
        timer.Start();
    }

    private static Brush BrushFrom(string value) =>
        new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
}

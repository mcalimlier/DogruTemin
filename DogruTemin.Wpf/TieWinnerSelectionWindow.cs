using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DogruTemin.Wpf.Models;

namespace DogruTemin.Wpf;

public sealed class TieWinnerSelectionWindow : Window
{
    private readonly ComboBox _firmSelector;
    private readonly List<Firm> _firms;

    public string SelectedFirmId { get; private set; } = string.Empty;

    public TieWinnerSelectionWindow(IEnumerable<Firm> firms, string totalText)
    {
        _firms = firms.ToList();
        Title = "DoğruTemin - Eşit Teklif";
        Width = 590;
        Height = 330;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(239, 244, 249));
        FontFamily = new FontFamily("Segoe UI");

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(68) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Content = root;

        var header = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(3, 28, 56)),
            Padding = new Thickness(22, 0, 22, 0)
        };
        header.Child = new TextBlock
        {
            Text = "EŞİT EN DÜŞÜK TEKLİF",
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 20,
            VerticalAlignment = VerticalAlignment.Center
        };
        root.Children.Add(header);

        var panel = new StackPanel { Margin = new Thickness(26, 22, 26, 20) };
        Grid.SetRow(panel, 1);
        root.Children.Add(panel);

        panel.Children.Add(new TextBlock
        {
            Text = $"Birden fazla firma aynı en düşük toplamı verdi ({totalText}). Resmî belgede uygun görülecek firmayı seçiniz.",
            FontSize = 15,
            Foreground = new SolidColorBrush(Color.FromRgb(28, 49, 70)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 15)
        });

        _firmSelector = new ComboBox
        {
            Height = 46,
            FontSize = 16,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(10, 0, 10, 0),
            ItemsSource = _firms,
            DisplayMemberPath = nameof(Firm.Name),
            SelectedIndex = _firms.Count > 0 ? 0 : -1,
            Margin = new Thickness(0, 0, 0, 20)
        };
        panel.Children.Add(_firmSelector);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button
        {
            Content = "VAZGEÇ",
            Width = 110,
            Height = 42,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 10, 0)
        };
        cancel.Click += (_, _) => { DialogResult = false; Close(); };
        buttons.Children.Add(cancel);

        var choose = new Button
        {
            Content = "BU FİRMAYI SEÇ",
            Width = 160,
            Height = 42,
            Background = new SolidColorBrush(Color.FromRgb(38, 103, 210)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            FontWeight = FontWeights.Bold
        };
        choose.Click += (_, _) =>
        {
            if (_firmSelector.SelectedItem is not Firm firm) return;
            SelectedFirmId = firm.Id;
            DialogResult = true;
            Close();
        };
        buttons.Children.Add(choose);
        panel.Children.Add(buttons);
    }
}

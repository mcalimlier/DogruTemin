using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DogruTemin.Wpf;

public partial class LargeDatePicker : UserControl
{
    public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register(
        nameof(SelectedDate),
        typeof(DateTime?),
        typeof(LargeDatePicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDatePropertyChanged));

    public static readonly RoutedEvent SelectedDateChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(SelectedDateChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<DateTime?>),
        typeof(LargeDatePicker));

    private bool _syncing;
    private static readonly SolidColorBrush NormalBorder = BrushFrom("#365874");
    private static readonly SolidColorBrush FocusBorder = BrushFrom("#FFB41F");

    public LargeDatePicker()
    {
        InitializeComponent();
        UpdateDisplay();
        GotKeyboardFocus += (_, _) => InputBorder.BorderBrush = FocusBorder;
        LostKeyboardFocus += (_, _) => InputBorder.BorderBrush = NormalBorder;
    }

    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public event RoutedPropertyChangedEventHandler<DateTime?> SelectedDateChanged
    {
        add => AddHandler(SelectedDateChangedEvent, value);
        remove => RemoveHandler(SelectedDateChangedEvent, value);
    }

    private static void OnSelectedDatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not LargeDatePicker picker) return;
        picker.UpdateDisplay();
        picker.SyncCalendar();
        picker.RaiseEvent(new RoutedPropertyChangedEventArgs<DateTime?>((DateTime?)e.OldValue, (DateTime?)e.NewValue, SelectedDateChangedEvent));
    }

    private void UpdateDisplay()
    {
        if (DateText is null) return;
        DateText.Text = SelectedDate.HasValue
            ? SelectedDate.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            : string.Empty;
        if (ClearButton is not null)
            ClearButton.Visibility = SelectedDate.HasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SyncCalendar()
    {
        if (CalendarControl is null || _syncing) return;
        _syncing = true;
        CalendarControl.SelectedDate = SelectedDate;
        CalendarControl.DisplayDate = SelectedDate ?? DateTime.Today;
        _syncing = false;
    }

    private void OpenCalendar()
    {
        Focus();
        SyncCalendar();
        CalendarPopup.IsOpen = true;
        CalendarControl.Focus();
    }

    private void ClearDate()
    {
        CalendarPopup.IsOpen = false;
        SelectedDate = null;
        Focus();
    }

    private void CalendarButton_Click(object sender, RoutedEventArgs e) => OpenCalendar();

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        ClearDate();
    }

    private void DateText_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        OpenCalendar();
    }

    private void Root_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Delete or Key.Back)
        {
            if (SelectedDate.HasValue)
            {
                ClearDate();
                e.Handled = true;
            }
            return;
        }

        if (e.Key is Key.Enter or Key.Space or Key.Down)
        {
            OpenCalendar();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && CalendarPopup.IsOpen)
        {
            CalendarPopup.IsOpen = false;
            Focus();
            e.Handled = true;
        }
    }

    private void CalendarControl_SelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        SelectedDate = CalendarControl.SelectedDate;
        CalendarPopup.IsOpen = false;
        Focus();
    }

    private static SolidColorBrush BrushFrom(string value) =>
        new((Color)ColorConverter.ConvertFromString(value));
}

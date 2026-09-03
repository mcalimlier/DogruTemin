using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

    public LargeDatePicker()
    {
        InitializeComponent();
        UpdateDisplay();
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
        DateText.Text = SelectedDate.HasValue ? SelectedDate.Value.ToString("dd.MM.yyyy") : string.Empty;
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
        SyncCalendar();
        CalendarPopup.IsOpen = true;
        CalendarControl.Focus();
    }

    private void CalendarButton_Click(object sender, RoutedEventArgs e) => OpenCalendar();

    private void DateText_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        OpenCalendar();
    }

    private void CalendarControl_SelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        SelectedDate = CalendarControl.SelectedDate;
        CalendarPopup.IsOpen = false;
    }
}

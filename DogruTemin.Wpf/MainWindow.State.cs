using System.Windows.Media;

namespace DogruTemin.Wpf;

public partial class MainWindow
{
    private void MarkDirty()
    {
        if (_suppressDirtyTracking) return;
        _hasUnsavedChanges = true;
        ShowDirtyStatus();
        UpdateDerivedState();
    }

    private void ShowDirtyStatus(string text = "Kaydedilmedi")
    {
        SaveStatusDot.Fill = new SolidColorBrush(Color.FromRgb(255, 180, 31));
        SaveStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 230, 183));
        SaveStatusText.Text = text;
    }

    private void SetSaveErrorStatus()
    {
        SaveStatusDot.Fill = new SolidColorBrush(Color.FromRgb(224, 82, 82));
        SaveStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 220, 220));
        SaveStatusText.Text = "Kayıt başarısız";
    }

    private void RefreshSaveStatusVisual()
    {
        if (HasUnsavedWork())
        {
            ShowDirtyStatus();
            return;
        }
        ShowSavedStatus();
    }

    private void ShowSavedStatus()
    {
        SaveStatusDot.Fill = new SolidColorBrush(Color.FromRgb(67, 199, 122));
        SaveStatusText.Foreground = new SolidColorBrush(Color.FromRgb(221, 246, 231));
        SaveStatusText.Text = _lastSavedAt.HasValue ? $"Kaydedildi • {_lastSavedAt.Value:HH:mm}" : "Kaydedildi";
        UpdateHomeActivitySummary();
    }

    private void SetSavedStatus(DateTime? savedAt = null)
    {
        _hasUnsavedChanges = false;
        _lastSavedAt = savedAt ?? DateTime.Now;
        ShowSavedStatus();
    }
}

using DogruTemin.Wpf.Models;
using ProcurementData = DogruTemin.Core.Models.ProcurementData;
using OutputDocumentType = DogruTemin.Core.Models.OutputDocumentType;
using FirmOfferCore = DogruTemin.Core.Models.FirmOffer;
using AppSettings = DogruTemin.Core.Models.ApplicationSettings;
using DogruTemin.Core.Services;
using DogruTemin.Wpf.Services;
using Microsoft.Win32;
using System.Windows.Documents;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

namespace DogruTemin.Wpf;

public partial class MainWindow
{
    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        UpdateMaximizeRestoreButton();
        Dispatcher.BeginInvoke(new Action(() =>
        {
            AppScaleViewbox.InvalidateMeasure();
            AppScaleViewbox.InvalidateArrange();
        }));
    }

    private void UpdateMaximizeRestoreButton()
    {
        if (MaximizeRestoreIcon is null || MaximizeRestoreButton is null) return;
        var isMaximized = WindowState == WindowState.Maximized;
        MaximizeRestoreIcon.Text = isMaximized ? "\uE923" : "\uE922";
        MaximizeRestoreButton.ToolTip = isMaximized ? "Geri yükle" : "Tam ekran";
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmUnsavedWork("kapat", "DoğruTemin - Kapat")) return;
        _closeApproved = true;
        RecoverySessionService.EndCleanly();
        Close();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_closeApproved) return;
        if (!ConfirmUnsavedWork("kapat", "DoğruTemin - Kapat"))
        {
            e.Cancel = true;
            return;
        }
        _closeApproved = true;
        RecoverySessionService.EndCleanly();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            Save_Click(this, new RoutedEventArgs());
            return;
        }

        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None) return;
        if (_currentView is not ("Institution" or "General" or "Firms" or "Offers" or "ApprovalDocument" or "Dates" or "Output")) return;
        if (e.OriginalSource is not TextBox textBox || textBox.AcceptsReturn) return;
        if (FindVisualParent<DataGrid>(textBox) is not null) return;

        textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        e.Handled = true;
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject? current = child;
        while (current is not null)
        {
            if (current is T match) return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_firmEditorDirty)
            {
                MessageBox.Show("Firma kartında henüz listeye aktarılmamış bilgi var. Önce firma bölümündeki EKLE/KAYDET düğmesiyle firma girişini tamamlayın veya TEMİZLE ile vazgeçin.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
                ShowView("Firms");
                return;
            }
            if (_productEditorDirty)
            {
                MessageBox.Show("Ürün kartında henüz listeye aktarılmamış bilgi var. Önce ürün bölümündeki EKLE/KAYDET düğmesiyle ürün girişini tamamlayın veya TEMİZLE ile vazgeçin.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
                ShowView("General");
                return;
            }
            if (_offerEditorDirty)
            {
                MessageBox.Show("Firmalar/Teklifler bölümünde kaydedilmemiş teklif değişikliği var. Önce TEKLİFİ KAYDET düğmesiyle teklif girişini tamamlayınız.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
                ShowView("Offers");
                return;
            }
            var data = BuildDataFromUi();
            var saveResult = _procurementWorkflow.Save(data, data.AutoBackup);
            if (saveResult.RequiresWarningConfirmation)
            {
                var answer = MessageBox.Show("Tarih sıralamasında kontrol edilmesi gereken noktalar var:\n\n" +
                    string.Join("\n", saveResult.Warnings.Select(i => "• " + i.Message)) +
                    "\n\nYine de taslak kaydedilsin mi?", "DoğruTemin - Tarih sırası", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (answer != MessageBoxResult.Yes) return;

                saveResult = _procurementWorkflow.Save(data, data.AutoBackup, allowDateWarnings: true);
            }

            if (!saveResult.SavedSuccessfully)
            {
                MessageBox.Show(string.Join("\n", saveResult.BlockingIssues.Take(8).Select(i => "• " + i.Message)), "DoğruTemin - Geçersiz veri", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var savedData = saveResult.Data!;
            _recordId = savedData.RecordId;
            _recordCreatedAt = savedData.CreatedAt;
            UpdateDerivedState(savedData);
            SetSavedStatus(savedData.LastSavedAt);
            RefreshArchiveRecords();
            if (!string.IsNullOrWhiteSpace(saveResult.BackupWarning))
            {
                MessageBox.Show("Kayıt tamamlandı; ancak otomatik yedekleme sırasında uyarı oluştu:\n\n" + saveResult.BackupWarning,
                    "DoğruTemin - Yedekleme Uyarısı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            SetSaveErrorStatus();
            MessageBox.Show("Kayıt sırasında hata oluştu: " + ex.Message, "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        switch (_currentView)
        {
            case "Firms":
                if (!_firmEditorDirty && _editingFirm is null) return;
                if (_firmEditorDirty && !ConfirmEditorClear("Firma giriş alanları temizlensin mi? Kaydedilmemiş firma kartı bilgileri silinecek.")) return;
                ClearFirmForm();
                RefreshSaveStatusVisual();
                break;
            case "General":
                if (!_productEditorDirty && _editingProduct is null) return;
                if (_productEditorDirty && !ConfirmEditorClear("Ürün giriş alanları temizlensin mi? Kaydedilmemiş ürün kartı bilgileri silinecek.")) return;
                ClearProductForm();
                RefreshSaveStatusVisual();
                break;
            case "Offers":
                if (!_offerEditorDirty) return;
                if (!ConfirmEditorClear("Teklif ekranındaki kaydedilmemiş değişiklikler geri alınsın mı?")) return;
                LoadOfferFirm(_offerEditingFirm);
                RefreshSaveStatusVisual();
                break;
            case "ApprovalDocument":
                if (!ApprovalDocumentDatePicker.SelectedDate.HasValue
                    && string.IsNullOrWhiteSpace(ApprovalDocumentNumberBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalAvailableBudgetBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalInvestmentProjectBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalBudgetCodeBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalAdvanceConditionsBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalPrequalificationSalePriceBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalNotesBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalSubmittedByNameBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalSubmittedByTitleBox.Text)) return;
                if (!ConfirmEditorClear("Onay Belgesi'ne özgü giriş alanları temizlensin mi?")) return;
                _suppressDirtyTracking = true;
                try
                {
                    ApprovalDocumentDatePicker.SelectedDate = null;
                    ApprovalDocumentNumberBox.Text = string.Empty;
                    ApprovalAvailableBudgetBox.Text = string.Empty;
                    ApprovalInvestmentProjectBox.Text = string.Empty;
                    ApprovalBudgetCodeBox.Text = string.Empty;
                    ApprovalAdvanceConditionsBox.Text = string.Empty;
                    ApprovalTenderProcedureBox.Text = "4734 S.K. 22/d Kapsamında";
                    ApprovalAnnouncementMethodBox.Text = "İlansız";
                    ApprovalPrequalificationSalePriceBox.Text = string.Empty;
                    ApprovalPriceDifferenceBasisBox.Text = "Ödenmeyecektir";
                    ApprovalNotesBox.Text = string.Empty;
                    ApprovalSubmittedByNameBox.Text = string.Empty;
                    ApprovalSubmittedByTitleBox.Text = string.Empty;
                }
                finally { _suppressDirtyTracking = false; }
                MarkDirty();
                break;
            case "Dates":
                if (!CostCalculationDatePicker.SelectedDate.HasValue && !MarketReportDatePicker.SelectedDate.HasValue
                    && !InspectionReportDatePicker.SelectedDate.HasValue && !DeliveryDatePicker.SelectedDate.HasValue) return;
                if (!ConfirmEditorClear("Belge tarihleri temizlensin mi? Bu değişiklik üstteki KAYDET düğmesine basılana kadar kalıcı olmayacaktır.")) return;
                _suppressDirtyTracking = true;
                try
                {
                    CostCalculationDatePicker.SelectedDate = null;
                    MarketReportDatePicker.SelectedDate = null;
                    InspectionReportDatePicker.SelectedDate = null;
                    DeliveryDatePicker.SelectedDate = null;
                }
                finally
                {
                    _suppressDirtyTracking = false;
                }
                MarkDirty();
                break;
            default:
                MessageBox.Show("Bu ekranda TEMİZLE ile kaldırılacak geçici giriş alanı yok.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
        }
    }

    private static bool ConfirmEditorClear(string message) =>
        MessageBox.Show(message, "DoğruTemin - Temizle", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private void TransientFirmEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressDirtyTracking) return;
        _firmEditorDirty = _editingFirm is not null || HasFirmEditorContent();
        RefreshSaveStatusVisual();
    }

    private void TransientProductEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressDirtyTracking) return;
        _productEditorDirty = _editingProduct is not null || HasProductEditorContent();
        RefreshSaveStatusVisual();
    }

    private void TransientProductEditor_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressDirtyTracking) return;
        _productEditorDirty = _editingProduct is not null || HasProductEditorContent();
        RefreshSaveStatusVisual();
    }
}

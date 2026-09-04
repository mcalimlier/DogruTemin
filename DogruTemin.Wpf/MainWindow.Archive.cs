using DogruTemin.Wpf.Models;
using DogruTemin.Wpf.Services;
using Microsoft.Win32;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DogruTemin.Wpf;

public partial class MainWindow
{
    private void RefreshArchiveRecords()
    {
        ArchiveRecords.Clear();
        foreach (var data in _storage.ListRecords())
        {
            var lastSaved = data.LastSavedAt ?? data.CreatedAt;
            var isActive = !string.IsNullOrWhiteSpace(_recordId) && data.RecordId == _recordId;
            ArchiveRecords.Add(new ArchiveRecordRow
            {
                RecordId = data.RecordId,
                Status = isActive ? "AKTİF" : "ARŞİV",
                IsActive = isActive,
                OperationNumber = string.IsNullOrWhiteSpace(data.OperationNumber) ? "—" : data.OperationNumber,
                JobTitle = string.IsNullOrWhiteSpace(data.JobTitle) ? "Başlıksız işlem" : data.JobTitle,
                ApprovalDate = data.Dates.ApprovalDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "—",
                ApprovalSortKey = data.Dates.ApprovalDate ?? DateTime.MinValue,
                ProductCount = data.Products.Count(p => !string.IsNullOrWhiteSpace(p.Name)),
                FirmCount = data.Firms.Count(f => !string.IsNullOrWhiteSpace(f.Name)),
                LastSaved = lastSaved.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
                LastSavedValue = lastSaved
            });
        }
        ApplyArchiveView();
        UpdateHomeActivitySummary();
    }

    private bool ArchiveRecordMatchesFilter(object item)
    {
        if (item is not ArchiveRecordRow row) return false;
        var filterIndex = ArchiveFilterBox?.SelectedIndex ?? 0;
        if (filterIndex == 1 && !row.IsActive) return false;
        if (filterIndex == 2 && row.IsActive) return false;
        var query = ArchiveSearchBox?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query)) return true;
        var searchable = $"{row.OperationNumber} {row.JobTitle} {row.ApprovalDate} {row.LastSaved} {row.Status}";
        return CultureInfo.GetCultureInfo("tr-TR").CompareInfo.IndexOf(searchable, query, CompareOptions.IgnoreCase) >= 0;
    }

    private void ApplyArchiveView()
    {
        if (_archiveView is null) return;
        _archiveView.Refresh();
        _archiveView.SortDescriptions.Clear();
        switch (ArchiveSortBox?.SelectedIndex ?? 0)
        {
            case 1:
                _archiveView.SortDescriptions.Add(new SortDescription(nameof(ArchiveRecordRow.LastSavedValue), ListSortDirection.Ascending));
                break;
            case 2:
                _archiveView.SortDescriptions.Add(new SortDescription(nameof(ArchiveRecordRow.ApprovalSortKey), ListSortDirection.Descending));
                _archiveView.SortDescriptions.Add(new SortDescription(nameof(ArchiveRecordRow.LastSavedValue), ListSortDirection.Descending));
                break;
            case 3:
                _archiveView.SortDescriptions.Add(new SortDescription(nameof(ArchiveRecordRow.JobTitle), ListSortDirection.Ascending));
                break;
            case 4:
                _archiveView.SortDescriptions.Add(new SortDescription(nameof(ArchiveRecordRow.OperationNumber), ListSortDirection.Ascending));
                break;
            default:
                _archiveView.SortDescriptions.Add(new SortDescription(nameof(ArchiveRecordRow.LastSavedValue), ListSortDirection.Descending));
                break;
        }
        var visible = _archiveView.Cast<object>().Count();
        ArchiveCountText.Text = visible == ArchiveRecords.Count ? $"{ArchiveRecords.Count} kayıt" : $"{visible} / {ArchiveRecords.Count} kayıt";
    }

    private void ArchiveSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyArchiveView();
    private void ArchiveFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyArchiveView();
    private void ArchiveSort_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyArchiveView();

    private void ClearArchiveFilters_Click(object sender, RoutedEventArgs e)
    {
        if (ArchiveSearchBox is not null) ArchiveSearchBox.Text = string.Empty;
        if (ArchiveFilterBox is not null) ArchiveFilterBox.SelectedIndex = 0;
        if (ArchiveSortBox is not null) ArchiveSortBox.SelectedIndex = 0;
        ApplyArchiveView();
    }

    private void ArchiveGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ArchiveGrid.SelectedItem is ArchiveRecordRow) OpenArchive_Click(sender, new RoutedEventArgs());
    }

    private bool ConfirmRecordSwitch() => ConfirmUnsavedWork("devam et", "DoğruTemin - İşlem değiştir");

    private bool ConfirmUnsavedWork(string actionText, string title)
    {
        if (!HasUnsavedWork()) return true;
        var result = MessageBox.Show($"Kaydedilmemiş değişiklikler var.\n\nEVET: Kaydet ve {actionText}\nHAYIR: Kaydetmeden {actionText}\nİPTAL: Programa dön", title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (result == MessageBoxResult.Cancel) return false;
        if (result == MessageBoxResult.Yes)
        {
            Save_Click(this, new RoutedEventArgs());
            return !HasUnsavedWork();
        }
        return true;
    }

    private bool HasUnsavedWork() => _hasUnsavedChanges || _offerEditorDirty || _firmEditorDirty || _productEditorDirty;

    private bool HasFirmEditorContent() =>
        !string.IsNullOrWhiteSpace(FirmNameBox?.Text)
        || !string.IsNullOrWhiteSpace(ContactNameBox?.Text)
        || !string.IsNullOrWhiteSpace(TaxNoBox?.Text)
        || !string.IsNullOrWhiteSpace(PhoneBox?.Text)
        || !string.IsNullOrWhiteSpace(AddressBox?.Text)
        || !string.IsNullOrWhiteSpace(EmailBox?.Text);

    private bool HasProductEditorContent()
    {
        var quantity = ProductQuantityBox?.Text?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(ProductNameBox?.Text)
            || !string.IsNullOrWhiteSpace(ProductSpecBox?.Text)
            || (quantity.Length > 0 && quantity != "1")
            || (ProductUnitBox?.SelectedIndex ?? 0) != 0;
    }

    private void NewOperation_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmRecordSwitch()) return;
        ReportStartup(64, "Ayarlar yükleniyor...");
        _settings = _storage.LoadSettings();
        _suppressDirtyTracking = true;
        InitializeBlankState(_settings);
        _suppressDirtyTracking = false;
        _storage.ClearActiveFile();
        UpdateDerivedState();
        _hasUnsavedChanges = true;
        ShowDirtyStatus("Kaydedilmedi • Yeni işlem");
        ShowView("General");
    }

    private void OpenArchive_Click(object sender, RoutedEventArgs e)
    {
        if (ArchiveGrid.SelectedItem is not ArchiveRecordRow row)
        {
            MessageBox.Show("Açmak için arşivden bir işlem seçiniz.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!ConfirmRecordSwitch()) return;
        var data = _storage.LoadRecord(row.RecordId, out var warning);
        if (data is null)
        {
            MessageBox.Show(warning ?? "İşlem açılamadı.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Warning);
            RefreshArchiveRecords();
            return;
        }
        _suppressDirtyTracking = true;
        ApplyDataToUi(data);
        ClearFirmForm();
        ClearProductForm();
        _suppressDirtyTracking = false;
        _storage.ActivateRecord(data);
        UpdateDerivedState(data);
        SetSavedStatus(data.LastSavedAt);
        ShowView("Home");
    }

    private bool PrepareSelectedArchiveAction(ArchiveRecordRow row, string actionText)
    {
        if (!row.IsActive || !HasUnsavedWork()) return true;
        var answer = MessageBox.Show($"Aktif işlemde kaydedilmemiş değişiklikler var.\n\n{actionText} işlemi son kaydedilmiş sürüm üzerinden yapılacaktır.\n\nEVET: Önce kaydet\nHAYIR: Son kaydedilmiş sürümle devam et\nİPTAL: Vazgeç", "DoğruTemin - Arşiv", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Cancel) return false;
        if (answer == MessageBoxResult.Yes)
        {
            Save_Click(this, new RoutedEventArgs());
            return !HasUnsavedWork();
        }
        return true;
    }

    private void DuplicateArchive_Click(object sender, RoutedEventArgs e)
    {
        if (ArchiveGrid.SelectedItem is not ArchiveRecordRow row)
        {
            MessageBox.Show("Kopyalamak için arşivden bir işlem seçiniz.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!PrepareSelectedArchiveAction(row, "Kopyalama")) return;
        var answer = MessageBox.Show($"'{row.JobTitle}' kaydı kopyalansın mı?\n\nYeni kopyanın İşlem No alanı boş bırakılacak ve iş adına ' - Kopya' eklenecektir.", "DoğruTemin - İşlem Kopyala", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;
        var copy = _storage.DuplicateRecord(row.RecordId, out var warning);
        if (copy is null)
        {
            MessageBox.Show(warning ?? "İşlem kopyalanamadı.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        RefreshArchiveRecords();
        SelectArchiveRecord(copy.RecordId);
        UiNotificationService.Show(this, "İşlem kopyası arşive eklendi. Yeni kopyanın İşlem No alanı boş bırakıldı.", TimeSpan.FromSeconds(4));
    }

    private void ExportSelectedArchive_Click(object sender, RoutedEventArgs e)
    {
        if (ArchiveGrid.SelectedItem is not ArchiveRecordRow row)
        {
            MessageBox.Show("Dışa aktarmak için arşivden bir işlem seçiniz.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!PrepareSelectedArchiveAction(row, "Dışa aktarma")) return;
        var baseName = row.OperationNumber != "—" ? row.OperationNumber : row.JobTitle;
        var dialog = new SaveFileDialog
        {
            Title = "İşlem Kaydını Dışa Aktar",
            Filter = "DoğruTemin işlem kaydı (*.json)|*.json",
            DefaultExt = ".json",
            AddExtension = true,
            FileName = $"DogruTemin_{SafeArchiveFileName(baseName)}_{DateTime.Now:yyyyMMdd}.json"
        };
        if (dialog.ShowDialog(this) != true) return;
        if (!_storage.ExportRecord(row.RecordId, dialog.FileName, out var warning))
        {
            MessageBox.Show(warning ?? "İşlem dışa aktarılamadı.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        UiNotificationService.Show(this, $"İşlem kaydı dışa aktarıldı:\n{dialog.FileName}", TimeSpan.FromSeconds(5));
    }

    private void ExportArchiveBackup_Click(object sender, RoutedEventArgs e)
    {
        if (HasUnsavedWork())
        {
            var answer = MessageBox.Show("Aktif işlemde kaydedilmemiş değişiklikler var. Arşiv yedeği yalnız diskteki son kaydedilmiş verileri içerir.\n\nYedek oluşturmadan önce aktif işlem kaydedilsin mi?", "DoğruTemin - Arşiv Yedeği", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Cancel) return;
            if (answer == MessageBoxResult.Yes)
            {
                Save_Click(this, new RoutedEventArgs());
                if (HasUnsavedWork()) return;
            }
        }
        var dialog = new SaveFileDialog
        {
            Title = "DoğruTemin Arşiv Yedeği",
            Filter = "ZIP arşivi (*.zip)|*.zip",
            DefaultExt = ".zip",
            AddExtension = true,
            FileName = $"DogruTemin_Arsiv_Yedegi_{DateTime.Now:yyyyMMdd_HHmm}.zip"
        };
        if (dialog.ShowDialog(this) != true) return;
        if (!_storage.ExportArchiveBackup(dialog.FileName, out var warning))
        {
            MessageBox.Show(warning ?? "Arşiv yedeği oluşturulamadı.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        UiNotificationService.Show(this, $"Arşiv yedeği oluşturuldu:\n{dialog.FileName}", TimeSpan.FromSeconds(5));
    }

    private void RestoreDeletedArchive_Click(object sender, RoutedEventArgs e)
    {
        var restored = _storage.RestoreMostRecentlyDeleted(out var warning);
        if (restored is null)
        {
            MessageBox.Show(warning ?? "Geri alınabilecek kayıt bulunamadı.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        RefreshArchiveRecords();
        SelectArchiveRecord(restored.RecordId);
        UiNotificationService.Show(this, $"Son silinen işlem geri alındı:\n{(string.IsNullOrWhiteSpace(restored.JobTitle) ? "Başlıksız işlem" : restored.JobTitle)}", TimeSpan.FromSeconds(4));
    }

    private void SelectArchiveRecord(string recordId)
    {
        var row = ArchiveRecords.FirstOrDefault(x => x.RecordId == recordId);
        if (row is null) return;
        ArchiveGrid.SelectedItem = row;
        ArchiveGrid.ScrollIntoView(row);
    }

    private static string SafeArchiveFileName(string? value)
    {
        var source = string.IsNullOrWhiteSpace(value) ? "Islem" : value.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(source.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return cleaned.Length > 70 ? cleaned[..70] : cleaned;
    }

    private void DeleteArchive_Click(object sender, RoutedEventArgs e)
    {
        if (ArchiveGrid.SelectedItem is not ArchiveRecordRow row)
        {
            MessageBox.Show("Silmek için arşivden bir işlem seçiniz.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (row.RecordId == _recordId)
        {
            MessageBox.Show("Aktif işlem arşivden silinemez. Önce YENİ İŞLEM başlatın veya başka bir işlemi açın.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var answer = MessageBox.Show($"'{row.JobTitle}' kaydı arşivden silinsin mi?\n\nKayıt güvenli 'Silinenler' yedeğine taşınacak ve SON SİLİNENİ GERİ AL ile geri getirilebilecektir.", "DoğruTemin - Arşiv", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;
        if (!_storage.DeleteRecord(row.RecordId))
        {
            MessageBox.Show("Arşiv kaydı silinemedi.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        RefreshArchiveRecords();
    }

    private void RefreshArchive_Click(object sender, RoutedEventArgs e) => RefreshArchiveRecords();
}

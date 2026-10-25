using ProcurementData = DogruTemin.Core.Models.ProcurementData;
using ValidationSeverity = DogruTemin.Core.Models.ValidationSeverity;
using OutputDocumentType = DogruTemin.Core.Models.OutputDocumentType;
using DogruTemin.Core.Services;
using DogruTemin.Wpf.Models;
using DogruTemin.Wpf.Services;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DogruTemin.Wpf;

public partial class MainWindow
{
    private void CommissionGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (!_suppressDirtyTracking) MarkDirty();
    }

    private void CommissionTitle_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_suppressDirtyTracking) MarkDirty();
    }

    private void AnyGlobalField_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_suppressDirtyTracking) MarkDirty();
    }

    private void AnyDate_Changed(object? sender, RoutedPropertyChangedEventArgs<DateTime?> e)
    {
        UpdateApprovalDateReference();
        if (_suppressDirtyTracking) return;
        MarkDirty();
        var warnings = ValidationService.DateLogicWarnings(BuildDataFromUi())
            .Select(x => x.Message)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();
        var warningText = string.Join("\n", warnings);
        if (warningText.Length == 0)
        {
            _lastDateSequenceWarning = string.Empty;
            return;
        }
        if (string.Equals(_lastDateSequenceWarning, warningText, StringComparison.Ordinal)) return;
        _lastDateSequenceWarning = warningText;
        MessageBox.Show(warningText, "DoğruTemin - Tarih Kontrolü", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void UpdateApprovalDateReference()
    {
        if (ApprovalDateReferenceText is null || ApprovalDocumentDatePicker is null) return;
        ApprovalDateReferenceText.Text = ApprovalDocumentDatePicker.SelectedDate is DateTime date
            ? $"Onay Belgesi tarihi: {date:dd.MM.yyyy}"
            : "Onay Belgesi tarihi: Henüz girilmedi";
    }

    internal static IReadOnlyList<DogruTemin.Core.Models.ValidationIssue> GetDateSaveBlockingIssues(
        DogruTemin.Core.Models.ProcessDates dates) =>
        DateValidationService.RequiredSequenceIssues(dates, ValidationSeverity.Critical);

    private void SaveDates_Click(object sender, RoutedEventArgs e)
    {
        var issues = GetDateSaveBlockingIssues(BuildDataFromUi().Dates);
        if (issues.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, issues.Select(issue => issue.Message)),
                "DoğruTemin - Tarih Sıralaması", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Save_Click(sender, e);
    }

    private void OutputSelection_Changed(object sender, RoutedEventArgs e)
    {
        if (!_suppressDirtyTracking) MarkDirty();
    }

    private void ControlRefresh_Click(object sender, RoutedEventArgs e) => RefreshControlCenter();

    private void ControlFix_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not string target) return;
        ShowView(target);
        if (target == "Offers") PrepareOffersView();
        if (target == "Institution" && button.DataContext is ValidationCenterRow row && row.Area.Contains("Komisyon", StringComparison.OrdinalIgnoreCase))
            Dispatcher.BeginInvoke(new Action(() => CommissionSectionAnchor?.BringIntoView()));
    }

    private void RefreshControlCenter(ProcurementData? supplied = null)
    {
        var data = supplied ?? BuildDataFromUi();
        var issues = ValidationService.ValidateWorkflow(data)
            .OrderBy(i => i.Severity == ValidationSeverity.Critical ? 0 : i.Severity == ValidationSeverity.Warning ? 1 : 2)
            .ThenBy(i => ControlAreaFor(i).Area)
            .ThenBy(i => i.Message)
            .ToList();

        ValidationRows.Clear();
        foreach (var issue in issues)
        {
            var mapping = ControlAreaFor(issue);
            ValidationRows.Add(new ValidationCenterRow
            {
                Severity = issue.Severity == ValidationSeverity.Critical ? "KRİTİK" : issue.Severity == ValidationSeverity.Warning ? "UYARI" : "BİLGİ",
                Area = mapping.Area,
                Message = issue.Message,
                TargetView = mapping.TargetView,
                SortOrder = issue.Severity == ValidationSeverity.Critical ? 0 : issue.Severity == ValidationSeverity.Warning ? 1 : 2
            });
        }

        var critical = issues.Count(i => i.Severity == ValidationSeverity.Critical);
        var warning = issues.Count(i => i.Severity == ValidationSeverity.Warning);
        ControlCriticalCountText.Text = critical.ToString(CultureInfo.InvariantCulture);
        ControlWarningCountText.Text = warning.ToString(CultureInfo.InvariantCulture);
        ControlResultCountText.Text = $"{issues.Count} kontrol noktası";
        ControlIssuesGrid.Visibility = issues.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        ControlEmptyText.Visibility = issues.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (critical > 0)
        {
            ControlOverallStatusText.Text = "EKSİK";
            ControlOverallStatusText.Foreground = new SolidColorBrush(Color.FromRgb(190, 61, 61));
            ControlOverallHintText.Text = $"{critical} kritik konu düzeltilmeli";
        }
        else if (warning > 0)
        {
            ControlOverallStatusText.Text = "KONTROL";
            ControlOverallStatusText.Foreground = new SolidColorBrush(Color.FromRgb(215, 117, 0));
            ControlOverallHintText.Text = $"{warning} uyarı gözden geçirilmeli";
        }
        else
        {
            ControlOverallStatusText.Text = "HAZIR";
            ControlOverallStatusText.Foreground = new SolidColorBrush(Color.FromRgb(36, 138, 80));
            ControlOverallHintText.Text = "Kritik hata veya uyarı yok";
        }
    }

    private static (string Area, string TargetView) ControlAreaFor(DogruTemin.Core.Models.ValidationIssue issue)
    {
        var field = (issue.Field ?? string.Empty).Trim();
        var message = (issue.Message ?? string.Empty).Trim();
        if (EqualsAny(field, "Alım / iş başlığı", "OperationNumber", "Komisyon görevlendirme tarihi", "Komisyon sayısı", "Harcama belgesi tarihi", "Harcama belgesi sayısı"))
            return ("Mal/Hizmet Bilgisi", "General");
        if (field.StartsWith("Dates", StringComparison.OrdinalIgnoreCase) || EqualsAny(field, "Yaklaşık Maliyet Hesap Cetveli tarihi", "Piyasa Fiyat Araştırması Tutanağı tarihi", "Muayene ve Kabul Tutanağı tarihi", "Teslim Alma / Kabul tarihi"))
            return ("Belgeler/Tarihler", "Dates");
        if (field.StartsWith("Offer:", StringComparison.OrdinalIgnoreCase) || field.StartsWith("Offers", StringComparison.OrdinalIgnoreCase) || message.Contains("teklif", StringComparison.OrdinalIgnoreCase))
            return ("Firmalar/Teklifler", "Offers");
        if (field.StartsWith("Firm:", StringComparison.OrdinalIgnoreCase) || field.Equals("Firms", StringComparison.OrdinalIgnoreCase) || message.Contains("firma", StringComparison.OrdinalIgnoreCase) && !message.Contains("komisyon", StringComparison.OrdinalIgnoreCase))
            return ("Firmalar/Teklifler", "Firms");
        if (field.StartsWith("Product:", StringComparison.OrdinalIgnoreCase) || field.StartsWith("Products", StringComparison.OrdinalIgnoreCase) || message.Contains("ürün", StringComparison.OrdinalIgnoreCase) || message.Contains("mal / hizmet", StringComparison.OrdinalIgnoreCase))
            return ("Mal/Hizmet Bilgisi", "General");
        if (field.StartsWith("Commission", StringComparison.OrdinalIgnoreCase))
            return ("Kurum Bilgisi / Komisyon", "Institution");
        if (field.StartsWith("Institution", StringComparison.OrdinalIgnoreCase) || message.Contains("kurum", StringComparison.OrdinalIgnoreCase) || message.Contains("idarenin", StringComparison.OrdinalIgnoreCase) || message.Contains("harcama yetkilisi", StringComparison.OrdinalIgnoreCase))
            return ("Kurum Bilgisi", "Institution");
        return ("Mal/Hizmet Bilgisi", "General");
    }

    private static bool EqualsAny(string value, params string[] candidates)
        => candidates.Any(candidate => value.Equals(candidate, StringComparison.OrdinalIgnoreCase));

    private void OutputPrecheck_Click(object sender, RoutedEventArgs e)
    {
        var data = BuildDataFromUi();
        var selected = OutputDocuments.Where(d => d.IsSelected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("En az bir belge seçiniz.", "Akıllı Ön Kontrol", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var lines = new List<string>();
        var missingCount = 0;
        foreach (var doc in selected)
        {
            var type = DocumentTypeFor(doc.Name);
            if (!OfficialDocumentService.IsImplemented(type))
            {
                lines.Add($"{doc.Name}: BU SÜRÜMDE DESTEKLENMİYOR");
                continue;
            }
            var core = DocumentReadinessService.GetExportCoreIssues(data, new[] { type });
            var advisory = ValidationService.ValidateDocument(data, type).Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().Count();
            missingCount += core.Count;
            lines.Add(core.Count == 0 ? $"{doc.Name}: ÇIKTIYA HAZIR" + (advisory > 0 ? $" • {advisory} uyarı" : string.Empty) : $"{doc.Name}: {core.Count} temel eksik");
        }
        var selectedTypes = selected.Where(d => OfficialDocumentService.IsImplemented(DocumentTypeFor(d.Name))).Select(d => DocumentTypeFor(d.Name)).ToList();
        var coreIssues = DocumentReadinessService.GetExportCoreIssues(data, selectedTypes);
        var body = string.Join("\n", lines);
        body += coreIssues.Count == 0 ? "\n\nPDF / YAZDIR: TEMEL KOŞULLAR HAZIR" : "\n\nPDF / YAZDIR İÇİN TAMAMLANMASI GEREKEN TEMEL KOŞULLAR:\n" + string.Join("\n", coreIssues.Take(12).Select(i => "• " + i));
        body += "\n\nNot: Önizleme eksik bilgilerle de çalışır. Temel çıktı koşulları hazırsa diğer eksikler yalnız uyarıdır; kullanıcı isterse PDF kaydedebilir veya yazdırabilir.";
        MessageBox.Show(body, "Çıktı Eksiklerini Göster", MessageBoxButton.OK, missingCount > 0 || coreIssues.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }

    private List<OutputDocumentType>? GetSelectedImplementedDocumentTypes(string actionTitle)
    {
        var selected = OutputDocuments.Where(d => d.IsSelected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Önce en az bir belge seçiniz.", actionTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }
        var unsupported = selected.Where(d => !OfficialDocumentService.IsImplemented(DocumentTypeFor(d.Name))).ToList();
        if (unsupported.Count > 0)
        {
            MessageBox.Show("Seçili belgeler arasında bu sürümde desteklenmeyen belge var:\n\n" + string.Join("\n", unsupported.Select(d => "• " + d.Name)), actionTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }
        return selected.Select(d => DocumentTypeFor(d.Name)).ToList();
    }

    private static bool ConfirmNonBlockingDocumentIssues(IReadOnlyList<string> messages, string actionName)
    {
        var distinctMessages = messages.Where(message => !string.IsNullOrWhiteSpace(message)).Distinct().Take(14).ToList();
        if (distinctMessages.Count == 0) return true;
        var body = "Belgede eksik veya kontrol edilmesi gereken bilgiler var.\n" +
                   "Bunlar önizlemeyi engellemez; temel çıktı koşulları sağlandığı için " + actionName + " işlemine devam edebilirsiniz.\n\n" +
                   string.Join("\n", distinctMessages.Select(message => "• " + message)) + "\n\nYine de devam etmek istiyor musunuz?";
        return MessageBox.Show(body, "DoğruTemin - Eksik Bilgi Uyarısı", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private FixedDocument? BuildSelectedOfficialDocumentForPreview()
    {
        var types = GetSelectedImplementedDocumentTypes("DoğruTemin - Önizleme");
        if (types is null) return null;

        var result = DocumentWorkflow.Prepare(BuildDataFromUi(), types, DocumentPreparationMode.Preview);
        return GetDocumentOrThrow(result);
    }

    private ProcurementData? _preparedExportData;
    private List<OutputDocumentType> _preparedExportTypes = new();

    private FixedDocument? BuildSelectedOfficialDocumentForExport(string actionName)
    {
        _preparedExportData = null;
        var types = GetSelectedImplementedDocumentTypes("DoğruTemin - Çıktı");
        if (types is null) return null;
        if (HasUnsavedWork() || string.IsNullOrWhiteSpace(_recordId))
        {
            if (_offerEditorDirty && !SaveOfferEditor(true)) return null;
            Save_Click(this, new RoutedEventArgs());
            if (HasUnsavedWork() || string.IsNullOrWhiteSpace(_recordId)) return null;
        }
        var data = _storage.LoadRecord(_recordId, out var warning) ?? throw new InvalidOperationException(warning);
        var result = DocumentWorkflow.Prepare(data, types, DocumentPreparationMode.Export);
        if (result.Status == DocumentPreparationStatus.Blocked)
        {
            MessageBox.Show("Seçili belge grubu için temel çıktı koşulları tamamlanmalıdır.\n\n1. grup (Ön Hazırlık): Kurum bilgisi + her iki komisyon + ürün(ler) ve miktar/adet.\n2. grup (Nihai İşlem): En az 1 ürün + en az 3 firma + yaklaşık/nihai teklifler + her iki komisyon.\n\nEksik olanlar:\n" + string.Join("\n", result.CoreIssues.Take(18).Select(i => "• " + i)), "DoğruTemin - Temel Çıktı Koşulları", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        if (result.Status == DocumentPreparationStatus.ConfirmationRequired)
        {
            if (!ConfirmNonBlockingDocumentIssues(result.ConfirmationMessages, actionName)) return null;
            result = DocumentWorkflow.Prepare(data, types, DocumentPreparationMode.Export, advisoriesConfirmed: true);
        }

        if (result.Status == DocumentPreparationStatus.WinnerSelectionRequired)
        {
            var tiedFirms = Firms.Where(f => result.TiedWinnerFirmIds.Contains(f.Id)).ToList();
            var chooser = new TieWinnerSelectionWindow(tiedFirms, NumberParser.Money(result.TiedMarketTotal ?? 0m)) { Owner = this };
            if (chooser.ShowDialog() != true || string.IsNullOrWhiteSpace(chooser.SelectedFirmId)) return null;
            _preferredWinnerFirmId = chooser.SelectedFirmId;
            MarkDirty();
            Save_Click(this, new RoutedEventArgs());
            if (HasUnsavedWork()) return null;
            data = _storage.LoadRecord(_recordId, out warning) ?? throw new InvalidOperationException(warning);
            result = DocumentWorkflow.Prepare(data, types, DocumentPreparationMode.Export, advisoriesConfirmed: true);
        }

        var document = GetDocumentOrThrow(result);
        if (document is not null)
        {
            PdfVectorWriter.Validate(document);
            _preparedExportData = data;
            _preparedExportTypes = types;
        }
        return document;
    }

    private static FixedDocument? GetDocumentOrThrow(DocumentWorkflowResult result)
    {
        if (result.Status == DocumentPreparationStatus.Failed)
            throw result.Error ?? new InvalidOperationException("Belge hazırlığı başarısız oldu.");

        return result.Status == DocumentPreparationStatus.Ready ? result.Document : null;
    }

    private void OutputPreview_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var document = BuildSelectedOfficialDocumentForPreview();
            if (document is null) return;
            new DocumentPreviewWindow(document) { Owner = this }.ShowDialog();
        }
        catch (Exception ex) { MessageBox.Show("Belge önizleme oluşturulamadı: " + ex.Message, "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void OutputPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var document = BuildSelectedOfficialDocumentForExport("PDF kaydetme");
            if (document is null) return;
            var path = SavePreparedOutput(document);
            UpdateHomeActivitySummary();
            UiNotificationService.Show(this, "Kayıtlı işlemden PDF oluşturuldu:\n" + path, TimeSpan.FromSeconds(5));
        }
        catch (Exception ex) { MessageBox.Show("PDF oluşturulamadı: " + ex.Message, "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void OutputPrint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var document = BuildSelectedOfficialDocumentForExport("yazdırma");
            if (document is null) return;
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true) return;
            SavePreparedOutput(document);
            dialog.PrintDocument(document.DocumentPaginator, "DoğruTemin Resmî Belgeleri");
        }
        catch (Exception ex) { MessageBox.Show("Yazdırma işlemi başlatılamadı: " + ex.Message, "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private string SavePreparedOutput(FixedDocument document)
    {
        var data = _preparedExportData ?? throw new InvalidOperationException("Kayıtlı belge kaynağı bulunamadı.");
        var path = new SavedDocumentService(_storage).Export(data, document, _preparedExportTypes);
        _lastOutput = data.LastOutput;
        RefreshArchiveRecords();
        return path;
    }

    private void ApprovalNoBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var raw = ApprovalNoBox.Text.Trim();
        if (raw.Length == 0) return;
        raw = Regex.Replace(raw, @"(?i)\bHYS\b\s*[-:/]*\s*", "").Trim();
        if (ApprovalNoBox.Text != raw)
        {
            _suppressDirtyTracking = true;
            ApprovalNoBox.Text = raw;
            _suppressDirtyTracking = false;
            MarkDirty();
        }
    }
}

using DogruTemin.Wpf.Models;
using FirmOfferCore = DogruTemin.Core.Models.FirmOffer;
using DogruTemin.Core.Services;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DogruTemin.Wpf;

public partial class MainWindow
{
    private void PrepareOffersView()
    {
        _suppressOfferEditorEvents = true;
        try
        {
            if (Firms.Count == 0)
            {
                OfferFirmSelector.SelectedItem = null;
                _offerEditingFirm = null;
                OfferRows.Clear();
                OfferTaxOfficeBox.Text = string.Empty;
                OfferDatePicker.SelectedDate = null;
                OfferSelectedTotalText.Text = "0,00 TL";
                SetOfferEditorBanner("FİRMA GEREKLİ", "Teklif girişi için önce en az bir firma ekleyin.", OfferBannerTone.Info);
                OfferSaveButton.IsEnabled = false;
                return;
            }

            if (Products.Count == 0)
            {
                if (OfferFirmSelector.SelectedItem is null) OfferFirmSelector.SelectedIndex = 0;
                _offerEditingFirm = OfferFirmSelector.SelectedItem as Firm;
                OfferRows.Clear();
                SetOfferEditorBanner("ÜRÜN GEREKLİ", "Teklif girişi için önce en az bir ürün ekleyin.", OfferBannerTone.Info);
                OfferSelectedTotalText.Text = "0,00 TL";
                OfferSaveButton.IsEnabled = false;
                return;
            }

            OfferSaveButton.IsEnabled = true;
            var selected = OfferFirmSelector.SelectedItem as Firm;
            if (selected is null || !Firms.Contains(selected))
            {
                _suppressOfferSelection = true;
                OfferFirmSelector.SelectedIndex = 0;
                _suppressOfferSelection = false;
                selected = OfferFirmSelector.SelectedItem as Firm;
            }
            LoadOfferFirm(selected);
        }
        finally
        {
            _suppressOfferEditorEvents = false;
        }
    }

    private void OfferFirmSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressOfferSelection || _suppressOfferEditorEvents) return;
        var next = OfferFirmSelector.SelectedItem as Firm;
        if (_offerEditorDirty && _offerEditingFirm is not null)
        {
            if (!SaveOfferEditor(true))
            {
                _suppressOfferSelection = true;
                OfferFirmSelector.SelectedItem = _offerEditingFirm;
                _suppressOfferSelection = false;
                return;
            }
        }
        LoadOfferFirm(next);
    }

    private void LoadOfferFirm(Firm? firm)
    {
        _suppressOfferEditorEvents = true;
        try
        {
            _offerEditingFirm = firm;
            _offerEditorDirty = false;
            OfferRows.Clear();
            if (firm is null)
            {
                OfferTaxOfficeBox.Text = string.Empty;
                OfferDatePicker.SelectedDate = null;
                OfferSelectedTotalText.Text = "0,00 TL";
                SetOfferEditorBanner("FİRMA SEÇİN", "Tekliflerini düzenlemek istediğiniz firmayı seçin.", OfferBannerTone.Info);
                return;
            }

            OfferTaxOfficeBox.Text = firm.TaxOffice ?? string.Empty;
            OfferDatePicker.SelectedDate = firm.OfferDate;
            foreach (var p in Products.Take(ProcurementLimits.MaxProducts))
            {
                var o = firm.Offers.FirstOrDefault(x => x.ProductId == p.Id);
                OfferRows.Add(new OfferEditorRow
                {
                    ProductId = p.Id,
                    No = p.No,
                    ProductName = p.Name,
                    QuantityDisplay = string.IsNullOrWhiteSpace(p.Quantity) ? "—" : $"{p.Quantity} {p.Unit}",
                    ApproximatePrice = o?.ApproximateUnitPrice?.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR")) ?? string.Empty,
                    FinalPrice = o?.FinalUnitPrice?.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR")) ?? string.Empty
                });
            }
            RefreshOfferEditorSummary(firm);
        }
        finally
        {
            _suppressOfferEditorEvents = false;
        }
    }

    private void OfferEditorText_Changed(object sender, TextChangedEventArgs e) => MarkOfferEditorDirty();
    private void OfferEditorDate_Changed(object sender, RoutedPropertyChangedEventArgs<DateTime?> e) => MarkOfferEditorDirty();
    private void OfferGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) => MarkOfferEditorDirty();

    private void MarkOfferEditorDirty()
    {
        if (_suppressOfferEditorEvents || _offerEditingFirm is null) return;
        _offerEditorDirty = true;
        if (!_suppressDirtyTracking) ShowDirtyStatus();
        var missingApprox = OfferRows.Count(r => r.IsApproximatePriceMissing);
        var missingFinal = OfferRows.Count(r => r.IsFinalPriceMissing);
        var parts = new List<string>();
        if (missingApprox > 0) parts.Add($"{missingApprox} yaklaşık fiyat eksik");
        if (missingFinal > 0) parts.Add($"{missingFinal} nihai fiyat eksik");
        var detail = parts.Count > 0 ? $"Kaydedilmemiş değişiklik var: {string.Join(", ", parts)}. Kırmızı alanları kontrol edin." : "Kaydedilmemiş değişiklik var. Tüm fiyat alanları dolu.";
        SetOfferEditorBanner("KAYDEDİLMEMİŞ DEĞİŞİKLİK", detail, OfferBannerTone.Dirty);
    }

    private void SaveOffer_Click(object sender, RoutedEventArgs e)
    {
        OfferGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        OfferGrid.CommitEdit(DataGridEditingUnit.Row, true);
        if (!SaveOfferEditor(true)) return;
        Save_Click(this, new RoutedEventArgs());
        if (!HasUnsavedWork())
        {
            LoadOfferFirm(_offerEditingFirm);
            SetOfferEditorBanner("TEKLİF KALICI KAYDEDİLDİ", "Teklif ve işlem bilgileri diske kaydedildi.", OfferBannerTone.Success);
        }
        else
            SetOfferEditorBanner("KALICI KAYIT TAMAMLANMADI", "Teklif değişikliği çalışma alanında duruyor. Kaydı tamamlayın veya değişiklikten vazgeçin.", OfferBannerTone.Warning);
    }

    private bool SaveOfferEditor(bool showErrors)
    {
        if (_offerEditingFirm is null) return true;
        if (Products.Count == 0) return true;

        var offers = new List<FirmOfferCore>();
        foreach (var row in OfferRows)
        {
            decimal? approx = null;
            decimal? final = null;
            if (!string.IsNullOrWhiteSpace(row.ApproximatePrice))
            {
                if (!NumberParser.TryPositiveDecimal(row.ApproximatePrice, out var value))
                {
                    if (showErrors) MessageBox.Show($"{row.No}. ürün yaklaşık birim fiyatı 0'dan büyük, en fazla iki ondalık basamaklı bir sayı olmalıdır.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                approx = value;
            }
            if (!string.IsNullOrWhiteSpace(row.FinalPrice))
            {
                if (!NumberParser.TryPositiveDecimal(row.FinalPrice, out var value))
                {
                    if (showErrors) MessageBox.Show($"{row.No}. ürün nihai birim fiyatı 0'dan büyük, en fazla iki ondalık basamaklı bir sayı olmalıdır.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                final = value;
            }
            offers.Add(new FirmOfferCore { ProductId = row.ProductId, ApproximateUnitPrice = approx, FinalUnitPrice = final });
        }

        _offerEditingFirm.TaxOffice = OfferTaxOfficeBox.Text.Trim();
        _offerEditingFirm.OfferDate = OfferDatePicker.SelectedDate;
        _offerEditingFirm.Offers = offers;
        _preferredWinnerFirmId = string.Empty;
        _offerEditorDirty = false;
        RefreshOfferEditorSummary(_offerEditingFirm, justSaved: true);
        MarkDirty();
        return true;
    }

    private void RefreshOfferEditorSummary(Firm firm, bool justSaved = false)
    {
        decimal total = 0;
        var finalComplete = Products.Count > 0;
        var missingFinal = 0;
        var missingApprox = 0;
        foreach (var p in Products.Take(ProcurementLimits.MaxProducts))
        {
            var o = firm.Offers.FirstOrDefault(x => x.ProductId == p.Id);
            if (o?.ApproximateUnitPrice is not > 0) missingApprox++;
            if (o?.FinalUnitPrice is not > 0 || !NumberParser.TryPositiveDecimal(p.Quantity, out var qty))
            {
                finalComplete = false;
                missingFinal++;
                continue;
            }
            total += o.FinalUnitPrice.Value * qty;
        }

        OfferSelectedTotalText.Text = NumberParser.Money(finalComplete ? total : 0m);
        var anyMissing = missingApprox > 0 || missingFinal > 0;
        if (!anyMissing)
        {
            SetOfferEditorBanner(
                justSaved ? "TEKLİF HAZIR • KAYIT BEKLİYOR" : "FİYATLAR TAMAM",
                justSaved ? $"{firm.Name}: teklif çalışma alanına alındı; işlem kaydı tamamlanana kadar değişiklik kalıcı değildir." : $"{firm.Name}: yaklaşık ve nihai fiyatların tamamı girildi.",
                OfferBannerTone.Success);
        }
        else
        {
            var details = new List<string>();
            if (missingApprox > 0) details.Add($"{missingApprox} yaklaşık fiyat eksik");
            if (missingFinal > 0) details.Add($"{missingFinal} nihai fiyat eksik");
            var consequence = missingFinal > 0 ? "Nihai fiyatı eksik firma değerlendirmeye alınmaz." : "Yaklaşık maliyet hesabı ve ilgili belge eksik kalır; firma nihai teklif değerlendirmesine katılabilir.";
            SetOfferEditorBanner(
                justSaved ? "KAYIT BEKLİYOR • EKSİK VAR" : "FİYAT EKSİK",
                $"{firm.Name}: {string.Join(", ", details)}. {consequence}" + (justSaved ? " Teklif çalışma alanında; kalıcı işlem kaydı henüz tamamlanmadı." : " Kırmızı işaretli alanları kontrol edin."),
                OfferBannerTone.Warning);
        }
    }

    private enum OfferBannerTone { Info, Dirty, Warning, Success }

    private void SetOfferEditorBanner(string title, string message, OfferBannerTone tone)
    {
        OfferEditorStatusTitle.Text = title;
        OfferEditorStatusText.Text = message;
        (Color background, Color border, Color iconBackground, Color titleColor, Color textColor, string icon) = tone switch
        {
            OfferBannerTone.Success => (Color.FromRgb(232, 248, 238), Color.FromRgb(139, 211, 164), Color.FromRgb(211, 242, 221), Color.FromRgb(31, 122, 69), Color.FromRgb(43, 108, 67), "✓"),
            OfferBannerTone.Warning => (Color.FromRgb(255, 245, 229), Color.FromRgb(244, 187, 92), Color.FromRgb(255, 231, 190), Color.FromRgb(170, 91, 0), Color.FromRgb(142, 84, 18), "!"),
            OfferBannerTone.Dirty => (Color.FromRgb(255, 247, 230), Color.FromRgb(241, 185, 78), Color.FromRgb(255, 232, 187), Color.FromRgb(171, 96, 0), Color.FromRgb(135, 81, 17), "!"),
            _ => (Color.FromRgb(234, 243, 255), Color.FromRgb(189, 213, 242), Color.FromRgb(216, 233, 255), Color.FromRgb(24, 75, 122), Color.FromRgb(54, 95, 135), "i")
        };
        OfferEditorStatusBanner.Background = new SolidColorBrush(background);
        OfferEditorStatusBanner.BorderBrush = new SolidColorBrush(border);
        OfferEditorStatusIconBorder.Background = new SolidColorBrush(iconBackground);
        OfferEditorStatusTitle.Foreground = new SolidColorBrush(titleColor);
        OfferEditorStatusText.Foreground = new SolidColorBrush(textColor);
        OfferEditorStatusIcon.Foreground = new SolidColorBrush(titleColor);
        OfferEditorStatusIcon.Text = icon;
    }
}

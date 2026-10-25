using AppSettings = DogruTemin.Core.Models.ApplicationSettings;
using DogruTemin.Core.Services;
using DogruTemin.Wpf.Models;
using DogruTemin.Wpf.Services;
using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DogruTemin.Wpf;

public partial class MainWindow
{
    private DogruTemin.Core.Models.InstitutionInfo BuildInstitutionFromUi() => new()
    {
        TopAuthority = InstitutionTopAuthorityBox.Text.Trim(),
        ShortName = InstitutionFullNameBox.Text.Trim(),
        FullName = InstitutionFullNameBox.Text.Trim(),
        Province = InstitutionProvinceBox.Text.Trim(),
        Address = InstitutionAddressBox.Text.Trim(),
        Phone = InstitutionPhoneBox.Text.Trim(),
        Email = InstitutionEmailBox.Text.Trim().ToLowerInvariant(),
        AuthorizedName = AuthorizedNameBox.Text.Trim(),
        AuthorizedDuty = AuthorizedDutyBox.Text.Trim(),
        AuthorizedTitle = AuthorizedTitleBox.Text.Trim()
    };

    private void SaveApplicationSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings = new AppSettings
            {
                Institution = BuildInstitutionFromUi(),
                DefaultUnit = "Adet",
                DefaultInspectionCommission = new List<DogruTemin.Core.Models.CommissionMember>(),
                DefaultMarketCommission = new List<DogruTemin.Core.Models.CommissionMember>()
            };
            _storage.SaveSettings(_settings);
            UiNotificationService.Show(this, "Kurum bilgileri kaydedildi. Yeni işlemlerde otomatik kullanılacak.", TimeSpan.FromSeconds(4));
        }
        catch (Exception ex)
        {
            MessageBox.Show("Kurum bilgileri kaydedilemedi: " + ex.Message, "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e) => OpenFolder(_storage.DataFolder);
    private void OpenBackupFolder_Click(object sender, RoutedEventArgs e) => OpenFolder(_storage.BackupFolder);
    private void OpenLogFolder_Click(object sender, RoutedEventArgs e) => OpenFolder(_storage.LogFolder);

    private void CreateDiagnosticPackage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DiagnosticPackageService.Create(_safety);
            UiNotificationService.Show(this, "Tanılama paketi oluşturuldu:\n" + path, TimeSpan.FromSeconds(6));
        }
        catch (Exception ex)
        {
            MessageBox.Show("Tanılama paketi oluşturulamadı:\n\n" + ex.Message, "DoğruTemin - Tanılama", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateSafetyPanel(SafetyCheckResult result)
    {
        if (AboutVersionText is null || DataIntegrityText is null) return;
        AboutVersionText.Text = $"DoğruTemin {AppVersionInfo.DisplayVersion}";
        AboutRuntimeText.Text = $"Windows {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")}  •  .NET {ApplicationSafetyService.RuntimeDescription}";
        AboutDataFolderText.Text = _storage.RootFolder;
        DataIntegrityText.Text = result.Message;
        DataIntegrityText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(result.IsHealthy ? "#7EE2A6" : "#FFA5A5"));
    }

    private void RunIntegrityCheck_Click(object sender, RoutedEventArgs e)
    {
        var result = _safety.CheckIntegrity();
        UpdateSafetyPanel(result);
        if (result.IsHealthy) UiNotificationService.Show(this, result.Message, TimeSpan.FromSeconds(4));
        else MessageBox.Show(result.Message, "DoğruTemin - Veri Bütünlüğü", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void CreateSafetyBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (HasUnsavedWork())
            {
                var answer = MessageBox.Show("Aktif işlemde kaydedilmemiş değişiklikler var. Güvenlik yedeğine son değişikliklerin de girmesi için önce kayıt yapılsın mı?", "DoğruTemin - Güvenlik Yedeği", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (answer == MessageBoxResult.Cancel) return;
                if (answer == MessageBoxResult.Yes)
                {
                    Save_Click(this, new RoutedEventArgs());
                    if (HasUnsavedWork()) return;
                }
            }
            var path = _safety.CreateManualSafetyBackup();
            UiNotificationService.Show(this, $"Güvenlik yedeği oluşturuldu:\n{path}", TimeSpan.FromSeconds(5));
            UpdateSafetyPanel(_safety.CheckIntegrity());
        }
        catch (Exception ex)
        {
            MessageBox.Show("Güvenlik yedeği oluşturulamadı: " + ex.Message, "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestoreSafetyBackup_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmUnsavedWork("yedekten geri yüklemeye devam et", "DoğruTemin - Yedekten Geri Yükle")) return;
        var dialog = new OpenFileDialog
        {
            Title = "DoğruTemin Yedeğini Seç",
            Filter = "DoğruTemin ZIP yedeği (*.zip)|*.zip|Tüm dosyalar (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true) return;
        var confirm = MessageBox.Show("Seçilen yedek geri yüklenecek. Mevcut veriler değiştirilmeden önce otomatik güvenlik yedeği oluşturulacaktır.\n\nDevam edilsin mi?", "DoğruTemin - Yedekten Geri Yükle", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;
        var result = _safety.RestoreFromBackup(dialog.FileName);
        if (!result.Success)
        {
            MessageBox.Show(result.Message, "DoğruTemin - Yedekten Geri Yükle", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        var message = result.Message;
        if (!string.IsNullOrWhiteSpace(result.SafetyBackupPath)) message += "\n\nGeri yükleme öncesi mevcut verinin güvenlik kopyası:\n" + result.SafetyBackupPath!;
        message += "\n\nYeni verilerin güvenli biçimde yüklenmesi için DoğruTemin yeniden başlatılacak.";
        MessageBox.Show(message, "DoğruTemin - Yedekten Geri Yükle", MessageBoxButton.OK, MessageBoxImage.Information);
        _closeApproved = true;
        RecoverySessionService.EndCleanly();
        RestartApplication();
    }

    private static void RestartApplication()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(exe) && File.Exists(exe))
            {
                App.ReleaseSingleInstanceForRestart();
                Process.Start(new ProcessStartInfo { FileName = exe, UseShellExecute = true });
            }
        }
        catch { }
        Application.Current.Shutdown();
    }

    private static void OpenFolder(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Klasör açılamadı: " + ex.Message, "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshStatisticsYears()
    {
        var statisticsYearBox = StatisticsYearBox;
        if (statisticsYearBox is null) return;
        var selected = statisticsYearBox.SelectedItem?.ToString();
        var years = _storage.ListRecords().Where(x => x.Dates.ApprovalDate.HasValue).Select(x => x.Dates.ApprovalDate!.Value.Year).Distinct().OrderByDescending(x => x).ToList();
        if (!years.Contains(DateTime.Now.Year)) years.Add(DateTime.Now.Year);
        years = years.Distinct().OrderByDescending(x => x).ToList();
        StatisticsYears.Clear();
        StatisticsYears.Add("Tüm Yıllar");
        foreach (var year in years) StatisticsYears.Add(year.ToString(CultureInfo.InvariantCulture));
        var target = !string.IsNullOrWhiteSpace(selected) && StatisticsYears.Contains(selected) ? selected : DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
        statisticsYearBox.SelectedItem = StatisticsYears.Contains(target) ? target : "Tüm Yıllar";
    }

    private void RefreshStatistics_Click(object sender, RoutedEventArgs e)
    {
        RefreshStatisticsYears();
        RefreshStatistics();
    }

    private void StatisticsYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_suppressDirtyTracking) RefreshStatistics();
    }

    private void RefreshStatistics()
    {
        if (StatisticsOperationCountText is null || StatisticsYearBox is null) return;
        var allRecords = _storage.ListRecords().ToList();
        var missingDateCount = allRecords.Count(x => !x.Dates.ApprovalDate.HasValue);
        var selectedText = StatisticsYearBox.SelectedItem?.ToString() ?? DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
        int? selectedYear = int.TryParse(selectedText, out var parsedYear) ? parsedYear : null;
        var records = allRecords.Where(x => x.Dates.ApprovalDate.HasValue).Where(x => !selectedYear.HasValue || x.Dates.ApprovalDate!.Value.Year == selectedYear.Value).OrderBy(x => x.Dates.ApprovalDate).ToList();
        decimal approximateTotal = 0m, finalTotal = 0m, savingsTotal = 0m;
        var financialCompleteCount = 0;
        var operationRows = new List<StatisticsOperationRow>();
        var monthCounts = new int[13];
        var monthTotals = new decimal[13];

        foreach (var data in records)
        {
            var calc = ProcurementEngine.Calculate(data);
            var date = data.Dates.ApprovalDate!.Value;
            monthCounts[date.Month]++;
            if (calc.MarketResultTotal > 0m) monthTotals[date.Month] += calc.MarketResultTotal;
            if (calc.ApproximateCostTotal > 0m && calc.MarketResultTotal > 0m)
            {
                financialCompleteCount++;
                approximateTotal += calc.ApproximateCostTotal;
                finalTotal += calc.MarketResultTotal;
                var savings = calc.ApproximateCostTotal - calc.MarketResultTotal;
                savingsTotal += savings;
                operationRows.Add(new StatisticsOperationRow
                {
                    RecordId = data.RecordId,
                    JobTitle = string.IsNullOrWhiteSpace(data.JobTitle) ? "Başlıksız işlem" : data.JobTitle,
                    Date = date,
                    DateText = date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                    ApproximateTotal = calc.ApproximateCostTotal,
                    FinalTotal = calc.MarketResultTotal,
                    Savings = savings,
                    FinalText = NumberParser.Money(calc.MarketResultTotal),
                    SavingsText = (savings >= 0m ? "+" : "") + NumberParser.Money(savings)
                });
            }
        }

        StatisticsOperationCountText.Text = records.Count.ToString(CultureInfo.GetCultureInfo("tr-TR"));
        StatisticsApproximateText.Text = approximateTotal > 0m ? NumberParser.Money(approximateTotal) : "—";
        StatisticsFinalText.Text = finalTotal > 0m ? NumberParser.Money(finalTotal) : "—";
        StatisticsSavingsText.Text = (approximateTotal > 0m || finalTotal > 0m) ? NumberParser.Money(savingsTotal) : "—";
        StatisticsSavingsText.Foreground = savingsTotal < 0m ? new SolidColorBrush(Color.FromRgb(190, 55, 55)) : new SolidColorBrush(Color.FromRgb(36, 138, 80));
        var rate = approximateTotal > 0m ? (savingsTotal / approximateTotal) * 100m : 0m;
        StatisticsSavingsRateText.Text = "%" + rate.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));
        StatisticsAverageText.Text = financialCompleteCount > 0 ? NumberParser.Money(finalTotal / financialCompleteCount) : "—";
        StatisticsExcludedText.Text = missingDateCount > 0 ? $"{missingDateCount} kayıt Harcama Belgesi tarihi olmadığı için dışarıda" : string.Empty;
        StatisticsMonthlyHintText.Text = $"{financialCompleteCount} finansal kaydı tamamlanmış işlem";

        MonthlyStatistics.Clear();
        var maxCount = Math.Max(1, monthCounts.Max());
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        for (var month = 1; month <= 12; month++)
        {
            MonthlyStatistics.Add(new MonthlyStatisticRow
            {
                MonthNumber = month,
                Month = tr.DateTimeFormat.GetAbbreviatedMonthName(month).TrimEnd('.').ToUpper(tr),
                Count = monthCounts[month],
                FinalTotal = monthTotals[month],
                TotalText = monthTotals[month] > 0m ? NumberParser.Money(monthTotals[month]) : "—",
                BarWidth = monthCounts[month] == 0 ? 0 : Math.Max(8, Math.Round(250.0 * monthCounts[month] / maxCount, 1))
            });
        }

        StatisticsOperations.Clear();
        foreach (var row in operationRows.OrderByDescending(x => x.FinalTotal).ThenByDescending(x => x.Date).Take(10)) StatisticsOperations.Add(row);
        var highest = operationRows.OrderByDescending(x => x.FinalTotal).FirstOrDefault();
        var lowest = operationRows.OrderBy(x => x.FinalTotal).FirstOrDefault();
        StatisticsHighestText.Text = highest is null ? "—" : $"{highest.JobTitle}  •  {highest.FinalText}";
        StatisticsLowestText.Text = lowest is null ? "—" : $"{lowest.JobTitle}  •  {lowest.FinalText}";
    }

    private int? SelectedStatisticsYear()
    {
        var selectedText = StatisticsYearBox?.SelectedItem?.ToString();
        return int.TryParse(selectedText, out var year) ? year : null;
    }

    private ManagementReportSnapshot BuildCurrentManagementReport()
    {
        var institutionName = _settings?.Institution?.FullName;
        return ManagementReportService.BuildSnapshot(_storage.ListRecords(), SelectedStatisticsYear(), institutionName);
    }

    private static string ReportPeriodFilePart(string period)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var value = new string(period.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return value.Replace(' ', '_');
    }

    private void ExportStatisticsPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var snapshot = BuildCurrentManagementReport();
            if (snapshot.IncludedOperationCount == 0)
            {
                MessageBox.Show("Seçili rapor döneminde Harcama Belgesi tarihi bulunan işlem yok.", "DoğruTemin - Yönetim Raporu", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var dialog = new SaveFileDialog
            {
                Title = "Yönetim Raporunu PDF Olarak Kaydet",
                Filter = "PDF Dosyası (*.pdf)|*.pdf",
                FileName = $"DogruTemin_Yonetim_Raporu_{ReportPeriodFilePart(snapshot.PeriodLabel)}.pdf",
                AddExtension = true,
                DefaultExt = ".pdf"
            };
            if (dialog.ShowDialog(this) != true) return;
            var document = ManagementReportService.BuildPdf(snapshot);
            PdfImageWriter.Save(document, dialog.FileName);
            UpdateHomeActivitySummary();
            UiNotificationService.Show(this, $"Yönetim raporu PDF olarak oluşturuldu:\n{dialog.FileName}", TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            MessageBox.Show("PDF yönetim raporu oluşturulamadı: " + ex.Message, "DoğruTemin - Yönetim Raporu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportStatisticsExcel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var snapshot = BuildCurrentManagementReport();
            if (snapshot.IncludedOperationCount == 0)
            {
                MessageBox.Show("Seçili rapor döneminde Harcama Belgesi tarihi bulunan işlem yok.", "DoğruTemin - Yönetim Raporu", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var dialog = new SaveFileDialog
            {
                Title = "Yönetim Raporunu Excel Olarak Kaydet",
                Filter = "Excel Çalışma Kitabı (*.xlsx)|*.xlsx",
                FileName = $"DogruTemin_Yonetim_Raporu_{ReportPeriodFilePart(snapshot.PeriodLabel)}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog(this) != true) return;
            ManagementReportService.SaveExcel(snapshot, dialog.FileName);
            UiNotificationService.Show(this, $"Yönetim raporu Excel olarak oluşturuldu:\n{dialog.FileName}", TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            MessageBox.Show("Excel yönetim raporu oluşturulamadı: " + ex.Message, "DoğruTemin - Yönetim Raporu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

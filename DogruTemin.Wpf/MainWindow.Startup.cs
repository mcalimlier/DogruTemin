using DogruTemin.Core.Models;
using DogruTemin.Core.Services;
using DogruTemin.Wpf.Services;
using System;
using System.Windows;
using System.Windows.Data;

namespace DogruTemin.Wpf;

public partial class MainWindow
{
    public MainWindow() : this(null)
    {
    }

    public MainWindow(Action<int, string>? startupProgress)
        : this(new StorageService(), startupProgress)
    {
    }

    internal MainWindow(StorageService storage, Action<int, string>? startupProgress = null)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _startupProgress = startupProgress;
        _procurementWorkflow = new ProcurementWorkflow(_storage);
        _archiveWorkflow = new ArchiveWorkflow(_storage);
        _safety = new ApplicationSafetyService(_storage);
        ReportStartup(22, "Arayüz bileşenleri hazırlanıyor...");
        InitializeComponent();
        ReportStartup(40, "Görsel kalite ve erişilebilirlik katmanı yükleniyor...");
        UiPolishService.Apply(this);
        SourceInitialized += MainWindow_SourceInitialized;

        ApplyTaskbarIcon();
        ConfigureFixedWindowForCurrentScreen();
        UpdateMaximizeRestoreButton();
        DataContext = this;
        _archiveView = CollectionViewSource.GetDefaultView(ArchiveRecords);
        _archiveView.Filter = ArchiveRecordMatchesFilter;
        ReportStartup(52, "Veri klasörleri ve güvenlik denetleniyor...");
        var startupSafety = _safety.InitializeAndCheck();
        RecoverySessionService.Begin(_storage, () => _safety);
        Loaded += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(RecoverySessionService.PendingNotice))
            {
                UiNotificationService.Show(this, RecoverySessionService.PendingNotice!, TimeSpan.FromSeconds(8));
                RecoverySessionService.ClearPendingNotice();
            }
        };
        ReportStartup(64, "Ayarlar yükleniyor...");
        _settings = _storage.LoadSettings();
        foreach (System.Windows.Controls.ComboBoxItem item in WindowSizeChoice.Items)
            if ((string)item.Tag == _settings.WindowSizePreference) WindowSizeChoice.SelectedItem = item;
        Loaded += (_, _) => ResponsiveLayoutService.ApplyPreference(this, _settings.WindowSizePreference);

        _suppressDirtyTracking = true;
        ReportStartup(73, "Aktif işlem ve kayıtlar yükleniyor...");
        var loaded = _storage.Load(out var loadWarning);
        if (loaded is null)
        {
            InitializeBlankState(_settings);
        }
        else
        {
            ApplyDataToUi(loaded);
            _storage.EnsureArchived(loaded);
        }
        SeedOutputDocuments();
        ReportStartup(84, "Belge ve rapor bileşenleri hazırlanıyor...");

        ProductCountText.Text = $"{Products.Count} / 10 ürün";
        _suppressDirtyTracking = false;
        UpdateDerivedState();
        SetSavedStatus(loaded?.LastSavedAt);
        RefreshArchiveRecords();
        RefreshStatisticsYears();
        ReportStartup(92, "Arşiv ve istatistik görünümü hazırlanıyor...");
        UpdateSafetyPanel(startupSafety);
        ShowView("Home");

        ReportStartup(97, "Hesap motoru son kontrolden geçiyor...");
        var selfTest = EngineSelfTest.Run();
        if (!selfTest.Success)
            MessageBox.Show("C# hesap motoru öz testi başarısız: " + selfTest.Message, "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error);
        if (!string.IsNullOrWhiteSpace(loadWarning))
            MessageBox.Show(loadWarning, "DoğruTemin - Veri kurtarma", MessageBoxButton.OK, MessageBoxImage.Warning);
        if (!startupSafety.IsHealthy)
            MessageBox.Show(startupSafety.Message, "DoğruTemin - Veri Güvenliği", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}

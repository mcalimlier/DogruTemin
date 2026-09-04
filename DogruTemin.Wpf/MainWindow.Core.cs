using DogruTemin.Wpf.Models;
using ProcurementData = DogruTemin.Core.Models.ProcurementData;
using ValidationSeverity = DogruTemin.Core.Models.ValidationSeverity;
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

public partial class MainWindow : Window
{
    public ObservableCollection<Firm> Firms { get; } = new();
    public ObservableCollection<ProductItem> Products { get; } = new();
    public ObservableCollection<CommissionMember> InspectionCommission { get; } = new();
    public ObservableCollection<CommissionMember> MarketCommission { get; } = new();
    public ObservableCollection<OutputDocument> OutputDocuments { get; } = new();
    public ObservableCollection<OutputDocument> PreparationOutputDocuments { get; } = new();
    public ObservableCollection<OutputDocument> FinalOutputDocuments { get; } = new();
    public ObservableCollection<OfferEditorRow> OfferRows { get; } = new();
    public ObservableCollection<ArchiveRecordRow> ArchiveRecords { get; } = new();
    public ObservableCollection<ValidationCenterRow> ValidationRows { get; } = new();
    public ObservableCollection<string> StatisticsYears { get; } = new();
    public ObservableCollection<MonthlyStatisticRow> MonthlyStatistics { get; } = new();
    public ObservableCollection<StatisticsOperationRow> StatisticsOperations { get; } = new();

    public ObservableCollection<string> CommissionTitles { get; } = new()
    {
        "Şube Müdürü", "Şef", "Memur", "Okul Müdürü", "Müdür Yardımcısı", "Öğretmen"
    };

    private Firm? _editingFirm;
    private ProductItem? _editingProduct;
    private bool _firmEditorDirty;
    private bool _productEditorDirty;
    private Firm? _offerEditingFirm;
    private bool _offerEditorDirty;
    private bool _suppressOfferEditorEvents;
    private bool _suppressOfferSelection;
    private bool _suppressDirtyTracking = true;
    private bool _hasUnsavedChanges;
    private DateTime? _lastSavedAt;

    private string _lastDateSequenceWarning = string.Empty;
    private bool _closeApproved;
    private string _currentView = "Home";
    private readonly StorageService _storage = new();
    private readonly ApplicationSafetyService _safety;
    private AppSettings _settings = new();
    private string _operationNumber = string.Empty;
    private string _recordId = string.Empty;
    private DateTime _recordCreatedAt = DateTime.Now;
    private string _preferredWinnerFirmId = string.Empty;
    private ICollectionView? _archiveView;
    private string _homeNextView = "General";
    private bool _dataEntryExpanded;
    private readonly Action<int, string>? _startupProgress;

    private const double DesignWidth = 1450.0;
    private const double DesignHeight = 945.0;
    private const int WmGetMinMaxInfo = 0x0024;
    private const uint MonitorDefaultToNearest = 0x00000002;
    private HwndSource? _windowSource;

    public MainWindow() : this(null)
    {
    }

    public MainWindow(Action<int, string>? startupProgress)
    {
        _startupProgress = startupProgress;
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

        ProductCountText.Text = $"{Products.Count} / 8 ürün";
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

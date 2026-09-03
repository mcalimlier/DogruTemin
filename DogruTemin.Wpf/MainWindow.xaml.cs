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

    // Borderless WPF penceresinin gerçek çalışma alanına (görev çubuğu hariç)
    // maksimize olmasını sağlar. Böylece R19 yerleşimi Viewbox içinde ölçeklenir,
    // paneller üst üste binmez ve görev çubuğunun üzerine taşılmaz.
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

    private void ReportStartup(int percent, string stage)
    {
        try { _startupProgress?.Invoke(percent, stage); } catch { }
    }

    private void ApplyTaskbarIcon()
    {
        try
        {
            var iconUri = new Uri("pack://application:,,,/Assets/AppIcon.ico", UriKind.Absolute);
            Icon = BitmapFrame.Create(iconUri);
        }
        catch
        {
            // The XAML/ApplicationIcon fallback remains active if the runtime icon cannot be loaded.
        }
    }

    private void ConfigureFixedWindowForCurrentScreen()
    {
        var workArea = SystemParameters.WorkArea;
        var maxWidth = System.Math.Min(DesignWidth, workArea.Width * 0.94);
        var maxHeight = System.Math.Min(DesignHeight, workArea.Height * 0.92);
        var scale = System.Math.Min(maxWidth / DesignWidth, maxHeight / DesignHeight);

        Width = System.Math.Round(DesignWidth * scale);
        Height = System.Math.Round(DesignHeight * scale);
        Left = workArea.Left + ((workArea.Width - Width) / 2.0);
        Top = workArea.Top + ((workArea.Height - Height) / 2.0);
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _windowSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        _windowSource?.AddHook(WindowMessageHook);
    }

    private IntPtr WindowMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmGetMinMaxInfo || lParam == IntPtr.Zero)
            return IntPtr.Zero;

        var minMax = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor != IntPtr.Zero)
        {
            var monitorInfo = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(monitor, ref monitorInfo))
            {
                var work = monitorInfo.rcWork;
                var bounds = monitorInfo.rcMonitor;

                minMax.ptMaxPosition.x = work.left - bounds.left;
                minMax.ptMaxPosition.y = work.top - bounds.top;
                minMax.ptMaxSize.x = work.right - work.left;
                minMax.ptMaxSize.y = work.bottom - work.top;
            }
        }

        Marshal.StructureToPtr(minMax, lParam, false);
        handled = true;
        return IntPtr.Zero;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint ptReserved;
        public NativePoint ptMaxSize;
        public NativePoint ptMaxPosition;
        public NativePoint ptMinTrackSize;
        public NativePoint ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int cbSize;
        public NativeRect rcMonitor;
        public NativeRect rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    private void InitializeBlankState(AppSettings? settings = null)
    {
        Firms.Clear();
        Products.Clear();
        InspectionCommission.Clear();
        MarketCommission.Clear();
        OfferRows.Clear();
        _editingFirm = null;
        _editingProduct = null;
        _offerEditingFirm = null;
        _offerEditorDirty = false;
        _preferredWinnerFirmId = string.Empty;
        _recordId = string.Empty;
        _recordCreatedAt = DateTime.Now;

        settings ??= _settings;
        for (var i = 1; i <= 3; i++)
        {
            InspectionCommission.Add(new CommissionMember { No = i });
            MarketCommission.Add(new CommissionMember { No = i });
        }

        AssignmentDatePicker.SelectedDate = null;
        ApprovalDatePicker.SelectedDate = null;
        CostCalculationDatePicker.SelectedDate = null;
        MarketReportDatePicker.SelectedDate = null;
        InspectionReportDatePicker.SelectedDate = null;
        DeliveryDatePicker.SelectedDate = null;

        JobTitleBox.Text = string.Empty;
        AssignmentNoBox.Text = string.Empty;
        ApprovalNoBox.Text = string.Empty;
        var preservedInstitution = settings.Institution;
        InstitutionTopAuthorityBox.Text = preservedInstitution?.TopAuthority ?? string.Empty;
        InstitutionFullNameBox.Text = !string.IsNullOrWhiteSpace(preservedInstitution?.FullName) ? preservedInstitution!.FullName : preservedInstitution?.ShortName ?? string.Empty;
        InstitutionProvinceBox.Text = preservedInstitution?.Province ?? string.Empty;
        InstitutionAddressBox.Text = preservedInstitution?.Address ?? string.Empty;
        InstitutionPhoneBox.Text = preservedInstitution?.Phone ?? string.Empty;
        InstitutionEmailBox.Text = preservedInstitution?.Email ?? string.Empty;
        AuthorizedNameBox.Text = preservedInstitution?.AuthorizedName ?? string.Empty;
        AuthorizedDutyBox.Text = preservedInstitution?.AuthorizedDuty ?? string.Empty;
        AuthorizedTitleBox.Text = preservedInstitution?.AuthorizedTitle ?? string.Empty;
        AutoBackupCheckBox.IsChecked = true;
        _operationNumber = string.Empty;
        ClearFirmForm();
        ClearProductForm();
    }

    private void SeedOutputDocuments()
    {
        OutputDocuments.Clear();
        PreparationOutputDocuments.Clear();
        FinalOutputDocuments.Clear();

        var approximateRequest = new OutputDocument { Name = "Yaklaşık Maliyet Talep Yazısı", IsSelected = false, Status = "Uyarı", Note = "Ön hazırlık belgesi" };
        var firmOfferForm = new OutputDocument { Name = "Firma Teklif Formu", IsSelected = false, Status = "Uyarı", Note = "Ön hazırlık belgesi" };
        var approximateCostSheet = new OutputDocument { Name = "Yaklaşık Maliyet Hesap Cetveli", IsSelected = false, Status = "Uyarı", Note = "Nihai işlem belgesi" };
        var marketResearchReport = new OutputDocument { Name = "Piyasa Fiyat Araştırması Tutanağı", IsSelected = true, Status = "Uyarı", Note = "Nihai işlem belgesi" };
        var inspectionReport = new OutputDocument { Name = "Muayene ve Kabul Tutanağı", IsSelected = true, Status = "Uyarı", Note = "Nihai işlem belgesi" };

        PreparationOutputDocuments.Add(approximateRequest);
        PreparationOutputDocuments.Add(firmOfferForm);

        FinalOutputDocuments.Add(approximateCostSheet);
        FinalOutputDocuments.Add(marketResearchReport);
        FinalOutputDocuments.Add(inspectionReport);

        foreach (var doc in PreparationOutputDocuments.Concat(FinalOutputDocuments))
            OutputDocuments.Add(doc);
    }

    private void ShowView(string view)
    {
        // Eski/haricî yönlendirmeler için Commission artık Kurum Bilgisi içindeki komisyon bölümüne gider.
        if (view == "Commission") view = "Institution";
        _currentView = view;

        HomeView.Visibility = view == "Home" ? Visibility.Visible : Visibility.Collapsed;
        GeneralView.Visibility = view == "General" ? Visibility.Visible : Visibility.Collapsed;
        FirmsOffersView.Visibility = view is "Firms" or "Offers" ? Visibility.Visible : Visibility.Collapsed;
        DocumentsDatesView.Visibility = view is "Dates" or "Output" ? Visibility.Visible : Visibility.Collapsed;
        InstitutionView.Visibility = view == "Institution" ? Visibility.Visible : Visibility.Collapsed;
        ControlView.Visibility = view == "Control" ? Visibility.Visible : Visibility.Collapsed;
        StatisticsView.Visibility = view == "Statistics" ? Visibility.Visible : Visibility.Collapsed;
        ArchiveView.Visibility = view == "Archive" ? Visibility.Visible : Visibility.Collapsed;
        AboutSafetyView.Visibility = view == "AboutSafety" ? Visibility.Visible : Visibility.Collapsed;

        // Firma/Teklif birleşik sayfasında ilgili alt bölümü görünür alana getir.
        // Belgeler/Tarihler R16'da varsayılan pencereye bütünüyle sığdığı için ayrıca kaydırılmaz.
        if (view == "Firms") Dispatcher.BeginInvoke(new Action(() => FirmsView?.BringIntoView()));
        if (view == "Offers") Dispatcher.BeginInvoke(new Action(() => OffersView?.BringIntoView()));

        var dataEntryView = view is "Institution" or "General" or "Firms" or "Offers" or "Dates" or "Output";
        if (dataEntryView)
        {
            _dataEntryExpanded = true;
            UpdateDataEntryMenuState();
        }

        NavHome.Tag = view == "Home" ? "Active" : null;
        NavDataEntry.Tag = dataEntryView ? "Active" : null;
        NavInstitution.Tag = view == "Institution" ? "Active" : null;
        NavGeneral.Tag = view == "General" ? "Active" : null;
        NavFirmsOffers.Tag = view is "Firms" or "Offers" ? "Active" : null;
        NavDocumentsDates.Tag = view is "Dates" or "Output" ? "Active" : null;
        NavControl.Tag = view == "Control" ? "Active" : null;
        NavStatistics.Tag = view == "Statistics" ? "Active" : null;
        NavArchive.Tag = view == "Archive" ? "Active" : null;
        NavAboutSafety.Tag = view == "AboutSafety" ? "Active" : null;
    }

    private void UpdateDataEntryMenuState()
    {
        if (DataEntrySubmenu is null || DataEntryChevron is null) return;
        DataEntrySubmenu.Visibility = _dataEntryExpanded ? Visibility.Visible : Visibility.Collapsed;
        DataEntryChevron.Text = _dataEntryExpanded ? "⌄" : "›";
    }

    private void NavDataEntry_Click(object sender, RoutedEventArgs e)
    {
        _dataEntryExpanded = !_dataEntryExpanded;
        UpdateDataEntryMenuState();
    }

    private void NavHome_Click(object sender, RoutedEventArgs e) => ShowView("Home");
    private void NavGeneral_Click(object sender, RoutedEventArgs e) => ShowView("General");
    private void NavCommission_Click(object sender, RoutedEventArgs e)
    {
        ShowView("Institution");
        Dispatcher.BeginInvoke(new Action(() => CommissionSectionAnchor?.BringIntoView()));
    }
    private void NavInstitution_Click(object sender, RoutedEventArgs e)
    {
        ShowView("Institution");
        Dispatcher.BeginInvoke(new Action(() => InstitutionView.ScrollToTop()));
    }
    private void NavFirmsOffers_Click(object sender, RoutedEventArgs e)
    {
        ShowView("Firms");
    }


    private void NavDocumentsDates_Click(object sender, RoutedEventArgs e)
    {
        ShowView("Dates");
    }

    private void NavOutput_Click(object sender, RoutedEventArgs e) => ShowView("Output");
    private void NavControl_Click(object sender, RoutedEventArgs e)
    {
        RefreshControlCenter();
        ShowView("Control");
    }
    private void NavStatistics_Click(object sender, RoutedEventArgs e)
    {
        RefreshStatisticsYears();
        RefreshStatistics();
        ShowView("Statistics");
    }

    private void HomeNextStepButton_Click(object sender, RoutedEventArgs e)
    {
        switch (_homeNextView)
        {
            case "Commission": NavCommission_Click(sender, e); break;
            case "Firms": ShowView("Firms"); break;
            case "Offers": ShowView("Offers"); PrepareOffersView(); break;
            case "Dates": ShowView("Dates"); break;
            case "Output": ShowView("Output"); break;
            default: ShowView("General"); break;
        }
    }

    private void NavArchive_Click(object sender, RoutedEventArgs e)
    {
        RefreshArchiveRecords();
        ShowView("Archive");
    }

    private void NavAboutSafety_Click(object sender, RoutedEventArgs e)
    {
        UpdateSafetyPanel(_safety.CheckIntegrity());
        ShowView("AboutSafety");
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Windows başlık çubuğu davranışı: çift tık = maksimize / geri yükle.
        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            e.Handled = true;
            return;
        }

        // Maksimize durumunda DragMove çağırmak yerleşimi bozabileceği için yalnız
        // normal pencere durumunda sürüklemeye izin verilir.
        if (WindowState == WindowState.Normal && e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximizeRestore();
    }

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        UpdateMaximizeRestoreButton();

        // R21: Viewbox pencerenin tamamını yeniden ölçsün. Borderless/maximize
        // geçişlerinde tasarım yüzeyi 1450x945 oranını koruyarak gerçek çalışma
        // alanına kadar büyür; yalnız ekran oranı farklıysa doğal letterbox kalır.
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
        if (_currentView is not ("Institution" or "General" or "Firms" or "Offers" or "Dates" or "Output")) return;
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
            var errors = ValidationService.ValidateDraft(data).Where(i => i.Severity == ValidationSeverity.Critical).ToList();
            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join("\n", errors.Take(8).Select(i => "• " + i.Message)), "DoğruTemin - Geçersiz veri", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dateWarnings = ValidationService.DateLogicWarnings(data);
            if (dateWarnings.Count > 0)
            {
                var answer = MessageBox.Show("Tarih sıralamasında kontrol edilmesi gereken noktalar var:\n\n" +
                    string.Join("\n", dateWarnings.Select(i => "• " + i.Message)) +
                    "\n\nYine de taslak kaydedilsin mi?", "DoğruTemin - Tarih sırası", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (answer != MessageBoxResult.Yes) return;
            }

            _storage.Save(data, data.AutoBackup);
            _recordId = data.RecordId;
            _recordCreatedAt = data.CreatedAt;
            UpdateDerivedState(data);
            SetSavedStatus(data.LastSavedAt);
            RefreshArchiveRecords();
            if (!string.IsNullOrWhiteSpace(_storage.LastBackupWarning))
            {
                MessageBox.Show("Kayıt tamamlandı; ancak otomatik yedekleme sırasında uyarı oluştu:\n\n" + _storage.LastBackupWarning,
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

    // ---------------- FİRMALAR ----------------
    private void AddOrUpdateFirm_Click(object sender, RoutedEventArgs e)
    {
        var name = FirmNameBox.Text.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show("Firma adını giriniz.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Warning);
            FirmNameBox.Focus();
            return;
        }

        if (_editingFirm is null && Firms.Count >= 5)
        {
            MessageBox.Show("En fazla 5 firma eklenebilir.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var taxNo = TaxNoBox.Text.Trim();
        var duplicate = Firms.FirstOrDefault(f => f != _editingFirm &&
            (string.Equals(f.Name.Trim(), name, StringComparison.OrdinalIgnoreCase) ||
             (taxNo.Length > 0 && string.Equals(f.TaxNo.Trim(), taxNo, StringComparison.OrdinalIgnoreCase))));
        if (duplicate is not null)
        {
            MessageBox.Show("Aynı firma adı veya vergi numarasıyla kayıtlı başka bir firma bulunuyor.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_editingFirm is null)
        {
            Firms.Add(new Firm
            {
                No = Firms.Count + 1,
                Name = name,
                TaxNo = taxNo,
                ContactName = ContactNameBox.Text.Trim(),
                Phone = PhoneBox.Text.Trim(),
                Email = EmailBox.Text.Trim(),
                Address = AddressBox.Text.Trim()
            });
        }
        else
        {
            _editingFirm.Name = name;
            _editingFirm.TaxNo = taxNo;
            _editingFirm.ContactName = ContactNameBox.Text.Trim();
            _editingFirm.Phone = PhoneBox.Text.Trim();
            _editingFirm.Email = EmailBox.Text.Trim();
            _editingFirm.Address = AddressBox.Text.Trim();
            FirmGrid.Items.Refresh();
        }

        _preferredWinnerFirmId = string.Empty;
        ReNumberFirms();
        MarkDirty();
        ClearFirmForm();
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (FirmGrid.SelectedItem is Firm firm) DeleteFirm(firm);
        else MessageBox.Show("Silmek için tablodan bir firma seçiniz.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void FirmGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void RowEdit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is Firm firm) BeginEditFirm(firm);
    }

    private void RowDelete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is Firm firm) DeleteFirm(firm);
    }

    private void BeginEditFirm(Firm firm)
    {
        _suppressDirtyTracking = true;
        _editingFirm = firm;
        FirmNameBox.Text = firm.Name;
        TaxNoBox.Text = firm.TaxNo;
        ContactNameBox.Text = firm.ContactName;
        PhoneBox.Text = firm.Phone;
        EmailBox.Text = firm.Email;
        AddressBox.Text = firm.Address;
        _firmEditorDirty = false;
        _suppressDirtyTracking = false;
        FirmActionButton.Content = "KAYDET";
        FirmActionButton.ToolTip = "Düzenlenen firma bilgilerini kaydet";
        FirmNameBox.Focus();
    }

    private void DeleteFirm(Firm firm)
    {
        var answer = MessageBox.Show($"'{firm.Name}' firmasını silmek istiyor musunuz?", "DoğruTemin", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;
        Firms.Remove(firm);
        _preferredWinnerFirmId = string.Empty;
        ReNumberFirms();
        MarkDirty();
        if (_editingFirm == firm) ClearFirmForm();
    }

    private void ReNumberFirms()
    {
        for (var i = 0; i < Firms.Count; i++) Firms[i].No = i + 1;
        FirmGrid.Items.Refresh();

        // Firm nesneleri INotifyPropertyChanged kullanmadığı için ad değişikliğinde ComboBox'ın
        // DisplayMemberPath metni kendiliğinden yenilenmez. Teklif firma listesini açıkça tazele.
        OfferFirmSelector.Items.Refresh();
        if (_offerEditingFirm is not null && Firms.Contains(_offerEditingFirm))
        {
            _suppressOfferSelection = true;
            OfferFirmSelector.SelectedItem = _offerEditingFirm;
            _suppressOfferSelection = false;
        }
    }

    private void ClearFirmForm()
    {
        var previousSuppress = _suppressDirtyTracking;
        _suppressDirtyTracking = true;
        _editingFirm = null;
        _firmEditorDirty = false;
        FirmNameBox.Clear();
        TaxNoBox.Clear();
        ContactNameBox.Clear();
        PhoneBox.Clear();
        EmailBox.Clear();
        AddressBox.Clear();
        FirmGrid.SelectedItem = null;
        FirmActionButton.Content = "EKLE";
        FirmActionButton.ToolTip = "Yeni firma ekle";
        _suppressDirtyTracking = previousSuppress;
    }

    // ---------------- ÜRÜNLER ----------------
    private void AddOrUpdateProduct_Click(object sender, RoutedEventArgs e)
    {
        var name = ProductNameBox.Text.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show("Mal / hizmet adını giriniz.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Warning);
            ProductNameBox.Focus();
            return;
        }

        if (_editingProduct is null && Products.Count >= 8)
        {
            MessageBox.Show("En fazla 8 ürün / mal / hizmet eklenebilir.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var unit = (ProductUnitBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Adet";
        var quantity = ProductQuantityBox.Text.Trim();
        if (!double.TryParse(quantity.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var quantityValue) || quantityValue <= 0)
        {
            MessageBox.Show("Miktar sıfırdan büyük bir sayı olmalıdır.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Warning);
            ProductQuantityBox.Focus();
            return;
        }

        if (_editingProduct is null)
        {
            Products.Add(new ProductItem { No = Products.Count + 1, Name = name, Specification = ProductSpecBox.Text.Trim(), Unit = unit, Quantity = quantity });
        }
        else
        {
            _editingProduct.Name = name;
            _editingProduct.Specification = ProductSpecBox.Text.Trim();
            _editingProduct.Unit = unit;
            _editingProduct.Quantity = quantity;
            ProductGrid.Items.Refresh();
        }

        _preferredWinnerFirmId = string.Empty;
        ReNumberProducts();
        ProductCountText.Text = $"{Products.Count} / 8 ürün";
        MarkDirty();
        ClearProductForm();
    }

    private void DeleteSelectedProduct_Click(object sender, RoutedEventArgs e)
    {
        if (ProductGrid.SelectedItem is ProductItem item) DeleteProduct(item);
        else MessageBox.Show("Silmek için tablodan bir ürün seçiniz.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ProductEdit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not ProductItem item) return;
        _suppressDirtyTracking = true;
        _editingProduct = item;
        ProductNameBox.Text = item.Name;
        ProductSpecBox.Text = item.Specification;
        ProductUnitBox.SelectedIndex = item.Unit == "Miktar" ? 1 : 0;
        ProductQuantityBox.Text = item.Quantity;
        _productEditorDirty = false;
        _suppressDirtyTracking = false;
        ProductActionButton.Content = "KAYDET";
        ProductNameBox.Focus();
    }

    private void ProductDelete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ProductItem item) DeleteProduct(item);
    }

    private void DeleteProduct(ProductItem item)
    {
        var answer = MessageBox.Show($"'{item.Name}' kalemi silinsin mi?", "DoğruTemin", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;
        Products.Remove(item);
        _preferredWinnerFirmId = string.Empty;
        foreach (var firm in Firms) firm.Offers.RemoveAll(o => o.ProductId == item.Id);
        _preferredWinnerFirmId = string.Empty;
        ReNumberProducts();
        ProductCountText.Text = $"{Products.Count} / 8 ürün";
        if (_editingProduct == item) ClearProductForm();
        MarkDirty();
    }

    private void ReNumberProducts()
    {
        for (var i = 0; i < Products.Count; i++) Products[i].No = i + 1;
        ProductGrid.Items.Refresh();
    }

    private void ClearProductForm()
    {
        var previousSuppress = _suppressDirtyTracking;
        _suppressDirtyTracking = true;
        _editingProduct = null;
        _productEditorDirty = false;
        ProductNameBox.Clear();
        ProductSpecBox.Clear();
        ProductUnitBox.SelectedIndex = 0;
        ProductQuantityBox.Text = "1";
        ProductGrid.SelectedItem = null;
        ProductActionButton.Content = "EKLE";
        _suppressDirtyTracking = previousSuppress;
    }

    // ---------------- KOMİSYON / TARİH / KURUM / ÇIKTI ----------------
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

    private void OutputSelection_Changed(object sender, RoutedEventArgs e)
    {
        if (!_suppressDirtyTracking) MarkDirty();
    }

    private void ControlRefresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshControlCenter();
    }

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

        // Bu dört alan BELGELER/TARİHLER bölümünde değil, MAL/HİZMET BİLGİSİ ekranındaki
        // GÖREVLENDİRME / HARCAMA BELGESİ kartındadır. Önceki metin-tahminli eşleme
        // "tarih" / "komisyon" kelimeleri yüzünden yanlış sekmeye gönderebiliyordu.
        if (EqualsAny(field,
                "Alım / iş başlığı",
                "OperationNumber",
                "Komisyon görevlendirme tarihi",
                "Komisyon sayısı",
                "Harcama belgesi tarihi",
                "Harcama belgesi sayısı"))
            return ("Mal/Hizmet Bilgisi", "General");

        // BELGELER/TARİHLER içindeki tarih sekmesinde yalnız bu dört tarih ve zorunlu sıra kontrolü vardır.
        if (field.StartsWith("Dates", StringComparison.OrdinalIgnoreCase) ||
            EqualsAny(field,
                "Yaklaşık Maliyet Hesap Cetveli tarihi",
                "Piyasa Fiyat Araştırması Tutanağı tarihi",
                "Muayene ve Kabul Tutanağı tarihi",
                "Teslim Alma / Kabul tarihi"))
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

            var core = GetExportCoreIssues(data, new[] { type });
            var advisory = ValidationService.ValidateDocument(data, type)
                .Select(i => i.Message)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct()
                .Count();

            missingCount += core.Count;
            lines.Add(core.Count == 0
                ? $"{doc.Name}: ÇIKTIYA HAZIR" + (advisory > 0 ? $" • {advisory} uyarı" : string.Empty)
                : $"{doc.Name}: {core.Count} temel eksik");
        }

        var selectedTypes = selected.Where(d => OfficialDocumentService.IsImplemented(DocumentTypeFor(d.Name))).Select(d => DocumentTypeFor(d.Name)).ToList();
        var coreIssues = GetExportCoreIssues(data, selectedTypes);
        var body = string.Join("\n", lines);
        body += coreIssues.Count == 0
            ? "\n\nPDF / YAZDIR: TEMEL KOŞULLAR HAZIR"
            : "\n\nPDF / YAZDIR İÇİN TAMAMLANMASI GEREKEN TEMEL KOŞULLAR:\n" + string.Join("\n", coreIssues.Take(12).Select(i => "• " + i));
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
            MessageBox.Show("Seçili belgeler arasında bu sürümde desteklenmeyen belge var:\n\n" +
                string.Join("\n", unsupported.Select(d => "• " + d.Name)),
                actionTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        return selected.Select(d => DocumentTypeFor(d.Name)).ToList();
    }

    private static bool IsPreparationDocument(OutputDocumentType type) =>
        type is OutputDocumentType.ApproximateRequest or OutputDocumentType.FirmOfferForm;

    private static bool IsFinalProcessDocument(OutputDocumentType type) =>
        type is OutputDocumentType.ApproximateCostSheet
            or OutputDocumentType.MarketResearchReport
            or OutputDocumentType.InspectionAcceptanceReport;

    private static List<string> GetExportCoreIssues(ProcurementData data, IReadOnlyCollection<OutputDocumentType> types)
    {
        var issues = new List<string>();

        if (types.Any(IsPreparationDocument))
            AddPreparationDocumentCoreIssues(data, issues);

        if (types.Any(IsFinalProcessDocument))
            AddFinalProcessDocumentCoreIssues(data, issues);

        return issues.Distinct().ToList();
    }

    private static void AddPreparationDocumentCoreIssues(ProcurementData data, List<string> issues)
    {
        AddProductCoreIssues(data, issues);
        AddCommissionCoreIssues(data.MarketCommission, "Piyasa Fiyat Araştırması Komisyonu", issues);
        AddCommissionCoreIssues(data.InspectionCommission, "Muayene ve Kabul Komisyonu", issues);

        if (string.IsNullOrWhiteSpace(data.Institution.TopAuthority))
            issues.Add("Kurum Bilgisi: Üst Makam eksik.");
        if (string.IsNullOrWhiteSpace(data.Institution.FullName))
            issues.Add("Kurum Bilgisi: İdarenin Tam Adı eksik.");
        if (string.IsNullOrWhiteSpace(data.Institution.Province))
            issues.Add("Kurum Bilgisi: İl eksik.");
        if (string.IsNullOrWhiteSpace(data.Institution.Address))
            issues.Add("Kurum Bilgisi: Adres eksik.");
        if (!ValidationService.ValidPhone(data.Institution.Phone))
            issues.Add("Kurum Bilgisi: Geçerli telefon bilgisi eksik.");
        if (!ValidationService.ValidEmail(data.Institution.Email))
            issues.Add("Kurum Bilgisi: Geçerli e-posta bilgisi eksik.");
    }

    private static void AddFinalProcessDocumentCoreIssues(ProcurementData data, List<string> issues)
    {
        var products = data.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Take(8).ToList();
        var firms = data.Firms.Where(f => !string.IsNullOrWhiteSpace(f.Name)).Take(5).ToList();

        AddProductCoreIssues(data, issues);

        if (firms.Count < 3)
            issues.Add($"En az 3 firma bulunmalıdır. Şu an {firms.Count} firma var.");

        if (products.Count > 0 && firms.Count >= 3)
        {
            foreach (var firm in firms)
            {
                foreach (var product in products)
                {
                    var offer = firm.Offers.FirstOrDefault(o => o.ProductId == product.Id);
                    if (!(offer?.ApproximateUnitPrice > 0))
                        issues.Add($"{firm.No}. firma / {product.No}. ürün yaklaşık teklifi eksik.");
                    if (!(offer?.FinalUnitPrice > 0))
                        issues.Add($"{firm.No}. firma / {product.No}. ürün nihai teklifi eksik.");
                }
            }
        }

        AddCommissionCoreIssues(data.MarketCommission, "Piyasa Fiyat Araştırması Komisyonu", issues);
        AddCommissionCoreIssues(data.InspectionCommission, "Muayene ve Kabul Komisyonu", issues);
    }

    private static void AddProductCoreIssues(ProcurementData data, List<string> issues)
    {
        var products = data.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Take(8).ToList();
        if (products.Count == 0)
        {
            issues.Add("En az 1 ürün / mal / hizmet bulunmalıdır.");
            return;
        }

        foreach (var product in products)
        {
            if (product.Unit is not ("Adet" or "Miktar"))
                issues.Add($"{product.No}. ürün için birim Adet veya Miktar olmalıdır.");
            if (!NumberParser.TryPositiveDecimal(product.Quantity, out _))
                issues.Add($"{product.No}. ürün için miktar/adet 0'dan büyük geçerli bir sayı olmalıdır.");
        }
    }

    private static void AddCommissionCoreIssues(IEnumerable<DogruTemin.Core.Models.CommissionMember> members, string label, List<string> issues)
    {
        var list = members.Take(3).ToList();
        for (var i = 0; i < 3; i++)
        {
            var member = i < list.Count ? list[i] : null;
            if (member is null || string.IsNullOrWhiteSpace(member.Name) || string.IsNullOrWhiteSpace(member.Title))
                issues.Add($"{label} {i + 1}. üye için ad soyad ve unvan bilgisi tamamlanmalıdır.");
        }
    }

    private static bool ConfirmNonBlockingDocumentIssues(ProcurementData data, IReadOnlyList<OutputDocumentType> types, string actionName)
    {
        var messages = types
            .SelectMany(type => ValidationService.ValidateDocument(data, type))
            .Select(i => i.Message)
            .Concat(ValidationService.DateLogicWarnings(data).Select(i => i.Message))
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .Take(14)
            .ToList();

        if (messages.Count == 0) return true;

        var body = "Belgede eksik veya kontrol edilmesi gereken bilgiler var.\n" +
                   "Bunlar önizlemeyi engellemez; temel çıktı koşulları sağlandığı için " + actionName + " işlemine devam edebilirsiniz.\n\n" +
                   string.Join("\n", messages.Select(m => "• " + m)) +
                   "\n\nYine de devam etmek istiyor musunuz?";

        return MessageBox.Show(body, "DoğruTemin - Eksik Bilgi Uyarısı", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private FixedDocument? BuildSelectedOfficialDocumentForPreview()
    {
        var types = GetSelectedImplementedDocumentTypes("DoğruTemin - Önizleme");
        if (types is null) return null;

        // Önizleme taslak görüntüdür. Eksik alanlar belge üzerinde boş bırakılabilir.
        return OfficialDocumentService.BuildDocument(BuildDataFromUi(), types);
    }

    private FixedDocument? BuildSelectedOfficialDocumentForExport(string actionName)
    {
        var types = GetSelectedImplementedDocumentTypes("DoğruTemin - Çıktı");
        if (types is null) return null;

        var data = BuildDataFromUi();
        var coreIssues = GetExportCoreIssues(data, types);
        if (coreIssues.Count > 0)
        {
            MessageBox.Show(
                "Seçili belge grubu için temel çıktı koşulları tamamlanmalıdır.\n\n" +
                "1. grup (Ön Hazırlık): Kurum bilgisi + her iki komisyon + ürün(ler) ve miktar/adet.\n" +
                "2. grup (Nihai İşlem): En az 1 ürün + en az 3 firma + yaklaşık/nihai teklifler + her iki komisyon.\n\n" +
                "Eksik olanlar:\n" + string.Join("\n", coreIssues.Take(18).Select(i => "• " + i)),
                "DoğruTemin - Temel Çıktı Koşulları", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        if (!ConfirmNonBlockingDocumentIssues(data, types, actionName))
            return null;

        var calc = ProcurementEngine.Calculate(data);
        var requiresWinner = types.Any(t => t is OutputDocumentType.MarketResearchReport or OutputDocumentType.InspectionAcceptanceReport);

        if (requiresWinner && calc.HasWinnerTie && string.IsNullOrWhiteSpace(calc.WinningFirmId))
        {
            var tiedFirms = Firms.Where(f => calc.TiedWinnerFirmIds.Contains(f.Id)).ToList();
            var chooser = new TieWinnerSelectionWindow(tiedFirms, NumberParser.Money(calc.MarketResultTotal)) { Owner = this };
            if (chooser.ShowDialog() != true || string.IsNullOrWhiteSpace(chooser.SelectedFirmId))
                return null;

            _preferredWinnerFirmId = chooser.SelectedFirmId;
            MarkDirty();
            data = BuildDataFromUi();
        }

        return OfficialDocumentService.BuildDocument(data, types);
    }

    private void OutputPreview_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var document = BuildSelectedOfficialDocumentForPreview();
            if (document is null) return;
            var preview = new DocumentPreviewWindow(document) { Owner = this };
            preview.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Belge önizleme oluşturulamadı: " + ex.Message, "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OutputPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var document = BuildSelectedOfficialDocumentForExport("PDF kaydetme");
            if (document is null) return;
            Directory.CreateDirectory(_storage.PdfFolder);
            var dialog = new SaveFileDialog
            {
                Title = "DoğruTemin PDF Kaydet",
                Filter = "PDF Belgesi (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                AddExtension = true,
                InitialDirectory = _storage.PdfFolder,
                FileName = $"DogruTemin_Belgeler_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            };
            if (dialog.ShowDialog(this) != true) return;
            PdfImageWriter.Save(document, dialog.FileName);

            UpdateHomeActivitySummary();
            UiNotificationService.Show(this, "PDF oluşturuldu:\n" + dialog.FileName, TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            MessageBox.Show("PDF oluşturulamadı: " + ex.Message, "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OutputPrint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var document = BuildSelectedOfficialDocumentForExport("yazdırma");
            if (document is null) return;
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true) return;
            dialog.PrintDocument(document.DocumentPaginator, "DoğruTemin Resmî Belgeleri");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Yazdırma işlemi başlatılamadı: " + ex.Message, "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApprovalNoBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var raw = ApprovalNoBox.Text.Trim();
        if (raw.Length == 0) return;
        raw = Regex.Replace(raw, @"(?i)\bHYS\b\s*[-:/]*\s*", "");
        raw = raw.Trim();
        if (ApprovalNoBox.Text != raw)
        {
            _suppressDirtyTracking = true;
            ApprovalNoBox.Text = raw;
            _suppressDirtyTracking = false;
            MarkDirty();
        }
    }

    // ---------------- BAĞIMSIZ C# VERİ / HESAP MOTORU ----------------
    private void ApplyDataToUi(ProcurementData data)
    {
        Firms.Clear();
        Products.Clear();
        InspectionCommission.Clear();
        MarketCommission.Clear();
        OfferRows.Clear();
        _offerEditingFirm = null;
        _offerEditorDirty = false;
        _suppressOfferSelection = true;
        OfferFirmSelector.SelectedItem = null;
        _suppressOfferSelection = false;

        foreach (var p in data.Products.Take(8))
            Products.Add(new ProductItem { Id = p.Id, No = p.No, Name = p.Name, Specification = p.Specification, Unit = p.Unit, Quantity = p.Quantity });
        foreach (var f in data.Firms.Take(5))
            Firms.Add(new Firm
            {
                Id = f.Id, No = f.No, Name = f.Name, TaxOffice = f.TaxOffice, TaxNo = f.TaxNo,
                ContactName = f.ContactName, Phone = f.Phone, Email = f.Email, Address = f.Address, OfferDate = f.OfferDate,
                Offers = f.Offers.Select(o => new FirmOfferCore { ProductId = o.ProductId, ApproximateUnitPrice = o.ApproximateUnitPrice, FinalUnitPrice = o.FinalUnitPrice }).ToList()
            });
        foreach (var m in data.InspectionCommission.Take(3)) InspectionCommission.Add(new CommissionMember { No = m.No, Name = m.Name, Title = m.Title });
        foreach (var m in data.MarketCommission.Take(3)) MarketCommission.Add(new CommissionMember { No = m.No, Name = m.Name, Title = m.Title });

        JobTitleBox.Text = data.JobTitle;
        AssignmentDatePicker.SelectedDate = data.Dates.AssignmentDate;
        AssignmentNoBox.Text = data.Dates.AssignmentNo;
        ApprovalDatePicker.SelectedDate = data.Dates.ApprovalDate;
        ApprovalNoBox.Text = TextNormalizer.Hys(data.Dates.ApprovalNo);
        CostCalculationDatePicker.SelectedDate = data.Dates.CostCalculationDate;
        // Eski kayıtlarda ayrı tutulan UYGUNDUR tarihi varsa tek Piyasa tarihi alanına taşınır.
        MarketReportDatePicker.SelectedDate = data.Dates.MarketReportDate ?? data.Dates.MarketApprovalDate;
        InspectionReportDatePicker.SelectedDate = data.Dates.InspectionReportDate;
        DeliveryDatePicker.SelectedDate = data.Dates.DeliveryDate;

        InstitutionTopAuthorityBox.Text = data.Institution.TopAuthority;
        InstitutionFullNameBox.Text = !string.IsNullOrWhiteSpace(data.Institution.FullName) ? data.Institution.FullName : data.Institution.ShortName;
        InstitutionProvinceBox.Text = data.Institution.Province;
        InstitutionAddressBox.Text = data.Institution.Address;
        InstitutionPhoneBox.Text = data.Institution.Phone;
        InstitutionEmailBox.Text = data.Institution.Email;
        AuthorizedNameBox.Text = data.Institution.AuthorizedName;
        AuthorizedDutyBox.Text = data.Institution.AuthorizedDuty;
        AuthorizedTitleBox.Text = data.Institution.AuthorizedTitle;
        AutoBackupCheckBox.IsChecked = data.AutoBackup;
        _operationNumber = data.OperationNumber ?? string.Empty;
        _recordId = data.RecordId ?? string.Empty;
        _recordCreatedAt = data.CreatedAt == default ? DateTime.Now : data.CreatedAt;
        _preferredWinnerFirmId = data.PreferredWinnerFirmId ?? string.Empty;
        ReNumberFirms();
        ReNumberProducts();
    }

    private ProcurementData BuildDataFromUi()
    {
        return new ProcurementData
        {
            RecordId = _recordId,
            CreatedAt = _recordCreatedAt,
            OperationNumber = _operationNumber,
            JobTitle = JobTitleBox.Text.Trim(),
            Products = Products.Take(8).Select(p => new DogruTemin.Core.Models.ProductItem
            {
                Id = p.Id, No = p.No, Name = p.Name.Trim(), Specification = p.Specification.Trim(), Unit = p.Unit, Quantity = p.Quantity.Trim()
            }).ToList(),
            Firms = Firms.Take(5).Select(f => new DogruTemin.Core.Models.Firm
            {
                Id = f.Id, No = f.No, Name = f.Name.Trim(), TaxOffice = f.TaxOffice.Trim(), TaxNo = f.TaxNo.Trim(),
                ContactName = f.ContactName.Trim(), Phone = f.Phone.Trim(), Email = f.Email.Trim().ToLowerInvariant(), Address = f.Address.Trim(), OfferDate = f.OfferDate,
                Offers = f.Offers.Select(o => new FirmOfferCore { ProductId = o.ProductId, ApproximateUnitPrice = o.ApproximateUnitPrice, FinalUnitPrice = o.FinalUnitPrice }).ToList()
            }).ToList(),
            InspectionCommission = InspectionCommission.Take(3).Select(m => new DogruTemin.Core.Models.CommissionMember { No = m.No, Name = m.Name.Trim(), Title = m.Title.Trim() }).ToList(),
            MarketCommission = MarketCommission.Take(3).Select(m => new DogruTemin.Core.Models.CommissionMember { No = m.No, Name = m.Name.Trim(), Title = m.Title.Trim() }).ToList(),
            Dates = new DogruTemin.Core.Models.ProcessDates
            {
                AssignmentDate = AssignmentDatePicker.SelectedDate, AssignmentNo = AssignmentNoBox.Text.Trim(),
                ApprovalDate = ApprovalDatePicker.SelectedDate, ApprovalNo = TextNormalizer.Hys(ApprovalNoBox.Text),
                CostCalculationDate = CostCalculationDatePicker.SelectedDate, MarketReportDate = MarketReportDatePicker.SelectedDate,
                InspectionReportDate = InspectionReportDatePicker.SelectedDate,
                DeliveryDate = DeliveryDatePicker.SelectedDate,
                // Piyasa UYGUNDUR tarihi ayrı bir kullanıcı alanı değildir; aynı tarih geriye dönük uyumluluk için de saklanır.
                MarketApprovalDate = MarketReportDatePicker.SelectedDate,
                // Teklif verme son tarihi kullanıcıdan alınmaz; resmî çıktıda sabit boş tarih şablonu kullanılır.
                OfferDeadlineDate = null
            },
            Institution = new DogruTemin.Core.Models.InstitutionInfo
            {
                TopAuthority = InstitutionTopAuthorityBox.Text.Trim(), ShortName = InstitutionFullNameBox.Text.Trim(), FullName = InstitutionFullNameBox.Text.Trim(),
                Province = InstitutionProvinceBox.Text.Trim(), Address = InstitutionAddressBox.Text.Trim(), Phone = InstitutionPhoneBox.Text.Trim(),
                Email = InstitutionEmailBox.Text.Trim().ToLowerInvariant(), AuthorizedName = AuthorizedNameBox.Text.Trim(),
                AuthorizedDuty = AuthorizedDutyBox.Text.Trim(), AuthorizedTitle = AuthorizedTitleBox.Text.Trim()
            },
            PreferredWinnerFirmId = _preferredWinnerFirmId,
            AutoBackup = AutoBackupCheckBox.IsChecked == true
        };
    }

    private void UpdateHomeProcessMap(ProcurementData data)
    {
        var products = data.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Take(8).ToList();
        var firms = data.Firms.Where(f => !string.IsNullOrWhiteSpace(f.Name)).Take(5).ToList();

        var institutionDone = !string.IsNullOrWhiteSpace(data.Institution.TopAuthority)
            && !string.IsNullOrWhiteSpace(data.Institution.FullName)
            && !string.IsNullOrWhiteSpace(data.Institution.Province)
            && !string.IsNullOrWhiteSpace(data.Institution.Address)
            && ValidationService.ValidPhone(data.Institution.Phone)
            && ValidationService.ValidEmail(data.Institution.Email);

        var inspectionCommissionDone = data.InspectionCommission.Take(3).Count(m =>
            !string.IsNullOrWhiteSpace(m.Name) && !string.IsNullOrWhiteSpace(m.Title)) == 3;
        var marketCommissionDone = data.MarketCommission.Take(3).Count(m =>
            !string.IsNullOrWhiteSpace(m.Name) && !string.IsNullOrWhiteSpace(m.Title)) == 3;
        var commissionDone = inspectionCommissionDone && marketCommissionDone;
        var institutionCommissionDone = institutionDone && commissionDone;

        SetHomeCombinedStage(
            HomeStageCommissionStatus,
            HomeStageCommissionDetail,
            institutionDone,
            commissionDone,
            "Kurum bilgisi ve iki komisyon hazır.",
            institutionDone ? "Kurum hazır • komisyonları tamamlayın." : commissionDone ? "Komisyonlar hazır • kurum bilgisini tamamlayın." : "Kurum bilgisi ve iki komisyonu tamamlayın.");

        var productsValid = products.Count > 0 && products.All(p =>
            p.Unit is "Adet" or "Miktar" && NumberParser.TryPositiveDecimal(p.Quantity, out _));
        var generalDone = !string.IsNullOrWhiteSpace(data.JobTitle)
            && productsValid
            && data.Dates.AssignmentDate.HasValue
            && !string.IsNullOrWhiteSpace(data.Dates.AssignmentNo)
            && data.Dates.ApprovalDate.HasValue
            && !string.IsNullOrWhiteSpace(data.Dates.ApprovalNo);
        SetHomeStage(HomeStageGeneralStatus, HomeStageGeneralDetail, generalDone,
            generalDone ? $"{products.Count} ürün ve temel işlem bilgileri hazır." : "İş başlığı, ürünler ve görevlendirme/harcama bilgilerini tamamlayın.");

        var firmsDone = firms.Count >= 3;
        var offersDone = firmsDone && products.Count > 0 && firms.All(f => products.All(p =>
        {
            var offer = f.Offers.FirstOrDefault(o => o.ProductId == p.Id);
            return offer?.ApproximateUnitPrice > 0m && offer.FinalUnitPrice > 0m;
        }));
        SetHomeCombinedStage(
            HomeStageFirmsStatus,
            HomeStageFirmsDetail,
            firmsDone,
            offersDone,
            $"{firms.Count} firma ve tüm teklifler hazır.",
            firmsDone ? $"{firms.Count} firma hazır • tekliflerde eksik var." : $"En az 3 firma gerekli; şu an {firms.Count} firma var.");

        var dateWarnings = ValidationService.DateLogicWarnings(data);
        var datesDone = data.Dates.CostCalculationDate.HasValue
            && data.Dates.MarketReportDate.HasValue
            && data.Dates.InspectionReportDate.HasValue
            && data.Dates.DeliveryDate.HasValue
            && dateWarnings.Count == 0;
        SetHomeStage(HomeStageDatesStatus, HomeStageDatesDetail, datesDone,
            datesDone ? "4 belge tarihi hazır ve zorunlu sıra geçerli." : "Belge tarihlerini tamamlayın; yanlış sıra varsa uyarı verilir.");

        var preparationTypes = new[]
        {
            OutputDocumentType.ApproximateRequest,
            OutputDocumentType.FirmOfferForm
        };
        var finalProcessTypes = new[]
        {
            OutputDocumentType.ApproximateCostSheet,
            OutputDocumentType.MarketResearchReport,
            OutputDocumentType.InspectionAcceptanceReport
        };
        var preparationIssues = GetExportCoreIssues(data, preparationTypes);
        var finalProcessIssues = GetExportCoreIssues(data, finalProcessTypes);
        var preparationReady = preparationIssues.Count == 0;
        var finalProcessReady = finalProcessIssues.Count == 0;
        var outputDone = preparationReady && finalProcessReady;
        SetHomeOutputStage(preparationReady, finalProcessReady, preparationIssues.Count, finalProcessIssues.Count);

        var firmsOffersDone = firmsDone && offersDone;
        var doneCount = new[] { institutionCommissionDone, generalDone, firmsOffersDone, datesDone, outputDone }.Count(x => x);
        HomeStageSummaryText.Text = $"{doneCount} / 5 TAMAM";

        if (!institutionCommissionDone)
        {
            _homeNextView = "Institution";
            HomeNextStepText.Text = "Kurum bilgisi ile piyasa ve muayene/kabul komisyonlarını tamamlayın.";
        }
        else if (!generalDone)
        {
            _homeNextView = "General";
            HomeNextStepText.Text = "İşlem, ürün ve görevlendirme/harcama bilgilerini tamamlayın.";
        }
        else if (!firmsDone)
        {
            _homeNextView = "Firms";
            HomeNextStepText.Text = "En az 3 firma ekleyin.";
        }
        else if (!offersDone)
        {
            _homeNextView = "Offers";
            HomeNextStepText.Text = "Firmaların yaklaşık ve nihai tekliflerini tamamlayın.";
        }
        else if (!datesDone)
        {
            _homeNextView = "Dates";
            HomeNextStepText.Text = "Belge tarihlerini girin ve tarih sırasını kontrol edin.";
        }
        else
        {
            _homeNextView = "Output";
            HomeNextStepText.Text = outputDone
                ? "Süreç hazır. Resmî belgeleri önizleyebilir, PDF kaydedebilir veya yazdırabilirsiniz."
                : "Çıktı ekranındaki kritik eksikleri giderin ve belgeleri önizleyin.";
        }
    }

    private static void SetHomeCombinedStage(TextBlock statusBlock, TextBlock detailBlock, bool firstReady, bool secondReady, string readyDetail, string incompleteDetail)
    {
        if (firstReady && secondReady)
        {
            statusBlock.Text = "TAMAM";
            statusBlock.Foreground = new SolidColorBrush(Color.FromRgb(126, 226, 166));
            detailBlock.Text = readyDetail;
            return;
        }

        if (firstReady || secondReady)
        {
            statusBlock.Text = "KISMİ";
            statusBlock.Foreground = new SolidColorBrush(Color.FromRgb(143, 183, 216));
        }
        else
        {
            statusBlock.Text = "EKSİK";
            statusBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 191, 54));
        }

        detailBlock.Text = incompleteDetail;
    }

    private static void SetHomeStage(TextBlock statusBlock, TextBlock detailBlock, bool completed, string detail)
    {
        statusBlock.Text = completed ? "TAMAM" : "EKSİK";
        statusBlock.Foreground = completed
            ? new SolidColorBrush(Color.FromRgb(126, 226, 166))
            : new SolidColorBrush(Color.FromRgb(255, 191, 54));
        detailBlock.Text = detail;
    }

    private void SetHomeOutputStage(bool preparationReady, bool finalReady, int preparationIssueCount, int finalIssueCount)
    {
        if (preparationReady && finalReady)
        {
            HomeStageOutputStatus.Text = "TAMAM";
            HomeStageOutputStatus.Foreground = new SolidColorBrush(Color.FromRgb(126, 226, 166));
            HomeStageOutputDetail.Text = "Ön hazırlık ve nihai işlem belgeleri üretime hazır.";
            return;
        }

        if (preparationReady || finalReady)
        {
            HomeStageOutputStatus.Text = "KISMİ";
            HomeStageOutputStatus.Foreground = new SolidColorBrush(Color.FromRgb(143, 183, 216));
        }
        else
        {
            HomeStageOutputStatus.Text = "EKSİK";
            HomeStageOutputStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 191, 54));
        }

        var preparationText = preparationReady ? "Ön hazırlık: hazır" : $"Ön hazırlık: {preparationIssueCount} eksik";
        var finalText = finalReady ? "Nihai: hazır" : $"Nihai: {finalIssueCount} eksik";
        HomeStageOutputDetail.Text = $"{preparationText} • {finalText}";
    }
    private void UpdateHomeActivitySummary()
    {
        if (HomeHeroLastSavedText is null) return;
        HomeHeroLastSavedText.Text = _lastSavedAt.HasValue
            ? _lastSavedAt.Value.ToString("dd.MM.yyyy • HH:mm", CultureInfo.InvariantCulture)
            : "Henüz kaydedilmedi";
    }
    private void UpdateDerivedState(ProcurementData? supplied = null)
    {
        if (_suppressDirtyTracking && supplied is null) return;
        var data = supplied ?? BuildDataFromUi();
        var calc = ProcurementEngine.Calculate(data);
        var readiness = ValidationService.ReadinessPercent(data);
        var draftIssues = ValidationService.ValidateDraft(data);
        var dateWarnings = ValidationService.DateLogicWarnings(data);
        var filledCommission = data.InspectionCommission.Count(m => !string.IsNullOrWhiteSpace(m.Name) && !string.IsNullOrWhiteSpace(m.Title))
            + data.MarketCommission.Count(m => !string.IsNullOrWhiteSpace(m.Name) && !string.IsNullOrWhiteSpace(m.Title));
        var firmCount = data.Firms.Count(f => !string.IsNullOrWhiteSpace(f.Name));
        var productCount = data.Products.Count(p => !string.IsNullOrWhiteSpace(p.Name));

        HomeProductCountText.Text = productCount.ToString(CultureInfo.InvariantCulture);
        HomeProductCountHintText.Text = productCount > 0 ? $"{productCount} / 8 kayıtlı" : "En az 1 gerekli";
        HomeFirmCountText.Text = firmCount.ToString(CultureInfo.InvariantCulture);
        HomeFirmCountHintText.Text = firmCount >= 3 ? $"{firmCount} firma hazır" : $"{firmCount} / en az 3";

        ActiveOperationText.Text = !string.IsNullOrWhiteSpace(data.JobTitle) ? data.JobTitle : (!string.IsNullOrWhiteSpace(data.OperationNumber) ? data.OperationNumber : "Yeni İşlem");
        HeaderReadinessText.Text = $"%{readiness}";
        UpdateReadinessRing(readiness);
        HomeJobTitleText.Text = string.IsNullOrWhiteSpace(data.JobTitle) ? "Henüz aktif işlem başlığı girilmedi" : data.JobTitle;
        HomeApprovalDateText.Text = data.Dates.ApprovalDate.HasValue ? $"Harcama belgesi tarihi: {data.Dates.ApprovalDate:dd.MM.yyyy}" : "Harcama belgesi tarihi: Henüz belirtilmedi";
        HomeOperationNumberText.Text = string.IsNullOrWhiteSpace(data.OperationNumber) ? "Henüz yok" : data.OperationNumber;
        HomeApproximateTotalText.Text = calc.ApproximateCostTotal > 0m ? NumberParser.Money(calc.ApproximateCostTotal) : "—";
        HomeFinalTotalText.Text = calc.MarketResultTotal > 0m ? NumberParser.Money(calc.MarketResultTotal) : "—";

        if (calc.ApproximateCostTotal > 0m && calc.MarketResultTotal > 0m)
        {
            var difference = calc.ApproximateCostTotal - calc.MarketResultTotal;
            var differencePercent = calc.ApproximateCostTotal == 0m ? 0m : difference / calc.ApproximateCostTotal * 100m;
            HomeDifferenceText.Text = NumberParser.Money(difference);
            HomeDifferenceText.Foreground = difference >= 0m
                ? new SolidColorBrush(Color.FromRgb(126, 226, 166))
                : new SolidColorBrush(Color.FromRgb(190, 61, 61));
            HomeDifferenceHintText.Text = difference >= 0m
                ? $"Yaklaşık maliyete göre %{Math.Abs(differencePercent):0.0} düşük"
                : $"Yaklaşık maliyete göre %{Math.Abs(differencePercent):0.0} yüksek";
        }
        else
        {
            HomeDifferenceText.Text = "—";
            HomeDifferenceText.Foreground = new SolidColorBrush(Color.FromRgb(31, 49, 67));
            HomeDifferenceHintText.Text = "Hesaplama bekleniyor";
        }

        if (calc.HasWinnerTie && string.IsNullOrWhiteSpace(calc.WinningFirmId))
        {
            HomeWinnerText.Text = "Seçim gerekli";
            HomeWinnerHintText.Text = "Eşit en düşük teklif var";
        }
        else if (!string.IsNullOrWhiteSpace(calc.WinningFirmName))
        {
            HomeWinnerText.Text = calc.WinningFirmName;
            HomeWinnerHintText.Text = calc.MarketResultTotal > 0m ? NumberParser.Money(calc.MarketResultTotal) : "Nihai toplam hazır";
        }
        else
        {
            HomeWinnerText.Text = "Henüz yok";
            HomeWinnerHintText.Text = firmCount < 3 ? "En az 3 firma gerekli" : "Nihai teklifler bekleniyor";
        }

        var workflowIssues = ValidationService.ValidateWorkflow(data);
        var homeWarningCount = workflowIssues.Count;
        HomeWarningCountText.Text = $"{homeWarningCount} UYARI";
        UpdateHomeActivitySummary();
        HomeReadinessText.Text = $"%{readiness}";
        HomeReadinessBar.Width = 690.0 * readiness / 100.0;
        HomeReadinessHintText.Text = readiness >= 100 && homeWarningCount == 0
            ? "Temel hazırlık tamam. Akıllı iş akışı tüm aşamaların durumunu tek bakışta gösteriyor."
            : homeWarningCount > 0
                ? $"{homeWarningCount} kontrol noktası dikkat istiyor. Akıllı iş akışı ilk eksik aşamayı gösteriyor."
                : "Süreç bilgileri girildikçe hazırlık durumu otomatik güncellenir.";
        UpdateHomeProcessMap(data);

        var selectedTypesForOutput = OutputDocuments
            .Where(d => d.IsSelected && OfficialDocumentService.IsImplemented(DocumentTypeFor(d.Name)))
            .Select(d => DocumentTypeFor(d.Name))
            .ToList();
        var selectedCoreIssues = GetExportCoreIssues(data, selectedTypesForOutput);
        var selectedAdvisories = OutputDocuments
            .Where(d => d.IsSelected && OfficialDocumentService.IsImplemented(DocumentTypeFor(d.Name)))
            .SelectMany(d => ValidationService.ValidateDocument(data, DocumentTypeFor(d.Name)))
            .Select(i => i.Message)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .ToList();

        OutputCommissionText.Text = $"{filledCommission} / 6";
        OutputFirmText.Text = selectedTypesForOutput.Any(IsFinalProcessDocument) ? $"{firmCount} / EN AZ 3" : "GEREKMEZ";
        OutputProductText.Text = $"{productCount} / EN AZ 1";
        OutputCriticalText.Text = selectedCoreIssues.Count.ToString(CultureInfo.InvariantCulture);
        OutputWarningText.Text = selectedAdvisories.Count.ToString(CultureInfo.InvariantCulture);
        OutputReadinessText.Text = $"%{readiness}";
        OutputReadinessBar.Width = 390.0 * readiness / 100.0;
        RefreshControlCenter(data);

        foreach (var doc in OutputDocuments)
        {
            var type = DocumentTypeFor(doc.Name);
            if (!OfficialDocumentService.IsImplemented(type))
            {
                doc.Status = "Sırada";
                doc.Note = "Bu sürümde belge motoruna bağlı değil";
                continue;
            }

            var core = GetExportCoreIssues(data, new[] { type });
            var advisoryCount = ValidationService.ValidateDocument(data, type)
                .Select(i => i.Message)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct()
                .Count();

            doc.Status = core.Count == 0 ? "Hazır" : "Uyarı";
            doc.Note = core.Count == 0
                ? (advisoryCount == 0 ? "Çıktı koşulları hazır" : $"Çıktı alınabilir • {advisoryCount} bilgi uyarısı")
                : core[0] + (core.Count > 1 ? $" (+{core.Count - 1})" : string.Empty);
        }
    }

    private void UpdateReadinessRing(int percent)
    {
        var p = Math.Clamp(percent, 0, 100);
        if (p <= 0)
        {
            HeaderReadinessArc.Data = Geometry.Empty;
            return;
        }
        var angle = p >= 100 ? 359.99 : 360.0 * p / 100.0;
        var radians = (angle - 90.0) * Math.PI / 180.0;
        var x = 24.0 + 20.0 * Math.Cos(radians);
        var y = 24.0 + 20.0 * Math.Sin(radians);
        var large = angle > 180.0 ? 1 : 0;
        HeaderReadinessArc.Data = Geometry.Parse(string.Format(CultureInfo.InvariantCulture, "M24,4 A20,20 0 {0} 1 {1:0.###},{2:0.###}", large, x, y));
    }

    private static OutputDocumentType DocumentTypeFor(string name) => name switch
    {
        "Yaklaşık Maliyet Talep Yazısı" => OutputDocumentType.ApproximateRequest,
        "Yaklaşık Maliyet Hesap Cetveli" => OutputDocumentType.ApproximateCostSheet,
        "Firma Teklif Formu" => OutputDocumentType.FirmOfferForm,
        "Piyasa Fiyat Araştırması Tutanağı" => OutputDocumentType.MarketResearchReport,
        "Muayene ve Kabul Tutanağı" => OutputDocumentType.InspectionAcceptanceReport,
        _ => OutputDocumentType.ApproximateRequest
    };

    // ---------------- TEKLİFLER ----------------
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
            foreach (var p in Products.Take(8))
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
        if (!_suppressDirtyTracking)
            ShowDirtyStatus();
        var missingApprox = OfferRows.Count(r => r.IsApproximatePriceMissing);
        var missingFinal = OfferRows.Count(r => r.IsFinalPriceMissing);
        var parts = new List<string>();
        if (missingApprox > 0) parts.Add($"{missingApprox} yaklaşık fiyat eksik");
        if (missingFinal > 0) parts.Add($"{missingFinal} nihai fiyat eksik");
        var detail = parts.Count > 0
            ? $"Kaydedilmemiş değişiklik var: {string.Join(", ", parts)}. Kırmızı alanları kontrol edin."
            : "Kaydedilmemiş değişiklik var. Tüm fiyat alanları dolu.";
        SetOfferEditorBanner("KAYDEDİLMEMİŞ DEĞİŞİKLİK", detail, OfferBannerTone.Dirty);
    }

    private void SaveOffer_Click(object sender, RoutedEventArgs e)
    {
        OfferGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        OfferGrid.CommitEdit(DataGridEditingUnit.Row, true);
        SaveOfferEditor(true);
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
                    if (showErrors) MessageBox.Show($"{row.No}. ürün yaklaşık birim fiyatı 0'dan büyük geçerli bir sayı olmalıdır.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                approx = value;
            }
            if (!string.IsNullOrWhiteSpace(row.FinalPrice))
            {
                if (!NumberParser.TryPositiveDecimal(row.FinalPrice, out var value))
                {
                    if (showErrors) MessageBox.Show($"{row.No}. ürün nihai birim fiyatı 0'dan büyük geçerli bir sayı olmalıdır.", "DoğruTemin", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        foreach (var p in Products.Take(8))
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
                justSaved ? "TEKLİF KAYDEDİLDİ" : "FİYATLAR TAMAM",
                justSaved
                    ? $"{firm.Name}: yaklaşık ve nihai fiyatların tamamı girildi. Kalıcı kayıt için üstteki KAYDET düğmesini kullanın."
                    : $"{firm.Name}: yaklaşık ve nihai fiyatların tamamı girildi.",
                OfferBannerTone.Success);
        }
        else
        {
            var details = new List<string>();
            if (missingApprox > 0) details.Add($"{missingApprox} yaklaşık fiyat eksik");
            if (missingFinal > 0) details.Add($"{missingFinal} nihai fiyat eksik");
            var consequence = missingFinal > 0
                ? "Nihai fiyatı eksik firma değerlendirmeye alınmaz."
                : "Yaklaşık maliyet hesabı ve ilgili belge eksik kalır; firma nihai teklif değerlendirmesine katılabilir.";

            SetOfferEditorBanner(
                justSaved ? "TEKLİF KAYDEDİLDİ • EKSİK VAR" : "FİYAT EKSİK",
                $"{firm.Name}: {string.Join(", ", details)}. {consequence}" +
                (justSaved ? " Kalıcı kayıt için üstteki KAYDET düğmesini kullanın." : " Kırmızı işaretli alanları kontrol edin."),
                OfferBannerTone.Warning);
        }
    }

    private enum OfferBannerTone
    {
        Info,
        Dirty,
        Warning,
        Success
    }

    private void SetOfferEditorBanner(string title, string message, OfferBannerTone tone)
    {
        OfferEditorStatusTitle.Text = title;
        OfferEditorStatusText.Text = message;

        (Color background, Color border, Color iconBackground, Color titleColor, Color textColor, string icon) = tone switch
        {
            OfferBannerTone.Success =>
                (Color.FromRgb(232, 248, 238), Color.FromRgb(139, 211, 164), Color.FromRgb(211, 242, 221), Color.FromRgb(31, 122, 69), Color.FromRgb(43, 108, 67), "✓"),
            OfferBannerTone.Warning =>
                (Color.FromRgb(255, 245, 229), Color.FromRgb(244, 187, 92), Color.FromRgb(255, 231, 190), Color.FromRgb(170, 91, 0), Color.FromRgb(142, 84, 18), "!"),
            OfferBannerTone.Dirty =>
                (Color.FromRgb(255, 247, 230), Color.FromRgb(241, 185, 78), Color.FromRgb(255, 232, 187), Color.FromRgb(171, 96, 0), Color.FromRgb(135, 81, 17), "!"),
            _ =>
                (Color.FromRgb(234, 243, 255), Color.FromRgb(189, 213, 242), Color.FromRgb(216, 233, 255), Color.FromRgb(24, 75, 122), Color.FromRgb(54, 95, 135), "i")
        };

        OfferEditorStatusBanner.Background = new SolidColorBrush(background);
        OfferEditorStatusBanner.BorderBrush = new SolidColorBrush(border);
        OfferEditorStatusIconBorder.Background = new SolidColorBrush(iconBackground);
        OfferEditorStatusTitle.Foreground = new SolidColorBrush(titleColor);
        OfferEditorStatusText.Foreground = new SolidColorBrush(textColor);
        OfferEditorStatusIcon.Foreground = new SolidColorBrush(titleColor);
        OfferEditorStatusIcon.Text = icon;
    }

    // ---------------- İŞLEM ARŞİVİ ----------------

    private DogruTemin.Core.Models.InstitutionInfo BuildInstitutionFromUi() => new()
    {
        TopAuthority = InstitutionTopAuthorityBox.Text.Trim(),
        ShortName = InstitutionFullNameBox.Text.Trim(), // Eski belge/veri modeliyle uyumluluk: ayrı kısa ad alanı kaldırıldı.
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
        DataIntegrityText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(result.IsHealthy ? "#167A46" : "#B42318"));
    }

    private void RunIntegrityCheck_Click(object sender, RoutedEventArgs e)
    {
        var result = _safety.CheckIntegrity();
        UpdateSafetyPanel(result);
        if (result.IsHealthy)
            UiNotificationService.Show(this, result.Message, TimeSpan.FromSeconds(4));
        else
            MessageBox.Show(result.Message, "DoğruTemin - Veri Bütünlüğü", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void CreateSafetyBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (HasUnsavedWork())
            {
                var answer = MessageBox.Show(
                    "Aktif işlemde kaydedilmemiş değişiklikler var. Güvenlik yedeğine son değişikliklerin de girmesi için önce kayıt yapılsın mı?",
                    "DoğruTemin - Güvenlik Yedeği", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
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

        var confirm = MessageBox.Show(
            "Seçilen yedek geri yüklenecek. Mevcut veriler değiştirilmeden önce otomatik güvenlik yedeği oluşturulacaktır.\n\nDevam edilsin mi?",
            "DoğruTemin - Yedekten Geri Yükle", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        var result = _safety.RestoreFromBackup(dialog.FileName);
        if (!result.Success)
        {
            MessageBox.Show(result.Message, "DoğruTemin - Yedekten Geri Yükle", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var message = result.Message;
        if (!string.IsNullOrWhiteSpace(result.SafetyBackupPath))
            message += "\n\nGeri yükleme öncesi mevcut verinin güvenlik kopyası:\n" + result.SafetyBackupPath!;
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
        var years = _storage.ListRecords()
            .Where(x => x.Dates.ApprovalDate.HasValue)
            .Select(x => x.Dates.ApprovalDate!.Value.Year)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();

        if (!years.Contains(DateTime.Now.Year)) years.Add(DateTime.Now.Year);
        years = years.Distinct().OrderByDescending(x => x).ToList();

        StatisticsYears.Clear();
        StatisticsYears.Add("Tüm Yıllar");
        foreach (var year in years) StatisticsYears.Add(year.ToString(CultureInfo.InvariantCulture));

        var target = !string.IsNullOrWhiteSpace(selected) && StatisticsYears.Contains(selected)
            ? selected
            : DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
        statisticsYearBox.SelectedItem = StatisticsYears.Contains(target) ? target : "Tüm Yıllar";
    }
    private void RefreshStatistics_Click(object sender, RoutedEventArgs e)
    {
        RefreshStatisticsYears();
        RefreshStatistics();
    }

    private void StatisticsYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_suppressDirtyTracking)
            RefreshStatistics();
    }

    private void RefreshStatistics()
    {
        if (StatisticsOperationCountText is null || StatisticsYearBox is null) return;

        var allRecords = _storage.ListRecords().ToList();
        var missingDateCount = allRecords.Count(x => !x.Dates.ApprovalDate.HasValue);
        var selectedText = StatisticsYearBox.SelectedItem?.ToString() ?? DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
        int? selectedYear = int.TryParse(selectedText, out var parsedYear) ? parsedYear : null;

        var records = allRecords
            .Where(x => x.Dates.ApprovalDate.HasValue)
            .Where(x => !selectedYear.HasValue || x.Dates.ApprovalDate!.Value.Year == selectedYear.Value)
            .OrderBy(x => x.Dates.ApprovalDate)
            .ToList();

        decimal approximateTotal = 0m;
        decimal finalTotal = 0m;
        decimal savingsTotal = 0m;
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
        foreach (var row in operationRows.OrderByDescending(x => x.FinalTotal).ThenByDescending(x => x.Date).Take(10))
            StatisticsOperations.Add(row);

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
        ArchiveCountText.Text = visible == ArchiveRecords.Count
            ? $"{ArchiveRecords.Count} kayıt"
            : $"{visible} / {ArchiveRecords.Count} kayıt";
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
        if (ArchiveGrid.SelectedItem is ArchiveRecordRow)
            OpenArchive_Click(sender, new RoutedEventArgs());
    }

    private bool ConfirmRecordSwitch() => ConfirmUnsavedWork("devam et", "DoğruTemin - İşlem değiştir");

    private bool ConfirmUnsavedWork(string actionText, string title)
    {
        if (!HasUnsavedWork()) return true;

        var result = MessageBox.Show(
            $"Kaydedilmemiş değişiklikler var.\n\nEVET: Kaydet ve {actionText}\nHAYIR: Kaydetmeden {actionText}\nİPTAL: Programa dön",
            title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
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
        var answer = MessageBox.Show(
            $"Aktif işlemde kaydedilmemiş değişiklikler var.\n\n{actionText} işlemi son kaydedilmiş sürüm üzerinden yapılacaktır.\n\nEVET: Önce kaydet\nHAYIR: Son kaydedilmiş sürümle devam et\nİPTAL: Vazgeç",
            "DoğruTemin - Arşiv", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
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

        var answer = MessageBox.Show(
            $"'{row.JobTitle}' kaydı kopyalansın mı?\n\nYeni kopyanın İşlem No alanı boş bırakılacak ve iş adına ' - Kopya' eklenecektir.",
            "DoğruTemin - İşlem Kopyala", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
            var answer = MessageBox.Show(
                "Aktif işlemde kaydedilmemiş değişiklikler var. Arşiv yedeği yalnız diskteki son kaydedilmiş verileri içerir.\n\nYedek oluşturmadan önce aktif işlem kaydedilsin mi?",
                "DoğruTemin - Arşiv Yedeği", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
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

    // ---------------- KAYIT DURUMU ----------------
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

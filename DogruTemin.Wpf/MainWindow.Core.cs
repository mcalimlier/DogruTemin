using DogruTemin.Wpf.Models;
using ProcurementData = DogruTemin.Core.Models.ProcurementData;
using ValidationSeverity = DogruTemin.Core.Models.ValidationSeverity;
using OutputDocumentType = DogruTemin.Core.Models.OutputDocumentType;
using FirmOfferCore = DogruTemin.Core.Models.FirmOffer;
using AppSettings = DogruTemin.Core.Models.ApplicationSettings;
using DogruTemin.Core.Services;
using DogruTemin.Wpf.Services;
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

}

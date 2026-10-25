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

public partial class MainWindow
{
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
        ResponsiveLayoutService.ApplyPreference(this, "Auto");
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
        ApprovalDocumentDatePicker.SelectedDate = null;
        MarketReportDatePicker.SelectedDate = null;
        InspectionReportDatePicker.SelectedDate = null;
        DeliveryDatePicker.SelectedDate = null;

        JobTitleBox.Text = string.Empty;
        AssignmentNoBox.Text = string.Empty;
        ApprovalNoBox.Text = string.Empty;
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

        var approximateRequest = new OutputDocument { Name = "Yaklaşık Maliyet Talep Yazısı", IsSelected = true, Status = "Uyarı", Note = "Ön hazırlık belgesi" };
        var firmOfferForm = new OutputDocument { Name = "Firma Teklif Formu", IsSelected = true, Status = "Uyarı", Note = "Ön hazırlık belgesi" };
        var approximateCostSheet = new OutputDocument { Name = "Yaklaşık Maliyet Hesap Cetveli", IsSelected = true, Status = "Uyarı", Note = "Nihai işlem belgesi" };
        var approvalDocument = new OutputDocument { Name = "Onay Belgesi", IsSelected = true, Status = "Uyarı", Note = "Nihai işlem belgesi" };
        var marketResearchReport = new OutputDocument { Name = "Piyasa Fiyat Araştırması Tutanağı", IsSelected = true, Status = "Uyarı", Note = "Nihai işlem belgesi" };
        var inspectionReport = new OutputDocument { Name = "Muayene ve Kabul Tutanağı", IsSelected = true, Status = "Uyarı", Note = "Nihai işlem belgesi" };

        PreparationOutputDocuments.Add(approximateRequest);
        PreparationOutputDocuments.Add(firmOfferForm);

        FinalOutputDocuments.Add(approximateCostSheet);
        FinalOutputDocuments.Add(approvalDocument);
        FinalOutputDocuments.Add(marketResearchReport);
        FinalOutputDocuments.Add(inspectionReport);

        foreach (var doc in PreparationOutputDocuments.Concat(FinalOutputDocuments))
            OutputDocuments.Add(doc);
    }

    private void ShowView(string view)
    {
        if (view == "Commission") view = "Institution";
        _currentView = view;
        WorkspaceLayout.MinWidth = view is "Settings" or "AboutSafety" ? 0 : 700;

        HomeView.Visibility = view == "Home" ? Visibility.Visible : Visibility.Collapsed;
        GeneralView.Visibility = view == "General" ? Visibility.Visible : Visibility.Collapsed;
        FirmsOffersView.Visibility = view is "Firms" or "Offers" ? Visibility.Visible : Visibility.Collapsed;
        DocumentsDatesView.Visibility = view is "Dates" or "Output" ? Visibility.Visible : Visibility.Collapsed;
        ApprovalDocumentView.Visibility = view == "ApprovalDocument" ? Visibility.Visible : Visibility.Collapsed;
        InstitutionView.Visibility = view == "Institution" ? Visibility.Visible : Visibility.Collapsed;
        ControlView.Visibility = view == "Control" ? Visibility.Visible : Visibility.Collapsed;
        StatisticsView.Visibility = view == "Statistics" ? Visibility.Visible : Visibility.Collapsed;
        ArchiveView.Visibility = view == "Archive" ? Visibility.Visible : Visibility.Collapsed;
        SettingsView.Visibility = view == "Settings" ? Visibility.Visible : Visibility.Collapsed;
        NavSettings.Tag = view == "Settings" ? "Active" : null;
        AboutSafetyView.Visibility = view == "AboutSafety" ? Visibility.Visible : Visibility.Collapsed;
        ResponsiveLayoutService.UpdateWorkspace(this);
        WorkspaceScroll.ScrollToHorizontalOffset(0);
        WorkspaceScroll.ScrollToVerticalOffset(0);

        if (view == "Firms") Dispatcher.BeginInvoke(new Action(() => FirmsView?.BringIntoView()));
        if (view == "Offers") Dispatcher.BeginInvoke(new Action(() => OffersView?.BringIntoView()));

        NavHome.Tag = view == "Home" ? "Active" : null;
        NavInstitution.Tag = view == "Institution" ? "Active" : null;
        NavGeneral.Tag = view == "General" ? "Active" : null;
        NavFirmsOffers.Tag = view is "Firms" or "Offers" ? "Active" : null;
        NavApprovalDocument.Tag = view == "ApprovalDocument" ? "Active" : null;
        NavDocumentsDates.Tag = view is "Dates" or "Output" ? "Active" : null;
        NavControl.Tag = view == "Control" ? "Active" : null;
        NavStatistics.Tag = view == "Statistics" ? "Active" : null;
        NavArchive.Tag = view == "Archive" ? "Active" : null;
        NavAboutSafety.Tag = view == "AboutSafety" ? "Active" : null;
    }

    private void NavSettings_Click(object sender, RoutedEventArgs e) => ShowView("Settings");

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
        if (_offerEditingFirm is null)
            PrepareOffersView();
    }
    private void NavApprovalDocument_Click(object sender, RoutedEventArgs e) => ShowView("ApprovalDocument");
    private void NavDocumentsDates_Click(object sender, RoutedEventArgs e) => ShowView("Dates");
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
        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            e.Handled = true;
            return;
        }

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
}

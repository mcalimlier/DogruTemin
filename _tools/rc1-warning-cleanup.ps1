$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Read([string]$path) { [IO.File]::ReadAllText((Join-Path $PWD $path)) }
function Write([string]$path, [string]$text) { [IO.File]::WriteAllText((Join-Path $PWD $path), $text, $utf8) }
function ReplaceRequired([string]$text,[string]$old,[string]$new,[string]$label) {
    if (-not $text.Contains($old)) { throw "$label bulunamadı." }
    return $text.Replace($old,$new)
}
function EnsureUsing([string]$text,[string]$usingLine) {
    if ($text.Contains($usingLine)) { return $text }
    $firstUsingEnd = $text.IndexOf("`n")
    if ($firstUsingEnd -lt 0) { throw "using ekleme noktası bulunamadı." }
    return $text.Insert($firstUsingEnd + 1, $usingLine + "`r`n")
}

# StorageService: dosya adlarında kültürden bağımsız zaman damgası.
$p='DogruTemin.Core/Services/StorageService.cs'; $t=Read $p
$t=EnsureUsing $t 'using System.Globalization;'
$t=ReplaceRequired $t 'DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")' 'DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture)' 'StorageService zaman damgası'
Write $p $t

# ValidationService: analyzerın doğruladığı stateless doğrulama yardımcıları static.
$p='DogruTemin.Core/Services/ValidationService.cs'; $t=Read $p
$t=$t.Replace('public IReadOnlyList<ValidationIssue> ValidateDraft(', 'public static IReadOnlyList<ValidationIssue> ValidateDraft(')
$t=$t.Replace('public IReadOnlyList<ValidationIssue> DateLogicWarnings(', 'public static IReadOnlyList<ValidationIssue> DateLogicWarnings(')
$t=$t.Replace('public IReadOnlyList<ValidationIssue> ValidateDocument(', 'public static IReadOnlyList<ValidationIssue> ValidateDocument(')
$t=$t.Replace('public int ReadinessPercent(', 'public static int ReadinessPercent(')
Write $p $t

# Tüm çağrıları static API'ye geçir; ValidateWorkflow instance olarak kalır.
Get-ChildItem -Recurse -Filter '*.cs' | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $text=[IO.File]::ReadAllText($_.FullName)
    $new=$text.Replace('_validation.ValidateDraft(', 'ValidationService.ValidateDraft(')
              .Replace('_validation.DateLogicWarnings(', 'ValidationService.DateLogicWarnings(')
              .Replace('_validation.ValidateDocument(', 'ValidationService.ValidateDocument(')
              .Replace('_validation.ReadinessPercent(', 'ValidationService.ReadinessPercent(')
              .Replace('new ValidationService().ValidateDraft(', 'ValidationService.ValidateDraft(')
              .Replace('new ValidationService().DateLogicWarnings(', 'ValidationService.DateLogicWarnings(')
              .Replace('new ValidationService().ValidateDocument(', 'ValidationService.ValidateDocument(')
              .Replace('new ValidationService().ReadinessPercent(', 'ValidationService.ReadinessPercent(')
    if ($new -ne $text) { [IO.File]::WriteAllText($_.FullName,$new,$utf8) }
}

# ProcurementEngine private stateless hesap yardımcısı.
$p='DogruTemin.Core/Services/ProcurementEngine.cs'; $t=Read $p
$t=$t.Replace('private void CalculateLowestCompleteFirm(', 'private static void CalculateLowestCompleteFirm(')
$t=$t.Replace('private bool CalculateLowestCompleteFirm(', 'private static bool CalculateLowestCompleteFirm(')
$t=$t.Replace('private string CalculateLowestCompleteFirm(', 'private static string CalculateLowestCompleteFirm(')
$t=$t.Replace('private Firm? CalculateLowestCompleteFirm(', 'private static Firm? CalculateLowestCompleteFirm(')
Write $p $t

# MainWindow: kullanıcıya gösterilen sayılar/tarihler kültürden bağımsız ve stateless yardımcı static.
$p='DogruTemin.Wpf/MainWindow.xaml.cs'; $t=Read $p
$t=$t.Replace('ControlCriticalCountText.Text = critical.ToString();','ControlCriticalCountText.Text = critical.ToString(CultureInfo.InvariantCulture);')
$t=$t.Replace('ControlWarningCountText.Text = warning.ToString();','ControlWarningCountText.Text = warning.ToString(CultureInfo.InvariantCulture);')
$t=$t.Replace('DateText = date.ToString("dd.MM.yyyy"),','DateText = date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),')
$t=$t.Replace('ApprovalDate = data.Dates.ApprovalDate?.ToString("dd.MM.yyyy") ?? "—",','ApprovalDate = data.Dates.ApprovalDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "—",')
$t=$t.Replace('LastSaved = lastSaved.ToString("dd.MM.yyyy HH:mm"),','LastSaved = lastSaved.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),')
$t=$t.Replace('private List<string> GetExportCoreIssues(', 'private static List<string> GetExportCoreIssues(')
$t=$t.Replace('new DiagnosticPackageService().Create(', 'DiagnosticPackageService.Create(')
$t=$t.Replace('_managementReports.BuildPdf(', 'ManagementReportService.BuildPdf(')
$t=$t.Replace('_managementReports.SaveExcel(', 'ManagementReportService.SaveExcel(')
Write $p $t

# Recovery marker: invariant timestamp/PID, interpolated StringBuilder analyzer uyarısı yok.
$p='DogruTemin.Wpf/RecoverySessionService.cs'; $t=Read $p
$t=EnsureUsing $t 'using System.Globalization;'
$old=@'
            var content = new StringBuilder()
                .AppendLine("DoğruTemin çalışma oturumu")
                .AppendLine($"Başlangıç: {DateTimeOffset.Now:O}")
                .AppendLine($"PID: {Environment.ProcessId}")
                .ToString();
'@
$new=@'
            var content = new StringBuilder()
                .AppendLine("DoğruTemin çalışma oturumu")
                .Append("Başlangıç: ").AppendLine(DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture))
                .Append("PID: ").AppendLine(Environment.ProcessId.ToString(CultureInfo.InvariantCulture))
                .ToString();
'@
$t=ReplaceRequired $t $old $new 'Recovery marker biçimi'
Write $p $t

# DiagnosticPackageService gerçek statik yardımcıdır; çağrı noktası yukarıda güncellendi.
$p='DogruTemin.Wpf/DiagnosticPackageService.cs'; $t=Read $p
$t=$t.Replace('public sealed class DiagnosticPackageService','public static class DiagnosticPackageService')
$t=$t.Replace('public string Create(ApplicationSafetyService safetyService)','public static string Create(ApplicationSafetyService safetyService)')
Write $p $t

# OfficialDocumentService: kültürden bağımsız sıra numaraları, stateless yardımcılar ve somut WPF dönüş tipleri.
$p='DogruTemin.Wpf/Services/OfficialDocumentService.cs'; $t=Read $p
$t=EnsureUsing $t 'using System.Globalization;'
$t=$t.Replace('.No.ToString()', '.No.ToString(CultureInfo.InvariantCulture)')
$t=$t.Replace('private FixedPage BuildApproximateRequestPage(', 'private static FixedPage BuildApproximateRequestPage(')
$t=$t.Replace('private FixedPage BuildFirmOfferFormPage(', 'private static FixedPage BuildFirmOfferFormPage(')
$t=$t.Replace('private FrameworkElement BuildInstitutionContactBlock(', 'private StackPanel BuildInstitutionContactBlock(')
$t=$t.Replace('private static FrameworkElement BuildInstitutionContactBlock(', 'private static StackPanel BuildInstitutionContactBlock(')
$t=$t.Replace('private FrameworkElement BuildSimpleSignatureRow(', 'private Grid BuildSimpleSignatureRow(')
$t=$t.Replace('private static FrameworkElement BuildSimpleSignatureRow(', 'private static Grid BuildSimpleSignatureRow(')
$t=$t.Replace('private FrameworkElement BuildLabeledSignatureRow(', 'private Grid BuildLabeledSignatureRow(')
$t=$t.Replace('private static FrameworkElement BuildLabeledSignatureRow(', 'private static Grid BuildLabeledSignatureRow(')
$t=$t.Replace('private FrameworkElement BuildApproximateRequestItemsTable(', 'private Grid BuildApproximateRequestItemsTable(')
$t=$t.Replace('private static FrameworkElement BuildApproximateRequestItemsTable(', 'private static Grid BuildApproximateRequestItemsTable(')
$t=$t.Replace('private FrameworkElement BuildFirmOfferItemsTable(', 'private Grid BuildFirmOfferItemsTable(')
$t=$t.Replace('private static FrameworkElement BuildFirmOfferItemsTable(', 'private static Grid BuildFirmOfferItemsTable(')
$t=$t.Replace('private FrameworkElement BuildApproximateCostTable(', 'private Grid BuildApproximateCostTable(')
$t=$t.Replace('private static FrameworkElement BuildApproximateCostTable(', 'private static Grid BuildApproximateCostTable(')
$t=$t.Replace('private FrameworkElement BuildAllFirmOffersTable(', 'private static Grid BuildAllFirmOffersTable(')
$t=$t.Replace('private static FrameworkElement BuildAllFirmOffersTable(', 'private static Grid BuildAllFirmOffersTable(')
$t=$t.Replace('private FrameworkElement BuildWinningFirmTable(', 'private static Grid BuildWinningFirmTable(')
$t=$t.Replace('private static FrameworkElement BuildWinningFirmTable(', 'private static Grid BuildWinningFirmTable(')
$t=$t.Replace('private FrameworkElement BuildInspectionItemsTable(', 'private static Grid BuildInspectionItemsTable(')
$t=$t.Replace('private static FrameworkElement BuildInspectionItemsTable(', 'private static Grid BuildInspectionItemsTable(')
$t=$t.Replace('private FrameworkElement BuildMarketSignatureRow(', 'private Grid BuildMarketSignatureRow(')
$t=$t.Replace('private static FrameworkElement BuildMarketSignatureRow(', 'private static Grid BuildMarketSignatureRow(')
$t=$t.Replace('private FrameworkElement BuildInspectionSignatureTable(', 'private Grid BuildInspectionSignatureTable(')
$t=$t.Replace('private static FrameworkElement BuildInspectionSignatureTable(', 'private static Grid BuildInspectionSignatureTable(')
Write $p $t

# Management report: iki stateless dışa aktarım helperı static, somut KPI tipi ve invariant tarih.
$p='DogruTemin.Wpf/Services/ManagementReportService.cs'; $t=Read $p
$t=$t.Replace('public FixedDocument BuildPdf(', 'public static FixedDocument BuildPdf(')
$t=$t.Replace('public void SaveExcel(', 'public static void SaveExcel(')
$t=$t.Replace('private static FrameworkElement BuildKpiRow(', 'private static UniformGrid BuildKpiRow(')
$t=$t.Replace('r.ApprovalDate.ToString("dd.MM.yy")', 'r.ApprovalDate.ToString("dd.MM.yy", CultureInfo.InvariantCulture)')
$t=$t.Replace('IReadOnlyList<double> widths', 'double[] widths')
Write $p $t

# Test isimleri analyzer dostu; davranış aynı. IDisposable kuralı tamamlanır.
$p='DogruTemin.Tests/StorageServiceTests.cs'; $t=Read $p
$t=$t.Replace('ExplicitRoot_CreatesExpectedFolderStructure','ExplicitRootCreatesExpectedFolderStructure')
$t=$t.Replace('Save_CreatesActiveAndArchiveRecord','SaveCreatesActiveAndArchiveRecord')
$t=$t.Replace('LegacyMigration_CopiesWithoutOverwritingExistingFiles','LegacyMigrationCopiesWithoutOverwritingExistingFiles')
$t=$t.Replace('        try { if (Directory.Exists(_temp)) Directory.Delete(_temp, true); } catch { }','        try { if (Directory.Exists(_temp)) Directory.Delete(_temp, true); } catch { }`r`n        GC.SuppressFinalize(this);')
Write $p $t

$p='DogruTemin.Tests/AppVersionInfoTests.cs'; $t=Read $p
$t=$t.Replace('Version_ComesFromCentralBuildProperties','VersionComesFromCentralBuildProperties')
Write $p $t

Write-Host 'RC1 hedefli analyzer temizliği uygulandı.' -ForegroundColor Green

$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Read-Normalized([string]$Path) {
    return [System.IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
}
function Write-Normalized([string]$Path, [string]$Text) {
    [System.IO.File]::WriteAllText($Path, $Text.Replace("`r`n", "`n"), $Utf8NoBom)
}
function Replace-First([string]$Text, [string]$Old, [string]$New, [string]$Label) {
    $i = $Text.IndexOf($Old, [System.StringComparison]::Ordinal)
    if ($i -lt 0) { throw "Anchor bulunamadı: $Label" }
    return $Text.Substring(0, $i) + $New + $Text.Substring($i + $Old.Length)
}

# -----------------------------------------------------------------------------
# 1) Resmî belge motoru: Onay Belgesi destek listesi + temiz başlık biçimi
# -----------------------------------------------------------------------------
$officialPath = 'DogruTemin.Wpf/Services/OfficialDocumentService.cs'
$official = Read-Normalized $officialPath

$implementedPattern = '(?s)public static bool IsImplemented\(OutputDocumentType type\)\s*=>\s*type is OutputDocumentType\.ApproximateRequest.*?OutputDocumentType\.InspectionAcceptanceReport;'
$implementedMatch = [regex]::Match($official, $implementedPattern)
if (-not $implementedMatch.Success) { throw 'IsImplemented bloğu bulunamadı.' }
$implementedBlock = $implementedMatch.Value
if ($implementedBlock -notmatch 'OutputDocumentType\.ApprovalDocument') {
    $implementedBlock = $implementedBlock.Replace(
        'or OutputDocumentType.ApproximateCostSheet',
        "or OutputDocumentType.ApproximateCostSheet`n            or OutputDocumentType.ApprovalDocument")
    $official = $official.Substring(0, $implementedMatch.Index) + $implementedBlock + $official.Substring($implementedMatch.Index + $implementedMatch.Length)
}

$oldQuantity = 'private static string ApprovalQuantitySummary(IReadOnlyCollection<ProductItem> products)'
$newQuantity = 'private static string ApprovalQuantitySummary(List<ProductItem> products)'
if ($official.Contains($oldQuantity)) {
    $official = $official.Replace($oldQuantity, $newQuantity)
} elseif (-not $official.Contains($newQuantity)) {
    throw 'ApprovalQuantitySummary imzası bulunamadı.'
}

$oldHeaderCell = 'AddRow(header, 28); AddCell(header, 1, 0, "BELGE TARİH VE SAYISI", true, 9.4, TextAlignment.Left); AddCell(header, 1, 1, $"{DateText(data.Dates.ApprovalDocumentDate)}  /  {Safe(info.DocumentNumber)}", false, 9.4, TextAlignment.Center);'
$newHeaderCell = @'
        var approvalHeaderValue = string.IsNullOrWhiteSpace(info.DocumentNumber)
            ? DateText(data.Dates.ApprovalDocumentDate)
            : $"{DateText(data.Dates.ApprovalDocumentDate)}  /  {Safe(info.DocumentNumber)}";
        AddRow(header, 28); AddCell(header, 1, 0, "BELGE TARİH VE SAYISI", true, 9.4, TextAlignment.Left); AddCell(header, 1, 1, approvalHeaderValue, false, 9.4, TextAlignment.Center);
'@
if ($official.Contains($oldHeaderCell)) {
    $official = $official.Replace($oldHeaderCell, $newHeaderCell.TrimEnd("`n"))
}
Write-Normalized $officialPath $official

# -----------------------------------------------------------------------------
# 2) Doğrulama: referansta karşılığı dolu alanlar zorunlu, boş olanlar opsiyonel
#    Zorunlu manuel alanlar: kullanılabilir ödenek, bütçe tertibi, ihale usulü,
#    ilanın şekli/adedi, fiyat farkı dayanağı, onaya sunan ad/unvan.
#    Opsiyonel: belge sayısı, yatırım proje no, avans şartları,
#    ön yeterlik/ihale dokümanı satış bedeli ve açıklama (boşsa iş tanımı basılır).
# -----------------------------------------------------------------------------
$validationPath = 'DogruTemin.Core/Services/ValidationService.cs'
$validation = Read-Normalized $validationPath

$approvalCaseRegex = [regex]::new('(?s)            case OutputDocumentType\.ApprovalDocument:\n.*?                break;\n            case OutputDocumentType\.MarketResearchReport:')
if (-not $approvalCaseRegex.IsMatch($validation)) { throw 'ApprovalDocument validation case bulunamadı.' }
$approvalCase = @'
            case OutputDocumentType.ApprovalDocument:
                RequireDate(data.Dates.ApprovalDocumentDate, issues, "Onay Belgesi tarihi");
                RequireText(data.ApprovalDocument.AvailableBudgetAmount, issues, "Kullanılabilir ödenek tutarı");
                RequireText(data.ApprovalDocument.BudgetCode, issues, "Bütçe tertibi");
                RequireText(data.ApprovalDocument.TenderProcedure, issues, "İhale usulü");
                RequireText(data.ApprovalDocument.AnnouncementMethod, issues, "İlanın şekli ve adedi");
                RequireText(data.ApprovalDocument.PriceDifferenceBasis, issues, "Fiyat farkı dayanağı");
                RequireText(data.ApprovalDocument.SubmittedByName, issues, "Onaya sunan ad soyad");
                RequireText(data.ApprovalDocument.SubmittedByTitle, issues, "Onaya sunan unvanı");
                RequireText(data.Institution.AuthorizedName, issues, "Harcama yetkilisi adı soyadı");
                RequireText(data.Institution.AuthorizedDuty, issues, "Harcama yetkilisi görevi");
                RequireCommission(data.MarketCommission, issues, "Piyasa araştırma görevlileri");
                RequireCommission(data.InspectionCommission, issues, "Muayene ve kabul komisyonu");
                RequireAtLeastThreeFirms(firms, issues);
                foreach (var firm in firms)
                    foreach (var product in products)
                        if (!(OfferFor(firm, product.Id)?.ApproximateUnitPrice > 0)) Critical(issues, "Offers", $"{firm.No}. firma / {product.No}. ürün yaklaşık fiyatı");
                break;
            case OutputDocumentType.MarketResearchReport:
'@
$validation = $approvalCaseRegex.Replace($validation, $approvalCase.TrimEnd("`n"), 1)

$workflowOld = @'
        RequireDate(data.Dates.CostCalculationDate, issues, "Yaklaşık Maliyet Hesap Cetveli tarihi");
        RequireDate(data.Dates.ApprovalDocumentDate, issues, "Onay Belgesi tarihi");
        RequireDate(data.Dates.MarketReportDate, issues, "Piyasa Fiyat Araştırması Tutanağı tarihi");
'@
$workflowNew = @'
        RequireDate(data.Dates.CostCalculationDate, issues, "Yaklaşık Maliyet Hesap Cetveli tarihi");
        RequireDate(data.Dates.ApprovalDocumentDate, issues, "Onay Belgesi tarihi");
        RequireText(data.ApprovalDocument.AvailableBudgetAmount, issues, "Kullanılabilir ödenek tutarı");
        RequireText(data.ApprovalDocument.BudgetCode, issues, "Bütçe tertibi");
        RequireText(data.ApprovalDocument.TenderProcedure, issues, "İhale usulü");
        RequireText(data.ApprovalDocument.AnnouncementMethod, issues, "İlanın şekli ve adedi");
        RequireText(data.ApprovalDocument.PriceDifferenceBasis, issues, "Fiyat farkı dayanağı");
        RequireText(data.ApprovalDocument.SubmittedByName, issues, "Onaya sunan ad soyad");
        RequireText(data.ApprovalDocument.SubmittedByTitle, issues, "Onaya sunan unvanı");
        RequireDate(data.Dates.MarketReportDate, issues, "Piyasa Fiyat Araştırması Tutanağı tarihi");
'@
if ($validation.Contains($workflowOld)) {
    $validation = $validation.Replace($workflowOld, $workflowNew)
} elseif (-not $validation.Contains('RequireText(data.ApprovalDocument.BudgetCode, issues, "Bütçe tertibi");')) {
    throw 'Workflow Onay Belgesi zorunluluk bloğu bulunamadı.'
}

$readinessOld = @'
            data.Dates.ApprovalDocumentDate.HasValue,
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.AvailableBudgetAmount)
'@
$readinessNew = @'
            data.Dates.ApprovalDocumentDate.HasValue,
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.AvailableBudgetAmount),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.BudgetCode),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.TenderProcedure),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.AnnouncementMethod),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.PriceDifferenceBasis),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.SubmittedByName),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.SubmittedByTitle)
'@
if ($validation.Contains($readinessOld)) {
    $validation = $validation.Replace($readinessOld, $readinessNew)
} elseif (-not $validation.Contains('!string.IsNullOrWhiteSpace(data.ApprovalDocument.BudgetCode),')) {
    throw 'Readiness Onay Belgesi bloğu bulunamadı.'
}
Write-Normalized $validationPath $validation

# -----------------------------------------------------------------------------
# 3) UI: zorunlu/opsiyonel alanları kullanıcıya açıkça göster
# -----------------------------------------------------------------------------
$xamlPath = 'DogruTemin.Wpf/MainWindow.xaml'
$xaml = Read-Normalized $xamlPath

$oldSubtitle = 'Yalnız Onay Belgesi''ne özgü bilgileri girin. Kurum, mal/hizmet, yaklaşık maliyet, komisyonlar ve harcama yetkilisi mevcut işlemden otomatik alınır.'
$newSubtitle = 'Referansta karşılığı dolu bilgiler zorunludur (*). Opsiyonel alanlar boş bırakılabilir; Açıklamalar boşsa İşin Tanımı otomatik kullanılır.'
if ($xaml.Contains($oldSubtitle)) { $xaml = $xaml.Replace($oldSubtitle, $newSubtitle) }

$labelReplacements = [ordered]@{
    'Text="Onay Belgesi Tarihi" Style="{StaticResource PremiumFieldLabel}"' = 'Text="Onay Belgesi Tarihi *" Style="{StaticResource PremiumFieldLabel}"'
    'Text="Belge Sayısı" Style="{StaticResource PremiumFieldLabel}"' = 'Text="Belge Sayısı (Opsiyonel)" Style="{StaticResource PremiumFieldLabel}"'
    'Text="Kullanılabilir Ödenek Tutarı" Style="{StaticResource PremiumFieldLabel}"' = 'Text="Kullanılabilir Ödenek Tutarı *" Style="{StaticResource PremiumFieldLabel}"'
    'Text="Yatırım Proje Numarası (varsa)" Style="{StaticResource PremiumFieldLabel}"' = 'Text="Yatırım Proje Numarası (Opsiyonel)" Style="{StaticResource PremiumFieldLabel}"'
    'Text="Bütçe Tertibi (varsa)" Style="{StaticResource PremiumFieldLabel}"' = 'Text="Bütçe Tertibi *" Style="{StaticResource PremiumFieldLabel}"'
    'Text="Avans Verilecekse Şartları" Style="{StaticResource PremiumFieldLabel}"' = 'Text="Avans Verilecekse Şartları (Opsiyonel)" Style="{StaticResource PremiumFieldLabel}"'
    'Text="İhale Usulü" Style="{StaticResource PremiumFieldLabel}"' = 'Text="İhale Usulü *" Style="{StaticResource PremiumFieldLabel}"'
    'Text="İlanın Şekli ve Adedi" Style="{StaticResource PremiumFieldLabel}"' = 'Text="İlanın Şekli ve Adedi *" Style="{StaticResource PremiumFieldLabel}"'
    'Text="Ön Yeterlik / İhale Dokümanı Satış Bedeli" Style="{StaticResource PremiumFieldLabel}"' = 'Text="Ön Yeterlik / İhale Dokümanı Satış Bedeli (Opsiyonel)" Style="{StaticResource PremiumFieldLabel}"'
    'Text="Fiyat Farkı Ödenecekse Dayanağı" Style="{StaticResource PremiumFieldLabel}"' = 'Text="Fiyat Farkı Ödenecekse Dayanağı *" Style="{StaticResource PremiumFieldLabel}"'
    'Text="Açıklamalar" Style="{StaticResource PremiumFieldLabel}"' = 'Text="Açıklamalar (Opsiyonel)" Style="{StaticResource PremiumFieldLabel}"'
}
foreach ($old in $labelReplacements.Keys) {
    $new = $labelReplacements[$old]
    if ($xaml.Contains($old)) { $xaml = $xaml.Replace($old, $new) }
}

$submittedNameOld = '<TextBlock Text="Ad Soyad" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalSubmittedByNameBox"'
$submittedNameNew = '<TextBlock Text="Ad Soyad *" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalSubmittedByNameBox"'
if ($xaml.Contains($submittedNameOld)) { $xaml = $xaml.Replace($submittedNameOld, $submittedNameNew) }
$submittedTitleOld = '<TextBlock Text="Unvanı" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalSubmittedByTitleBox"'
$submittedTitleNew = '<TextBlock Text="Unvanı *" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalSubmittedByTitleBox"'
if ($xaml.Contains($submittedTitleOld)) { $xaml = $xaml.Replace($submittedTitleOld, $submittedTitleNew) }
Write-Normalized $xamlPath $xaml

# -----------------------------------------------------------------------------
# 4) Otomatik test: opsiyoneller boşken Onay Belgesi geçerli; dolu referans alanları zorunlu
# -----------------------------------------------------------------------------
$testPath = 'DogruTemin.Tests/ValidationTests.cs'
$tests = Read-Normalized $testPath
$testMarker = @'
    [Fact]
    public void DuplicateFirmNameIsDetectedIgnoringPunctuationAndCase()
'@
if ($tests -notmatch 'ApprovalDocumentAllowsReferenceBlankOptionalFields') {
$testBlock = @'
    [Fact]
    public void ApprovalDocumentAllowsReferenceBlankOptionalFields()
    {
        var product = new ProductItem { No = 1, Name = "Kırtasiye", Unit = "Adet", Quantity = "1" };
        Firm FirmWithPrice(int no, decimal price) => new()
        {
            No = no,
            Name = $"Firma {no}",
            Offers = new() { new FirmOffer { ProductId = product.Id, ApproximateUnitPrice = price } }
        };
        List<CommissionMember> Commission(string prefix) => new()
        {
            new CommissionMember { No = 1, Name = $"{prefix} 1", Title = "Başkan" },
            new CommissionMember { No = 2, Name = $"{prefix} 2", Title = "Üye" },
            new CommissionMember { No = 3, Name = $"{prefix} 3", Title = "Üye" }
        };

        var data = new ProcurementData
        {
            JobTitle = "Kırtasiye alımı",
            Products = new() { product },
            Firms = new() { FirmWithPrice(1, 100), FirmWithPrice(2, 110), FirmWithPrice(3, 120) },
            MarketCommission = Commission("Piyasa"),
            InspectionCommission = Commission("Muayene"),
            Dates = new ProcessDates { ApprovalDocumentDate = new DateTime(2026, 9, 2) },
            Institution = new InstitutionInfo
            {
                TopAuthority = "Kaymakamlık",
                FullName = "İlçe Milli Eğitim Müdürlüğü",
                Province = "Uşak",
                AuthorizedName = "Yetkili Adı",
                AuthorizedDuty = "İlçe Milli Eğitim Müdürü"
            },
            ApprovalDocument = new ApprovalDocumentInfo
            {
                DocumentNumber = string.Empty,
                AvailableBudgetAmount = "70000",
                InvestmentProjectNumber = string.Empty,
                BudgetCode = "40.149.423.292.13.68.01.03.02",
                AdvanceConditions = string.Empty,
                TenderProcedure = "4734 S.K. 22/d Kapsamında",
                AnnouncementMethod = "İlansız",
                PrequalificationDocumentSalePrice = string.Empty,
                PriceDifferenceBasis = "Ödenmeyecektir",
                Notes = string.Empty,
                SubmittedByName = "Hali ÖZENÇ",
                SubmittedByTitle = "Şube Müdürü"
            }
        };

        var issues = ValidationService.ValidateDocument(data, OutputDocumentType.ApprovalDocument);

        Assert.Empty(issues);
    }

    [Fact]
    public void ApprovalDocumentRequiresReferencePopulatedManualFields()
    {
        var data = new ProcurementData
        {
            ApprovalDocument = new ApprovalDocumentInfo
            {
                AvailableBudgetAmount = string.Empty,
                BudgetCode = string.Empty,
                TenderProcedure = string.Empty,
                AnnouncementMethod = string.Empty,
                PriceDifferenceBasis = string.Empty,
                SubmittedByName = string.Empty,
                SubmittedByTitle = string.Empty
            }
        };

        var issues = ValidationService.ValidateDocument(data, OutputDocumentType.ApprovalDocument);

        Assert.Contains(issues, i => i.Message == "Kullanılabilir ödenek tutarı");
        Assert.Contains(issues, i => i.Message == "Bütçe tertibi");
        Assert.Contains(issues, i => i.Message == "İhale usulü");
        Assert.Contains(issues, i => i.Message == "İlanın şekli ve adedi");
        Assert.Contains(issues, i => i.Message == "Fiyat farkı dayanağı");
        Assert.DoesNotContain(issues, i => i.Message.Contains("Yatırım Proje", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(issues, i => i.Message.Contains("Avans", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(issues, i => i.Message.Contains("Ön Yeterlik", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(issues, i => i.Message.Contains("Belge sayısı", StringComparison.OrdinalIgnoreCase));
    }

'@
    if (-not $tests.Contains($testMarker)) { throw 'ValidationTests ekleme noktası bulunamadı.' }
    $tests = Replace-First $tests $testMarker ($testBlock + $testMarker) 'Onay Belgesi validation tests'
}
Write-Normalized $testPath $tests

# Derle + test
& dotnet restore 'DogruTemin.Wpf/DogruTemin.Wpf.csproj' | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'WPF restore başarısız' }
& dotnet restore 'DogruTemin.Tests/DogruTemin.Tests.csproj' | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Test restore başarısız' }
& dotnet build 'DogruTemin.Wpf/DogruTemin.Wpf.csproj' -c Release --no-restore | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Release build başarısız' }
& dotnet test 'DogruTemin.Tests/DogruTemin.Tests.csproj' -c Release --no-restore | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Testler başarısız' }

git config user.name 'mcalimlier'
git config user.email '123293878+mcalimlier@users.noreply.github.com'
git add $officialPath $validationPath $xamlPath $testPath
if (-not (git diff --cached --quiet)) {
    git commit -m 'Onay Belgesinde referans doluluk kuralını uygula'
    git push
}

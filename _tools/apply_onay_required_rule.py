from pathlib import Path
import re


def read(path: str) -> str:
    return Path(path).read_text(encoding="utf-8").replace("\r\n", "\n")


def write(path: str, text: str) -> None:
    Path(path).write_text(text.replace("\r\n", "\n"), encoding="utf-8", newline="\n")


def replace_once(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count == 0:
        raise RuntimeError(f"Anchor bulunamadı: {label}")
    if count > 1:
        raise RuntimeError(f"Anchor birden fazla: {label}")
    return text.replace(old, new, 1)


# 1) ValidationService ---------------------------------------------------------
vp = "DogruTemin.Core/Services/ValidationService.cs"
v = read(vp)

case_pattern = re.compile(
    r'            case OutputDocumentType\.ApprovalDocument:\n.*?'
    r'                break;\n'
    r'            case OutputDocumentType\.MarketResearchReport:',
    re.S,
)
case_replacement = '''            case OutputDocumentType.ApprovalDocument:
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
            case OutputDocumentType.MarketResearchReport:'''
if not case_pattern.search(v):
    raise RuntimeError("ApprovalDocument validation case bulunamadı")
v = case_pattern.sub(case_replacement, v, count=1)

workflow_old = '''        RequireDate(data.Dates.CostCalculationDate, issues, "Yaklaşık Maliyet Hesap Cetveli tarihi");
        RequireDate(data.Dates.ApprovalDocumentDate, issues, "Onay Belgesi tarihi");
        RequireDate(data.Dates.MarketReportDate, issues, "Piyasa Fiyat Araştırması Tutanağı tarihi");'''
workflow_new = '''        RequireDate(data.Dates.CostCalculationDate, issues, "Yaklaşık Maliyet Hesap Cetveli tarihi");
        RequireDate(data.Dates.ApprovalDocumentDate, issues, "Onay Belgesi tarihi");
        RequireText(data.ApprovalDocument.AvailableBudgetAmount, issues, "Kullanılabilir ödenek tutarı");
        RequireText(data.ApprovalDocument.BudgetCode, issues, "Bütçe tertibi");
        RequireText(data.ApprovalDocument.TenderProcedure, issues, "İhale usulü");
        RequireText(data.ApprovalDocument.AnnouncementMethod, issues, "İlanın şekli ve adedi");
        RequireText(data.ApprovalDocument.PriceDifferenceBasis, issues, "Fiyat farkı dayanağı");
        RequireText(data.ApprovalDocument.SubmittedByName, issues, "Onaya sunan ad soyad");
        RequireText(data.ApprovalDocument.SubmittedByTitle, issues, "Onaya sunan unvanı");
        RequireDate(data.Dates.MarketReportDate, issues, "Piyasa Fiyat Araştırması Tutanağı tarihi");'''
if workflow_old in v:
    v = replace_once(v, workflow_old, workflow_new, "workflow approval requirements")
elif 'RequireDate(data.Dates.ApprovalDocumentDate, issues, "Onay Belgesi tarihi");\n        RequireText(data.ApprovalDocument.AvailableBudgetAmount' not in v:
    raise RuntimeError("Workflow Onay Belgesi zorunluluk bloğu bulunamadı")

readiness_old = '''            data.Dates.ApprovalDocumentDate.HasValue,
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.AvailableBudgetAmount)'''
readiness_new = '''            data.Dates.ApprovalDocumentDate.HasValue,
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.AvailableBudgetAmount),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.BudgetCode),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.TenderProcedure),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.AnnouncementMethod),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.PriceDifferenceBasis),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.SubmittedByName),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.SubmittedByTitle)'''
if readiness_old in v:
    v = replace_once(v, readiness_old, readiness_new, "readiness approval requirements")
elif '!string.IsNullOrWhiteSpace(data.ApprovalDocument.SubmittedByTitle)' not in v:
    raise RuntimeError("Readiness Onay Belgesi bloğu bulunamadı")

write(vp, v)

# 2) MainWindow.xaml -----------------------------------------------------------
xp = "DogruTemin.Wpf/MainWindow.xaml"
x = read(xp)

subtitle_old = "Yalnız Onay Belgesi'ne özgü bilgileri girin. Kurum, mal/hizmet, yaklaşık maliyet, komisyonlar ve harcama yetkilisi mevcut işlemden otomatik alınır."
subtitle_new = "Referansta karşılığı dolu bilgiler zorunludur (*). Opsiyonel alanlar boş bırakılabilir; Açıklamalar boşsa İşin Tanımı otomatik kullanılır."
if subtitle_old in x:
    x = x.replace(subtitle_old, subtitle_new, 1)

repls = {
    'Text="Onay Belgesi Tarihi" Style="{StaticResource PremiumFieldLabel}"': 'Text="Onay Belgesi Tarihi *" Style="{StaticResource PremiumFieldLabel}"',
    'Text="Belge Sayısı" Style="{StaticResource PremiumFieldLabel}"': 'Text="Belge Sayısı (Opsiyonel)" Style="{StaticResource PremiumFieldLabel}"',
    'Text="Kullanılabilir Ödenek Tutarı" Style="{StaticResource PremiumFieldLabel}"': 'Text="Kullanılabilir Ödenek Tutarı *" Style="{StaticResource PremiumFieldLabel}"',
    'Text="Yatırım Proje Numarası (varsa)" Style="{StaticResource PremiumFieldLabel}"': 'Text="Yatırım Proje Numarası (Opsiyonel)" Style="{StaticResource PremiumFieldLabel}"',
    'Text="Bütçe Tertibi (varsa)" Style="{StaticResource PremiumFieldLabel}"': 'Text="Bütçe Tertibi *" Style="{StaticResource PremiumFieldLabel}"',
    'Text="Avans Verilecekse Şartları" Style="{StaticResource PremiumFieldLabel}"': 'Text="Avans Verilecekse Şartları (Opsiyonel)" Style="{StaticResource PremiumFieldLabel}"',
    'Text="İhale Usulü" Style="{StaticResource PremiumFieldLabel}"': 'Text="İhale Usulü *" Style="{StaticResource PremiumFieldLabel}"',
    'Text="İlanın Şekli ve Adedi" Style="{StaticResource PremiumFieldLabel}"': 'Text="İlanın Şekli ve Adedi *" Style="{StaticResource PremiumFieldLabel}"',
    'Text="Ön Yeterlik / İhale Dokümanı Satış Bedeli" Style="{StaticResource PremiumFieldLabel}"': 'Text="Ön Yeterlik / İhale Dokümanı Satış Bedeli (Opsiyonel)" Style="{StaticResource PremiumFieldLabel}"',
    'Text="Fiyat Farkı Ödenecekse Dayanağı" Style="{StaticResource PremiumFieldLabel}"': 'Text="Fiyat Farkı Ödenecekse Dayanağı *" Style="{StaticResource PremiumFieldLabel}"',
}
for old, new in repls.items():
    if old in x:
        x = x.replace(old, new, 1)

notes_old = '<TextBlock Text="Açıklamalar" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalNotesBox"'
notes_new = '<TextBlock Text="Açıklamalar (Opsiyonel)" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalNotesBox"'
if notes_old in x:
    x = x.replace(notes_old, notes_new, 1)

submitted_name_old = '<TextBlock Text="Ad Soyad" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalSubmittedByNameBox"'
submitted_name_new = '<TextBlock Text="Ad Soyad *" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalSubmittedByNameBox"'
if submitted_name_old in x:
    x = x.replace(submitted_name_old, submitted_name_new, 1)

submitted_title_old = '<TextBlock Text="Unvanı" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalSubmittedByTitleBox"'
submitted_title_new = '<TextBlock Text="Unvanı *" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalSubmittedByTitleBox"'
if submitted_title_old in x:
    x = x.replace(submitted_title_old, submitted_title_new, 1)

write(xp, x)

# 3) ValidationTests -----------------------------------------------------------
tp = "DogruTemin.Tests/ValidationTests.cs"
t = read(tp)
if "ApprovalDocumentAllowsReferenceBlankOptionalFields" not in t:
    marker = '''    [Fact]\n    public void DuplicateFirmNameIsDetectedIgnoringPunctuationAndCase()'''
    if marker not in t:
        raise RuntimeError("ValidationTests ekleme noktası bulunamadı")
    block = r'''    [Fact]
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
                SubmittedByName = "Halil ÖZENÇ",
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

'''
    t = t.replace(marker, block + marker, 1)
write(tp, t)

print("Onay Belgesi zorunlu/opsiyonel alan kuralı kaynaklara uygulandı.")

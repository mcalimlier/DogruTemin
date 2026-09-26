using System.Text.RegularExpressions;

using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

public static class ValidationService
{
    public static IReadOnlyList<ValidationIssue> ValidateDraft(ProcurementData data)
    {
        var issues = new List<ValidationIssue>();
        if (data.Products.Count > 8) Critical(issues, "Products", "En fazla 8 ürün / mal / hizmet eklenebilir.");
        if (data.Firms.Count > 5) Critical(issues, "Firms", "En fazla 5 firma eklenebilir.");

        foreach (var product in data.Products.Take(8))
        {
            if (!string.IsNullOrWhiteSpace(product.Unit) && product.Unit is not ("Adet" or "Miktar"))
                Critical(issues, $"Product:{product.Id}:Unit", $"{product.No}. ürünün birimi Adet veya Miktar olmalıdır.");
            if (!string.IsNullOrWhiteSpace(product.Quantity) && !NumberParser.TryPositiveDecimal(product.Quantity, out _))
                Critical(issues, $"Product:{product.Id}:Quantity", $"{product.No}. ürün miktarı 0'dan büyük geçerli bir sayı olmalıdır.");
        }

        foreach (var firm in data.Firms.Take(5))
        {
            if (!string.IsNullOrWhiteSpace(firm.TaxNo) && !InputValidationService.ValidTaxNo(firm.TaxNo))
                Critical(issues, $"Firm:{firm.Id}:TaxNo", $"{firm.No}. firmanın vergi numarası 10 veya 11 rakam olmalıdır.");
            if (!string.IsNullOrWhiteSpace(firm.Phone) && !InputValidationService.ValidPhone(firm.Phone))
                Critical(issues, $"Firm:{firm.Id}:Phone", $"{firm.No}. firmanın telefonu geçersiz.");
            if (!string.IsNullOrWhiteSpace(firm.Email) && !InputValidationService.ValidEmail(firm.Email))
                Critical(issues, $"Firm:{firm.Id}:Email", $"{firm.No}. firmanın e-posta adresi geçersiz.");
            foreach (var offer in firm.Offers)
            {
                if (offer.ApproximateUnitPrice is <= 0) Critical(issues, $"Offer:{firm.Id}:{offer.ProductId}:Approx", $"{firm.No}. firma için girilen yaklaşık fiyat geçersiz.");
                if (offer.FinalUnitPrice is <= 0) Critical(issues, $"Offer:{firm.Id}:{offer.ProductId}:Final", $"{firm.No}. firma için girilen nihai fiyat geçersiz.");
            }
        }

        var activeFirms = data.Firms.Where(f => !string.IsNullOrWhiteSpace(f.Name)).Take(5).ToList();
        for (var i = 0; i < activeFirms.Count; i++)
        for (var j = i + 1; j < activeFirms.Count; j++)
        {
            var sameName = TextNormalizer.NormalizeFirmName(activeFirms[i].Name) == TextNormalizer.NormalizeFirmName(activeFirms[j].Name);
            var aTax = TextNormalizer.DigitsOnly(activeFirms[i].TaxNo);
            var bTax = TextNormalizer.DigitsOnly(activeFirms[j].TaxNo);
            var sameTax = aTax.Length > 0 && aTax == bTax;
            if (sameName || sameTax) Critical(issues, "Firms", $"{activeFirms[i].No}. ve {activeFirms[j].No}. firma mükerrer görünüyor.");
        }

        if (!string.IsNullOrWhiteSpace(data.Institution.Phone) && !InputValidationService.ValidPhone(data.Institution.Phone))
            Critical(issues, "Institution.Phone", "Kurum telefonu 10-15 rakam içermelidir.");
        if (!string.IsNullOrWhiteSpace(data.Institution.Email) && !InputValidationService.ValidEmail(data.Institution.Email))
            Critical(issues, "Institution.Email", "Geçerli bir kurum e-posta adresi giriniz.");

        return issues;
    }


    public static IReadOnlyList<ValidationIssue> ValidateWorkflow(ProcurementData data)
    {
        var issues = new List<ValidationIssue>();

        foreach (var issue in ValidateDraft(data))
            AddUnique(issues, issue);

        RequireText(data.JobTitle, issues, "Alım / iş başlığı");
        if (string.IsNullOrWhiteSpace(data.OperationNumber))
            Warning(issues, "OperationNumber", "İşlem numarası boş. Arşivde işlemi bulmayı kolaylaştırmak için işlem numarası girmeniz önerilir.");

        var products = data.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Take(8).ToList();
        var firms = data.Firms.Where(f => !string.IsNullOrWhiteSpace(f.Name)).Take(5).ToList();

        if (products.Count == 0)
            Critical(issues, "Products", "En az bir ürün / mal / hizmet eklenmelidir.");

        for (var i = 0; i < products.Count; i++)
        {
            var p = products[i];
            if (p.Unit is not ("Adet" or "Miktar"))
                Critical(issues, $"Product:{p.Id}:Unit", $"{p.No}. ürünün birimi Adet veya Miktar olmalıdır.");
            if (!NumberParser.TryPositiveDecimal(p.Quantity, out _))
                Critical(issues, $"Product:{p.Id}:Quantity", $"{p.No}. ürün miktarı 0'dan büyük geçerli bir sayı olmalıdır.");
        }

        for (var i = 0; i < products.Count; i++)
        for (var j = i + 1; j < products.Count; j++)
        {
            var a = NormalizeProduct(products[i]);
            var b = NormalizeProduct(products[j]);
            if (a.Length > 0 && a == b)
                Warning(issues, "Products.Duplicate", $"{products[i].No}. ve {products[j].No}. ürün aynı veya çok benzer görünüyor. Mükerrer ürün olup olmadığını kontrol edin.");
        }

        if (firms.Count < 3)
            Critical(issues, "Firms", $"En az 3 firma gereklidir; şu an {firms.Count} firma var.");

        RequireCommission(data.MarketCommission, issues, "Piyasa Fiyat Araştırması Komisyonu");
        RequireCommission(data.InspectionCommission, issues, "Muayene ve Kabul Komisyonu");

        RequireText(data.Institution.TopAuthority, issues, "Kurum üst makamı");
        RequireText(data.Institution.FullName, issues, "İdarenin tam adı");
        RequireText(data.Institution.Province, issues, "Kurum ili");
        RequireText(data.Institution.Address, issues, "Kurum adresi");
        RequireText(data.Institution.AuthorizedName, issues, "Harcama yetkilisi adı soyadı");
        RequireText(data.Institution.AuthorizedDuty, issues, "Harcama yetkilisi görevi");
        if (!InputValidationService.ValidPhone(data.Institution.Phone)) Critical(issues, "Institution.Phone", "Kurum telefonu 10-15 rakam içermelidir.");
        if (!InputValidationService.ValidEmail(data.Institution.Email)) Critical(issues, "Institution.Email", "Geçerli bir kurum e-posta adresi giriniz.");

        RequireDate(data.Dates.AssignmentDate, issues, "Komisyon görevlendirme tarihi");
        RequireText(data.Dates.AssignmentNo, issues, "Komisyon sayısı");
        RequireDate(data.Dates.ApprovalDate, issues, "Harcama belgesi tarihi");
        RequireText(data.Dates.ApprovalNo, issues, "Harcama belgesi sayısı");
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
        RequireDate(data.Dates.InspectionReportDate, issues, "Muayene ve Kabul Tutanağı tarihi");
        RequireDate(data.Dates.DeliveryDate, issues, "Teslim Alma / Kabul tarihi");
        issues.AddRange(DateValidationService.RequiredSequenceIssues(data.Dates, ValidationSeverity.Critical));

        foreach (var firm in firms)
        {
            foreach (var product in products)
            {
                var offer = OfferFor(firm, product.Id);
                if (!(offer?.ApproximateUnitPrice > 0))
                    Critical(issues, $"Offer:{firm.Id}:{product.Id}:Approx", $"{firm.No}. firma / {product.No}. ürün için yaklaşık maliyet teklifi eksik.");
                if (!(offer?.FinalUnitPrice > 0))
                    Critical(issues, $"Offer:{firm.Id}:{product.Id}:Final", $"{firm.No}. firma / {product.No}. ürün için nihai teklif eksik.");
            }
        }

        if (products.Count > 0 && firms.Count >= 3)
        {
            var calc = ProcurementEngine.Calculate(data);
            if (calc.HasWinnerTie && string.IsNullOrWhiteSpace(calc.WinningFirmId))
                Warning(issues, "Offers.WinnerTie", "Eşit en düşük toplam teklif var. Kazanan firma seçimi yapılmalıdır.");
            var winner = firms.FirstOrDefault(f => f.Id == calc.WinningFirmId);
            if (winner is not null && string.IsNullOrWhiteSpace(winner.Address))
                Critical(issues, $"Firm:{winner.Id}:Address", "Kazanan firmanın adresi eksik.");
        }

        return issues
            .GroupBy(i => new { i.Severity, i.Field, i.Message })
            .Select(g => g.First())
            .ToList();
    }

    private static void AddUnique(List<ValidationIssue> issues, ValidationIssue issue)
    {
        if (!issues.Any(i => i.Severity == issue.Severity && i.Field == issue.Field && i.Message == issue.Message))
            issues.Add(issue);
    }

    private static string NormalizeProduct(ProductItem product)
    {
        var text = TextNormalizer.SingleLine(product.Name ?? string.Empty).ToUpperInvariant();
        return Regex.Replace(text, @"[^\p{L}\p{Nd}]", string.Empty);
    }

    public static IReadOnlyList<ValidationIssue> DateLogicWarnings(ProcurementData data)
    {
        // Zorunlu kronoloji:
        // Kurul/Komisyon Onay <= Yaklaşık Maliyet < Onay Belgesi < Piyasa Fiyat Araştırması <= Muayene/Kabul.
        // Harcama belgesi ve teslim/kabul tarihi bu zincirin dışında ayrıca yönetilir.
        return DateValidationService.RequiredSequenceIssues(data.Dates, ValidationSeverity.Warning);
    }

    public static IReadOnlyList<ValidationIssue> ValidateDocument(ProcurementData data, OutputDocumentType document) =>
        DocumentValidationService.Validate(data, document);

    public static int ReadinessPercent(ProcurementData data)
    {
        var checks = new List<bool>
        {
            !string.IsNullOrWhiteSpace(data.JobTitle),
            data.Products.Any(p => !string.IsNullOrWhiteSpace(p.Name)),
            data.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).All(p => p.Unit is "Adet" or "Miktar" && NumberParser.TryPositiveDecimal(p.Quantity, out _)),
            data.Firms.Count(f => !string.IsNullOrWhiteSpace(f.Name)) >= 3,
            data.InspectionCommission.Count(m => !string.IsNullOrWhiteSpace(m.Name) && !string.IsNullOrWhiteSpace(m.Title)) == 3,
            data.MarketCommission.Count(m => !string.IsNullOrWhiteSpace(m.Name) && !string.IsNullOrWhiteSpace(m.Title)) == 3,
            !string.IsNullOrWhiteSpace(data.Institution.TopAuthority),
            !string.IsNullOrWhiteSpace(data.Institution.FullName),
            !string.IsNullOrWhiteSpace(data.Institution.Province),
            data.Dates.ApprovalDate.HasValue,
            !string.IsNullOrWhiteSpace(data.Dates.ApprovalNo),
            data.Dates.ApprovalDocumentDate.HasValue,
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.AvailableBudgetAmount),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.BudgetCode),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.TenderProcedure),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.AnnouncementMethod),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.PriceDifferenceBasis),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.SubmittedByName),
            !string.IsNullOrWhiteSpace(data.ApprovalDocument.SubmittedByTitle)
        };
        return (int)Math.Round(checks.Count(c => c) * 100.0 / checks.Count);
    }


    private static FirmOffer? OfferFor(Firm firm, string productId) =>
        firm.Offers.FirstOrDefault(o => o.ProductId == productId);

    private static void RequireCommission(List<CommissionMember> members, List<ValidationIssue> issues, string label)
    {
        for (var i = 0; i < 3; i++)
        {
            var m = i < members.Count ? members[i] : null;
            if (m is null || string.IsNullOrWhiteSpace(m.Name))
                Critical(issues, "Commission", $"{label} {i + 1}. ad soyad");
            if (m is null || string.IsNullOrWhiteSpace(m.Title))
                Critical(issues, "Commission", $"{label} {i + 1}. unvan");
        }
    }

    private static void RequireText(string? value, List<ValidationIssue> issues, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            Critical(issues, message, message);
    }

    private static void RequireDate(DateTime? value, List<ValidationIssue> issues, string message)
    {
        if (!value.HasValue)
            Critical(issues, message, message);
    }

    private static void Critical(List<ValidationIssue> issues, string field, string message) =>
        issues.Add(new ValidationIssue
        {
            Severity = ValidationSeverity.Critical,
            Field = field,
            Message = message
        });

    private static void Warning(List<ValidationIssue> issues, string field, string message) =>
        issues.Add(new ValidationIssue
        {
            Severity = ValidationSeverity.Warning,
            Field = field,
            Message = message
        });

    // Backward-compatible facade for existing callers/tests.
    public static bool ValidTaxNo(string? text) => InputValidationService.ValidTaxNo(text);
    public static bool ValidPhone(string? text) => InputValidationService.ValidPhone(text);
    public static bool ValidEmail(string? text) => InputValidationService.ValidEmail(text);
}

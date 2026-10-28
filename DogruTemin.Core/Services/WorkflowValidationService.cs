using System.Text.RegularExpressions;

using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

public static class WorkflowValidationService
{
    public static IReadOnlyList<ValidationIssue> Validate(ProcurementData data)
    {
        var issues = new List<ValidationIssue>();

        foreach (var issue in ValidationService.ValidateDraft(data))
            AddUnique(issues, issue);

        RequireText(data.JobTitle, issues, "Alım / iş başlığı");
        if (string.IsNullOrWhiteSpace(data.OperationNumber))
            Warning(issues, "OperationNumber", "İşlem numarası boş. Arşivde işlemi bulmayı kolaylaştırmak için işlem numarası girmeniz önerilir.");

        var products = data.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Take(ProcurementLimits.MaxProducts).ToList();
        var firms = data.Firms.Where(f => !string.IsNullOrWhiteSpace(f.Name)).Take(5).ToList();

        if (products.Count == 0)
            Critical(issues, "Products", "En az bir ürün / mal / hizmet eklenmelidir.");

        foreach (var product in products)
        {
            if (!ProductUnits.IsSupported(product.Unit))
                Critical(issues, $"Product:{product.Id}:Unit", $"{product.No}. ürünün birimi geçerli bir ölçü birimi olmalıdır.");
            if (!NumberParser.TryPositiveDecimal(product.Quantity, out _))
                Critical(issues, $"Product:{product.Id}:Quantity", $"{product.No}. ürün miktarı 0'dan büyük geçerli bir sayı olmalıdır.");
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
        if (!InputValidationService.ValidPhone(data.Institution.Phone))
            Critical(issues, "Institution.Phone", "Kurum telefonu 10-15 rakam içermelidir.");
        if (!InputValidationService.ValidEmail(data.Institution.Email))
            Critical(issues, "Institution.Email", "Geçerli bir kurum e-posta adresi giriniz.");

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
        foreach (var product in products)
        {
            var offer = OfferFor(firm, product.Id);
            if (!(offer?.ApproximateUnitPrice > 0))
                Critical(issues, $"Offer:{firm.Id}:{product.Id}:Approx", $"{firm.No}. firma / {product.No}. ürün için yaklaşık maliyet teklifi eksik.");
            if (!(offer?.FinalUnitPrice > 0))
                Critical(issues, $"Offer:{firm.Id}:{product.Id}:Final", $"{firm.No}. firma / {product.No}. ürün için nihai teklif eksik.");
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

    private static FirmOffer? OfferFor(Firm firm, string productId) =>
        firm.Offers.FirstOrDefault(o => o.ProductId == productId);

    private static void RequireCommission(List<CommissionMember> members, List<ValidationIssue> issues, string label)
    {
        for (var i = 0; i < 3; i++)
        {
            var member = i < members.Count ? members[i] : null;
            if (member is null || string.IsNullOrWhiteSpace(member.Name))
                Critical(issues, "Commission", $"{label} {i + 1}. ad soyad");
            if (member is null || string.IsNullOrWhiteSpace(member.Title))
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
}

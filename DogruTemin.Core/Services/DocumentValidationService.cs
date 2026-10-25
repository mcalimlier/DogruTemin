using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

public static class DocumentValidationService
{
    public static IReadOnlyList<ValidationIssue> Validate(ProcurementData data, OutputDocumentType document)
    {
        var issues = new List<ValidationIssue>();
        CommonRequirements(data, issues);

        issues.AddRange(DateValidationService.RequiredSequenceIssues(data.Dates, ValidationSeverity.Critical));

        var products = data.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Take(ProcurementLimits.MaxProducts).ToList();
        var firms = data.Firms.Where(f => !string.IsNullOrWhiteSpace(f.Name)).Take(5).ToList();

        switch (document)
        {
            case OutputDocumentType.ApproximateRequest:
                RequireDate(data.Dates.AssignmentDate, issues, "Komisyon görevlendirme tarihi");
                RequireText(data.Dates.AssignmentNo, issues, "Komisyon sayısı");
                RequireText(data.Institution.Address, issues, "Kurum adresi");
                RequirePhone(data.Institution.Phone, issues, "Kurum telefonu");
                RequireEmail(data.Institution.Email, issues, "Kurum e-posta adresi");
                RequireCommission(data.MarketCommission, issues, "Piyasa komisyonu");
                break;

            case OutputDocumentType.ApproximateCostSheet:
                RequireDate(data.Dates.CostCalculationDate, issues, "Hesap cetveli tarihi");
                RequireAtLeastThreeFirms(firms, issues);
                foreach (var firm in firms)
                    foreach (var product in products)
                        if (!(OfferFor(firm, product.Id)?.ApproximateUnitPrice > 0))
                            Critical(issues, "Offers", $"{firm.No}. firma / {product.No}. ürün yaklaşık fiyatı");
                break;

            case OutputDocumentType.FirmOfferForm:
                RequireDate(data.Dates.ApprovalDate, issues, "Harcama belgesi tarihi");
                RequireText(data.Dates.ApprovalNo, issues, "Harcama belgesi sayısı");
                RequireText(data.Institution.Address, issues, "Kurum adresi");
                RequirePhone(data.Institution.Phone, issues, "Kurum telefonu");
                RequireEmail(data.Institution.Email, issues, "Kurum e-posta adresi");
                RequireCommission(data.MarketCommission, issues, "Teklif formu komisyonu");
                break;

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
                        if (!(OfferFor(firm, product.Id)?.ApproximateUnitPrice > 0))
                            Critical(issues, "Offers", $"{firm.No}. firma / {product.No}. ürün yaklaşık fiyatı");
                break;

            case OutputDocumentType.MarketResearchReport:
                RequireDate(data.Dates.ApprovalDate, issues, "Harcama belgesi tarihi");
                RequireText(data.Dates.ApprovalNo, issues, "Harcama belgesi sayısı");
                RequireDate(data.Dates.MarketReportDate, issues, "Piyasa tutanak tarihi");
                RequireText(data.Institution.AuthorizedName, issues, "Harcama yetkilisi adı soyadı");
                RequireText(data.Institution.AuthorizedDuty, issues, "Harcama yetkilisi görevi");
                RequireCommission(data.MarketCommission, issues, "Piyasa komisyonu");
                RequireMarketData(data, products, firms, issues);
                break;

            case OutputDocumentType.InspectionAcceptanceReport:
                RequireDate(data.Dates.InspectionReportDate, issues, "Muayene tutanak tarihi");
                RequireDate(data.Dates.DeliveryDate, issues, "Teslim alma tarihi");
                RequireDate(data.Dates.AssignmentDate, issues, "Komisyon görevlendirme tarihi");
                RequireText(data.Dates.AssignmentNo, issues, "Komisyon sayısı");
                RequireDate(data.Dates.ApprovalDate, issues, "Harcama belgesi tarihi");
                RequireText(data.Dates.ApprovalNo, issues, "Harcama belgesi sayısı");
                RequireCommission(data.InspectionCommission, issues, "Muayene komisyonu");
                RequireMarketData(data, products, firms, issues);
                break;
        }

        return issues;
    }

    private static void CommonRequirements(ProcurementData data, List<ValidationIssue> issues)
    {
        RequireText(data.JobTitle, issues, "Alım / iş başlığı");
        RequireText(data.Institution.TopAuthority, issues, "Kurum üst makamı");
        RequireText(data.Institution.FullName, issues, "İdarenin tam adı");
        RequireText(data.Institution.Province, issues, "Kurum ili");

        var products = data.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Take(ProcurementLimits.MaxProducts).ToList();
        if (products.Count == 0)
            Critical(issues, "Products", "En az bir ürün / hizmet");

        foreach (var product in products)
        {
            if (product.Unit is not ("Adet" or "Miktar"))
                Critical(issues, $"Product:{product.Id}:Unit", $"{product.No}. ürün birimi");
            if (!NumberParser.TryPositiveDecimal(product.Quantity, out _))
                Critical(issues, $"Product:{product.Id}:Quantity", $"{product.No}. ürün miktarı");
        }
    }

    private static void RequireMarketData(ProcurementData data, List<ProductItem> products, List<Firm> firms, List<ValidationIssue> issues)
    {
        RequireAtLeastThreeFirms(firms, issues);

        var complete = firms.Any(f => products.Count > 0 &&
            products.All(p => OfferFor(f, p.Id)?.FinalUnitPrice > 0));

        if (!complete)
        {
            Critical(issues, "Offers", "Tüm aktif ürünlere nihai teklif veren en az bir firma");
            return;
        }

        var calc = ProcurementEngine.Calculate(data);
        var winner = firms.FirstOrDefault(f => f.Id == calc.WinningFirmId);
        if (winner is not null && string.IsNullOrWhiteSpace(winner.Address))
            Critical(issues, $"Firm:{winner.Id}:Address", "Kazanan firmanın adresi");
    }

    private static FirmOffer? OfferFor(Firm firm, string productId) =>
        firm.Offers.FirstOrDefault(o => o.ProductId == productId);

    private static void RequireAtLeastThreeFirms(List<Firm> firms, List<ValidationIssue> issues)
    {
        if (firms.Count < 3)
            Critical(issues, "Firms", "En az 3 firma");
    }

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

    private static void RequirePhone(string? value, List<ValidationIssue> issues, string message)
    {
        if (!InputValidationService.ValidPhone(value))
            Critical(issues, "Institution.Phone", message);
    }

    private static void RequireEmail(string? value, List<ValidationIssue> issues, string message)
    {
        if (!InputValidationService.ValidEmail(value))
            Critical(issues, "Institution.Email", message);
    }

    private static void Critical(List<ValidationIssue> issues, string field, string message) =>
        issues.Add(new ValidationIssue
        {
            Severity = ValidationSeverity.Critical,
            Field = field,
            Message = message
        });
}

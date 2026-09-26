using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

/// <summary>
/// Belge üretimi için temel zorunlulukları UI'dan bağımsız hesaplar.
/// UI yalnızca bu servisin sonucunu gösterir.
/// </summary>
public static class DocumentReadinessService
{
    public static bool IsPreparationDocument(OutputDocumentType type) =>
        type is OutputDocumentType.ApproximateRequest or OutputDocumentType.FirmOfferForm;

    public static bool IsFinalProcessDocument(OutputDocumentType type) =>
        type is OutputDocumentType.ApproximateCostSheet
            or OutputDocumentType.ApprovalDocument
            or OutputDocumentType.MarketResearchReport
            or OutputDocumentType.InspectionAcceptanceReport;

    public static List<string> GetExportCoreIssues(
        ProcurementData data,
        IReadOnlyCollection<OutputDocumentType> types)
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
        var products = data.Products
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Take(8)
            .ToList();
        var firms = data.Firms
            .Where(f => !string.IsNullOrWhiteSpace(f.Name))
            .Take(5)
            .ToList();

        AddProductCoreIssues(data, issues);

        if (firms.Count < 3)
            issues.Add($"En az 3 firma bulunmalıdır. Şu an {firms.Count} firma var.");

        if (products.Count > 0 && firms.Count >= 3)
        {
            foreach (var firm in firms)
            foreach (var product in products)
            {
                var offer = firm.Offers.FirstOrDefault(o => o.ProductId == product.Id);
                if (offer?.ApproximateUnitPrice <= 0)
                    issues.Add($"{firm.No}. firma / {product.No}. ürün yaklaşık teklifi eksik.");
                if (offer?.FinalUnitPrice <= 0)
                    issues.Add($"{firm.No}. firma / {product.No}. ürün nihai teklifi eksik.");
            }
        }

        AddCommissionCoreIssues(data.MarketCommission, "Piyasa Fiyat Araştırması Komisyonu", issues);
        AddCommissionCoreIssues(data.InspectionCommission, "Muayene ve Kabul Komisyonu", issues);
    }

    private static void AddProductCoreIssues(ProcurementData data, List<string> issues)
    {
        var products = data.Products
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Take(8)
            .ToList();

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

    private static void AddCommissionCoreIssues(
        IEnumerable<CommissionMember> members,
        string label,
        List<string> issues)
    {
        var list = members.Take(3).ToList();

        for (var i = 0; i < 3; i++)
        {
            var member = i < list.Count ? list[i] : null;
            if (member is null
                || string.IsNullOrWhiteSpace(member.Name)
                || string.IsNullOrWhiteSpace(member.Title))
            {
                issues.Add($"{label} {i + 1}. üye için ad soyad ve unvan bilgisi tamamlanmalıdır.");
            }
        }
    }
}

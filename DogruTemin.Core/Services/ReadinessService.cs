using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

public static class ReadinessService
{
    public static int CalculatePercent(ProcurementData data)
    {
        var checks = new List<bool>
        {
            !string.IsNullOrWhiteSpace(data.JobTitle),
            data.Products.Any(p => !string.IsNullOrWhiteSpace(p.Name)),
            data.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .All(p => p.Unit is "Adet" or "Miktar" && NumberParser.TryPositiveDecimal(p.Quantity, out _)),
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
}

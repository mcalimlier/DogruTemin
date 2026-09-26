using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

public static class DateValidationService
{
    public static IReadOnlyList<ValidationIssue> RequiredSequenceIssues(ProcessDates dates, ValidationSeverity severity)
    {
        var issues = new List<ValidationIssue>();

        if (dates.AssignmentDate.HasValue && dates.CostCalculationDate.HasValue &&
            dates.AssignmentDate.Value.Date > dates.CostCalculationDate.Value.Date)
            AddIssue(issues, severity, "Kurul / Komisyon Onay Tarihi, Yaklaşık Maliyet Hesap Cetveli tarihinden sonra olamaz.");

        if (dates.CostCalculationDate.HasValue && dates.ApprovalDocumentDate.HasValue &&
            dates.CostCalculationDate.Value.Date >= dates.ApprovalDocumentDate.Value.Date)
            AddIssue(issues, severity, "Yaklaşık Maliyet Hesap Cetveli Tarihi, Onay Belgesi tarihinden MUTLAKA önce olmalıdır.");

        if (dates.ApprovalDocumentDate.HasValue && dates.MarketReportDate.HasValue &&
            dates.ApprovalDocumentDate.Value.Date >= dates.MarketReportDate.Value.Date)
            AddIssue(issues, severity, "Onay Belgesi tarihi, Piyasa Fiyat Araştırması Tutanağı tarihinden MUTLAKA önce olmalıdır.");

        if (dates.MarketReportDate.HasValue && dates.InspectionReportDate.HasValue &&
            dates.MarketReportDate.Value.Date > dates.InspectionReportDate.Value.Date)
            AddIssue(issues, severity, "Piyasa Fiyat Araştırması Tutanağı tarihi, Muayene ve Kabul Tutanağı tarihinden sonra olamaz.");

        return issues;
    }

    private static void AddIssue(List<ValidationIssue> issues, ValidationSeverity severity, string message)
    {
        issues.Add(new ValidationIssue
        {
            Severity = severity,
            Field = "Dates.RequiredSequence",
            Message = message
        });
    }
}

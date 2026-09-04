using DogruTemin.Core.Models;
using DogruTemin.Core.Services;

namespace DogruTemin.Tests;

public class ValidationTests
{
    [Fact]
    public void RequiredDateChainAllowsEqualityOnlyAtOuterLinks()
    {
        var data = new ProcurementData
        {
            Dates = new ProcessDates
            {
                AssignmentDate = new DateTime(2026, 9, 10),
                CostCalculationDate = new DateTime(2026, 9, 10),
                ApprovalDocumentDate = new DateTime(2026, 9, 11),
                MarketReportDate = new DateTime(2026, 9, 12),
                InspectionReportDate = new DateTime(2026, 9, 12)
            }
        };

        Assert.Empty(ValidationService.DateLogicWarnings(data));
    }

    [Fact]
    public void CostDateMustBeBeforeApprovalDocumentDate()
    {
        var data = new ProcurementData { Dates = new ProcessDates
        {
            CostCalculationDate = new DateTime(2026, 9, 10),
            ApprovalDocumentDate = new DateTime(2026, 9, 10)
        }};
        Assert.Contains(ValidationService.DateLogicWarnings(data), i => i.Message.Contains("Onay Belgesi", StringComparison.Ordinal));
    }

    [Fact]
    public void ApprovalDocumentDateMustBeBeforeMarketDate()
    {
        var data = new ProcurementData { Dates = new ProcessDates
        {
            ApprovalDocumentDate = new DateTime(2026, 9, 12),
            MarketReportDate = new DateTime(2026, 9, 12)
        }};
        Assert.Contains(ValidationService.DateLogicWarnings(data), i => i.Message.Contains("Piyasa Fiyat", StringComparison.Ordinal));
    }

    [Fact]
    public void CommissionApprovalCannotBeAfterCostDate()
    {
        var data = new ProcurementData { Dates = new ProcessDates
        {
            AssignmentDate = new DateTime(2026, 9, 11),
            CostCalculationDate = new DateTime(2026, 9, 10)
        }};
        Assert.NotEmpty(ValidationService.DateLogicWarnings(data));
    }

    [Fact]
    public void MarketAndInspectionDatesMayBeEqualButNotReverse()
    {
        var equal = new ProcurementData { Dates = new ProcessDates
        {
            MarketReportDate = new DateTime(2026, 9, 12),
            InspectionReportDate = new DateTime(2026, 9, 12)
        }};
        Assert.Empty(ValidationService.DateLogicWarnings(equal));

        equal.Dates.MarketReportDate = new DateTime(2026, 9, 13);
        Assert.NotEmpty(ValidationService.DateLogicWarnings(equal));
    }

    [Fact]
    public void DuplicateFirmNameIsDetectedIgnoringPunctuationAndCase()
    {
        var data = new ProcurementData
        {
            Firms = new()
            {
                new Firm { No = 1, Name = "Örnek Ltd. Şti." },
                new Firm { No = 2, Name = "örnek ltd şti" }
            }
        };

        var issues = ValidationService.ValidateDraft(data);

        Assert.Contains(issues, issue => issue.Message.Contains("mükerrer", StringComparison.OrdinalIgnoreCase));
    }
}

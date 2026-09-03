using DogruTemin.Core.Models;
using DogruTemin.Core.Services;

namespace DogruTemin.Tests;

public class ValidationTests
{
    [Fact]
    public void MarketDateMustBeAfterCostCalculationDate()
    {
        var data = new ProcurementData
        {
            Dates = new ProcessDates
            {
                CostCalculationDate = new DateTime(2026, 9, 10),
                MarketReportDate = new DateTime(2026, 9, 10)
            }
        };

        var issues = new ValidationService().DateLogicWarnings(data);

        Assert.Contains(issues, issue => issue.Message.Contains("MUTLAKA önce", StringComparison.Ordinal));
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

        var issues = new ValidationService().ValidateDraft(data);

        Assert.Contains(issues, issue => issue.Message.Contains("mükerrer", StringComparison.OrdinalIgnoreCase));
    }
}

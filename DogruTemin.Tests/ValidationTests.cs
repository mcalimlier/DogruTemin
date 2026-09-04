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

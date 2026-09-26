using DogruTemin.Core.Models;
using DogruTemin.Core.Services;

namespace DogruTemin.Tests;

public sealed class GoldenScenarioTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "DogruTemin_Golden_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void CompleteProcurementFlowValidatesCalculatesSavesAndReloads()
    {
        var data = BuildScenario();

        var draftIssues = ValidationService.ValidateDraft(data);
        var workflowIssues = ValidationService.ValidateWorkflow(data);
        var readiness = ValidationService.ReadinessPercent(data);
        var calculation = ProcurementEngine.Calculate(data);

        Assert.DoesNotContain(draftIssues, issue => issue.Severity == ValidationSeverity.Critical);
        Assert.DoesNotContain(workflowIssues, issue => issue.Severity == ValidationSeverity.Critical);
        Assert.Equal(100, readiness);
        Assert.False(calculation.HasWinnerTie);
        Assert.Equal("firm-b", calculation.WinningFirmId);
        Assert.Equal("Firma B", calculation.WinningFirmName);
        Assert.Equal(100m, calculation.MarketResultTotal);

        var storage = new StorageService(_root);

        // İlk kayıt mevcut aktif dosya olmadığı için yedek üretmez.
        // İkinci kayıt, gerçek uygulamadaki mevcut kaydın üzerine yazma akışını
        // temsil eder ve önceki aktif dosyanın yedeğini oluşturur.
        storage.Save(data, createBackup: true);
        storage.Save(data, createBackup: true);

        var loaded = storage.Load(out var loadError);

        Assert.Null(loadError);
        Assert.NotNull(loaded);
        Assert.Equal(data.RecordId, loaded.RecordId);
        Assert.Equal(data.OperationNumber, loaded.OperationNumber);
        Assert.Equal(2, loaded.Products.Count);
        Assert.Equal(3, loaded.Firms.Count);

        var reloadedCalculation = ProcurementEngine.Calculate(loaded);

        Assert.Equal(calculation.WinningFirmId, reloadedCalculation.WinningFirmId);
        Assert.Equal(calculation.MarketResultTotal, reloadedCalculation.MarketResultTotal);
        Assert.NotEmpty(Directory.EnumerateFiles(
            storage.BackupFolder,
            "DogruTemin_*.json",
            SearchOption.TopDirectoryOnly));
    }

    private static ProcurementData BuildScenario()
    {
        var dates = new ProcessDates
        {
            AssignmentDate = new DateTime(2026, 9, 1),
            AssignmentNo = "2026/101",
            ApprovalDate = new DateTime(2026, 9, 2),
            ApprovalNo = "2026/102",
            CostCalculationDate = new DateTime(2026, 9, 3),
            ApprovalDocumentDate = new DateTime(2026, 9, 4),
            MarketReportDate = new DateTime(2026, 9, 5),
            InspectionReportDate = new DateTime(2026, 9, 6),
            DeliveryDate = new DateTime(2026, 9, 7)
        };

        return new ProcurementData
        {
            SchemaVersion = 4,
            RecordId = "golden-scenario-1",
            OperationNumber = "DT-2026-001",
            JobTitle = "Kırtasiye ve sarf malzemesi alımı",
            Products = new()
            {
                new ProductItem
                {
                    Id = "product-1",
                    No = 1,
                    Name = "A4 Kağıt",
                    Unit = "Adet",
                    Quantity = "2"
                },
                new ProductItem
                {
                    Id = "product-2",
                    No = 2,
                    Name = "Toner",
                    Unit = "Miktar",
                    Quantity = "3"
                }
            },
            Firms = new()
            {
                Firm("firm-a", 1, "Firma A", "1000000000", "9000000001", 30m, 30m),
                Firm("firm-b", 2, "Firma B", "1000000001", "9000000002", 20m, 20m),
                Firm("firm-c", 3, "Firma C", "1000000002", "9000000003", 40m, 40m)
            },
            InspectionCommission = Commission("Muayene Üyesi"),
            MarketCommission = Commission("Piyasa Üyesi"),
            Dates = dates,
            Institution = new InstitutionInfo
            {
                TopAuthority = "İl Milli Eğitim Müdürlüğü",
                FullName = "Örnek Kamu Kurumu Müdürlüğü",
                Province = "İzmir",
                Address = "Örnek Mahallesi Örnek Caddesi No:1",
                Phone = "2320000000",
                Email = "kurum@example.gov.tr",
                AuthorizedName = "Ahmet Yetkili",
                AuthorizedDuty = "Harcama Yetkilisi"
            },
            ApprovalDocument = new ApprovalDocumentInfo
            {
                AvailableBudgetAmount = "1000",
                BudgetCode = "2026-01",
                TenderProcedure = "4734 S.K. 22/d Kapsamında",
                AnnouncementMethod = "İlansız",
                PriceDifferenceBasis = "Ödenmeyecektir",
                SubmittedByName = "Mehmet Sunan",
                SubmittedByTitle = "Şube Müdürü"
            }
        };
    }

    private static Firm Firm(
        string id,
        int no,
        string name,
        string taxNo,
        string phone,
        decimal firstPrice,
        decimal secondPrice) =>
        new()
        {
            Id = id,
            No = no,
            Name = name,
            TaxNo = taxNo,
            Phone = phone,
            Email = $"firma{no}@example.gov.tr",
            Address = $"Örnek Firma {no} Adresi",
            Offers = new()
            {
                new FirmOffer
                {
                    ProductId = "product-1",
                    ApproximateUnitPrice = firstPrice,
                    FinalUnitPrice = firstPrice
                },
                new FirmOffer
                {
                    ProductId = "product-2",
                    ApproximateUnitPrice = secondPrice,
                    FinalUnitPrice = secondPrice
                }
            }
        };

    private static List<CommissionMember> Commission(string prefix) =>
        new()
        {
            new CommissionMember { No = 1, Name = prefix + " 1", Title = "Başkan" },
            new CommissionMember { No = 2, Name = prefix + " 2", Title = "Üye" },
            new CommissionMember { No = 3, Name = prefix + " 3", Title = "Üye" }
        };

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
        catch
        {
        }

        GC.SuppressFinalize(this);
    }
}

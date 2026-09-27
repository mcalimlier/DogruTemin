using System.Threading;
using DogruTemin.Core.Models;
using DogruTemin.Core.Services;
using DogruTemin.Wpf.Services;

namespace DogruTemin.Tests;

public sealed class DocumentWorkflowTests
{
    [Fact]
    public void PrepareExportWithCoreReadinessIssuesReturnsBlockedWithoutDocument()
    {
        var data = new ProcurementData();
        var preferredWinnerBefore = data.PreferredWinnerFirmId;

        var result = RunSta(() => DocumentWorkflow.Prepare(
            data,
            new[] { OutputDocumentType.ApproximateRequest },
            DocumentPreparationMode.Export));

        Assert.Equal(DocumentPreparationStatus.Blocked, result.Status);
        Assert.NotEmpty(result.CoreIssues);
        Assert.Null(result.Document);
        Assert.Equal(preferredWinnerBefore, data.PreferredWinnerFirmId);
    }

    [Fact]
    public void PreparePreviewWithIncompleteDataReturnsReadyDocument()
    {
        var data = new ProcurementData();
        var preferredWinnerBefore = data.PreferredWinnerFirmId;

        var result = RunSta(() => DocumentWorkflow.Prepare(
            data,
            new[] { OutputDocumentType.ApproximateRequest },
            DocumentPreparationMode.Preview));

        Assert.Equal(DocumentPreparationStatus.Ready, result.Status);
        Assert.NotNull(result.Document);
        Assert.Single(result.Document.Pages);
        Assert.Empty(result.CoreIssues);
        Assert.Empty(result.ConfirmationMessages);
        Assert.Equal(preferredWinnerBefore, data.PreferredWinnerFirmId);
    }

    [Fact]
    public void PrepareExportWithAdvisoriesReturnsConfirmationRequiredBeforeTieSelection()
    {
        var data = CreateTieScenario();
        data.Dates.MarketReportDate = null;
        var preferredWinnerBefore = data.PreferredWinnerFirmId;

        var result = RunSta(() => DocumentWorkflow.Prepare(
            data,
            new[] { OutputDocumentType.MarketResearchReport },
            DocumentPreparationMode.Export));

        Assert.Equal(DocumentPreparationStatus.ConfirmationRequired, result.Status);
        Assert.NotEmpty(result.ConfirmationMessages);
        Assert.Empty(result.TiedWinnerFirmIds);
        Assert.Null(result.Document);
        Assert.Equal(preferredWinnerBefore, data.PreferredWinnerFirmId);
    }

    [Fact]
    public void PrepareExportWithUnresolvedMarketTieReturnsWinnerSelectionRequired()
    {
        var data = CreateTieScenario();
        var preferredWinnerBefore = data.PreferredWinnerFirmId;
        var expectedIds = ProcurementEngine.Calculate(data).TiedWinnerFirmIds;

        var result = RunSta(() => DocumentWorkflow.Prepare(
            data,
            new[] { OutputDocumentType.MarketResearchReport },
            DocumentPreparationMode.Export,
            advisoriesConfirmed: true));

        Assert.Equal(DocumentPreparationStatus.WinnerSelectionRequired, result.Status);
        Assert.Equal(expectedIds, result.TiedWinnerFirmIds);
        Assert.Equal(10m, result.TiedMarketTotal);
        Assert.Null(result.Document);
        Assert.Equal(preferredWinnerBefore, data.PreferredWinnerFirmId);
    }

    [Fact]
    public void PrepareExportWithTieForPreparationDocumentDoesNotRequestWinner()
    {
        var data = CreateTieScenario();
        var preferredWinnerBefore = data.PreferredWinnerFirmId;

        var result = RunSta(() => DocumentWorkflow.Prepare(
            data,
            new[] { OutputDocumentType.ApproximateRequest },
            DocumentPreparationMode.Export,
            advisoriesConfirmed: true));

        Assert.Equal(DocumentPreparationStatus.Ready, result.Status);
        Assert.NotNull(result.Document);
        Assert.Empty(result.TiedWinnerFirmIds);
        Assert.Equal(preferredWinnerBefore, data.PreferredWinnerFirmId);
    }

    [Fact]
    public void PrepareExportWithPreferredWinnerBuildsDocumentWithoutTiePrompt()
    {
        var data = CreateTieScenario();
        data.PreferredWinnerFirmId = "firm-b";
        var preferredWinnerBefore = data.PreferredWinnerFirmId;

        var result = RunSta(() => DocumentWorkflow.Prepare(
            data,
            new[] { OutputDocumentType.MarketResearchReport },
            DocumentPreparationMode.Export,
            advisoriesConfirmed: true));

        Assert.Equal(DocumentPreparationStatus.Ready, result.Status);
        Assert.NotNull(result.Document);
        Assert.Empty(result.TiedWinnerFirmIds);
        Assert.Equal(preferredWinnerBefore, data.PreferredWinnerFirmId);
    }

    [Fact]
    public void PrepareExportWithUnsupportedEnumValueReturnsFailed()
    {
        var data = CreateTieScenario();
        var preferredWinnerBefore = data.PreferredWinnerFirmId;

        var result = RunSta(() => DocumentWorkflow.Prepare(
            data,
            new[] { (OutputDocumentType)999 },
            DocumentPreparationMode.Export,
            advisoriesConfirmed: true));

        Assert.Equal(DocumentPreparationStatus.Failed, result.Status);
        Assert.Null(result.Document);
        Assert.IsType<NotSupportedException>(result.Error);
        Assert.Equal(preferredWinnerBefore, data.PreferredWinnerFirmId);
    }

    private static ProcurementData CreateTieScenario()
    {
        var data = new ProcurementData
        {
            JobTitle = "Kırtasiye alımı",
            Products = new()
            {
                new ProductItem
                {
                    Id = "product-1",
                    No = 1,
                    Name = "A4 kağıt",
                    Unit = "Adet",
                    Quantity = "1"
                }
            },
            Firms = new()
            {
                CreateFirm("firm-a", 1, "Firma A", 10m),
                CreateFirm("firm-b", 2, "Firma B", 10m),
                CreateFirm("firm-c", 3, "Firma C", 20m)
            },
            MarketCommission = CreateCommission("Piyasa"),
            InspectionCommission = CreateCommission("Muayene"),
            Institution = new InstitutionInfo
            {
                TopAuthority = "İl Müdürlüğü",
                FullName = "Örnek Kurum Müdürlüğü",
                Province = "İzmir",
                Address = "Örnek adres",
                Phone = "2320000000",
                Email = "kurum@example.gov.tr",
                AuthorizedName = "Ahmet Yetkili",
                AuthorizedDuty = "Harcama Yetkilisi"
            },
            Dates = new ProcessDates
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
            }
        };

        return data;
    }

    private static Firm CreateFirm(string id, int number, string name, decimal price) => new()
    {
        Id = id,
        No = number,
        Name = name,
        Address = $"{name} adresi",
        Offers = new()
        {
            new FirmOffer
            {
                ProductId = "product-1",
                ApproximateUnitPrice = price,
                FinalUnitPrice = price
            }
        }
    };

    private static List<CommissionMember> CreateCommission(string label) => new()
    {
        new CommissionMember { No = 1, Name = label + " 1", Title = "Başkan" },
        new CommissionMember { No = 2, Name = label + " 2", Title = "Üye" },
        new CommissionMember { No = 3, Name = label + " 3", Title = "Üye" }
    };

    private static T RunSta<T>(Func<T> action)
    {
        T? result = default;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
            throw failure;

        return result!;
    }
}

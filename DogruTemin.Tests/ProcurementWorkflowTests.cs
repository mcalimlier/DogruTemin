using DogruTemin.Core.Models;
using DogruTemin.Core.Services;

namespace DogruTemin.Tests;

public sealed class ProcurementWorkflowTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "DogruTemin_Workflow_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveBlocksCriticalDraftErrorsWithoutWriting()
    {
        var storage = new StorageService(_root);
        var workflow = new ProcurementWorkflow(storage);
        var data = new ProcurementData
        {
            RecordId = "blocked",
            OperationNumber = "DT-2026-BLOCKED",
            Dates = new ProcessDates
            {
                AssignmentDate = new DateTime(2026, 2, 2),
                CostCalculationDate = new DateTime(2026, 2, 1)
            }
        };

        for (var i = 1; i <= 11; i++)
        {
            data.Products.Add(new ProductItem
            {
                Id = $"product-{i}",
                No = i,
                Name = $"Ürün {i}",
                Unit = "Adet",
                Quantity = "1"
            });
        }

        var result = workflow.Save(data);

        Assert.False(result.SavedSuccessfully);
        Assert.False(result.RequiresWarningConfirmation);
        Assert.NotEmpty(result.BlockingIssues);
        Assert.Null(result.Data);
        Assert.False(File.Exists(storage.ActiveFile));
    }

    [Fact]
    public void SavePersistsValidDataAndReturnsWarningsSeparately()
    {
        var storage = new StorageService(_root);
        var workflow = new ProcurementWorkflow(storage);
        var data = new ProcurementData
        {
            SchemaVersion = SchemaMigrationService.CurrentSchemaVersion,
            RecordId = "workflow-valid",
            OperationNumber = "DT-2026-002",
            JobTitle = "Workflow test"
        };

        data.Products.Add(new ProductItem
        {
            Id = "product-1",
            No = 1,
            Name = "A4 Kağıt",
            Unit = "Adet",
            Quantity = "1"
        });

        data.Firms.Add(new Firm
        {
            Id = "firm-1",
            No = 1,
            Name = "Firma",
            TaxNo = "1000000000",
            Phone = "9000000001",
            Email = "firma@example.gov.tr",
            Address = "Örnek adres"
        });

        var result = workflow.Save(data, createBackup: false);

        Assert.True(result.SavedSuccessfully);
        Assert.Empty(result.BlockingIssues);
        Assert.NotNull(result.Data);
        Assert.Equal(data.RecordId, result.Data.RecordId);
        Assert.True(File.Exists(storage.ActiveFile));
        Assert.True(File.Exists(Path.Combine(storage.RecordsFolder, $"islem_{data.RecordId}.json")));
    }

    [Fact]
    public void SaveDoesNotPersistDateWarningsWithoutExplicitConfirmation()
    {
        var storage = new StorageService(_root);
        var workflow = new ProcurementWorkflow(storage);
        var data = new ProcurementData
        {
            RecordId = "date-warning",
            Dates = new ProcessDates
            {
                AssignmentDate = new DateTime(2026, 2, 2),
                CostCalculationDate = new DateTime(2026, 2, 1)
            }
        };

        var result = workflow.Save(data, createBackup: false);

        Assert.False(result.SavedSuccessfully);
        Assert.True(result.RequiresWarningConfirmation);
        Assert.Empty(result.BlockingIssues);
        Assert.NotEmpty(result.Warnings);
        Assert.False(File.Exists(storage.ActiveFile));

        var confirmed = workflow.Save(data, createBackup: false, allowDateWarnings: true);

        Assert.True(confirmed.SavedSuccessfully);
        Assert.False(confirmed.RequiresWarningConfirmation);
        Assert.NotEmpty(confirmed.Warnings);
        Assert.True(File.Exists(storage.ActiveFile));
    }

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
    }
}

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
            OperationNumber = "DT-2026-BLOCKED"
        };

        var result = workflow.Save(data);

        Assert.False(result.SavedSuccessfully);
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

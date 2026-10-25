using DogruTemin.Core.Services;

namespace DogruTemin.Tests;

public sealed class RecordIntegrityTests : IDisposable
{
    [Fact]
    public void TrailingZerosDoNotChangeRecordIdentity()
    {
        var first = ProcurementBoundaryTests.Scenario(1);
        first.Firms[0].Offers[0].FinalUnitPrice = 19.53m;
        var before = RecordIntegrity.ContentHash(first);
        first.Firms[0].Offers[0].FinalUnitPrice = 19.5300m;
        Assert.Equal(before, RecordIntegrity.ContentHash(first));
    }
    private readonly string _root = Path.Combine(Path.GetTempPath(), "DogruTemin_Integrity_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void ChangedRecordRequiresConfirmationAndKeepsOneArchiveRow()
    {
        var storage = new StorageService(_root);
        var workflow = new ProcurementWorkflow(storage);
        var data = ProcurementBoundaryTests.Scenario(1);
        Assert.True(workflow.Save(data).SavedSuccessfully);
        data.JobTitle = "Düzeltilmiş işlem";
        Assert.True(workflow.Save(data).RequiresReplacementConfirmation);
        Assert.NotEqual(data.JobTitle, storage.LoadRecord(data.RecordId, out _)!.JobTitle);
        Assert.True(workflow.Save(data, confirmReplacement: true).SavedSuccessfully);
        Assert.Single(storage.ListRecords());
        Assert.Equal(2, data.Revision);
    }

    [Fact]
    public void StaleEditorCannotOverwriteNewerRecord()
    {
        var storage = new StorageService(_root);
        var data = ProcurementBoundaryTests.Scenario(1);
        storage.Save(data);
        var stale = storage.LoadRecord(data.RecordId, out _)!;
        data.JobTitle = "Yeni";
        storage.Save(data);
        stale.JobTitle = "Eski";
        Assert.Throws<InvalidOperationException>(() => storage.Save(stale));
        Assert.Equal("Yeni", storage.LoadRecord(data.RecordId, out _)!.JobTitle);
    }

    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}

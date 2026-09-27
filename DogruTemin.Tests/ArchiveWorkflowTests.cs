using DogruTemin.Core.Models;
using DogruTemin.Core.Services;

namespace DogruTemin.Tests;

public sealed class ArchiveWorkflowTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "DogruTemin_ArchiveWorkflow_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void ConstructorRejectsNullStorage()
    {
        Assert.Throws<ArgumentNullException>(() => new ArchiveWorkflow(null!));
    }

    [Fact]
    public void CanDeleteBlocksActiveRecordWithoutChangingArchiveFile()
    {
        var storage = NewStorage("can-delete-active");
        storage.Save(NewRecord("active", "Aktif işlem"), createBackup: false);
        var recordFile = Path.Combine(storage.RecordsFolder, "islem_active.json");
        var before = File.ReadAllText(recordFile);
        var result = ArchiveWorkflow.CanDelete("active", "active");

        Assert.Equal(ArchiveWorkflowStatus.Blocked, result.Status);
        Assert.Equal(before, File.ReadAllText(recordFile));
        Assert.Empty(Directory.GetFiles(storage.DeletedFolder, "silinen_*.json"));
    }

    [Fact]
    public void CanDeleteAllowsRecordWhenNoActiveRecordId()
    {
        var storage = NewStorage("can-delete-no-active");
        storage.Save(NewRecord("archived", "Arşiv işlem"), createBackup: false);
        var result = ArchiveWorkflow.CanDelete("archived", null);

        Assert.Equal(ArchiveWorkflowStatus.Ready, result.Status);
        Assert.Single(storage.ListRecords());
    }

    [Fact]
    public void CanDeleteAllowsInactiveRecord()
    {
        var storage = NewStorage("can-delete-inactive");
        storage.Save(NewRecord("archived", "Arşiv işlem"), createBackup: false);
        var result = ArchiveWorkflow.CanDelete("archived", "another-active-record");

        Assert.Equal(ArchiveWorkflowStatus.Ready, result.Status);
        Assert.Single(storage.ListRecords());
    }

    [Fact]
    public void DeleteBlocksActiveRecordEvenWhenCalledDirectly()
    {
        var storage = NewStorage("delete-active");
        storage.Save(NewRecord("active", "Aktif işlem"), createBackup: false);
        var workflow = new ArchiveWorkflow(storage);

        var result = workflow.Delete("active", "active");

        Assert.Equal(ArchiveWorkflowStatus.Blocked, result.Status);
        Assert.Contains(storage.ListRecords(), item => item.RecordId == "active");
        Assert.Empty(Directory.GetFiles(storage.DeletedFolder, "silinen_*.json"));
    }

    [Fact]
    public void DeleteNonActiveRecordCreatesRecoverableDeletedCopy()
    {
        var storage = NewStorage("delete-inactive");
        storage.Save(NewRecord("archived", "Geri alınacak işlem"), createBackup: false);
        var workflow = new ArchiveWorkflow(storage);

        var deleteResult = workflow.Delete("archived", null);

        Assert.Equal(ArchiveWorkflowStatus.Completed, deleteResult.Status);
        Assert.DoesNotContain(storage.ListRecords(), item => item.RecordId == "archived");
        Assert.Single(Directory.GetFiles(storage.DeletedFolder, "silinen_*.json"));

        var restoreResult = workflow.RestoreMostRecentlyDeleted();

        Assert.Equal(ArchiveWorkflowStatus.Completed, restoreResult.Status);
        Assert.Equal("archived", restoreResult.RestoredRecord?.RecordId);
        Assert.Equal("Geri alınacak işlem", restoreResult.RestoredRecord?.JobTitle);
    }

    [Fact]
    public void RestoreMostRecentlyDeletedWhenEmptyReturnsFailureAndStorageWarning()
    {
        var storage = NewStorage("restore-empty");
        var workflow = new ArchiveWorkflow(storage);

        var result = workflow.RestoreMostRecentlyDeleted();

        Assert.Equal(ArchiveWorkflowStatus.Failed, result.Status);
        Assert.Null(result.RestoredRecord);
        Assert.Equal("Geri alınabilecek silinmiş işlem bulunamadı.", result.Warning);
    }

    [Fact]
    public void RestoreMostRecentlyDeletedWhenMultipleRecordsRestoresNewest()
    {
        var storage = NewStorage("restore-newest");
        storage.Save(NewRecord("older", "Eski silinen"), createBackup: false);
        storage.Save(NewRecord("newest", "En son silinen"), createBackup: false);
        var workflow = new ArchiveWorkflow(storage);
        Assert.Equal(ArchiveWorkflowStatus.Completed, workflow.Delete("older", null).Status);
        Assert.Equal(ArchiveWorkflowStatus.Completed, workflow.Delete("newest", null).Status);

        var deletedFiles = Directory.GetFiles(storage.DeletedFolder, "silinen_*.json");
        var olderFile = Assert.Single(deletedFiles, file => file.Contains("_older.json", StringComparison.Ordinal));
        var newestFile = Assert.Single(deletedFiles, file => file.Contains("_newest.json", StringComparison.Ordinal));
        File.SetLastWriteTimeUtc(olderFile, DateTime.UtcNow.AddMinutes(-2));
        File.SetLastWriteTimeUtc(newestFile, DateTime.UtcNow.AddMinutes(-1));

        var result = workflow.RestoreMostRecentlyDeleted();

        Assert.Equal(ArchiveWorkflowStatus.Completed, result.Status);
        Assert.Equal("newest", result.RestoredRecord?.RecordId);
        Assert.Equal("En son silinen", result.RestoredRecord?.JobTitle);
        Assert.DoesNotContain(storage.ListRecords(), item => item.RecordId == "older");
    }

    [Fact]
    public void RestoreMostRecentlyDeletedWhenRecordIdWasReusedCreatesNewId()
    {
        var storage = NewStorage("restore-id-collision");
        storage.Save(NewRecord("reused-id", "Silinen sürüm"), createBackup: false);
        var workflow = new ArchiveWorkflow(storage);
        Assert.Equal(ArchiveWorkflowStatus.Completed, workflow.Delete("reused-id", null).Status);
        storage.Save(NewRecord("reused-id", "Yeni arşiv sürümü"), createBackup: false);

        var result = workflow.RestoreMostRecentlyDeleted();

        Assert.Equal(ArchiveWorkflowStatus.Completed, result.Status);
        Assert.NotNull(result.RestoredRecord);
        Assert.NotEqual("reused-id", result.RestoredRecord!.RecordId);
        Assert.Equal("Silinen sürüm", result.RestoredRecord.JobTitle);
        Assert.Contains(storage.ListRecords(), item => item.RecordId == "reused-id" && item.JobTitle == "Yeni arşiv sürümü");
    }

    [Fact]
    public void RestoreMostRecentlyDeletedWhenLatestFileIsCorruptReturnsFailureAndKeepsFile()
    {
        var storage = NewStorage("restore-corrupt");
        storage.Save(NewRecord("corrupt", "Bozuk yedek"), createBackup: false);
        var workflow = new ArchiveWorkflow(storage);
        Assert.Equal(ArchiveWorkflowStatus.Completed, workflow.Delete("corrupt", null).Status);
        var deletedFile = Assert.Single(Directory.GetFiles(storage.DeletedFolder, "silinen_*.json"));
        File.WriteAllText(deletedFile, "{bozuk json");
        File.SetLastWriteTimeUtc(deletedFile, DateTime.UtcNow.AddMinutes(-1));

        var result = workflow.RestoreMostRecentlyDeleted();

        Assert.Equal(ArchiveWorkflowStatus.Failed, result.Status);
        Assert.Null(result.RestoredRecord);
        Assert.NotNull(result.Warning);
        Assert.True(File.Exists(deletedFile));
        Assert.Equal("{bozuk json", File.ReadAllText(deletedFile));
    }

    private StorageService NewStorage(string name)
    {
        var path = Path.Combine(_root, name);
        Directory.CreateDirectory(path);
        return new StorageService(path);
    }

    private static ProcurementData NewRecord(string recordId, string jobTitle) => new()
    {
        RecordId = recordId,
        JobTitle = jobTitle
    };

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // Temporary test data must not leak into later runs.
        }

        GC.SuppressFinalize(this);
    }
}

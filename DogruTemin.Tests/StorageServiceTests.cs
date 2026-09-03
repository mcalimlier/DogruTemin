using DogruTemin.Core.Models;
using DogruTemin.Core.Services;

namespace DogruTemin.Tests;

public class StorageServiceTests : IDisposable
{
    private readonly string _temp = Path.Combine(Path.GetTempPath(), "DogruTeminTests_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void ExplicitRoot_CreatesExpectedFolderStructure()
    {
        var root = Path.Combine(_temp, "root");
        var storage = new StorageService(root);

        Assert.Equal(Path.GetFullPath(root), storage.RootFolder);
        Assert.True(Directory.Exists(storage.DataFolder));
        Assert.True(Directory.Exists(storage.RecordsFolder));
        Assert.True(Directory.Exists(storage.BackupFolder));
        Assert.True(Directory.Exists(storage.PdfFolder));
        Assert.True(Directory.Exists(storage.LogFolder));
    }

    [Fact]
    public void Save_CreatesActiveAndArchiveRecord()
    {
        var storage = new StorageService(Path.Combine(_temp, "save"));
        var data = new ProcurementData { RecordId = "abc", JobTitle = "Test alımı" };

        storage.Save(data, createBackup: false);

        Assert.True(File.Exists(storage.ActiveFile));
        Assert.Contains(storage.ListRecords(), record => record.RecordId == "abc");
        Assert.Equal("Test alımı", storage.Load(out _)?.JobTitle);
    }

    [Fact]
    public void LegacyMigration_CopiesWithoutOverwritingExistingFiles()
    {
        var legacy = Path.Combine(_temp, "legacy");
        var preferred = Path.Combine(_temp, "preferred");
        Directory.CreateDirectory(Path.Combine(legacy, "Veri"));
        Directory.CreateDirectory(Path.Combine(preferred, "Veri"));
        File.WriteAllText(Path.Combine(legacy, "Veri", "eski.txt"), "legacy");
        File.WriteAllText(Path.Combine(legacy, "Veri", "ortak.txt"), "legacy-value");
        File.WriteAllText(Path.Combine(preferred, "Veri", "ortak.txt"), "preferred-value");

        _ = new StorageService(preferred, legacy);

        Assert.Equal("legacy", File.ReadAllText(Path.Combine(preferred, "Veri", "eski.txt")));
        Assert.Equal("preferred-value", File.ReadAllText(Path.Combine(preferred, "Veri", "ortak.txt")));
        Assert.Equal("legacy-value", File.ReadAllText(Path.Combine(legacy, "Veri", "ortak.txt")));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_temp)) Directory.Delete(_temp, true); } catch { }
    }
}

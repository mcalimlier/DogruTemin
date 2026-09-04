using DogruTemin.Core.Models;
using DogruTemin.Core.Services;
using DogruTemin.Wpf.Services;
using System.IO.Compression;
using System.Text;

namespace DogruTemin.Tests;

public sealed class DataSafetyIntegrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "DogruTemin_DataSafety_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void ManualBackupRestoreRoundTripRestoresActiveAndArchiveData()
    {
        var storage = NewStorage("roundtrip");
        var safety = new ApplicationSafetyService(storage);
        var data = new ProcurementData
        {
            RecordId = "roundtrip-record",
            JobTitle = "Asıl işlem",
            OperationNumber = "2026/42"
        };
        data.Products.Add(new ProductItem { Name = "Kalem", Quantity = "10", Unit = "Adet" });
        storage.Save(data, createBackup: false);

        var backup = safety.CreateManualSafetyBackup("Test");

        data.JobTitle = "DEĞİŞTİ";
        storage.Save(data, createBackup: false);
        File.Delete(Path.Combine(storage.RecordsFolder, "islem_roundtrip-record.json"));

        var result = safety.RestoreFromBackup(backup);

        Assert.True(result.Success, result.Message);
        Assert.Equal("Asıl işlem", storage.Load(out _)?.JobTitle);
        Assert.Contains(storage.ListRecords(), item => item.RecordId == "roundtrip-record" && item.JobTitle == "Asıl işlem");
        Assert.NotNull(result.SafetyBackupPath);
        Assert.True(File.Exists(result.SafetyBackupPath));
    }

    [Fact]
    public void RestoreRejectsZipSlipPathsAndKeepsCurrentData()
    {
        var storage = NewStorage("zipslip");
        var safety = new ApplicationSafetyService(storage);
        storage.Save(new ProcurementData { RecordId = "safe", JobTitle = "Korunacak işlem" }, createBackup: false);

        var maliciousZip = Path.Combine(_root, "malicious.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(maliciousZip)!);
        using (var archive = ZipFile.Open(maliciousZip, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("../disari.txt");
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write("istenmeyen");
        }

        var result = safety.RestoreFromBackup(maliciousZip);

        Assert.False(result.Success);
        Assert.Equal("Korunacak işlem", storage.Load(out _)?.JobTitle);
        Assert.False(File.Exists(Path.Combine(_root, "disari.txt")));
    }

    [Fact]
    public void CorruptJsonIsDetectedWithoutDestroyingOriginalFile()
    {
        var storage = NewStorage("corrupt");
        var safety = new ApplicationSafetyService(storage);
        File.WriteAllText(storage.ActiveFile, "{not-json", Encoding.UTF8);

        var result = safety.CheckIntegrity();
        var loaded = storage.Load(out var warning);

        Assert.False(result.IsHealthy);
        Assert.True(result.ProblemCount > 0);
        Assert.Null(loaded);
        Assert.NotNull(warning);
        Assert.True(File.Exists(storage.ActiveFile));
        Assert.NotEmpty(Directory.GetFiles(storage.DataFolder, "aktif_islem_bozuk_*.json"));
    }

    private StorageService NewStorage(string name)
    {
        var path = Path.Combine(_root, name);
        Directory.CreateDirectory(path);
        return new StorageService(path);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { }
        GC.SuppressFinalize(this);
    }
}

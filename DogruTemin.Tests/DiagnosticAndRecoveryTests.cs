using DogruTemin.Core.Services;
using DogruTemin.Wpf;
using DogruTemin.Wpf.Services;
using System.IO.Compression;
using System.Text;

namespace DogruTemin.Tests;

public sealed class DiagnosticAndRecoveryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "DogruTemin_Diagnostic_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void DiagnosticPackageContainsSummaryAndLogsButNeverJsonOrPdf()
    {
        var storage = NewStorage("diagnostic");
        var safety = new ApplicationSafetyService(storage);
        File.WriteAllText(storage.SettingsFile, "{\"secret\":true}", Encoding.UTF8);
        File.WriteAllBytes(Path.Combine(storage.PdfFolder, "secret.pdf"), Encoding.ASCII.GetBytes("%PDF-1.4\nsecret"));
        File.WriteAllText(Path.Combine(storage.LogFolder, "test.log"), $"C:\\Users\\{Environment.UserName}\\DoğruTemin hata", Encoding.UTF8);

        var package = DiagnosticPackageService.Create(safety);

        Assert.True(File.Exists(package));
        using var zip = ZipFile.OpenRead(package);
        var names = zip.Entries.Select(entry => entry.FullName).ToArray();
        Assert.Contains("Tanilama.txt", names);
        Assert.Contains("Loglar/test.log", names);
        Assert.DoesNotContain(names, name => name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

        var logEntry = Assert.Single(zip.Entries, entry => entry.FullName == "Loglar/test.log");
        using var reader = new StreamReader(logEntry.Open(), Encoding.UTF8);
        var logText = reader.ReadToEnd();
        Assert.DoesNotContain(Environment.UserName, logText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StaleSessionMarkerTriggersRecoveryBackupAndCleanExitRemovesMarker()
    {
        var storage = NewStorage("recovery");
        storage.Save(new DogruTemin.Core.Models.ProcurementData { RecordId = "recovery", JobTitle = "Korunacak" }, createBackup: false);
        var marker = Path.Combine(storage.DataFolder, "oturum_acik.marker");
        File.WriteAllText(marker, "PID: 999999\n", Encoding.UTF8);

        RecoverySessionService.Begin(storage, () => new ApplicationSafetyService(storage));

        Assert.NotNull(RecoverySessionService.PendingNotice);
        Assert.Contains("güvenlik yedeği", RecoverySessionService.PendingNotice, StringComparison.CurrentCultureIgnoreCase);
        Assert.True(File.Exists(marker));
        Assert.NotEmpty(Directory.EnumerateFiles(Path.Combine(storage.BackupFolder, "Güvenlik"), "*.zip"));

        RecoverySessionService.EndCleanly();
        RecoverySessionService.ClearPendingNotice();
        Assert.False(File.Exists(marker));
    }

    private StorageService NewStorage(string name)
    {
        var path = Path.Combine(_root, name);
        Directory.CreateDirectory(path);
        return new StorageService(path);
    }

    public void Dispose()
    {
        try { RecoverySessionService.EndCleanly(); } catch { }
        RecoverySessionService.ClearPendingNotice();
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { }
        GC.SuppressFinalize(this);
    }
}

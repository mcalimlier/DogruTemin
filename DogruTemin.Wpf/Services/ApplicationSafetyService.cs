using DogruTemin.Wpf;
using DogruTemin.Core.Services;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

namespace DogruTemin.Wpf.Services;

public sealed class ApplicationSafetyService
{
    private readonly StorageService _storage;

    public ApplicationSafetyService(StorageService storage)
    {
        _storage = storage;
    }

    public string CurrentVersion => AppVersionInfo.Version;

    public string InformationalVersion => AppVersionInfo.InformationalVersion;

    public string RuntimeDescription => $"{Environment.Version} / {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}";

    public string BackupFolder => _storage.BackupFolder;
    public string LogFolder => _storage.LogFolder;

    public SafetyCheckResult InitializeAndCheck()
    {
        var messages = new List<string>();
        string? startupBackup = null;

        try
        {
            EnsureFoldersWritable();
        }
        catch (Exception ex)
        {
            return new SafetyCheckResult(false, "Veri klasörlerine yazma erişimi doğrulanamadı: " + ex.Message, null, 1);
        }

        try
        {
            startupBackup = EnsureVersionBackup();
            if (!string.IsNullOrWhiteSpace(startupBackup))
                messages.Add("Sürüm geçişi için otomatik güvenlik yedeği oluşturuldu.");
        }
        catch (Exception ex)
        {
            messages.Add("Sürüm güvenlik yedeği oluşturulamadı: " + ex.Message);
        }

        var integrity = CheckIntegrity();
        if (!string.IsNullOrWhiteSpace(integrity.Message)) messages.Add(integrity.Message);

        return new SafetyCheckResult(
            integrity.IsHealthy,
            string.Join(" ", messages),
            startupBackup,
            integrity.ProblemCount);
    }

    public SafetyCheckResult CheckIntegrity()
    {
        var problems = new List<string>();
        var checkedJson = 0;

        try
        {
            EnsureFoldersWritable();
        }
        catch (Exception ex)
        {
            problems.Add("Klasör erişimi: " + ex.Message);
        }

        var files = new List<string>();
        if (File.Exists(_storage.ActiveFile)) files.Add(_storage.ActiveFile);
        if (File.Exists(_storage.InstitutionFile)) files.Add(_storage.InstitutionFile);
        if (File.Exists(_storage.SettingsFile)) files.Add(_storage.SettingsFile);
        if (Directory.Exists(_storage.RecordsFolder)) files.AddRange(Directory.GetFiles(_storage.RecordsFolder, "*.json"));

        foreach (var file in files.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            checkedJson++;
            try
            {
                using var stream = File.OpenRead(file);
                using var _ = JsonDocument.Parse(stream);
            }
            catch (Exception ex)
            {
                problems.Add($"{Path.GetFileName(file)} okunamadı ({ex.Message})");
            }
        }

        if (problems.Count == 0)
        {
            var recordCount = Directory.Exists(_storage.RecordsFolder)
                ? Directory.GetFiles(_storage.RecordsFolder, "islem_*.json").Length
                : 0;
            return new SafetyCheckResult(true, $"Veri bütünlüğü sağlıklı. {recordCount} arşiv kaydı, {checkedJson} JSON dosyası kontrol edildi.", null, 0);
        }

        return new SafetyCheckResult(false, $"{problems.Count} veri problemi bulundu: {string.Join(" | ", problems.Take(3))}", null, problems.Count);
    }

    public string CreateManualSafetyBackup(string reason = "Manuel")
    {
        Directory.CreateDirectory(_storage.BackupFolder);
        var safetyFolder = Path.Combine(_storage.BackupFolder, "Güvenlik");
        Directory.CreateDirectory(safetyFolder);
        var safeReason = string.Concat(reason.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));
        if (string.IsNullOrWhiteSpace(safeReason)) safeReason = "Manuel";
        var path = Path.Combine(safetyFolder, $"DogruTemin_{safeReason}_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
        CreateDataBackup(path);
        PruneZipBackups(safetyFolder, 25);
        return path;
    }

    public RestoreResult RestoreFromBackup(string zipPath)
    {
        if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
            return new RestoreResult(false, "Seçilen yedek dosyası bulunamadı.", null);

        var tempRoot = Path.Combine(Path.GetTempPath(), "DogruTemin_Restore_" + Guid.NewGuid().ToString("N"));
        var extracted = Path.Combine(tempRoot, "Extracted");
        Directory.CreateDirectory(extracted);

        try
        {
            ValidateAndExtractBackup(zipPath, extracted);
            var sourceRoot = ResolveBackupDataRoot(extracted);
            ValidateExtractedData(sourceRoot);

            var beforeRestoreFolder = Path.Combine(_storage.BackupFolder, "Geri_Yukleme_Oncesi");
            Directory.CreateDirectory(beforeRestoreFolder);
            var beforeRestore = Path.Combine(beforeRestoreFolder, $"DogruTemin_GeriYuklemeOncesi_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
            CreateDataBackup(beforeRestore);
            PruneZipBackups(beforeRestoreFolder, 20);

            var stagingFolder = Path.Combine(_storage.RootFolder, "Veri_yeni_" + Guid.NewGuid().ToString("N"));
            var oldFolder = Path.Combine(_storage.RootFolder, "Veri_onceki_" + Guid.NewGuid().ToString("N"));
            CopyDirectory(sourceRoot, stagingFolder);
            try
            {
                if (Directory.Exists(_storage.DataFolder)) Directory.Move(_storage.DataFolder, oldFolder);
                Directory.Move(stagingFolder, _storage.DataFolder);
                if (Directory.Exists(oldFolder)) Directory.Delete(oldFolder, true);
            }
            catch
            {
                try
                {
                    if (Directory.Exists(_storage.DataFolder)) Directory.Delete(_storage.DataFolder, true);
                    if (Directory.Exists(oldFolder)) Directory.Move(oldFolder, _storage.DataFolder);
                    if (Directory.Exists(stagingFolder)) Directory.Delete(stagingFolder, true);
                }
                catch { }
                throw;
            }

            return new RestoreResult(true, "Yedek başarıyla geri yüklendi.", beforeRestore);
        }
        catch (Exception ex)
        {
            return new RestoreResult(false, "Yedek geri yüklenemedi: " + ex.Message, null);
        }
        finally
        {
            try { if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true); } catch { }
        }
    }

    private string? EnsureVersionBackup()
    {
        Directory.CreateDirectory(_storage.DataFolder);
        var marker = Path.Combine(_storage.DataFolder, "uygulama_surumu.txt");
        var previousVersion = File.Exists(marker) ? File.ReadAllText(marker).Trim() : string.Empty;
        var currentVersion = CurrentVersion;

        string? backup = null;
        var hasExistingData = Directory.EnumerateFiles(_storage.DataFolder, "*", SearchOption.AllDirectories)
            .Any(path => !path.Equals(marker, StringComparison.OrdinalIgnoreCase));

        if (hasExistingData && !string.Equals(previousVersion, currentVersion, StringComparison.OrdinalIgnoreCase))
        {
            var updateFolder = Path.Combine(_storage.BackupFolder, "Güncelleme_Öncesi");
            Directory.CreateDirectory(updateFolder);
            var from = string.IsNullOrWhiteSpace(previousVersion) ? "EskiSurum" : SafeFileToken(previousVersion);
            backup = Path.Combine(updateFolder, $"DogruTemin_{from}_to_{SafeFileToken(currentVersion)}_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
            CreateDataBackup(backup);
            PruneZipBackups(updateFolder, 15);
        }

        File.WriteAllText(marker, currentVersion);
        return backup;
    }

    private void EnsureFoldersWritable()
    {
        foreach (var folder in new[] { _storage.RootFolder, _storage.DataFolder, _storage.RecordsFolder, _storage.BackupFolder, _storage.PdfFolder, _storage.LogFolder })
        {
            Directory.CreateDirectory(folder);
            var probe = Path.Combine(folder, $".dogrutemin_write_test_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
        }
    }

    private void CreateDataBackup(string destinationZip)
    {
        Directory.CreateDirectory(_storage.DataFolder);
        var destinationFolder = Path.GetDirectoryName(destinationZip);
        if (!string.IsNullOrWhiteSpace(destinationFolder)) Directory.CreateDirectory(destinationFolder);
        if (File.Exists(destinationZip)) File.Delete(destinationZip);
        ZipFile.CreateFromDirectory(_storage.DataFolder, destinationZip, CompressionLevel.Optimal, false);
    }

    private static void ValidateAndExtractBackup(string zipPath, string destination)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        if (archive.Entries.Count == 0) throw new InvalidDataException("ZIP yedeği boş.");
        var basePath = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.FullName)) continue;
            var target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
            if (!target.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Yedek içinde geçersiz dosya yolu bulundu.");
        }
        archive.ExtractToDirectory(destination, true);
    }

    private static string ResolveBackupDataRoot(string extracted)
    {
        var directJson = Directory.GetFiles(extracted, "*.json", SearchOption.TopDirectoryOnly).Length > 0;
        var directRecords = Directory.Exists(Path.Combine(extracted, "İşlemler"));
        if (directJson || directRecords) return extracted;

        var dataFolder = Directory.GetDirectories(extracted)
            .FirstOrDefault(d => string.Equals(Path.GetFileName(d), "Veri", StringComparison.OrdinalIgnoreCase));
        return dataFolder ?? extracted;
    }

    private static void ValidateExtractedData(string sourceRoot)
    {
        var jsonFiles = Directory.GetFiles(sourceRoot, "*.json", SearchOption.AllDirectories);
        if (jsonFiles.Length == 0) throw new InvalidDataException("Bu ZIP, geçerli bir DoğruTemin veri yedeği içermiyor.");

        var hasKnownData = File.Exists(Path.Combine(sourceRoot, "aktif_islem.json"))
            || File.Exists(Path.Combine(sourceRoot, "uygulama_ayarlari.json"))
            || File.Exists(Path.Combine(sourceRoot, "kurum_ayarlari.json"))
            || (Directory.Exists(Path.Combine(sourceRoot, "İşlemler")) && Directory.GetFiles(Path.Combine(sourceRoot, "İşlemler"), "islem_*.json").Length > 0);
        if (!hasKnownData) throw new InvalidDataException("ZIP içinde DoğruTemin'e ait tanınan veri dosyaları bulunamadı.");

        foreach (var file in jsonFiles)
        {
            using var stream = File.OpenRead(file);
            using var _ = JsonDocument.Parse(stream);
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        foreach (var directory in Directory.GetDirectories(source))
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }

    private static void PruneZipBackups(string folder, int keep)
    {
        if (!Directory.Exists(folder)) return;
        foreach (var file in new DirectoryInfo(folder).GetFiles("*.zip").OrderByDescending(x => x.LastWriteTimeUtc).Skip(keep))
        {
            try { file.Delete(); } catch { }
        }
    }

    private static string SafeFileToken(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(c => invalid.Contains(c) ? '_' : c));
    }
}

public sealed record SafetyCheckResult(bool IsHealthy, string Message, string? BackupPath, int ProblemCount);
public sealed record RestoreResult(bool Success, string Message, string? SafetyBackupPath);

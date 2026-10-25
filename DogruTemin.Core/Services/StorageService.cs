using System.Text.Json;
using System.Globalization;
using System.Text.Json.Serialization;
using System.IO.Compression;
using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

public class StorageService
{
    public string? LastBackupWarning { get; private set; }
    public List<string> LastArchiveWarnings { get; } = new();
    private readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string RootFolder { get; private set; } = string.Empty;
    public string DataFolder { get; private set; } = string.Empty;
    public string RecordsFolder { get; private set; } = string.Empty;
    public string BackupFolder { get; private set; } = string.Empty;
    public string RecordBackupFolder { get; private set; } = string.Empty;
    public string DeletedFolder { get; private set; } = string.Empty;
    public string PdfFolder { get; private set; } = string.Empty;
    public string LogFolder { get; private set; } = string.Empty;
    public string ActiveFile { get; private set; } = string.Empty;
    public string InstitutionFile { get; private set; } = string.Empty;
    public string SettingsFile { get; private set; } = string.Empty;

    public StorageService()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var preferred = Path.Combine(desktop, "DoğruTemin");
        var legacy = Path.Combine(documents, "DoğruTemin");
        var fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DoğruTemin");
        var root = preferred;

        try
        {
            EnsureRootFolders(root);
        }
        catch
        {
            root = fallback;
            EnsureRootFolders(root);
        }

        InitializePaths(root, legacy);
    }

    /// <summary>
    /// Testler ve kontrollü veri taşıma senaryoları için açık kök klasör desteği.
    /// Normal uygulama akışı parametresiz kurucuyu kullanır.
    /// </summary>
    public StorageService(string rootFolder, string? legacyRoot = null)
    {
        if (string.IsNullOrWhiteSpace(rootFolder)) throw new ArgumentException("Kök klasör boş olamaz.", nameof(rootFolder));
        var root = Path.GetFullPath(rootFolder);
        EnsureRootFolders(root);
        InitializePaths(root, legacyRoot);
    }

    private void InitializePaths(string root, string? legacyRoot)
    {
        // DoğruFatura ile aynı kullanıcı veri yaklaşımı: yeni kök Masaüstü\DoğruTemin.
        // Eski Belgeler\DoğruTemin verileri ilk açılışta KOPYALANIR; kaynak silinmez ve
        // hedefte aynı adla dosya varsa üzerine yazılmaz. Böylece 4.1.x -> 4.2 geçişi güvenlidir.
        if (!string.IsNullOrWhiteSpace(legacyRoot)
            && !Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)
                .Equals(Path.GetFullPath(legacyRoot).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            TryMigrateLegacyFolder(legacyRoot, root);
        }

        RootFolder = root;
        DataFolder = Path.Combine(root, "Veri");
        RecordsFolder = Path.Combine(DataFolder, "İşlemler");
        BackupFolder = Path.Combine(root, "Yedekler");
        RecordBackupFolder = Path.Combine(BackupFolder, "Kayıtlar");
        DeletedFolder = Path.Combine(BackupFolder, "Silinenler");
        PdfFolder = Path.Combine(root, "PDF");
        LogFolder = Path.Combine(root, "Loglar");
        ActiveFile = Path.Combine(DataFolder, "aktif_islem.json");
        InstitutionFile = Path.Combine(DataFolder, "kurum_ayarlari.json");
        SettingsFile = Path.Combine(DataFolder, "uygulama_ayarlari.json");
    }

    private static void EnsureRootFolders(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "Veri"));
        Directory.CreateDirectory(Path.Combine(root, "Veri", "İşlemler"));
        Directory.CreateDirectory(Path.Combine(root, "Yedekler"));
        Directory.CreateDirectory(Path.Combine(root, "Yedekler", "Kayıtlar"));
        Directory.CreateDirectory(Path.Combine(root, "Yedekler", "Silinenler"));
        Directory.CreateDirectory(Path.Combine(root, "PDF"));
        Directory.CreateDirectory(Path.Combine(root, "Loglar"));
    }

    private static void TryMigrateLegacyFolder(string sourceRoot, string destinationRoot)
    {
        try
        {
            if (!Directory.Exists(sourceRoot)) return;
            CopyDirectoryNoOverwrite(sourceRoot, destinationRoot);
        }
        catch
        {
            // Taşıma yardımcı bir güvenlik katmanıdır. Eski veri yerinde kaldığı için
            // bir izin/çakışma sorunu programın açılışını engellemez.
        }
    }

    private static void CopyDirectoryNoOverwrite(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            var target = Path.Combine(destination, Path.GetFileName(file));
            if (!File.Exists(target)) File.Copy(file, target, false);
        }
        foreach (var directory in Directory.GetDirectories(source))
            CopyDirectoryNoOverwrite(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }

    public ProcurementData? Load(out string? warning)
    {
        warning = null;
        if (!File.Exists(ActiveFile)) return null;
        try
        {
            var data = Deserialize(File.ReadAllText(ActiveFile));
            Normalize(data);
            return data;
        }
        catch (Exception ex)
        {
            try
            {
                var corrupt = Path.Combine(DataFolder, $"aktif_islem_bozuk_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                File.Copy(ActiveFile, corrupt, true);
                warning = $"Kayıt dosyası okunamadı. Bozuk dosyanın kopyası korundu:\n{corrupt}\n\n{ex.Message}";
            }
            catch { warning = "Kayıt dosyası okunamadı: " + ex.Message; }
            return null;
        }
    }

    public ProcurementData? LoadRecord(string recordId, out string? warning)
    {
        warning = null;
        try
        {
            var file = RecordFile(recordId);
            if (!File.Exists(file))
            {
                warning = "Seçilen işlem dosyası bulunamadı.";
                return null;
            }
            var data = Deserialize(File.ReadAllText(file));
            Normalize(data);
            return data;
        }
        catch (Exception ex)
        {
            warning = "İşlem kaydı açılamadı: " + ex.Message;
            return null;
        }
    }

    public IReadOnlyList<ProcurementData> ListRecords()
    {
        LastArchiveWarnings.Clear();
        Directory.CreateDirectory(RecordsFolder);
        var result = new List<ProcurementData>();
        foreach (var file in Directory.GetFiles(RecordsFolder, "islem_*.json"))
        {
            try
            {
                var data = Deserialize(File.ReadAllText(file));
                Normalize(data);
                result.Add(data);
            }
            catch (Exception ex)
            {
                LastArchiveWarnings.Add(Path.GetFileName(file) + ": " + ex.Message);
            }
        }
        return result.OrderByDescending(x => x.LastSavedAt ?? x.CreatedAt).ToList();
    }

    public void Save(ProcurementData data, bool createBackup = true)
    {
        SchemaMigrationService.Migrate(data);
        Normalize(data);
        var invalid = DraftValidationService.Validate(data).Where(i => i.Severity == ValidationSeverity.Critical).ToList();
        if (invalid.Count > 0) throw new InvalidDataException(string.Join("\n", invalid.Select(i => i.Message)));
        using var held = new FileStream(RecordFile(data.RecordId) + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var previous = LoadRecord(data.RecordId, out var readWarning);
        if (previous is null && File.Exists(RecordFile(data.RecordId)))
            throw new InvalidDataException(readWarning ?? "Mevcut kayıt okunamıyor; üzerine yazılmadı.");
        if (previous is not null && previous.Revision != data.Revision)
            throw new InvalidOperationException("İşlem başka bir işlemde değişmiş. Arşivden güncel kaydı yeniden açınız; eski bilgiler üzerine yazılmadı.");
        var changed = previous is null || RecordIntegrity.ContentHash(previous) != RecordIntegrity.ContentHash(data);
        var originalRevision = data.Revision;
        data.Revision = changed ? checked((previous?.Revision ?? 0) + 1) : Math.Max(1, data.Revision);
        data.LastSavedAt = DateTime.Now;
        Directory.CreateDirectory(DataFolder);
        Directory.CreateDirectory(RecordsFolder);
        Directory.CreateDirectory(BackupFolder);
        Directory.CreateDirectory(RecordBackupFolder);
        Directory.CreateDirectory(DeletedFolder);

        LastBackupWarning = null;
        if (createBackup)
        {
            try
            {
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
                if (File.Exists(ActiveFile))
                {
                    var activeBackup = Path.Combine(BackupFolder, $"DogruTemin_{stamp}.json");
                    File.Copy(ActiveFile, activeBackup, false);
                }

                var recordFile = RecordFile(data.RecordId);
                if (File.Exists(recordFile))
                {
                    var recordBackup = Path.Combine(RecordBackupFolder, $"islem_{data.RecordId}_{stamp}.json");
                    File.Copy(recordFile, recordBackup, false);
                }
            }
            catch (Exception ex)
            {
                LastBackupWarning = "Yedek kopya oluşturulamadı; ana kayıt yine güvenli biçimde kaydedildi. " + ex.Message;
            }
        }

        var json = JsonSerializer.Serialize(data, _json);
        try
        {
            PreserveSchemaUpgradeSource(RecordFile(data.RecordId));
            PreserveSchemaUpgradeSource(ActiveFile);
            AtomicWrite(RecordFile(data.RecordId), json);
        }
        catch { data.Revision = originalRevision; throw; }
        try { AtomicWrite(ActiveFile, json); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { LastBackupWarning = "İşlem arşive kaydedildi; aktif çalışma kopyası güncellenemedi. İşlemi arşivden yeniden açınız. " + ex.Message; }
        // Aktif kayıt kurum bilgisini kendi içinde taşır. Global varsayılan profil yalnız AYARLARI KAYDET ile değişir.
        SaveInstitution(data.Institution);
        if (createBackup)
        {
            PruneBackups(30);
            PruneRecordBackups(100);
        }
    }

    public void EnsureArchived(ProcurementData data)
    {
        Normalize(data);
        Directory.CreateDirectory(RecordsFolder);
        PreserveSchemaUpgradeSource(ActiveFile);
        var json = JsonSerializer.Serialize(data, _json);
        var file = RecordFile(data.RecordId);
        if (!File.Exists(file)) AtomicWrite(file, json);
        // Eski aktif kayda RecordId/SchemaVersion yazılarak her açılışta yeni arşiv kimliği üretilmesi önlenir.
        AtomicWrite(ActiveFile, json);
        SaveInstitution(data.Institution);
    }

    public string OutputFolderFor(string recordId)
    {
        if (string.IsNullOrWhiteSpace(recordId) || recordId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || recordId is "." or "..")
            throw new InvalidDataException("Geçersiz işlem kimliği.");
        return Path.Combine(PdfFolder, recordId);
    }

    public void RegisterOutput(ProcurementData source, SavedOutputInfo output, Action? publishFiles = null)
    {
        using var held = new FileStream(RecordFile(source.RecordId) + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var current = LoadRecord(source.RecordId, out var warning) ?? throw new InvalidDataException(warning);
        if (current.Revision != source.Revision || RecordIntegrity.ContentHash(current) != output.ContentHash)
            throw new InvalidOperationException("Belge hazırlanırken kayıt değişti. Güncel kayıttan yeniden belge oluşturunuz.");
        publishFiles?.Invoke();
        current.LastOutput = output;
        AtomicWrite(RecordFile(source.RecordId), JsonSerializer.Serialize(current, _json));
        try
        {
            var active = Load(out _);
            if (active?.RecordId == current.RecordId)
                AtomicWrite(ActiveFile, JsonSerializer.Serialize(current, _json));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { /* Archive is authoritative; active metadata can be refreshed on reopen. */ }
        source.LastOutput = output;
    }

    public void ActivateRecord(ProcurementData data)
    {
        Normalize(data);
        Directory.CreateDirectory(DataFolder);
        AtomicWrite(ActiveFile, JsonSerializer.Serialize(data, _json));
    }

    public void ClearActiveFile()
    {
        try
        {
            if (File.Exists(ActiveFile)) File.Delete(ActiveFile);
        }
        catch { }
    }

    public bool DeleteRecord(string recordId)
    {
        try
        {
            var file = RecordFile(recordId);
            if (!File.Exists(file)) return true;
            Directory.CreateDirectory(DeletedFolder);
            var deleted = Path.Combine(DeletedFolder, $"silinen_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{recordId}.json");
            File.Copy(file, deleted, false);
            File.Delete(file);
            PruneDeletedRecords(30);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ProcurementData? RestoreMostRecentlyDeleted(out string? warning)
    {
        warning = null;
        try
        {
            Directory.CreateDirectory(DeletedFolder);
            var latest = new DirectoryInfo(DeletedFolder).GetFiles("silinen_*.json")
                .OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
            if (latest is null)
            {
                warning = "Geri alınabilecek silinmiş işlem bulunamadı.";
                return null;
            }

            var data = Deserialize(File.ReadAllText(latest.FullName));
            Normalize(data);
            var target = RecordFile(data.RecordId);
            if (File.Exists(target))
            {
                data.RecordId = Guid.NewGuid().ToString("N");
                data.CreatedAt = DateTime.Now;
                data.LastSavedAt = DateTime.Now;
                target = RecordFile(data.RecordId);
            }
            AtomicWrite(target, JsonSerializer.Serialize(data, _json));
            latest.Delete();
            return data;
        }
        catch (Exception ex)
        {
            warning = "Silinen işlem geri alınamadı: " + ex.Message;
            return null;
        }
    }

    public ProcurementData? DuplicateRecord(string recordId, out string? warning)
    {
        warning = null;
        try
        {
            var source = LoadRecord(recordId, out warning);
            if (source is null) return null;

            var copy = Deserialize(JsonSerializer.Serialize(source, _json));
            copy.RecordId = Guid.NewGuid().ToString("N");
            copy.Revision = 0;
            copy.LastOutput = null;
            copy.CreatedAt = DateTime.Now;
            copy.LastSavedAt = DateTime.Now;
            copy.OperationNumber = string.Empty;
            copy.JobTitle = (string.IsNullOrWhiteSpace(copy.JobTitle) ? "Başlıksız işlem" : copy.JobTitle.Trim()) + " - Kopya";
            Normalize(copy);
            Directory.CreateDirectory(RecordsFolder);
            AtomicWrite(RecordFile(copy.RecordId), JsonSerializer.Serialize(copy, _json));
            return copy;
        }
        catch (Exception ex)
        {
            warning = "İşlem kopyalanamadı: " + ex.Message;
            return null;
        }
    }

    public bool ExportRecord(string recordId, string destinationFile, out string? warning)
    {
        warning = null;
        try
        {
            var source = RecordFile(recordId);
            if (!File.Exists(source))
            {
                warning = "Seçilen işlem dosyası bulunamadı.";
                return false;
            }
            var folder = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrWhiteSpace(folder)) Directory.CreateDirectory(folder);
            File.Copy(source, destinationFile, true);
            return true;
        }
        catch (Exception ex)
        {
            warning = "İşlem dışa aktarılamadı: " + ex.Message;
            return false;
        }
    }

    public bool ExportArchiveBackup(string destinationZip, out string? warning)
    {
        warning = null;
        try
        {
            var fullDestination = Path.GetFullPath(destinationZip);
            var fullDataFolder = Path.GetFullPath(DataFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (fullDestination.StartsWith(fullDataFolder, StringComparison.OrdinalIgnoreCase))
            {
                warning = "Arşiv yedeği Veri klasörünün içine kaydedilemez. Masaüstü veya başka bir klasör seçiniz.";
                return false;
            }

            var folder = Path.GetDirectoryName(fullDestination);
            if (!string.IsNullOrWhiteSpace(folder)) Directory.CreateDirectory(folder);
            if (File.Exists(fullDestination)) File.Delete(fullDestination);
            ZipFile.CreateFromDirectory(DataFolder, fullDestination, CompressionLevel.Optimal, false);
            return true;
        }
        catch (Exception ex)
        {
            warning = "Arşiv yedeği oluşturulamadı: " + ex.Message;
            return false;
        }
    }


    public ApplicationSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var settings = JsonSerializer.Deserialize<ApplicationSettings>(File.ReadAllText(SettingsFile), _json) ?? new ApplicationSettings();
                NormalizeSettings(settings);
                return settings;
            }
        }
        catch
        {
            // Bozuk ayar dosyasında eski kurum ayarına geri düşülür.
        }

        var migrated = new ApplicationSettings
        {
            Institution = LoadInstitution() ?? new InstitutionInfo(),
            DefaultUnit = "Adet"
        };
        NormalizeSettings(migrated);
        return migrated;
    }

    public void SaveSettings(ApplicationSettings settings)
    {
        NormalizeSettings(settings);
        Directory.CreateDirectory(DataFolder);
        AtomicWrite(SettingsFile, JsonSerializer.Serialize(settings, _json));
        // Eski sürümlerle geriye uyumluluk için kurum profili yardımcı dosyası da güncel tutulur.
        SaveInstitution(settings.Institution);
    }

    public InstitutionInfo? LoadInstitution()
    {
        try
        {
            if (!File.Exists(InstitutionFile)) return null;
            return JsonSerializer.Deserialize<InstitutionInfo>(File.ReadAllText(InstitutionFile), _json);
        }
        catch { return null; }
    }

    public void SaveInstitution(InstitutionInfo institution)
    {
        try
        {
            Directory.CreateDirectory(DataFolder);
            AtomicWrite(InstitutionFile, JsonSerializer.Serialize(institution, _json));
        }
        catch
        {
            // Kurum ayarları ana işlem kaydında da bulunduğu için bu yardımcı kayıt başarısız olsa bile ana kayıt korunur.
        }
    }

    public void PruneBackups(int keep)
    {
        if (!Directory.Exists(BackupFolder)) return;
        foreach (var file in new DirectoryInfo(BackupFolder).GetFiles("DogruTemin_*.json").OrderByDescending(f => f.CreationTimeUtc).Skip(keep))
        {
            try { file.Delete(); } catch { }
        }
    }

    private void PruneRecordBackups(int keep)
    {
        if (!Directory.Exists(RecordBackupFolder)) return;
        foreach (var file in new DirectoryInfo(RecordBackupFolder).GetFiles("islem_*.json").OrderByDescending(f => f.CreationTimeUtc).Skip(keep))
        {
            try { file.Delete(); } catch { }
        }
    }

    private void PruneDeletedRecords(int keep)
    {
        if (!Directory.Exists(DeletedFolder)) return;
        foreach (var file in new DirectoryInfo(DeletedFolder).GetFiles("silinen_*.json").OrderByDescending(f => f.CreationTimeUtc).Skip(keep))
        {
            try { file.Delete(); } catch { }
        }
    }

    private ProcurementData Deserialize(string json)
    {
        var data = JsonSerializer.Deserialize<ProcurementData>(json, _json);
        if (data is null) throw new InvalidDataException("Kayıt dosyası boş.");
        return SchemaMigrationService.Migrate(data);
    }

    private void PreserveSchemaUpgradeSource(string file)
    {
        if (!File.Exists(file)) return;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("SchemaVersion", out var version)
                && version.TryGetInt32(out var number) && number == SchemaMigrationService.CurrentSchemaVersion) return;
        }
        catch (JsonException) { /* Preserve an unreadable active copy before recovery overwrites it. */ }
        Directory.CreateDirectory(RecordBackupFolder);
        var destination = Path.Combine(RecordBackupFolder, $"gecis_v{SchemaMigrationService.CurrentSchemaVersion}_{Path.GetFileName(file)}");
        if (!File.Exists(destination)) File.Copy(file, destination, overwrite: false);
    }

    private string RecordFile(string recordId)
    {
        if (recordId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || recordId is "." or "..")
            throw new InvalidDataException("Geçersiz işlem kimliği; kayıt dosyası değiştirilmedi.");
        return Path.Combine(RecordsFolder, $"islem_{recordId}.json");
    }

    private static void AtomicWrite(string file, string content)
    {
        var temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(content);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }
            File.Move(temp, file, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static void NormalizeSettings(ApplicationSettings settings)
    {
        if (settings.WindowSizePreference is not ("Auto" or "1280x720" or "1366x768" or "1920x1080" or "Maximized"))
            settings.WindowSizePreference = "Auto";
        settings.SchemaVersion = Math.Max(settings.SchemaVersion, 1);
        settings.Institution ??= new InstitutionInfo();
        settings.DefaultInspectionCommission ??= new List<CommissionMember>();
        settings.DefaultMarketCommission ??= new List<CommissionMember>();
        settings.DefaultInspectionCommission = NormalizeCommissionTemplate(settings.DefaultInspectionCommission);
        settings.DefaultMarketCommission = NormalizeCommissionTemplate(settings.DefaultMarketCommission);
        settings.DefaultUnit = settings.DefaultUnit == "Miktar" ? "Miktar" : "Adet";
    }

    private static List<CommissionMember> NormalizeCommissionTemplate(IEnumerable<CommissionMember> source)
    {
        var result = source.Take(3).Select((m, i) => new CommissionMember
        {
            No = i + 1,
            Name = m.Name?.Trim() ?? string.Empty,
            Title = m.Title?.Trim() ?? string.Empty
        }).ToList();
        while (result.Count < 3)
            result.Add(new CommissionMember { No = result.Count + 1 });
        return result;
    }

    private static void Normalize(ProcurementData data)
    {
        ProcurementLimits.EnsureCapacity(data);
        data.SchemaVersion = SchemaMigrationService.CurrentSchemaVersion;
        if (string.IsNullOrWhiteSpace(data.RecordId)) data.RecordId = Guid.NewGuid().ToString("N");
        if (data.CreatedAt == default) data.CreatedAt = data.LastSavedAt ?? DateTime.Now;
        data.Products ??= new(); data.Firms ??= new(); data.InspectionCommission ??= new(); data.MarketCommission ??= new();
        data.Dates ??= new(); data.Institution ??= new(); data.ApprovalDocument ??= new();
        for (var i = 0; i < data.Products.Count; i++) { data.Products[i].No = i + 1; if (string.IsNullOrWhiteSpace(data.Products[i].Id)) data.Products[i].Id = Guid.NewGuid().ToString("N"); }
        for (var i = 0; i < data.Firms.Count; i++)
        {
            data.Firms[i].No = i + 1;
            if (string.IsNullOrWhiteSpace(data.Firms[i].Id)) data.Firms[i].Id = Guid.NewGuid().ToString("N");
            data.Firms[i].Offers ??= new();
            if (data.Firms[i].Offers.Any(o =>
                (o.ApproximateUnitPrice.HasValue && !ProcurementLimits.HasMoneyPrecision(o.ApproximateUnitPrice.Value))
                || (o.FinalUnitPrice.HasValue && !ProcurementLimits.HasMoneyPrecision(o.FinalUnitPrice.Value))))
                throw new InvalidDataException("Kayıtta iki ondalıktan fazla birim fiyat var. Fiyatlar yuvarlanmadı; kaynak dosya korunuyor.");
        }
        data.Dates.ApprovalNo = TextNormalizer.Hys(data.Dates.ApprovalNo);
    }
}

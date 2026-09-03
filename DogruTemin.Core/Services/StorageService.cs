using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO.Compression;
using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

public class StorageService
{
    public string? LastBackupWarning { get; private set; }
    private readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string RootFolder { get; }
    public string DataFolder { get; }
    public string RecordsFolder { get; }
    public string BackupFolder { get; }
    public string RecordBackupFolder { get; }
    public string DeletedFolder { get; }
    public string PdfFolder { get; }
    public string ActiveFile { get; }
    public string InstitutionFile { get; }
    public string SettingsFile { get; }

    public StorageService()
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var preferred = Path.Combine(docs, "DoğruTemin");
        var fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DoğruTemin");
        var root = preferred;
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Veri"));
            Directory.CreateDirectory(Path.Combine(root, "Veri", "İşlemler"));
            Directory.CreateDirectory(Path.Combine(root, "Yedekler"));
            Directory.CreateDirectory(Path.Combine(root, "Yedekler", "Kayıtlar"));
            Directory.CreateDirectory(Path.Combine(root, "Yedekler", "Silinenler"));
            Directory.CreateDirectory(Path.Combine(root, "PDF"));
        }
        catch
        {
            root = fallback;
            Directory.CreateDirectory(Path.Combine(root, "Veri"));
            Directory.CreateDirectory(Path.Combine(root, "Veri", "İşlemler"));
            Directory.CreateDirectory(Path.Combine(root, "Yedekler"));
            Directory.CreateDirectory(Path.Combine(root, "Yedekler", "Kayıtlar"));
            Directory.CreateDirectory(Path.Combine(root, "Yedekler", "Silinenler"));
            Directory.CreateDirectory(Path.Combine(root, "PDF"));
        }
        RootFolder = root;
        DataFolder = Path.Combine(root, "Veri");
        RecordsFolder = Path.Combine(DataFolder, "İşlemler");
        BackupFolder = Path.Combine(root, "Yedekler");
        RecordBackupFolder = Path.Combine(BackupFolder, "Kayıtlar");
        DeletedFolder = Path.Combine(BackupFolder, "Silinenler");
        PdfFolder = Path.Combine(root, "PDF");
        ActiveFile = Path.Combine(DataFolder, "aktif_islem.json");
        InstitutionFile = Path.Combine(DataFolder, "kurum_ayarlari.json");
        SettingsFile = Path.Combine(DataFolder, "uygulama_ayarlari.json");
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
            catch
            {
                // Tek bir bozuk arşiv kaydı, diğer kayıtların listelenmesini engellemez.
            }
        }
        return result.OrderByDescending(x => x.LastSavedAt ?? x.CreatedAt).ToList();
    }

    public void Save(ProcurementData data, bool createBackup = true)
    {
        Normalize(data);
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
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
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
        AtomicWrite(ActiveFile, json);
        AtomicWrite(RecordFile(data.RecordId), json);
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
        var json = JsonSerializer.Serialize(data, _json);
        var file = RecordFile(data.RecordId);
        if (!File.Exists(file)) AtomicWrite(file, json);
        // Eski aktif kayda RecordId/SchemaVersion yazılarak her açılışta yeni arşiv kimliği üretilmesi önlenir.
        AtomicWrite(ActiveFile, json);
        SaveInstitution(data.Institution);
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
        return data;
    }

    private string RecordFile(string recordId) => Path.Combine(RecordsFolder, $"islem_{recordId}.json");

    private static void AtomicWrite(string file, string content)
    {
        var temp = file + ".tmp";
        File.WriteAllText(temp, content);
        File.Move(temp, file, true);
    }

    private static void NormalizeSettings(ApplicationSettings settings)
    {
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
        data.SchemaVersion = Math.Max(data.SchemaVersion, 3);
        if (string.IsNullOrWhiteSpace(data.RecordId)) data.RecordId = Guid.NewGuid().ToString("N");
        if (data.CreatedAt == default) data.CreatedAt = data.LastSavedAt ?? DateTime.Now;
        data.Products ??= new(); data.Firms ??= new(); data.InspectionCommission ??= new(); data.MarketCommission ??= new();
        data.Dates ??= new(); data.Institution ??= new();
        data.Products = data.Products.Take(8).ToList();
        data.Firms = data.Firms.Take(5).ToList();
        for (var i = 0; i < data.Products.Count; i++) { data.Products[i].No = i + 1; if (string.IsNullOrWhiteSpace(data.Products[i].Id)) data.Products[i].Id = Guid.NewGuid().ToString("N"); }
        for (var i = 0; i < data.Firms.Count; i++)
        {
            data.Firms[i].No = i + 1;
            if (string.IsNullOrWhiteSpace(data.Firms[i].Id)) data.Firms[i].Id = Guid.NewGuid().ToString("N");
            data.Firms[i].Offers ??= new();
        }
        data.Dates.ApprovalNo = TextNormalizer.Hys(data.Dates.ApprovalNo);
    }
}

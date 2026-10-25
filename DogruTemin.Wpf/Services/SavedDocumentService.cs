using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Documents;
using DogruTemin.Core.Models;
using DogruTemin.Core.Services;

namespace DogruTemin.Wpf.Services;

public sealed class SavedDocumentService(StorageService storage)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public string Export(ProcurementData source, FixedDocument document, IReadOnlyCollection<OutputDocumentType> types)
    {
        var current = storage.LoadRecord(source.RecordId, out var warning)
            ?? throw new InvalidDataException(warning ?? "Önce işlemi kaydediniz.");
        var hash = RecordIntegrity.ContentHash(source);
        if (current.Revision != source.Revision || RecordIntegrity.ContentHash(current) != hash)
            throw new InvalidOperationException("Ekran ile kayıtlı işlem farklı. Önce değişiklikleri kaydediniz.");
        var folder = storage.OutputFolderFor(source.RecordId);
        var staging = folder + ".new_" + Guid.NewGuid().ToString("N");
        var oldFolder = folder + ".old_" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(staging);
        var path = Path.Combine(folder, "DogruTemin_Belgeler.pdf");
        var stagedPdf = Path.Combine(staging, "DogruTemin_Belgeler.pdf");
        var published = false;
        var committed = false;
        try
        {
            PdfVectorWriter.Save(document, stagedPdf);
            var output = new SavedOutputInfo
            {
                RecordRevision = source.Revision,
                ContentHash = hash,
                PdfHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(stagedPdf))),
                PdfPath = path,
                GeneratedAt = DateTime.Now,
                ApplicationVersion = AppVersionInfo.DisplayVersion,
                DocumentTypes = types.ToList()
            };
            // The saved archive is the authoritative input snapshot; this portable copy travels with the PDF.
            var snapshotData = JsonSerializer.Deserialize<ProcurementData>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)!;
            snapshotData.LastOutput = null;
            var snapshot = JsonSerializer.Serialize(new { Output = output, Data = snapshotData }, JsonOptions);
            var snapshotPath = Path.Combine(staging, "Belge_Kaynak.json");
            File.WriteAllText(snapshotPath, snapshot);
            output.SnapshotHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(snapshotPath)));
            storage.RegisterOutput(source, output, () =>
            {
                if (Directory.Exists(folder)) Directory.Move(folder, oldFolder);
                try { Directory.Move(staging, folder); published = true; }
                catch { if (Directory.Exists(oldFolder)) Directory.Move(oldFolder, folder); throw; }
            });
            committed = true;
            return path;
        }
        catch
        {
            if (published)
            {
                Directory.Delete(folder, true);
                if (Directory.Exists(oldFolder)) Directory.Move(oldFolder, folder);
            }
            throw;
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            if (committed && Directory.Exists(oldFolder))
            {
                try { Directory.Delete(oldFolder, true); }
                catch (IOException) { /* Keep the previous package if a viewer still holds it. */ }
                catch (UnauthorizedAccessException) { /* Cleanup must not invalidate a published package. */ }
            }
        }
    }

    public static string OutputStatus(ProcurementData data)
    {
        if (data.LastOutput is null) return "Henüz üretilmedi";
        if (!RecordIntegrity.HasCurrentOutput(data)) return "Değişti • yeniden üretin";
        if (!File.Exists(data.LastOutput.PdfPath)) return "PDF bulunamadı";
        try
        {
            var snapshotPath = Path.Combine(Path.GetDirectoryName(data.LastOutput.PdfPath)!, "Belge_Kaynak.json");
            if (!File.Exists(snapshotPath)) return "Kaynak bilgisi bulunamadı";
            if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(snapshotPath))) != data.LastOutput.SnapshotHash)
                return "Kaynak değişmiş • yeniden üretin";
            return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(data.LastOutput.PdfPath))) == data.LastOutput.PdfHash
                ? "Güncel PDF ✓" : "PDF değişmiş • yeniden üretin";
        }
        catch (IOException) { return "PDF okunamıyor"; }
        catch (UnauthorizedAccessException) { return "PDF erişimi yok"; }
    }
}

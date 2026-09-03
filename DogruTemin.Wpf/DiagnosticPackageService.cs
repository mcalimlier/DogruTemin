using DogruTemin.Wpf.Services;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DogruTemin.Wpf;

public sealed class DiagnosticPackageService
{
    private const int MaximumLogFiles = 5;
    private const int MaximumLogCharacters = 250_000;

    public string Create(ApplicationSafetyService safetyService)
    {
        ArgumentNullException.ThrowIfNull(safetyService);
        var integrity = safetyService.CheckIntegrity();
        var folder = Path.Combine(safetyService.BackupFolder, "Tanılama");
        Directory.CreateDirectory(folder);
        var path = UniquePath(folder, $"DogruTemin_Tanilama_{DateTime.Now:yyyyMMdd_HHmmss}", ".zip");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create, Encoding.UTF8);
        WriteEntry(archive, "Tanilama.txt", BuildSummary(safetyService, integrity));

        if (Directory.Exists(safetyService.LogFolder))
        {
            foreach (var log in new DirectoryInfo(safetyService.LogFolder).GetFiles("*.log")
                         .OrderByDescending(file => file.LastWriteTimeUtc).Take(MaximumLogFiles))
            {
                try
                {
                    var text = File.ReadAllText(log.FullName, Encoding.UTF8);
                    if (text.Length > MaximumLogCharacters) text = text[^MaximumLogCharacters..];
                    WriteEntry(archive, "Loglar/" + log.Name, Sanitize(text));
                }
                catch { }
            }
        }
        return path;
    }

    private static string BuildSummary(ApplicationSafetyService service, SafetyCheckResult integrity)
    {
        var builder = new StringBuilder();
        builder.AppendLine("DoğruTemin Tanılama Paketi");
        builder.AppendLine(new string('=', 48));
        builder.AppendLine($"Oluşturma       : {DateTimeOffset.Now:dd.MM.yyyy HH:mm:ss zzz}");
        builder.AppendLine($"Sürüm           : {service.InformationalVersion}");
        builder.AppendLine($"Çalışma ortamı  : {service.RuntimeDescription}");
        builder.AppendLine($"Veri bütünlüğü  : {(integrity.IsHealthy ? "Sağlıklı" : "Sorun bulundu")}");
        builder.AppendLine($"Sorun sayısı    : {integrity.ProblemCount}");
        builder.AppendLine($"Açıklama        : {integrity.Message}");
        builder.AppendLine();
        builder.AppendLine("Gizlilik notu: Bu paket işlem JSON verilerini veya PDF belgelerini içermez.");
        builder.AppendLine("Yalnız uygulama/ortam özeti ve en fazla son 5 hata günlüğü eklenir.");
        return Sanitize(builder.ToString());
    }

    private static string Sanitize(string value)
    {
        var sanitized = value;
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(profile)) sanitized = sanitized.Replace(profile, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(Environment.UserName)) sanitized = sanitized.Replace(Environment.UserName, "%USERNAME%", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(Environment.MachineName)) sanitized = sanitized.Replace(Environment.MachineName, "%COMPUTERNAME%", StringComparison.OrdinalIgnoreCase);
        return sanitized;
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string UniquePath(string folder, string baseName, string extension)
    {
        var path = Path.Combine(folder, baseName + extension);
        var suffix = 2;
        while (File.Exists(path)) path = Path.Combine(folder, $"{baseName}_{suffix++}{extension}");
        return path;
    }
}

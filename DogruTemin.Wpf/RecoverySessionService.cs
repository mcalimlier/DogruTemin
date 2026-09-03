using DogruTemin.Core.Services;
using System.IO;
using System.Text;

namespace DogruTemin.Wpf;

public static class RecoverySessionService
{
    private static readonly object Sync = new();
    private static string? _activeMarkerPath;
    public static string? PendingNotice { get; private set; }

    public static void Begin(StorageService storage, Func<Services.ApplicationSafetyService> safetyServiceFactory)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(safetyServiceFactory);
        lock (Sync)
        {
            Directory.CreateDirectory(storage.DataFolder);
            var marker = Path.Combine(storage.DataFolder, "oturum_acik.marker");
            PendingNotice = null;
            if (File.Exists(marker))
            {
                try
                {
                    var backup = safetyServiceFactory().CreateManualSafetyBackup("BeklenmeyenKapanmaSonrasi");
                    PendingNotice = "Önceki DoğruTemin oturumu normal kapanmamış görünüyor. Mevcut veriler korunarak güvenlik yedeği alındı:\n" + backup;
                }
                catch (Exception ex)
                {
                    PendingNotice = "Önceki DoğruTemin oturumu normal kapanmamış görünüyor. Otomatik yedek oluşturulamadı; Hakkında / Güvenlik ekranından veri bütünlüğünü kontrol edin. Ayrıntı: " + ex.Message;
                }
            }

            var content = new StringBuilder()
                .AppendLine("DoğruTemin çalışma oturumu")
                .AppendLine($"Başlangıç: {DateTimeOffset.Now:O}")
                .AppendLine($"PID: {Environment.ProcessId}")
                .ToString();
            File.WriteAllText(marker, content, new UTF8Encoding(false));
            _activeMarkerPath = marker;
        }
    }

    public static void EndCleanly()
    {
        lock (Sync)
        {
            if (string.IsNullOrWhiteSpace(_activeMarkerPath)) return;
            try { if (File.Exists(_activeMarkerPath)) File.Delete(_activeMarkerPath); }
            finally { _activeMarkerPath = null; }
        }
    }

    public static void ClearPendingNotice() => PendingNotice = null;
}

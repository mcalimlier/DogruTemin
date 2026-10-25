namespace DogruTemin.Core.Models;

public sealed class SavedOutputInfo
{
    public int RecordRevision { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string PdfHash { get; set; } = string.Empty;
    public string SnapshotHash { get; set; } = string.Empty;
    public string PdfPath { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = string.Empty;
    public string TemplateVersion { get; set; } = "2026-10-25";
    public DateTime GeneratedAt { get; set; }
    public List<OutputDocumentType> DocumentTypes { get; set; } = new();
}

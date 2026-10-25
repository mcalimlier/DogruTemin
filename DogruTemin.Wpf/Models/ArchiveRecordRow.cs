namespace DogruTemin.Wpf.Models;

public sealed class ArchiveRecordRow
{
    public string RecordId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string OperationNumber { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string ApprovalDate { get; set; } = string.Empty;
    public DateTime ApprovalSortKey { get; set; } = DateTime.MinValue;
    public int ProductCount { get; set; }
    public int FirmCount { get; set; }
    public string ScopeCounts => $"{ProductCount} / {FirmCount}";
    public string FinalTotal { get; set; } = string.Empty;
    public decimal FinalTotalValue { get; set; }
    public string OutputStatus { get; set; } = string.Empty;
    public string LastSaved { get; set; } = string.Empty;
    public DateTime LastSavedValue { get; set; }
}

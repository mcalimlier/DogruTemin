namespace DogruTemin.Core.Models;

public class ProcurementData
{
    public int SchemaVersion { get; set; } = 4;
    public string RecordId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string OperationNumber { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public List<ProductItem> Products { get; set; } = new();
    public List<Firm> Firms { get; set; } = new();
    public List<CommissionMember> InspectionCommission { get; set; } = new();
    public List<CommissionMember> MarketCommission { get; set; } = new();
    public ProcessDates Dates { get; set; } = new();
    public InstitutionInfo Institution { get; set; } = new();
    public ApprovalDocumentInfo ApprovalDocument { get; set; } = new();
    public string PreferredWinnerFirmId { get; set; } = string.Empty;
    public bool AutoBackup { get; set; } = true;
    public DateTime? LastSavedAt { get; set; }
}

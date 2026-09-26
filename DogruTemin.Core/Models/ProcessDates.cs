namespace DogruTemin.Core.Models;

public class ProcessDates
{
    public DateTime? AssignmentDate { get; set; }
    public string AssignmentNo { get; set; } = string.Empty;
    public DateTime? ApprovalDate { get; set; }
    public string ApprovalNo { get; set; } = string.Empty;
    public DateTime? CostCalculationDate { get; set; }
    public DateTime? ApprovalDocumentDate { get; set; }
    public DateTime? MarketReportDate { get; set; }
    public DateTime? InspectionReportDate { get; set; }
    public DateTime? OfferDeadlineDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? MarketApprovalDate { get; set; }
}

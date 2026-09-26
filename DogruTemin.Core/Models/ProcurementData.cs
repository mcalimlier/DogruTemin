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

public class Firm
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int No { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TaxOffice { get; set; } = string.Empty;
    public string TaxNo { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime? OfferDate { get; set; }
    public List<FirmOffer> Offers { get; set; } = new();
}

public class ProductItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int No { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string Unit { get; set; } = "Adet";
    public string Quantity { get; set; } = "1";
}

public class FirmOffer
{
    public string ProductId { get; set; } = string.Empty;
    public decimal? ApproximateUnitPrice { get; set; }
    public decimal? FinalUnitPrice { get; set; }
}

public class CommissionMember
{
    public int No { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

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

public class InstitutionInfo
{
    public string TopAuthority { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AuthorizedName { get; set; } = string.Empty;
    public string AuthorizedDuty { get; set; } = string.Empty;
    public string AuthorizedTitle { get; set; } = string.Empty;
}

public class ApprovalDocumentInfo
{
    public string DocumentNumber { get; set; } = string.Empty;
    public string AvailableBudgetAmount { get; set; } = string.Empty;
    public string InvestmentProjectNumber { get; set; } = string.Empty;
    public string BudgetCode { get; set; } = string.Empty;
    public string AdvanceConditions { get; set; } = string.Empty;
    public string TenderProcedure { get; set; } = "4734 S.K. 22/d Kapsamında";
    public string AnnouncementMethod { get; set; } = "İlansız";
    public string PrequalificationDocumentSalePrice { get; set; } = string.Empty;
    public string PriceDifferenceBasis { get; set; } = "Ödenmeyecektir";
    public string Notes { get; set; } = string.Empty;
    public string SubmittedByName { get; set; } = string.Empty;
    public string SubmittedByTitle { get; set; } = string.Empty;
}

public class ApplicationSettings
{
    public int SchemaVersion { get; set; } = 1;
    public InstitutionInfo Institution { get; set; } = new();
    public List<CommissionMember> DefaultInspectionCommission { get; set; } = new();
    public List<CommissionMember> DefaultMarketCommission { get; set; } = new();
    public string DefaultUnit { get; set; } = "Adet";
}

public enum ValidationSeverity
{
    Info,
    Warning,
    Critical
}

public enum OutputDocumentType
{
    ApproximateRequest = 1,
    ApproximateCostSheet = 2,
    FirmOfferForm = 3,
    MarketResearchReport = 4,
    InspectionAcceptanceReport = 5,
    ApprovalDocument = 6
}

public class ValidationIssue
{
    public ValidationSeverity Severity { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

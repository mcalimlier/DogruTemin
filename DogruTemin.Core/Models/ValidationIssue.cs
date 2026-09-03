namespace DogruTemin.Core.Models;

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
    InspectionAcceptanceReport = 5
}

public class ValidationIssue
{
    public ValidationSeverity Severity { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

public static class ValidationService
{
    public static IReadOnlyList<ValidationIssue> ValidateDraft(ProcurementData data) =>
        DraftValidationService.Validate(data);

    public static IReadOnlyList<ValidationIssue> ValidateWorkflow(ProcurementData data) =>
        WorkflowValidationService.Validate(data);

    public static IReadOnlyList<ValidationIssue> DateLogicWarnings(ProcurementData data) =>
        DateValidationService.RequiredSequenceIssues(data.Dates, ValidationSeverity.Warning);

    public static IReadOnlyList<ValidationIssue> ValidateDocument(ProcurementData data, OutputDocumentType document) =>
        DocumentValidationService.Validate(data, document);

    public static int ReadinessPercent(ProcurementData data) =>
        ReadinessService.CalculatePercent(data);

    // Backward-compatible facade for existing callers/tests.
    public static bool ValidTaxNo(string? text) => InputValidationService.ValidTaxNo(text);
    public static bool ValidPhone(string? text) => InputValidationService.ValidPhone(text);
    public static bool ValidEmail(string? text) => InputValidationService.ValidEmail(text);
}

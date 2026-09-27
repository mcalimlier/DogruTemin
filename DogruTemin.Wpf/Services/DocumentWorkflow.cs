using DogruTemin.Core.Models;
using DogruTemin.Core.Services;
using System.Windows.Documents;

namespace DogruTemin.Wpf.Services;

/// <summary>
/// Coordinates deterministic document checks and generation without UI interaction.
/// </summary>
public static class DocumentWorkflow
{
    public static DocumentWorkflowResult Prepare(
        ProcurementData data,
        IReadOnlyCollection<OutputDocumentType> types,
        DocumentPreparationMode mode,
        bool advisoriesConfirmed = false)
    {
        try
        {
            if (mode == DocumentPreparationMode.Preview)
                return Ready(OfficialDocumentService.BuildDocument(data, types));

            var coreIssues = DocumentReadinessService.GetExportCoreIssues(data, types);
            if (coreIssues.Count > 0)
            {
                return new DocumentWorkflowResult(
                    DocumentPreparationStatus.Blocked,
                    null,
                    coreIssues,
                    Array.Empty<string>(),
                    Array.Empty<string>());
            }

            var confirmationMessages = types
                .SelectMany(type => ValidationService.ValidateDocument(data, type))
                .Select(issue => issue.Message)
                .Concat(ValidationService.DateLogicWarnings(data).Select(issue => issue.Message))
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct()
                .ToList();

            if (confirmationMessages.Count > 0 && !advisoriesConfirmed)
            {
                return new DocumentWorkflowResult(
                    DocumentPreparationStatus.ConfirmationRequired,
                    null,
                    Array.Empty<string>(),
                    confirmationMessages,
                    Array.Empty<string>());
            }

            var calculation = ProcurementEngine.Calculate(data);
            var requiresWinner = types.Any(type => type is
                OutputDocumentType.MarketResearchReport or
                OutputDocumentType.InspectionAcceptanceReport);
            if (requiresWinner && calculation.HasWinnerTie && string.IsNullOrWhiteSpace(calculation.WinningFirmId))
            {
                return new DocumentWorkflowResult(
                    DocumentPreparationStatus.WinnerSelectionRequired,
                    null,
                    Array.Empty<string>(),
                    confirmationMessages,
                    calculation.TiedWinnerFirmIds.ToList());
            }

            return Ready(OfficialDocumentService.BuildDocument(data, types), confirmationMessages);
        }
        catch (Exception ex)
        {
            return new DocumentWorkflowResult(
                DocumentPreparationStatus.Failed,
                null,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                ex);
        }
    }

    private static DocumentWorkflowResult Ready(
        FixedDocument document,
        IReadOnlyList<string>? confirmationMessages = null) =>
        new(
            DocumentPreparationStatus.Ready,
            document,
            Array.Empty<string>(),
            confirmationMessages ?? Array.Empty<string>(),
            Array.Empty<string>());
}

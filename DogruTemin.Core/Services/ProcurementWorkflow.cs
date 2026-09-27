using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

/// <summary>
/// Application-level orchestration for the procurement save workflow.
/// UI code can consume the result without knowing validation or storage details.
/// </summary>
public sealed class ProcurementWorkflow
{
    private readonly StorageService _storage;

    public ProcurementWorkflow(StorageService storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public ProcurementWorkflowResult Save(
        ProcurementData data,
        bool createBackup = true,
        bool allowDateWarnings = false)
    {
        ArgumentNullException.ThrowIfNull(data);

        var draftIssues = ValidationService.ValidateDraft(data);
        var criticalDraftIssues = draftIssues
            .Where(issue => issue.Severity == ValidationSeverity.Critical)
            .ToList();

        if (criticalDraftIssues.Count > 0)
            return ProcurementWorkflowResult.Blocked(criticalDraftIssues);

        var dateWarnings = ValidationService.DateLogicWarnings(data);
        if (dateWarnings.Count > 0 && !allowDateWarnings)
            return ProcurementWorkflowResult.AwaitingWarningConfirmation(dateWarnings);

        _storage.Save(data, createBackup);

        return ProcurementWorkflowResult.Saved(
            data,
            dateWarnings,
            _storage.LastBackupWarning);
    }
}

/// <summary>
/// Result of a procurement workflow operation.
/// Presentation code decides how issues are shown to the user.
/// </summary>
public sealed class ProcurementWorkflowResult
{
    private ProcurementWorkflowResult(
        bool saved,
        IReadOnlyList<ValidationIssue> blockingIssues,
        IReadOnlyList<ValidationIssue> warnings,
        string? backupWarning,
        ProcurementData? data,
        bool requiresWarningConfirmation)
    {
        SavedSuccessfully = saved;
        BlockingIssues = blockingIssues;
        Warnings = warnings;
        BackupWarning = backupWarning;
        Data = data;
        RequiresWarningConfirmation = requiresWarningConfirmation;
    }

    public bool SavedSuccessfully { get; }
    public IReadOnlyList<ValidationIssue> BlockingIssues { get; }
    public IReadOnlyList<ValidationIssue> Warnings { get; }
    public string? BackupWarning { get; }
    public ProcurementData? Data { get; }
    public bool RequiresWarningConfirmation { get; }

    public static ProcurementWorkflowResult Blocked(
        IReadOnlyList<ValidationIssue> blockingIssues) =>
        new(false, blockingIssues, Array.Empty<ValidationIssue>(), null, null, false);

    public static ProcurementWorkflowResult AwaitingWarningConfirmation(
        IReadOnlyList<ValidationIssue> warnings) =>
        new(false, Array.Empty<ValidationIssue>(), warnings, null, null, true);

    public static ProcurementWorkflowResult Saved(
        ProcurementData data,
        IReadOnlyList<ValidationIssue> warnings,
        string? backupWarning) =>
        new(true, Array.Empty<ValidationIssue>(), warnings, backupWarning, data, false);
}

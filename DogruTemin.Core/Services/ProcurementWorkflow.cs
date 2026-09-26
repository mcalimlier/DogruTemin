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
        bool createBackup = true)
    {
        ArgumentNullException.ThrowIfNull(data);

        var draftIssues = ValidationService.ValidateDraft(data);
        var criticalDraftIssues = draftIssues
            .Where(issue => issue.Severity == ValidationSeverity.Critical)
            .ToList();

        if (criticalDraftIssues.Count > 0)
            return ProcurementWorkflowResult.Blocked(criticalDraftIssues);

        var dateWarnings = ValidationService.DateLogicWarnings(data);

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
        ProcurementData? data)
    {
        SavedSuccessfully = saved;
        BlockingIssues = blockingIssues;
        Warnings = warnings;
        BackupWarning = backupWarning;
        Data = data;
    }

    public bool SavedSuccessfully { get; }
    public IReadOnlyList<ValidationIssue> BlockingIssues { get; }
    public IReadOnlyList<ValidationIssue> Warnings { get; }
    public string? BackupWarning { get; }
    public ProcurementData? Data { get; }

    public static ProcurementWorkflowResult Blocked(
        IReadOnlyList<ValidationIssue> blockingIssues) =>
        new(false, blockingIssues, Array.Empty<ValidationIssue>(), null, null);

    public static ProcurementWorkflowResult Saved(
        ProcurementData data,
        IReadOnlyList<ValidationIssue> warnings,
        string? backupWarning) =>
        new(true, Array.Empty<ValidationIssue>(), warnings, backupWarning, data);
}

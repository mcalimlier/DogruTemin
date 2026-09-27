using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

/// <summary>
/// Orchestrates archive deletion and deleted-record recovery without depending on UI presentation.
/// </summary>
public sealed class ArchiveWorkflow
{
    private readonly StorageService _storage;

    public ArchiveWorkflow(StorageService storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public static ArchiveWorkflowResult CanDelete(string recordId, string? activeRecordId)
    {
        var isActiveRecord = !string.IsNullOrWhiteSpace(activeRecordId)
            && string.Equals(recordId, activeRecordId, StringComparison.Ordinal);

        return isActiveRecord
            ? ArchiveWorkflowResult.Blocked()
            : ArchiveWorkflowResult.Ready();
    }

    public ArchiveWorkflowResult Delete(string recordId, string? activeRecordId)
    {
        if (CanDelete(recordId, activeRecordId).Status == ArchiveWorkflowStatus.Blocked)
            return ArchiveWorkflowResult.Blocked();

        return _storage.DeleteRecord(recordId)
            ? ArchiveWorkflowResult.Completed()
            : ArchiveWorkflowResult.Failed();
    }

    public ArchiveWorkflowResult RestoreMostRecentlyDeleted()
    {
        var record = _storage.RestoreMostRecentlyDeleted(out var warning);
        return record is null
            ? ArchiveWorkflowResult.Failed(warning)
            : ArchiveWorkflowResult.Completed(record, warning);
    }
}

public enum ArchiveWorkflowStatus
{
    Ready,
    Completed,
    Blocked,
    Failed
}

/// <summary>
/// Result of an archive workflow operation. Presentation code decides how to show warnings.
/// </summary>
public sealed class ArchiveWorkflowResult
{
    private ArchiveWorkflowResult(
        ArchiveWorkflowStatus status,
        ProcurementData? restoredRecord = null,
        string? warning = null)
    {
        Status = status;
        RestoredRecord = restoredRecord;
        Warning = warning;
    }

    public ArchiveWorkflowStatus Status { get; }
    public ProcurementData? RestoredRecord { get; }
    public string? Warning { get; }

    internal static ArchiveWorkflowResult Ready() =>
        new(ArchiveWorkflowStatus.Ready);

    internal static ArchiveWorkflowResult Completed(
        ProcurementData? restoredRecord = null,
        string? warning = null) =>
        new(ArchiveWorkflowStatus.Completed, restoredRecord, warning);

    internal static ArchiveWorkflowResult Blocked() =>
        new(ArchiveWorkflowStatus.Blocked);

    internal static ArchiveWorkflowResult Failed(string? warning = null) =>
        new(ArchiveWorkflowStatus.Failed, warning: warning);
}

using System.Windows.Documents;

namespace DogruTemin.Wpf.Services;

public enum DocumentPreparationMode
{
    Preview,
    Export
}

public enum DocumentPreparationStatus
{
    Ready,
    Blocked,
    ConfirmationRequired,
    WinnerSelectionRequired,
    Failed
}

public sealed record DocumentWorkflowResult(
    DocumentPreparationStatus Status,
    FixedDocument? Document,
    IReadOnlyList<string> CoreIssues,
    IReadOnlyList<string> ConfirmationMessages,
    IReadOnlyList<string> TiedWinnerFirmIds,
    decimal? TiedMarketTotal = null,
    Exception? Error = null);

namespace DogruTemin.Wpf.Models;

public sealed class ManagementReportSnapshot
{
    public string PeriodLabel { get; set; } = string.Empty;
    public string InstitutionName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public int IncludedOperationCount { get; set; }
    public int FinancialCompleteCount { get; set; }
    public int ExcludedMissingDateCount { get; set; }
    public decimal ApproximateTotal { get; set; }
    public decimal FinalTotal { get; set; }
    public decimal SavingsTotal { get; set; }
    public decimal SavingsRate { get; set; }
    public decimal AverageFinalTotal { get; set; }
    public List<ManagementReportMonthRow> Months { get; set; } = new();
    public List<ManagementReportOperationRow> Operations { get; set; } = new();
}

public sealed class ManagementReportMonthRow
{
    public int MonthNumber { get; set; }
    public string Month { get; set; } = string.Empty;
    public int OperationCount { get; set; }
    public decimal FinalTotal { get; set; }
}

public sealed class ManagementReportOperationRow
{
    public string RecordId { get; set; } = string.Empty;
    public DateTime ApprovalDate { get; set; }
    public string OperationNumber { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public decimal ApproximateTotal { get; set; }
    public decimal FinalTotal { get; set; }
    public decimal Savings { get; set; }
    public bool FinancialComplete { get; set; }
}

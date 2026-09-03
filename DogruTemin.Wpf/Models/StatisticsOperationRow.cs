namespace DogruTemin.Wpf.Models;

public sealed class StatisticsOperationRow
{
    public string RecordId { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string DateText { get; set; } = string.Empty;
    public decimal ApproximateTotal { get; set; }
    public decimal FinalTotal { get; set; }
    public decimal Savings { get; set; }
    public string FinalText { get; set; } = string.Empty;
    public string SavingsText { get; set; } = string.Empty;
}

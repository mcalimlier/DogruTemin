namespace DogruTemin.Wpf.Models;

public sealed class MonthlyStatisticRow
{
    public int MonthNumber { get; set; }
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal FinalTotal { get; set; }
    public string TotalText { get; set; } = string.Empty;
    public double BarWidth { get; set; }
}

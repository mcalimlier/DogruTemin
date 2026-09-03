namespace DogruTemin.Core.Models;

public class ProductCalculation
{
    public string ProductId { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal? ApproximateAverageUnitPrice { get; set; }
    public decimal? ApproximateLineTotal { get; set; }
    public decimal? WinningUnitPrice { get; set; }
    public decimal? WinningLineTotal { get; set; }
    public string WinningFirmId { get; set; } = string.Empty;
    public string WinningFirmName { get; set; } = string.Empty;
    public decimal? EstimatedCurrentUnitPrice { get; set; }
}

public class CalculationResult
{
    public decimal ApproximateCostTotal { get; set; }
    public decimal MarketResultTotal { get; set; }
    public decimal EstimatedCurrentTotal { get; set; }
    public string WinningFirmId { get; set; } = string.Empty;
    public string WinningFirmName { get; set; } = string.Empty;
    public bool HasWinnerTie { get; set; }
    public List<string> TiedWinnerFirmIds { get; set; } = new();
    public List<ProductCalculation> Products { get; set; } = new();
    public List<string> WinningFirmNames { get; set; } = new();
}

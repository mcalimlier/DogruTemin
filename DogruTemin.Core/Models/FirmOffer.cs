namespace DogruTemin.Core.Models;

public class FirmOffer
{
    public string ProductId { get; set; } = string.Empty;
    public decimal? ApproximateUnitPrice { get; set; }
    public decimal? FinalUnitPrice { get; set; }
}

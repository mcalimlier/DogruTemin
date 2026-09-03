namespace DogruTemin.Core.Models;

public class ProductItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int No { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string Unit { get; set; } = "Adet";
    public string Quantity { get; set; } = "1";
}

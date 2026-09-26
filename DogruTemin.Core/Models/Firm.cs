namespace DogruTemin.Core.Models;

public class Firm
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int No { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TaxOffice { get; set; } = string.Empty;
    public string TaxNo { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime? OfferDate { get; set; }
    public List<FirmOffer> Offers { get; set; } = new();
}

using DogruTemin.Core.Models;
using DogruTemin.Core.Services;

namespace DogruTemin.Tests;

public class ProcurementEngineTests
{
    [Fact]
    public void LowestCompleteFirmWinsWholeProcurement()
    {
        var p1 = new ProductItem { Id = "p1", Name = "Kağıt", Quantity = "2" };
        var p2 = new ProductItem { Id = "p2", Name = "Kalem", Quantity = "3" };
        var data = new ProcurementData
        {
            Products = new() { p1, p2 },
            Firms = new()
            {
                Firm("a", "Firma A", ("p1", 10m), ("p2", 20m)),
                Firm("b", "Firma B", ("p1", 12m), ("p2", 15m)),
                Firm("c", "Firma C", ("p1", 13m), ("p2", 30m))
            }
        };

        var result = ProcurementEngine.Calculate(data);

        Assert.Equal("b", result.WinningFirmId);
        Assert.Equal("Firma B", result.WinningFirmName);
        Assert.Equal(69m, result.MarketResultTotal);
        Assert.False(result.HasWinnerTie);
    }

    [Fact]
    public void TieDoesNotAutoSelectWinnerWithoutPreference()
    {
        var p = new ProductItem { Id = "p1", Name = "Ürün", Quantity = "1" };
        var data = new ProcurementData
        {
            Products = new() { p },
            Firms = new()
            {
                Firm("a", "Firma A", ("p1", 10m)),
                Firm("b", "Firma B", ("p1", 10m)),
                Firm("c", "Firma C", ("p1", 12m))
            }
        };

        var result = ProcurementEngine.Calculate(data);

        Assert.True(result.HasWinnerTie);
        Assert.Equal(2, result.TiedWinnerFirmIds.Count);
        Assert.True(string.IsNullOrWhiteSpace(result.WinningFirmId));
    }

    private static Firm Firm(string id, string name, params (string ProductId, decimal Price)[] prices) =>
        new()
        {
            Id = id,
            Name = name,
            Offers = prices.Select(x => new FirmOffer
            {
                ProductId = x.ProductId,
                ApproximateUnitPrice = x.Price,
                FinalUnitPrice = x.Price
            }).ToList()
        };
}

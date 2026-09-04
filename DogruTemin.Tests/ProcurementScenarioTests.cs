using DogruTemin.Core.Models;
using DogruTemin.Core.Services;

namespace DogruTemin.Tests;

public sealed class ProcurementScenarioTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "DogruTemin_Scenario_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void MultiProductMultiFirmFlowSurvivesSaveReloadAndKeepsCalculation()
    {
        var data = new ProcurementData
        {
            RecordId = "scenario-1",
            JobTitle = "Kırtasiye ve sarf alımı",
            Products = new()
            {
                new ProductItem { Id = "p1", No = 1, Name = "A4 Kağıt", Unit = "Adet", Quantity = "10" },
                new ProductItem { Id = "p2", No = 2, Name = "Toner", Unit = "Adet", Quantity = "2" },
                new ProductItem { Id = "p3", No = 3, Name = "Temizlik Malzemesi", Unit = "Miktar", Quantity = "5,5" }
            },
            Firms = new()
            {
                Firm("a", "Firma A", ("p1", 100m), ("p2", 750m), ("p3", 80m)),
                Firm("b", "Firma B", ("p1", 95m), ("p2", 700m), ("p3", 75m)),
                Firm("c", "Firma C", ("p1", 120m), ("p2", 680m), ("p3", 90m)),
                Firm("d", "Firma D", ("p1", 90m), ("p2", 720m), ("p3", 85m))
            }
        };

        var before = ProcurementEngine.Calculate(data);
        var storage = new StorageService(Path.Combine(_root, "data"));
        storage.Save(data, createBackup: true);
        storage.Save(data, createBackup: true);
        var loaded = storage.Load(out var loadError);

        Assert.Null(loadError);
        Assert.NotNull(loaded);
        Assert.Equal(3, loaded.Products.Count);
        Assert.Equal("Adet", loaded.Products[0].Unit);
        Assert.Equal("Miktar", loaded.Products[2].Unit);
        Assert.Equal(4, loaded.Firms.Count);

        var after = ProcurementEngine.Calculate(loaded);
        Assert.Equal(before.WinningFirmId, after.WinningFirmId);
        Assert.Equal(before.MarketResultTotal, after.MarketResultTotal);
        Assert.Equal("b", after.WinningFirmId);
        Assert.Equal(2762.5m, after.MarketResultTotal);
        Assert.NotEmpty(Directory.EnumerateFiles(storage.BackupFolder, "DogruTemin_*.json", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public void FirmWithMissingOfferCannotWinEvenWhenItsKnownPricesAreLower()
    {
        var data = new ProcurementData
        {
            Products = new()
            {
                new ProductItem { Id = "p1", Name = "Ürün 1", Quantity = "2" },
                new ProductItem { Id = "p2", Name = "Ürün 2", Quantity = "3" }
            },
            Firms = new()
            {
                Firm("incomplete", "Eksik Firma", ("p1", 1m)),
                Firm("complete", "Tam Firma", ("p1", 10m), ("p2", 20m)),
                Firm("other", "Diğer Firma", ("p1", 12m), ("p2", 22m))
            }
        };

        var result = ProcurementEngine.Calculate(data);

        Assert.Equal("complete", result.WinningFirmId);
        Assert.Equal(80m, result.MarketResultTotal);
    }

    [Fact]
    public void PreferredFirmIsUsedWhenLowestTotalIsTied()
    {
        var data = new ProcurementData
        {
            PreferredWinnerFirmId = "b",
            Products = new()
            {
                new ProductItem { Id = "p1", Name = "Ürün", Quantity = "2" }
            },
            Firms = new()
            {
                Firm("a", "Firma A", ("p1", 10m)),
                Firm("b", "Firma B", ("p1", 10m)),
                Firm("c", "Firma C", ("p1", 12m))
            }
        };

        var result = ProcurementEngine.Calculate(data);

        Assert.True(result.HasWinnerTie);
        Assert.Equal("b", result.WinningFirmId);
        Assert.Equal("Firma B", result.WinningFirmName);
        Assert.Equal(20m, result.MarketResultTotal);
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

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { }
        GC.SuppressFinalize(this);
    }
}

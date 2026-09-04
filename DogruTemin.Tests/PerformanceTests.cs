using DogruTemin.Core.Models;
using DogruTemin.Core.Services;
using System.Diagnostics;

namespace DogruTemin.Tests;

public sealed class PerformanceTests
{
    [Fact]
    public void ProcurementEngineHandlesRepeatedFullCalculationsWithinReasonableTime()
    {
        var data = BuildProcurement();
        var watch = Stopwatch.StartNew();
        CalculationResult? last = null;

        for (var i = 0; i < 5_000; i++)
            last = ProcurementEngine.Calculate(data);

        watch.Stop();
        Assert.NotNull(last);
        Assert.Equal("f1", last.WinningFirmId);
        Assert.True(
            watch.Elapsed < TimeSpan.FromSeconds(8),
            $"5.000 tam hesaplama {watch.Elapsed.TotalSeconds:0.00} sn sürdü.");
    }

    private static ProcurementData BuildProcurement()
    {
        var products = Enumerable.Range(1, 8)
            .Select(index => new ProductItem
            {
                Id = $"p{index}",
                No = index,
                Name = $"Ürün {index}",
                Quantity = (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                Unit = index % 2 == 0 ? "Miktar" : "Adet"
            })
            .ToList();

        var firms = Enumerable.Range(1, 5)
            .Select(firmIndex =>
            {
                var firm = new Firm
                {
                    Id = $"f{firmIndex}",
                    No = firmIndex,
                    Name = $"Firma {firmIndex}"
                };
                foreach (var product in products)
                {
                    firm.Offers.Add(new FirmOffer
                    {
                        ProductId = product.Id,
                        ApproximateUnitPrice = 100m + firmIndex,
                        FinalUnitPrice = 80m + firmIndex
                    });
                }
                return firm;
            })
            .ToList();

        return new ProcurementData { Products = products, Firms = firms };
    }
}

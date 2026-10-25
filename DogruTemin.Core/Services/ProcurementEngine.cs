using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

public static class ProcurementEngine
{
    private const decimal TieTolerance = 0.000001m;

    public static CalculationResult Calculate(ProcurementData data)
    {
        ProcurementLimits.EnsureCapacity(data);
        var result = new CalculationResult();
        var products = data.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Take(ProcurementLimits.MaxProducts).ToList();
        var firms = data.Firms.Where(f => !string.IsNullOrWhiteSpace(f.Name)).Take(5).ToList();

        foreach (var product in products)
        {
            var pc = new ProductCalculation { ProductId = product.Id };
            var hasQuantity = NumberParser.TryPositiveDecimal(product.Quantity, out var qty);
            if (hasQuantity) pc.Quantity = qty;

            var approx = firms.Select(f => OfferFor(f, product.Id)?.ApproximateUnitPrice)
                .Where(IsPositive).Select(v => v!.Value).ToList();
            if (approx.Count > 0)
            {
                pc.ApproximateAverageUnitPrice = approx.Average();
                if (hasQuantity)
                {
                    pc.ApproximateLineTotal = pc.ApproximateAverageUnitPrice.Value * qty;
                    result.ApproximateCostTotal += pc.ApproximateLineTotal.Value;
                }
            }
            result.Products.Add(pc);
        }

        CalculateLowestCompleteFirm(data, products, firms, result);
        result.EstimatedCurrentTotal = result.MarketResultTotal;
        result.WinningFirmNames = string.IsNullOrWhiteSpace(result.WinningFirmName)
            ? new List<string>()
            : new List<string> { result.WinningFirmName };
        return result;
    }

    private static FirmOffer? OfferFor(Firm firm, string productId) => firm.Offers.FirstOrDefault(o => o.ProductId == productId);
    private static bool IsPositive(decimal? value) => value.HasValue && value.Value > 0;
    private static bool Equal(decimal a, decimal b) => Math.Abs(a - b) < TieTolerance;

    private static void CalculateLowestCompleteFirm(ProcurementData data, List<ProductItem> products, List<Firm> firms, CalculationResult result)
    {
        if (products.Count == 0) return;

        var candidates = new List<(Firm Firm, decimal Total)>();
        foreach (var firm in firms)
        {
            decimal total = 0;
            var valid = true;
            foreach (var product in products)
            {
                var price = OfferFor(firm, product.Id)?.FinalUnitPrice;
                if (!IsPositive(price) || !NumberParser.TryPositiveDecimal(product.Quantity, out var qty))
                {
                    valid = false;
                    break;
                }
                total += price!.Value * qty;
            }
            if (valid && total > 0) candidates.Add((firm, total));
        }
        if (candidates.Count == 0) return;

        var min = candidates.Min(x => x.Total);
        var tied = candidates.Where(x => Equal(x.Total, min)).ToList();
        result.HasWinnerTie = tied.Count > 1;
        result.TiedWinnerFirmIds = tied.Select(x => x.Firm.Id).ToList();
        result.MarketResultTotal = min;

        (Firm Firm, decimal Total)? chosen = null;
        if (tied.Count == 1)
        {
            chosen = tied[0];
        }
        else if (!string.IsNullOrWhiteSpace(data.PreferredWinnerFirmId))
        {
            var preferred = tied.FirstOrDefault(x => x.Firm.Id == data.PreferredWinnerFirmId);
            if (preferred.Firm is not null) chosen = preferred;
        }

        // Eşitlik varsa kullanıcı seçim yapmadan otomatik kazanan atanmaz.
        if (chosen is null) return;

        result.WinningFirmId = chosen.Value.Firm.Id;
        result.WinningFirmName = chosen.Value.Firm.Name;
        result.MarketResultTotal = chosen.Value.Total;

        foreach (var product in products)
        {
            var price = OfferFor(chosen.Value.Firm, product.Id)?.FinalUnitPrice;
            if (!IsPositive(price) || !NumberParser.TryPositiveDecimal(product.Quantity, out var qty)) continue;
            var pc = result.Products.First(p => p.ProductId == product.Id);
            pc.WinningUnitPrice = price;
            pc.WinningLineTotal = price!.Value * qty;
            pc.EstimatedCurrentUnitPrice = price;
            pc.WinningFirmId = chosen.Value.Firm.Id;
            pc.WinningFirmName = chosen.Value.Firm.Name;
        }
    }
}

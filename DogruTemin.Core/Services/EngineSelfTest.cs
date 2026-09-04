using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

public static class EngineSelfTest
{
    public static (bool Success, string Message) Run()
    {
        try
        {
            // Tamamen yapay dahili test; kullanıcı verisi değildir ve uygulamaya yüklenmez.
            var p1 = new ProductItem { No = 1, Name = "Test A", Unit = "Adet", Quantity = "2" };
            var p2 = new ProductItem { No = 2, Name = "Test B", Unit = "Adet", Quantity = "3" };
            var data = new ProcurementData { Products = new() { p1, p2 } };
            data.Firms.Add(F("Test Firma A", p1, 10, 6, p2, 20, 5)); // nihai: 80
            data.Firms.Add(F("Test Firma B", p1, 12, 6, p2, 15, 5)); // nihai: 69 -> kazanan
            data.Firms.Add(F("Eksik Firma", p1, 1, 6, p2, null, 5));  // değerlendirme dışı

            var result = ProcurementEngine.Calculate(data);
            if (result.MarketResultTotal != 69m || result.WinningFirmName != "Test Firma B")
                return (false, "Miktar x nihai birim fiyat toplamı öz testi başarısız.");
            if (result.Products.First(x => x.ProductId == p1.Id).WinningLineTotal != 24m ||
                result.Products.First(x => x.ProductId == p2.Id).WinningLineTotal != 45m)
                return (false, "Nihai satır toplamı öz testi başarısız.");
            // Yaklaşık birim fiyat mutlaka miktarla çarpılarak satır/toplam maliyete dönüşmelidir.
            var approx1 = result.Products.First(x => x.ProductId == p1.Id).ApproximateLineTotal; // 6 x 2 = 12
            var approx2 = result.Products.First(x => x.ProductId == p2.Id).ApproximateLineTotal; // 5 x 3 = 15
            if (approx1 != 12m || approx2 != 15m || result.ApproximateCostTotal != 27m)
                return (false, "Yaklaşık maliyet miktar çarpımı öz testi başarısız.");

            // Eşit toplamda otomatik kazanan atanmadığını da doğrula.
            var tie = new ProcurementData { Products = new() { p1 } };
            tie.Firms.Add(F("Eşit A", p1, 10, 6, p2, null, null));
            tie.Firms.Add(F("Eşit B", p1, 10, 6, p2, null, null));
            var tieResult = ProcurementEngine.Calculate(tie);
            if (!tieResult.HasWinnerTie || !string.IsNullOrWhiteSpace(tieResult.WinningFirmId))
                return (false, "Eşit teklif güvenlik öz testi başarısız.");

            tie.PreferredWinnerFirmId = tie.Firms[1].Id;
            tieResult = ProcurementEngine.Calculate(tie);
            if (tieResult.WinningFirmId != tie.Firms[1].Id)
                return (false, "Eşit teklif kullanıcı seçimi öz testi başarısız.");

            return (true, "Bağımsız hesap motoru öz testi başarılı.");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    private static Firm F(string name, ProductItem p1, decimal? f1, decimal? a1, ProductItem p2, decimal? f2, decimal? a2)
    {
        var firm = new Firm { Name = name };
        if (f1.HasValue || a1.HasValue) firm.Offers.Add(new FirmOffer { ProductId = p1.Id, FinalUnitPrice = f1, ApproximateUnitPrice = a1 });
        if (f2.HasValue || a2.HasValue) firm.Offers.Add(new FirmOffer { ProductId = p2.Id, FinalUnitPrice = f2, ApproximateUnitPrice = a2 });
        return firm;
    }
}

using DogruTemin.Core.Models;
using DogruTemin.Core.Services;

namespace DogruTemin.Tests;

public sealed class ProcurementBoundaryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "DogruTemin_Boundary_" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("19,539", false)]
    [InlineData("19,53", true)]
    [InlineData("1.234,56", true)]
    [InlineData("19,5300", true)]
    public void NumericInputHasAtMostTwoSignificantDecimalPlaces(string text, bool valid)
        => Assert.Equal(valid, NumberParser.TryPositiveDecimal(text, out _));

    [Fact]
    public void TenProductsAreSavedAndCalculatedWithoutTruncation()
    {
        var data = Scenario(10);
        Assert.DoesNotContain(ValidationService.ValidateDraft(data), i => i.Severity == ValidationSeverity.Critical);
        var storage = new StorageService(_root);
        storage.Save(data);
        var loaded = storage.LoadRecord(data.RecordId, out var warning);
        Assert.Null(warning);
        Assert.Equal(10, loaded!.Products.Count);
        Assert.Equal(10m, ProcurementEngine.Calculate(loaded).MarketResultTotal);
    }

    [Theory]
    [InlineData(11, 1)]
    [InlineData(1, 6)]
    public void StorageRejectsOverCapacityWithoutMutatingInputOrExistingRecord(int products, int firms)
    {
        var storage = new StorageService(_root);
        var original = Scenario(1);
        storage.Save(original);
        var before = File.ReadAllText(storage.ActiveFile);
        var invalid = Scenario(products);
        invalid.RecordId = original.RecordId;
        invalid.Firms = Enumerable.Range(0, firms).Select(i => new Firm { Name = "Firma " + i }).ToList();
        Assert.Throws<InvalidDataException>(() => storage.Save(invalid));
        Assert.Equal(products, invalid.Products.Count);
        Assert.Equal(firms, invalid.Firms.Count);
        Assert.Equal(before, File.ReadAllText(storage.ActiveFile));
    }

    internal static ProcurementData Scenario(int count) => new()
    {
        JobTitle = "Deneme alımı",
        Products = Enumerable.Range(1, count).Select(i => new ProductItem { Id = "p" + i, No = i, Name = "Ürün " + i, Unit = "Adet", Quantity = "1" }).ToList(),
        Firms = new() { new Firm { Id = "f1", Name = "Deneme Firması", Offers = Enumerable.Range(1, count).Select(i => new FirmOffer { ProductId = "p" + i, ApproximateUnitPrice = 1m, FinalUnitPrice = 1m }).ToList() } }
    };

    [Fact]
    public void OverCapacityArchiveIsReportedAndSourceFileIsUntouched()
    {
        var storage = new StorageService(_root);
        var data = Scenario(11);
        data.RecordId = "oversize";
        var path = Path.Combine(storage.RecordsFolder, "islem_oversize.json");
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        File.WriteAllText(path, json);
        Assert.Null(storage.LoadRecord(data.RecordId, out var warning));
        Assert.Contains("10 ürün", warning, StringComparison.Ordinal);
        Assert.Empty(storage.ListRecords());
        Assert.Single(storage.LastArchiveWarnings);
        Assert.Equal(json, File.ReadAllText(path));
    }

    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}

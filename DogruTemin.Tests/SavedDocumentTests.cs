using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using DogruTemin.Core.Models;
using DogruTemin.Core.Services;
using DogruTemin.Wpf.Services;

namespace DogruTemin.Tests;

public sealed class SavedDocumentTests
{
    [Fact]
    public void PdfCarriesAllTenProductsAndArchiveTracksCorrectionsAndFileChanges()
    {
        Exception? failure = null;
        var root = Path.Combine(Path.GetTempPath(), "DogruTemin_Output_" + Guid.NewGuid().ToString("N"));
        var thread = new Thread(() =>
        {
            try
            {
                var storage = new StorageService(root);
                var data = ProcurementBoundaryTests.Scenario(10);
                data.Institution.FullName = "Çankaya İlçe Millî Eğitim Müdürlüğü";
                data.Institution.TopAuthority = "Ankara Valiliği";
                data.Institution.AuthorizedName = "Murat Örnek";
                data.Institution.AuthorizedTitle = "Müdür";
                data.ApprovalDocument.DocumentNumber = "2026/30";
                data.ApprovalDocument.AvailableBudgetAmount = "100000";
                data.ApprovalDocument.SubmittedByName = "Elif Örnek";
                data.ApprovalDocument.SubmittedByTitle = "Şef";
                data.InspectionCommission = Enumerable.Range(1, 3).Select(i => new CommissionMember { No = i, Name = "Muayene Üyesi " + i, Title = "Öğretmen" }).ToList();
                data.MarketCommission = Enumerable.Range(1, 3).Select(i => new CommissionMember { No = i, Name = "Piyasa Üyesi " + i, Title = "Öğretmen" }).ToList();
                data.JobTitle = "Kırtasiye alımı — Şube ihtiyaçları";
                data.Firms[0].Offers.ForEach(o => { o.ApproximateUnitPrice = 19.53m; o.FinalUnitPrice = 19.53m; });
                for (var firm = 2; firm <= 5; firm++)
                    data.Firms.Add(new Firm { Id = "f" + firm, Name = "Firma " + firm, Offers = data.Products.Select(p => new FirmOffer { ProductId = p.Id, ApproximateUnitPrice = 19.53m + firm, FinalUnitPrice = 19.53m + firm }).ToList() });
                data.Dates.CostCalculationDate = new DateTime(2026, 10, 20);
                data.Dates.AssignmentDate = new DateTime(2026, 10, 18);
                data.Dates.AssignmentNo = "2026/10";
                data.Dates.ApprovalDate = new DateTime(2026, 10, 19);
                data.Dates.ApprovalNo = "2026/20";
                data.Dates.ApprovalDocumentDate = new DateTime(2026, 10, 21);
                data.Dates.MarketReportDate = new DateTime(2026, 10, 22);
                data.Dates.InspectionReportDate = new DateTime(2026, 10, 23);
                data.Dates.DeliveryDate = new DateTime(2026, 10, 23);
                storage.Save(data);
                var types = new[] { OutputDocumentType.ApproximateRequest, OutputDocumentType.FirmOfferForm, OutputDocumentType.ApproximateCostSheet, OutputDocumentType.ApprovalDocument, OutputDocumentType.MarketResearchReport, OutputDocumentType.InspectionAcceptanceReport };
                var document = OfficialDocumentService.BuildDocument(data, types);
                var service = new SavedDocumentService(storage);
                var path = service.Export(data, document, types);
                Assert.Equal("Güncel PDF ✓", SavedDocumentService.OutputStatus(storage.LoadRecord(data.RecordId, out _)!));
                using (var pdf = UglyToad.PdfPig.PdfDocument.Open(path))
                {
                    Assert.Equal(6, pdf.NumberOfPages);
                    var text = string.Join(" ", pdf.GetPages().Select(p => p.Text));
                    for (var i = 1; i <= 10; i++)
                    {
                        Assert.Contains("Ürün " + i, text, StringComparison.Ordinal);
                        foreach (var pageNumber in new[] { 1, 2, 3, 5, 6 })
                            Assert.Contains("Ürün " + i, pdf.GetPage(pageNumber).Text, StringComparison.Ordinal);
                    }
                    Assert.Contains("195,30", text, StringComparison.Ordinal);
                    Assert.Contains("Deneme Firması", text, StringComparison.Ordinal);
                    Assert.Contains("Çankaya", text, StringComparison.Ordinal);
                    foreach (var expected in new[] { "Murat Örnek", "Elif Örnek", "2026/30", "20.10.2026", "22.10.2026", "23.10.2026", "Muayene Üyesi 3", "Piyasa Üyesi 3" })
                        Assert.Contains(expected, text, StringComparison.Ordinal);
                    Assert.All(pdf.GetPages(), page =>
                    {
                        Assert.InRange(Math.Min(page.Width, page.Height), 594, 596);
                        Assert.InRange(Math.Max(page.Width, page.Height), 841, 843);
                    });
                }
                var acceptance = Environment.GetEnvironmentVariable("DOGRU_TEMIN_ACCEPTANCE_DIR");
                if (!string.IsNullOrWhiteSpace(acceptance))
                { Directory.CreateDirectory(acceptance); File.Copy(path, Path.Combine(acceptance, "ten-products-six-documents.pdf"), true); }
                Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(path)!, "Belge_Kaynak.json")));
                var snapshotPath = Path.Combine(Path.GetDirectoryName(path)!, "Belge_Kaynak.json");
                var snapshotBytes = File.ReadAllBytes(snapshotPath);
                File.AppendAllText(snapshotPath, "changed");
                Assert.Contains("Kaynak değişmiş", SavedDocumentService.OutputStatus(data), StringComparison.Ordinal);
                File.WriteAllBytes(snapshotPath, snapshotBytes);
                File.AppendAllText(path, "changed");
                Assert.Contains("PDF değişmiş", SavedDocumentService.OutputStatus(data), StringComparison.Ordinal);
                service.Export(data, document, types);
                var previousSource = storage.LoadRecord(data.RecordId, out _)!;
                data.JobTitle = "Düzeltilmiş alım";
                var workflow = new ProcurementWorkflow(storage);
                Assert.True(workflow.Save(data, allowDateWarnings: true).RequiresReplacementConfirmation);
                Assert.True(workflow.Save(data, allowDateWarnings: true, confirmReplacement: true).SavedSuccessfully);
                Assert.Contains("yeniden üretin", SavedDocumentService.OutputStatus(data), StringComparison.Ordinal);
                Assert.Throws<InvalidOperationException>(() => service.Export(previousSource, document, types));
                var updated = OfficialDocumentService.BuildDocument(data, types);
                Assert.Equal(path, service.Export(data, updated, types));
                Assert.Single(storage.ListRecords());
                Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.pdf"));
                Assert.Equal("Güncel PDF ✓", SavedDocumentService.OutputStatus(data));
            }
            catch (Exception ex) { failure = new InvalidOperationException(ex.Message + " " + ex.Data["Layout"], ex); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(90)), "PDF kabulü zaman aşımı");
        try { if (failure is not null) throw failure; }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void UnregisteredOrStaleDataCannotGenerateTrackedOutput()
    {
        var root = Path.Combine(Path.GetTempPath(), "DogruTemin_Unregistered_" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new SavedDocumentService(new StorageService(root));
            Assert.Throws<InvalidDataException>(() => service.Export(ProcurementBoundaryTests.Scenario(1), null!, Array.Empty<OutputDocumentType>()));
        }
        finally { Directory.Delete(root, true); }
    }
}

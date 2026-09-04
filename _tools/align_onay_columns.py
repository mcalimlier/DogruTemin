from pathlib import Path

service_path = Path("DogruTemin.Wpf/Services/OfficialDocumentService.cs")
text = service_path.read_text(encoding="utf-8").replace("\r\n", "\n")


def replace_once(old: str, new: str, label: str) -> None:
    global text
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{label}: beklenen 1 eşleşme, bulunan {count}")
    text = text.replace(old, new, 1)


replace_once(
    '''        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(285) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });''',
    '''        var header = new Grid();
        AddApprovalSplitColumns(header);''',
    "Onay Belgesi üst başlık kolonları",
)

replace_once(
    '''        var details = new Grid { Margin = new Thickness(0, 0, 0, 0) };
        details.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(285) });
        details.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });''',
    '''        var details = new Grid { Margin = new Thickness(0, 0, 0, 0) };
        AddApprovalSplitColumns(details);''',
    "Onay Belgesi detay kolonları",
)

replace_once(
    '''        var approval = new Grid();
        approval.ColumnDefinitions.Add(new ColumnDefinition()); approval.ColumnDefinitions.Add(new ColumnDefinition());''',
    '''        var approval = new Grid();
        AddApprovalSplitColumns(approval);''',
    "Onay bölümü kolonları",
)

replace_once(
    '''    private static Grid BuildApprovalCommissionTable(string title, IEnumerable<CommissionMember> source)
    {
        var members = source.Take(3).ToList();
        while (members.Count < 3) members.Add(new CommissionMember());
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });''',
    '''    private static Grid BuildApprovalCommissionTable(string title, IEnumerable<CommissionMember> source)
    {
        var members = source.Take(3).ToList();
        while (members.Count < 3) members.Add(new CommissionMember());
        var grid = new Grid();
        AddApprovalSplitColumns(grid);''',
    "Onay Belgesi komisyon kolonları",
)

helper_anchor = '''    private static Grid BuildApprovalCommissionTable(string title, IEnumerable<CommissionMember> source)'''
helper = '''    // Onay Belgesi'nde ONAY bölümünün merkez çizgisi tüm iki sütunlu bloklar için tek referanstır.
    // Üst başlık, detaylar ve komisyon tabloları aynı %50 / %50 dikey aksı kullanır.
    private static void AddApprovalSplitColumns(Grid grid)
    {
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
    }

'''
if "private static void AddApprovalSplitColumns(Grid grid)" not in text:
    if helper_anchor not in text:
        raise RuntimeError("Ortak Onay Belgesi kolon yardımcısı ekleme noktası bulunamadı")
    text = text.replace(helper_anchor, helper + helper_anchor, 1)

service_path.write_text(text, encoding="utf-8", newline="\n")

# Görsel hizanın gelecekte yanlışlıkla bozulmaması için belge ağacını test et.
test_path = Path("DogruTemin.Tests/ApprovalDocumentLayoutTests.cs")
test_path.write_text(r'''using System.Threading;
using System.Windows.Controls;
using System.Windows.Documents;
using DogruTemin.Core.Models;
using DogruTemin.Wpf.Services;

namespace DogruTemin.Tests;

public class ApprovalDocumentLayoutTests
{
    [Fact]
    public void ApprovalDocumentTwoColumnBlocksShareTheSameCenterAxis()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var document = OfficialDocumentService.BuildDocument(
                    new ProcurementData(),
                    new[] { OutputDocumentType.ApprovalDocument });

                var page = Assert.IsType<FixedPage>(document.Pages[0].Child);
                var root = Assert.IsType<Grid>(page.Children[0]);
                var body = Assert.IsType<StackPanel>(root.Children[0]);

                // Header + details + piyasa komisyonu + muayene komisyonu + ONAY.
                var splitGrids = body.Children
                    .OfType<Grid>()
                    .Where(g => g.ColumnDefinitions.Count == 2)
                    .ToList();

                Assert.Equal(5, splitGrids.Count);
                foreach (var grid in splitGrids)
                {
                    Assert.True(grid.ColumnDefinitions[0].Width.IsStar);
                    Assert.True(grid.ColumnDefinitions[1].Width.IsStar);
                    Assert.Equal(1d, grid.ColumnDefinitions[0].Width.Value, 8);
                    Assert.Equal(1d, grid.ColumnDefinitions[1].Width.Value, 8);
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
            throw failure;
    }
}
''', encoding="utf-8", newline="\n")

print("Onay Belgesi ortak merkez aksı ve regresyon testi hazırlandı.")

using System.Threading;
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

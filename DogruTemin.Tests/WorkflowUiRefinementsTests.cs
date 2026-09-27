using System.Xml.Linq;
using DogruTemin.Core.Models;

namespace DogruTemin.Tests;

public sealed class WorkflowUiRefinementsTests
{
    [Fact]
    public void FirmPhoneConversionsUseOneFixedNationalZero()
    {
        Assert.Equal("3121234567", global::DogruTemin.Wpf.MainWindow.PhoneForEntry("03121234567"));
        Assert.Equal("03121234567", global::DogruTemin.Wpf.MainWindow.PhoneForStorage("3121234567"));
        Assert.Equal(string.Empty, global::DogruTemin.Wpf.MainWindow.PhoneForStorage(""));
        Assert.Equal("3121234567", global::DogruTemin.Wpf.MainWindow.PhoneForEntry(global::DogruTemin.Wpf.MainWindow.PhoneForStorage("3121234567")));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("312123456", false)]
    [InlineData("3121234567", true)]
    [InlineData("(312) 123-45-67", true)]
    [InlineData("31212345678", false)]
    [InlineData("abcdefghij", false)]
    [InlineData("03121234567", false)]
    public void FirmPhoneEntryRequiresExactlyTenDigitsWhenPresent(string input, bool expected) =>
        Assert.Equal(expected, global::DogruTemin.Wpf.MainWindow.IsValidPhoneEntry(input));

    [Fact]
    public void CommissionApprovalFieldsLiveInInstitutionViewAndKeepTheirNames()
    {
        var document = LoadMainWindowXaml();
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var fields = document.Descendants().Where(element =>
            (string?)element.Attribute(x + "Name") is "AssignmentDatePicker" or "AssignmentNoBox").ToArray();

        Assert.Equal(2, fields.Length);
        foreach (var field in fields)
        {
            Assert.Contains(field.Ancestors(), ancestor => (string?)ancestor.Attribute(x + "Name") == "InstitutionView");
            Assert.DoesNotContain(field.Ancestors(), ancestor => (string?)ancestor.Attribute(x + "Name") == "GeneralView");
        }

        Assert.Contains(document.Descendants().Where(element => element.Name.LocalName == "TextBlock"),
            element => (string?)element.Attribute("Text") == "Komisyon Onay Sayısı");
    }

    [Fact]
    public void ApprovalDocumentUsesNoOuterScrollbarAndDatesShowApprovalDateReference()
    {
        var document = LoadMainWindowXaml();
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var approvalView = document.Descendants().Single(element => (string?)element.Attribute(x + "Name") == "ApprovalDocumentView");
        Assert.Equal("Disabled", (string?)approvalView.Attribute("VerticalScrollBarVisibility"));
        Assert.Contains(document.Descendants(), element => (string?)element.Attribute(x + "Name") == "ApprovalDateReferenceText");
        Assert.Contains(document.Descendants(), element => (string?)element.Attribute("Click") == "SaveDates_Click");
    }

    [Fact]
    public void FirmAndOfferPanelsStretchToTheirSharedRow()
    {
        var document = LoadMainWindowXaml();
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        foreach (var name in new[] { "FirmsView", "OffersView" })
        {
            var panel = document.Descendants().Single(element => (string?)element.Attribute(x + "Name") == name);
            Assert.Equal("Stretch", (string?)panel.Attribute("VerticalAlignment"));
        }
    }

    [Fact]
    public void DateSaveUsesCriticalExistingChronologyRules()
    {
        var valid = new ProcessDates
        {
            AssignmentDate = new DateTime(2026, 1, 1),
            CostCalculationDate = new DateTime(2026, 1, 2),
            ApprovalDocumentDate = new DateTime(2026, 1, 3),
            MarketReportDate = new DateTime(2026, 1, 4),
            InspectionReportDate = new DateTime(2026, 1, 4)
        };
        Assert.Empty(global::DogruTemin.Wpf.MainWindow.GetDateSaveBlockingIssues(valid));
        Assert.Empty(global::DogruTemin.Wpf.MainWindow.GetDateSaveBlockingIssues(new ProcessDates()));

        valid.ApprovalDocumentDate = valid.CostCalculationDate;
        Assert.NotEmpty(global::DogruTemin.Wpf.MainWindow.GetDateSaveBlockingIssues(valid));
    }

    [Fact]
    public void AllOutputDocumentsAreSeededSelected()
    {
        var directory = FindRepositoryRoot();
        var shell = File.ReadAllText(Path.Combine(directory, "DogruTemin.Wpf", "MainWindow.Shell.cs"));
        var seed = shell[(shell.IndexOf("private void SeedOutputDocuments", StringComparison.Ordinal))..];
        var initialSelections = System.Text.RegularExpressions.Regex.Matches(seed, "IsSelected\\s*=\\s*true");
        Assert.Equal(6, initialSelections.Count);
    }

    [Fact]
    public void ComboBoxToggleCoversTheWholeControlSurface()
    {
        var app = XDocument.Load(Path.Combine(FindRepositoryRoot(), "DogruTemin.Wpf", "App.xaml"));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var toggle = app.Descendants().Single(element => (string?)element.Attribute(x + "Name") == "DropDownToggle");
        Assert.Equal("Stretch", (string?)toggle.Attribute("HorizontalAlignment"));
    }

    [Fact]
    public void InstitutionSaveButtonFitsItsCaption()
    {
        var document = LoadMainWindowXaml();
        var button = document.Descendants().Single(element => (string?)element.Attribute("Content") == "KURUM BİLGİSİNİ KAYDET");
        Assert.Null(button.Attribute("Width"));
        Assert.True(double.Parse((string)button.Attribute("MinWidth")!, System.Globalization.CultureInfo.InvariantCulture) >= 240);
    }

    [Fact]
    public void ApprovalDateReferenceIsProminentAndAboveDateCard()
    {
        var document = LoadMainWindowXaml();
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var datesView = document.Descendants().Single(element => (string?)element.Attribute(x + "Name") == "DatesView");
        var children = datesView.Elements().ToList();
        var referenceIndex = children.FindIndex(element => element.Descendants().Any(descendant => (string?)descendant.Attribute(x + "Name") == "ApprovalDateReferenceText"));
        var dateCardIndex = children.FindIndex(element => (string?)element.Attribute("Style") == "{StaticResource PremiumPanel}");
        Assert.InRange(referenceIndex, 0, dateCardIndex - 1);

        var reference = document.Descendants().Single(element => (string?)element.Attribute(x + "Name") == "ApprovalDateReferenceText");
        Assert.Equal("15", (string?)reference.Attribute("FontSize"));
        Assert.Equal("SemiBold", (string?)reference.Attribute("FontWeight"));
    }

    [Fact]
    public void OutputCardsHaveMoreVerticalRoomAndReadableText()
    {
        var document = LoadMainWindowXaml();
        var cardGrid = document.Descendants().Single(element => (string?)element.Attribute("MinHeight") == "390");
        Assert.Equal("390", (string?)cardGrid.Attribute("MinHeight"));
        Assert.Equal("Auto", (string?)document.Descendants().Single(element => (string?)element.Attribute("{http://schemas.microsoft.com/winfx/2006/xaml}Name") == "DocumentsDatesView").Attribute("VerticalScrollBarVisibility"));
    }

    private static XDocument LoadMainWindowXaml()
    {
        var root = FindRepositoryRoot();
        return XDocument.Load(Path.Combine(root, "DogruTemin.Wpf", "MainWindow.xaml"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DogruTemin.Wpf", "MainWindow.xaml"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from the test output directory.");
    }
}

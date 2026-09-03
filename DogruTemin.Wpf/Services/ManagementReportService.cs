using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Security;
using DogruTemin.Core.Models;
using DogruTemin.Core.Services;
using DogruTemin.Wpf.Models;

namespace DogruTemin.Wpf.Services;

public sealed class ManagementReportService
{
    private const double A4LandscapeWidth = 1122.5197;
    private const double A4LandscapeHeight = 793.7008;
    private const double Cm = 37.7952755906;
    private const double Margin = 0.75 * Cm;
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private readonly ProcurementEngine _engine = new();

    public ManagementReportSnapshot BuildSnapshot(IEnumerable<ProcurementData> source, int? selectedYear, string? institutionName = null)
    {
        var all = source.ToList();
        var missingDateCount = all.Count(x => !x.Dates.ApprovalDate.HasValue);
        var records = all
            .Where(x => x.Dates.ApprovalDate.HasValue)
            .Where(x => !selectedYear.HasValue || x.Dates.ApprovalDate!.Value.Year == selectedYear.Value)
            .OrderBy(x => x.Dates.ApprovalDate)
            .ToList();

        var snapshot = new ManagementReportSnapshot
        {
            PeriodLabel = selectedYear.HasValue ? selectedYear.Value.ToString(CultureInfo.InvariantCulture) : "Tüm Yıllar",
            InstitutionName = string.IsNullOrWhiteSpace(institutionName)
                ? records.Select(x => x.Institution.FullName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "DoğruTemin"
                : institutionName.Trim(),
            GeneratedAt = DateTime.Now,
            IncludedOperationCount = records.Count,
            ExcludedMissingDateCount = missingDateCount
        };

        var monthCounts = new int[13];
        var monthTotals = new decimal[13];

        foreach (var data in records)
        {
            var calc = _engine.Calculate(data);
            var date = data.Dates.ApprovalDate!.Value;
            monthCounts[date.Month]++;
            if (calc.MarketResultTotal > 0m) monthTotals[date.Month] += calc.MarketResultTotal;

            var complete = calc.ApproximateCostTotal > 0m && calc.MarketResultTotal > 0m;
            var savings = complete ? calc.ApproximateCostTotal - calc.MarketResultTotal : 0m;
            if (complete)
            {
                snapshot.FinancialCompleteCount++;
                snapshot.ApproximateTotal += calc.ApproximateCostTotal;
                snapshot.FinalTotal += calc.MarketResultTotal;
                snapshot.SavingsTotal += savings;
            }

            snapshot.Operations.Add(new ManagementReportOperationRow
            {
                RecordId = data.RecordId,
                ApprovalDate = date,
                OperationNumber = string.IsNullOrWhiteSpace(data.OperationNumber) ? "—" : data.OperationNumber.Trim(),
                JobTitle = string.IsNullOrWhiteSpace(data.JobTitle) ? "Başlıksız işlem" : data.JobTitle.Trim(),
                ApproximateTotal = calc.ApproximateCostTotal,
                FinalTotal = calc.MarketResultTotal,
                Savings = savings,
                FinancialComplete = complete
            });
        }

        snapshot.SavingsRate = snapshot.ApproximateTotal > 0m ? snapshot.SavingsTotal / snapshot.ApproximateTotal : 0m;
        snapshot.AverageFinalTotal = snapshot.FinancialCompleteCount > 0 ? snapshot.FinalTotal / snapshot.FinancialCompleteCount : 0m;

        for (var month = 1; month <= 12; month++)
        {
            snapshot.Months.Add(new ManagementReportMonthRow
            {
                MonthNumber = month,
                Month = Tr.DateTimeFormat.GetMonthName(month),
                OperationCount = monthCounts[month],
                FinalTotal = monthTotals[month]
            });
        }

        return snapshot;
    }

    public FixedDocument BuildPdf(ManagementReportSnapshot snapshot)
    {
        var document = new FixedDocument();
        var page = NewPage();
        var root = NewPageRoot(page);
        var body = new StackPanel();
        root.Children.Add(body);

        body.Children.Add(Text("DOĞRUTEMİN", 11.5, true, TextAlignment.Left, "Bahnschrift SemiBold"));
        body.Children.Add(Text("YÖNETİM RAPORU", 24, true, TextAlignment.Center, "Bahnschrift SemiBold", new Thickness(0, 1, 0, 2)));
        body.Children.Add(Text(snapshot.InstitutionName, Fit(snapshot.InstitutionName, 13.5, 78, 10.5), true, TextAlignment.Center, "Cambria"));
        body.Children.Add(Text($"Rapor Dönemi: {snapshot.PeriodLabel}     |     Oluşturma: {snapshot.GeneratedAt:dd.MM.yyyy HH:mm}", 10.5, false, TextAlignment.Center, "Cambria", new Thickness(0, 2, 0, 10)));

        body.Children.Add(BuildKpiRow(snapshot));

        var contentGrid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });

        var monthly = BuildMonthlyPanel(snapshot);
        Grid.SetColumn(monthly, 0);
        contentGrid.Children.Add(monthly);

        var operations = BuildTopOperationsPanel(snapshot);
        Grid.SetColumn(operations, 2);
        contentGrid.Children.Add(operations);
        body.Children.Add(contentGrid);

        var noteParts = new List<string>
        {
            $"Rapora dahil işlem: {snapshot.IncludedOperationCount}",
            $"Finansal hesabı tamamlanmış işlem: {snapshot.FinancialCompleteCount}"
        };
        if (snapshot.ExcludedMissingDateCount > 0)
            noteParts.Add($"Harcama Belgesi tarihi olmadığı için rapor dışı: {snapshot.ExcludedMissingDateCount}");
        body.Children.Add(Text(string.Join("   •   ", noteParts), 9.8, false, TextAlignment.Left, "Cambria", new Thickness(0, 10, 0, 0)));
        body.Children.Add(Text("Not: Tasarruf göstergeleri yalnız yaklaşık maliyet ve nihai tutarı birlikte oluşmuş işlemlerden hesaplanır.", 9.3, false, TextAlignment.Left, "Cambria", new Thickness(0, 3, 0, 0)));

        var pc = new PageContent();
        ((System.Windows.Markup.IAddChild)pc).AddChild(page);
        document.Pages.Add(pc);
        return document;
    }

    public void SaveExcel(ManagementReportSnapshot snapshot, string path)
    {
        var folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(folder)) Directory.CreateDirectory(folder);
        if (File.Exists(path)) File.Delete(path);

        using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create, false, Encoding.UTF8);

        AddText(zip, "[Content_Types].xml", ContentTypesXml());
        AddText(zip, "_rels/.rels", RootRelsXml());
        AddText(zip, "xl/workbook.xml", WorkbookXml());
        AddText(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml());
        AddText(zip, "xl/styles.xml", StylesXml());
        AddText(zip, "xl/worksheets/sheet1.xml", SummarySheetXml(snapshot));
        AddText(zip, "xl/worksheets/sheet2.xml", MonthlySheetXml(snapshot));
        AddText(zip, "xl/worksheets/sheet3.xml", DetailSheetXml(snapshot));
        AddText(zip, "docProps/core.xml", CorePropsXml(snapshot));
        AddText(zip, "docProps/app.xml", AppPropsXml());
    }

    private static FrameworkElement BuildKpiRow(ManagementReportSnapshot s)
    {
        var grid = new UniformGrid { Columns = 5 };
        grid.Children.Add(Kpi("İŞLEM SAYISI", s.IncludedOperationCount.ToString(Tr), "rapora dahil"));
        grid.Children.Add(Kpi("YAKLAŞIK MALİYET", Money(s.ApproximateTotal), "KDV hariç"));
        grid.Children.Add(Kpi("NİHAİ TOPLAM", Money(s.FinalTotal), "kazanan teklifler"));
        grid.Children.Add(Kpi("TOPLAM TASARRUF", Money(s.SavingsTotal), "%" + (s.SavingsRate * 100m).ToString("N2", Tr)));
        grid.Children.Add(Kpi("ORTALAMA İŞLEM", Money(s.AverageFinalTotal), "nihai tutar / işlem"));
        return grid;
    }

    private static Border Kpi(string title, string value, string sub)
    {
        var panel = new StackPanel();
        panel.Children.Add(Text(title, 8.7, true, TextAlignment.Left, "Bahnschrift SemiBold"));
        panel.Children.Add(Text(value, Fit(value, 12.7, 22, 9.2), true, TextAlignment.Left, "Cambria", new Thickness(0, 3, 0, 0)));
        panel.Children.Add(Text(sub, 8.3, false, TextAlignment.Left, "Cambria", new Thickness(0, 1, 0, 0)));
        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(196, 208, 220)),
            BorderThickness = new Thickness(0.8),
            Background = new SolidColorBrush(Color.FromRgb(247, 250, 253)),
            Padding = new Thickness(10, 7, 10, 7),
            Margin = new Thickness(2, 0, 2, 0),
            Child = panel
        };
    }

    private static Border BuildMonthlyPanel(ManagementReportSnapshot s)
    {
        var panel = new StackPanel();
        panel.Children.Add(Text("AYLIK DAĞILIM", 12.5, true, TextAlignment.Left, "Bahnschrift SemiBold", new Thickness(0, 0, 0, 5)));
        var table = new Grid();
        table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(115) });
        table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        AddRow(table, 24);
        AddCell(table, 0, 0, "AY", true, 9.2);
        AddCell(table, 0, 1, "İŞLEM", true, 9.2);
        AddCell(table, 0, 2, "NİHAİ TOPLAM", true, 9.2);
        for (var i = 0; i < s.Months.Count; i++)
        {
            AddRow(table, 21);
            var m = s.Months[i];
            AddCell(table, i + 1, 0, m.Month, false, 8.9, TextAlignment.Left);
            AddCell(table, i + 1, 1, m.OperationCount.ToString(Tr), false, 8.9);
            AddCell(table, i + 1, 2, m.FinalTotal > 0 ? Money(m.FinalTotal) : "—", false, 8.9, TextAlignment.Right);
        }
        panel.Children.Add(table);
        return new Border { BorderBrush = new SolidColorBrush(Color.FromRgb(211, 220, 229)), BorderThickness = new Thickness(0.8), Padding = new Thickness(10), Child = panel };
    }

    private static Border BuildTopOperationsPanel(ManagementReportSnapshot s)
    {
        var panel = new StackPanel();
        panel.Children.Add(Text("EN YÜKSEK NİHAİ TUTARLI İŞLEMLER", 12.5, true, TextAlignment.Left, "Bahnschrift SemiBold", new Thickness(0, 0, 0, 5)));
        var table = new Grid();
        table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
        table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(115) });
        AddRow(table, 24);
        AddCell(table, 0, 0, "TARİH", true, 9.0);
        AddCell(table, 0, 1, "İŞ / ALIM", true, 9.0);
        AddCell(table, 0, 2, "NİHAİ", true, 9.0);
        AddCell(table, 0, 3, "TASARRUF", true, 9.0);
        var rows = s.Operations.Where(x => x.FinancialComplete).OrderByDescending(x => x.FinalTotal).Take(10).ToList();
        if (rows.Count == 0)
        {
            AddRow(table, 28);
            AddCell(table, 1, 0, "Finansal hesabı tamamlanmış işlem bulunmuyor.", false, 9.2, TextAlignment.Left, 4);
        }
        else
        {
            for (var i = 0; i < rows.Count; i++)
            {
                AddRow(table, 24);
                var r = rows[i];
                AddCell(table, i + 1, 0, r.ApprovalDate.ToString("dd.MM.yy"), false, 8.6);
                AddCell(table, i + 1, 1, r.JobTitle, false, Fit(r.JobTitle, 8.6, 38, 7.0), TextAlignment.Left);
                AddCell(table, i + 1, 2, Money(r.FinalTotal), false, 8.6, TextAlignment.Right);
                AddCell(table, i + 1, 3, SignedMoney(r.Savings), false, 8.6, TextAlignment.Right);
            }
        }
        panel.Children.Add(table);
        return new Border { BorderBrush = new SolidColorBrush(Color.FromRgb(211, 220, 229)), BorderThickness = new Thickness(0.8), Padding = new Thickness(10), Child = panel };
    }

    private static FixedPage NewPage() => new() { Width = A4LandscapeWidth, Height = A4LandscapeHeight, Background = Brushes.White };

    private static Grid NewPageRoot(FixedPage page)
    {
        var root = new Grid { Width = A4LandscapeWidth - 2 * Margin, Height = A4LandscapeHeight - 2 * Margin };
        FixedPage.SetLeft(root, Margin);
        FixedPage.SetTop(root, Margin);
        page.Children.Add(root);
        return root;
    }

    private static TextBlock Text(string? value, double size, bool bold, TextAlignment alignment, string font, Thickness? margin = null) => new()
    {
        Text = value ?? string.Empty,
        FontFamily = new FontFamily(font),
        FontSize = size,
        FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
        Foreground = Brushes.Black,
        TextAlignment = alignment,
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = margin ?? new Thickness(0)
    };

    private static void AddRow(Grid grid, double height) => grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(height) });

    private static void AddCell(Grid grid, int row, int col, string text, bool bold, double fontSize, TextAlignment alignment = TextAlignment.Center, int colSpan = 1)
    {
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(195, 205, 215)),
            BorderThickness = new Thickness(0.6),
            Padding = new Thickness(4, 1, 4, 1),
            Child = Text(text, fontSize, bold, alignment, bold ? "Bahnschrift SemiBold" : "Cambria")
        };
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        if (colSpan > 1) Grid.SetColumnSpan(border, colSpan);
        grid.Children.Add(border);
    }

    private static double Fit(string? value, double baseSize, int softChars, double minimum)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= softChars) return baseSize;
        var ratio = Math.Sqrt(softChars / (double)value.Length);
        return Math.Max(minimum, Math.Round(baseSize * ratio, 1));
    }

    private static string Money(decimal value) => value.ToString("N2", Tr) + " ₺";
    private static string SignedMoney(decimal value) => (value >= 0 ? "+" : string.Empty) + Money(value);

    private static void AddText(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string ContentTypesXml() => """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
  <Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
  <Override PartName="/xl/worksheets/sheet3.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
</Types>
""";

    private static string RootRelsXml() => """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>
""";

    private static string WorkbookXml() => """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <workbookPr date1904="0"/>
  <bookViews><workbookView activeTab="0"/></bookViews>
  <sheets>
    <sheet name="Yönetim Özeti" sheetId="1" r:id="rId1"/>
    <sheet name="Aylık Dağılım" sheetId="2" r:id="rId2"/>
    <sheet name="İşlem Detayı" sheetId="3" r:id="rId3"/>
  </sheets>
  <calcPr calcId="191029" fullCalcOnLoad="1" forceFullCalc="1"/>
</workbook>
""";

    private static string WorkbookRelsXml() => """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet3.xml"/>
  <Relationship Id="rId4" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>
""";

    private static string StylesXml() => """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <numFmts count="3">
    <numFmt numFmtId="164" formatCode="#,##0.00 &quot;₺&quot;"/>
    <numFmt numFmtId="165" formatCode="0.00%"/>
    <numFmt numFmtId="166" formatCode="dd.mm.yyyy"/>
  </numFmts>
  <fonts count="5">
    <font><sz val="11"/><name val="Cambria"/></font>
    <font><b/><sz val="18"/><color rgb="FF123B6C"/><name val="Bahnschrift SemiBold"/></font>
    <font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Bahnschrift SemiBold"/></font>
    <font><b/><sz val="11"/><color rgb="FF123B6C"/><name val="Bahnschrift SemiBold"/></font>
    <font><b/><sz val="13"/><color rgb="FF123B6C"/><name val="Cambria"/></font>
  </fonts>
  <fills count="4">
    <fill><patternFill patternType="none"/></fill>
    <fill><patternFill patternType="gray125"/></fill>
    <fill><patternFill patternType="solid"><fgColor rgb="FF123B6C"/><bgColor indexed="64"/></patternFill></fill>
    <fill><patternFill patternType="solid"><fgColor rgb="FFEAF3FF"/><bgColor indexed="64"/></patternFill></fill>
  </fills>
  <borders count="2">
    <border><left/><right/><top/><bottom/><diagonal/></border>
    <border><left style="thin"><color rgb="FFD5E0EA"/></left><right style="thin"><color rgb="FFD5E0EA"/></right><top style="thin"><color rgb="FFD5E0EA"/></top><bottom style="thin"><color rgb="FFD5E0EA"/></bottom><diagonal/></border>
  </borders>
  <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
  <cellXfs count="9">
    <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
    <xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
    <xf numFmtId="0" fontId="2" fillId="2" borderId="1" xfId="0" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
    <xf numFmtId="0" fontId="3" fillId="3" borderId="1" xfId="0" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
    <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
    <xf numFmtId="164" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf>
    <xf numFmtId="165" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf>
    <xf numFmtId="166" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
    <xf numFmtId="0" fontId="4" fillId="3" borderId="1" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf>
  </cellXfs>
  <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
</styleSheet>
""";

    private static string SummarySheetXml(ManagementReportSnapshot s)
    {
        var detailLast = Math.Max(2, s.Operations.Count + 1);
        var sb = SheetStart(new[] { 5.0, 26.0, 22.0, 22.0, 22.0, 22.0 });
        sb.Append(Row(1, CellString("A1", "DOĞRUTEMİN - YÖNETİM RAPORU", 1)));
        sb.Append(Row(2, CellString("A2", s.InstitutionName, 8)));
        sb.Append(Row(3, CellString("A3", $"Rapor Dönemi: {s.PeriodLabel} | Oluşturma: {s.GeneratedAt:dd.MM.yyyy HH:mm}", 4)));
        sb.Append(Row(5, CellString("A5", "Gösterge", 2), CellString("B5", "Değer", 2), CellString("C5", "Açıklama", 2)));
        sb.Append(Row(6, CellString("A6", "İşlem Sayısı", 3), FormulaNumber("B6", $"COUNTA('İşlem Detayı'!A2:A{detailLast})", s.IncludedOperationCount, 4), CellString("C6", "Harcama Belgesi tarihine göre rapora dahil", 4)));
        sb.Append(Row(7, CellString("A7", "Toplam Yaklaşık Maliyet", 3), FormulaNumber("B7", $"SUMIFS('İşlem Detayı'!E2:E{detailLast},'İşlem Detayı'!H2:H{detailLast},\"Tamam\")", s.ApproximateTotal, 5), CellString("C7", "KDV hariç", 4)));
        sb.Append(Row(8, CellString("A8", "Toplam Nihai Tutar", 3), FormulaNumber("B8", $"SUMIFS('İşlem Detayı'!F2:F{detailLast},'İşlem Detayı'!H2:H{detailLast},\"Tamam\")", s.FinalTotal, 5), CellString("C8", "Kazanan teklifler", 4)));
        sb.Append(Row(9, CellString("A9", "Toplam Tasarruf", 3), FormulaNumber("B9", "B7-B8", s.SavingsTotal, 5), CellString("C9", "Yaklaşık maliyet - nihai toplam", 4)));
        sb.Append(Row(10, CellString("A10", "Tasarruf Oranı", 3), FormulaNumber("B10", "IF(B7>0,B9/B7,0)", s.SavingsRate, 6), CellString("C10", "Yalnız finansal hesabı tamamlanmış işlemler", 4)));
        sb.Append(Row(11, CellString("A11", "Ortalama İşlem", 3), FormulaNumber("B11", $"IF(COUNTIF('İşlem Detayı'!H2:H{detailLast},\"Tamam\")>0,B8/COUNTIF('İşlem Detayı'!H2:H{detailLast},\"Tamam\"),0)", s.AverageFinalTotal, 5), CellString("C11", "Nihai tutar / finansal tamamlanmış işlem", 4)));
        sb.Append(Row(13, CellString("A13", "RAPOR NOTLARI", 2)));
        sb.Append(Row(14, CellString("A14", $"Finansal hesabı tamamlanmış işlem: {s.FinancialCompleteCount}", 4)));
        sb.Append(Row(15, CellString("A15", $"Harcama Belgesi tarihi olmadığı için rapor dışı kayıt: {s.ExcludedMissingDateCount}", 4)));
        sb.Append(Row(16, CellString("A16", "Tasarruf göstergeleri yalnız yaklaşık maliyet ve nihai tutarı birlikte oluşmuş işlemlerden hesaplanır.", 4)));
        sb.Append("</sheetData><mergeCells count=\"7\"><mergeCell ref=\"A1:F1\"/><mergeCell ref=\"A2:F2\"/><mergeCell ref=\"A3:F3\"/><mergeCell ref=\"A13:F13\"/><mergeCell ref=\"A14:F14\"/><mergeCell ref=\"A15:F15\"/><mergeCell ref=\"A16:F16\"/></mergeCells>");
        sb.Append(PageSettingsLandscape());
        sb.Append("</worksheet>");
        return sb.ToString();
    }

    private static string MonthlySheetXml(ManagementReportSnapshot s)
    {
        var sb = SheetStart(new[] { 14.0, 15.0, 24.0 });
        sb.Append(Row(1, CellString("A1", "AY", 2), CellString("B1", "İŞLEM SAYISI", 2), CellString("C1", "NİHAİ TOPLAM", 2)));
        for (var i = 0; i < s.Months.Count; i++)
        {
            var row = i + 2;
            var m = s.Months[i];
            sb.Append(Row(row, CellString($"A{row}", m.Month, 4), CellNumber($"B{row}", m.OperationCount, 4), CellNumber($"C{row}", m.FinalTotal, 5)));
        }
        sb.Append("</sheetData><autoFilter ref=\"A1:C13\"/>");
        sb.Append(PageSettingsLandscape());
        sb.Append("</worksheet>");
        return sb.ToString();
    }

    private static string DetailSheetXml(ManagementReportSnapshot s)
    {
        var sb = SheetStart(new[] { 7.0, 13.0, 18.0, 52.0, 22.0, 22.0, 22.0, 17.0 });
        sb.Append(Row(1,
            CellString("A1", "NO", 2), CellString("B1", "TARİH", 2), CellString("C1", "İŞLEM NO", 2), CellString("D1", "İŞ / ALIM ADI", 2),
            CellString("E1", "YAKLAŞIK MALİYET", 2), CellString("F1", "NİHAİ TOPLAM", 2), CellString("G1", "TASARRUF", 2), CellString("H1", "DURUM", 2)));

        for (var i = 0; i < s.Operations.Count; i++)
        {
            var row = i + 2;
            var op = s.Operations[i];
            var dateSerial = op.ApprovalDate.Date.ToOADate();
            var savingsFormula = $"E{row}-F{row}";
            sb.Append(Row(row,
                CellNumber($"A{row}", i + 1, 4),
                CellNumber($"B{row}", (decimal)dateSerial, 7),
                CellString($"C{row}", op.OperationNumber, 4),
                CellString($"D{row}", op.JobTitle, 4),
                op.ApproximateTotal > 0m ? CellNumber($"E{row}", op.ApproximateTotal, 5) : CellBlank($"E{row}", 5),
                op.FinalTotal > 0m ? CellNumber($"F{row}", op.FinalTotal, 5) : CellBlank($"F{row}", 5),
                op.FinancialComplete ? FormulaNumber($"G{row}", savingsFormula, op.Savings, 5) : CellBlank($"G{row}", 5),
                CellString($"H{row}", op.FinancialComplete ? "Tamam" : "Finansal veri eksik", 4)));
        }

        var last = Math.Max(1, s.Operations.Count + 1);
        sb.Append($"</sheetData><autoFilter ref=\"A1:H{last}\"/>");
        sb.Append(PageSettingsLandscape());
        sb.Append("</worksheet>");
        return sb.ToString();
    }

    private static StringBuilder SheetStart(IReadOnlyList<double> widths)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        sb.Append("<sheetViews><sheetView workbookViewId=\"0\" showGridLines=\"0\"/></sheetViews><cols>");
        for (var i = 0; i < widths.Count; i++) sb.Append($"<col min=\"{i + 1}\" max=\"{i + 1}\" width=\"{widths[i].ToString("0.##", CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");
        sb.Append("</cols><sheetData>");
        return sb;
    }

    private static string Row(int row, params string[] cells) => $"<row r=\"{row}\">{string.Concat(cells)}</row>";

    private static string CellString(string reference, string? value, int style)
    {
        var safe = XmlEscape(value ?? string.Empty);
        return $"<c r=\"{reference}\" s=\"{style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{safe}</t></is></c>";
    }

    private static string CellNumber(string reference, decimal value, int style) =>
        $"<c r=\"{reference}\" s=\"{style}\"><v>{value.ToString(CultureInfo.InvariantCulture)}</v></c>";

    private static string FormulaNumber(string reference, string formula, decimal cachedValue, int style) =>
        $"<c r=\"{reference}\" s=\"{style}\"><f>{XmlEscape(formula)}</f><v>{cachedValue.ToString(CultureInfo.InvariantCulture)}</v></c>";

    private static string CellBlank(string reference, int style) => $"<c r=\"{reference}\" s=\"{style}\"/>";
    private static string XmlEscape(string value) => SecurityElement.Escape(value) ?? string.Empty;

    private static string PageSettingsLandscape() => "<pageMargins left=\"0.3\" right=\"0.3\" top=\"0.4\" bottom=\"0.4\" header=\"0.2\" footer=\"0.2\"/><pageSetup orientation=\"landscape\" paperSize=\"9\" fitToWidth=\"1\" fitToHeight=\"0\"/>";

    private static string CorePropsXml(ManagementReportSnapshot s)
    {
        var when = s.GeneratedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        return $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><dc:title>DoğruTemin Yönetim Raporu - {XmlEscape(s.PeriodLabel)}</dc:title><dc:creator>DoğruTemin</dc:creator><dcterms:created xsi:type=\"dcterms:W3CDTF\">{when}</dcterms:created><dcterms:modified xsi:type=\"dcterms:W3CDTF\">{when}</dcterms:modified></cp:coreProperties>";
    }

    private static string AppPropsXml() => """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>DoğruTemin</Application>
</Properties>
""";
}

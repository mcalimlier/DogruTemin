using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DogruTemin.Core.Models;
using DogruTemin.Core.Services;
using OutputDocument = DogruTemin.Wpf.Models.OutputDocument;

namespace DogruTemin.Wpf.Services;

public sealed class OfficialDocumentService
{
    public const double A4PortraitWidth = 793.7008;
    public const double A4PortraitHeight = 1122.5197;
    public const double A4LandscapeWidth = A4PortraitHeight;
    public const double A4LandscapeHeight = A4PortraitWidth;
    private const double Cm = 37.7952755906;
    private const double Margin = 0.75 * Cm;

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly Brush Black = Brushes.Black;
    private static readonly Thickness ThinBorder = new(0.75);
    private static readonly Thickness MediumBorder = new(1.05);


    public static FixedDocument BuildDocument(ProcurementData data, IEnumerable<OutputDocumentType> documentTypes)
    {
        var doc = new FixedDocument();
        foreach (var type in documentTypes)
        {
            FixedPage page = type switch
            {
                OutputDocumentType.ApproximateRequest => BuildApproximateRequestPage(data),
                OutputDocumentType.ApproximateCostSheet => BuildApproximateCostSheetPage(data),
                OutputDocumentType.ApprovalDocument => BuildApprovalDocumentPage(data),
                OutputDocumentType.FirmOfferForm => BuildFirmOfferFormPage(data),
                OutputDocumentType.MarketResearchReport => BuildMarketResearchPage(data),
                OutputDocumentType.InspectionAcceptanceReport => BuildInspectionAcceptancePage(data),
                _ => throw new NotSupportedException("Bu belge resmî belge motorunda uygulanmadı.")
            };
            var pc = new PageContent();
            ((System.Windows.Markup.IAddChild)pc).AddChild(page);
            doc.Pages.Add(pc);
        }
        return doc;
    }

    public static bool IsImplemented(OutputDocumentType type) =>
        type is OutputDocumentType.ApproximateRequest
            or OutputDocumentType.ApproximateCostSheet
            or OutputDocumentType.FirmOfferForm
            or OutputDocumentType.MarketResearchReport
            or OutputDocumentType.InspectionAcceptanceReport;

    private static FixedPage BuildApproximateRequestPage(ProcurementData data)
    {
        var products = ActiveProducts(data);
        var compact = products.Count >= 6;
        var page = NewPage(A4PortraitWidth, A4PortraitHeight);
        var root = NewPageRoot(page, A4PortraitWidth, A4PortraitHeight);
        var body = new StackPanel { Orientation = Orientation.Vertical };
        root.Children.Add(body);

        AddInstitutionHeader(body, data, compact);

        body.Children.Add(T("Alım ve Yetkilendirilen Görevlilere İlişkin", compact ? 14.0 : 16.0, true, TextAlignment.Left,
            new Thickness(0, compact ? 10 : 16, 0, 2), "Cambria"));
        body.Children.Add(T($"Komisyon Görevlendirme Tarih ve Sayısı: {DateText(data.Dates.AssignmentDate)} tarih ve {Safe(data.Dates.AssignmentNo)} sayılı Müdürlük Makam Olur'u.",
            compact ? 13.5 : 15.5, false, TextAlignment.Left, new Thickness(0, 0, 0, compact ? 18 : 28), "Cambria"));

        body.Children.Add(T("YAKLAŞIK MALİYET TESPİTİNE YÖNELİK PİYASA FİYAT ARAŞTIRMASI",
            compact ? 15.2 : 16.8, true, TextAlignment.Center, new Thickness(0, 0, 0, compact ? 24 : 34), "Cambria"));
        body.Children.Add(T("İLGİLİ KİŞİ VEYA FİRMAYA", compact ? 15.0 : 16.5, true, TextAlignment.Center,
            new Thickness(0, 0, 0, compact ? 16 : 22), "Cambria"));

        var requestText = "İdaremizce satın alınması / yaptırılması düşünülen aşağıda cinsi, miktarı, özellikleri ve diğer şartları yazılı mal, hizmet ya da yapım işlerinin 4734 sayılı Kamu İhale Kanununun 22/d maddesi gereği yaklaşık maliyetinin tespit edilmesinde değerlendirilmek üzere KDV hariç olmak üzere piyasada satış fiyatlarının bildirilmesini rica ederim.";
        body.Children.Add(PlainText(requestText, compact ? 13.8 : 15.7, TextAlignment.Justify,
            new Thickness(0, 0, 0, compact ? 16 : 24), compact ? 74 : 94, "Cambria"));

        body.Children.Add(BuildSimpleSignatureRow(data.MarketCommission, compact));
        body.Children.Add(BuildInstitutionContactBlock(data, compact, new Thickness(0, compact ? 16 : 24, 0, compact ? 10 : 14)));
        body.Children.Add(BuildApproximateRequestItemsTable(products, compact));

        body.Children.Add(T("YALNIZ/KDV HARİÇ …………………………………………………………………………………………………",
            compact ? 12.2 : 13.8, false, TextAlignment.Left, new Thickness(0, compact ? 12 : 18, 0, 0), "Cambria"));
        body.Children.Add(T("NOT: Teklifler KDV hariç olarak verilecektir.", compact ? 12.0 : 13.5, true, TextAlignment.Left,
            new Thickness(0, compact ? 18 : 28, 0, 0), "Cambria"));

        var stamp = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, compact ? 82 : 106, 10, 0)
        };
        stamp.Children.Add(T("Firma", compact ? 12.2 : 13.8, false, TextAlignment.Center, null, "Cambria"));
        stamp.Children.Add(T("İmza/Kaşe", compact ? 12.2 : 13.8, false, TextAlignment.Center, null, "Cambria"));
        body.Children.Add(stamp);
        return page;
    }

    private static FixedPage BuildApproximateCostSheetPage(ProcurementData data)
    {
        var products = ActiveProducts(data);
        var firms = ActiveFirms(data);
        var calc = ProcurementEngine.Calculate(data);
        var compact = products.Count >= 6 || firms.Count >= 4;

        var page = NewPage(A4LandscapeWidth, A4LandscapeHeight);
        var root = NewPageRoot(page, A4LandscapeWidth, A4LandscapeHeight);
        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(layout);

        var body = new StackPanel { Orientation = Orientation.Vertical };
        Grid.SetRow(body, 0);
        layout.Children.Add(body);

        body.Children.Add(T("YAKLAŞIK MALİYET HESAP CETVELİ", compact ? 16.5 : 18.0, true, TextAlignment.Center,
            new Thickness(0, 0, 0, compact ? 8 : 12)));

        var info = new Grid { Margin = new Thickness(0, 0, 0, compact ? 8 : 12) };
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 120 : 135) });
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 135 : 150) });
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 125 : 145) });
        AddApproxInfoRow(info, 0, "İDARENİN ADI :", data.Institution.FullName, "Düzenleme Tarihi:", DateText(data.Dates.CostCalculationDate), compact);
        AddApproxInfoRow(info, 1, "İŞİN ADI :", data.JobTitle, string.Empty, string.Empty, compact);
        body.Children.Add(info);

        body.Children.Add(BuildApproximateCostTable(products, firms, calc, compact));
        body.Children.Add(T("Belgeyi Düzenleyenler;", compact ? 10.4 : 11.5, false, TextAlignment.Left,
            new Thickness(0, compact ? 10 : 16, 0, compact ? 6 : 10)));
        body.Children.Add(BuildLabeledSignatureRow(data.MarketCommission, compact));

        var attachment = T($"EK: {firms.Count} Adet Firma Teklifi", compact ? 9.8 : 10.8, false, TextAlignment.Left,
            new Thickness(0, 0, 0, 0));
        attachment.HorizontalAlignment = HorizontalAlignment.Left;
        attachment.VerticalAlignment = VerticalAlignment.Bottom;
        Grid.SetRow(attachment, 1);
        layout.Children.Add(attachment);
        return page;
    }

    private static FixedPage BuildFirmOfferFormPage(ProcurementData data)
    {
        var products = ActiveProducts(data);
        var compact = products.Count >= 6;
        var page = NewPage(A4PortraitWidth, A4PortraitHeight);
        var root = NewPageRoot(page, A4PortraitWidth, A4PortraitHeight);
        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(layout);

        var body = new StackPanel { Orientation = Orientation.Vertical };
        Grid.SetRow(body, 0);
        layout.Children.Add(body);

        AddInstitutionHeader(body, data, compact);

        var meta = new Grid { Margin = new Thickness(0, compact ? 10 : 16, 0, compact ? 18 : 26) };
        meta.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 190 : 220) });
        meta.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        meta.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, MinHeight = compact ? 24 : 28 });
        meta.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, MinHeight = compact ? 24 : 28 });

        var hbLabel = T("Harcama Belgesi Tarih ve Sayısı:", compact ? 13.2 : 15.0, true, TextAlignment.Left, null, "Cambria");
        var hbText = $"{DateText(data.Dates.ApprovalDate)} tarih ve HYS - {Safe(data.Dates.ApprovalNo)} sayılı Harcama Talimatı Onay Belgesi";
        var hbValue = T(hbText,
            FitFontSize(hbText, compact ? 13.0 : 14.8, compact ? 62 : 72, compact ? 10.8 : 11.8),
            false, TextAlignment.Left, null, "Cambria");
        var konuLabel = T("Konu:", compact ? 13.2 : 15.0, true, TextAlignment.Left, null, "Cambria");
        var konuValue = T("Teklifiniz", compact ? 13.0 : 14.8, false, TextAlignment.Left, null, "Cambria");
        Grid.SetRow(hbLabel, 0); Grid.SetColumn(hbLabel, 0);
        Grid.SetRow(hbValue, 0); Grid.SetColumn(hbValue, 1);
        Grid.SetRow(konuLabel, 1); Grid.SetColumn(konuLabel, 0);
        Grid.SetRow(konuValue, 1); Grid.SetColumn(konuValue, 1);
        meta.Children.Add(hbLabel); meta.Children.Add(hbValue); meta.Children.Add(konuLabel); meta.Children.Add(konuValue);
        body.Children.Add(meta);

        body.Children.Add(T("İLGİLİ KURUMA / FİRMAYA", compact ? 15.0 : 16.5, true, TextAlignment.Center,
            new Thickness(0, 0, 0, compact ? 2 : 4), "Cambria"));
        body.Children.Add(T(data.Institution.Province.ToUpper(Tr), compact ? 13.5 : 15.0, false, TextAlignment.Right,
            new Thickness(0, 0, 0, compact ? 12 : 18), "Cambria"));

        var offerText = $"Kurumumuzun ihtiyacı olan aşağıda cinsi ve vasıfları belirtilen malzemeler 4734 sayılı Kamu İhale Kanununun 22/d maddesi gereğince doğrudan temin usulü ile alınacaktır. İlgilendiğiniz takdirde KDV hariç fiyat teklifinizi en geç {DocumentConstants.OfferDeadlinePlaceholder} tarihi mesai bitimine kadar göndermenizi, teklifinizde teslimat süresinin de bildirilmesini rica ederim.";
        body.Children.Add(PlainText(offerText, compact ? 13.8 : 15.7, TextAlignment.Justify,
            new Thickness(0, 0, 0, compact ? 14 : 22), compact ? 78 : 98, "Cambria"));

        body.Children.Add(BuildSimpleSignatureRow(data.MarketCommission, compact));
        body.Children.Add(BuildInstitutionContactBlock(data, compact, new Thickness(0, compact ? 16 : 24, 0, compact ? 10 : 14)));
        body.Children.Add(BuildFirmOfferItemsTable(products, compact));

        var accept = "Yukarıda belirtilen ve İdarenizce satın alınacak olan malların / hizmetlerin cinsi, özellikleri, miktarı ve diğer şartlarını okudum. KDV hariç toplam ........................................................... (..............................) Türk Lirası (TL) bedelle vermeyi kabul ve taahhüt ederim.";
        body.Children.Add(PlainText(accept, compact ? 12.6 : 14.2, TextAlignment.Justify,
            new Thickness(0, compact ? 12 : 18, 0, compact ? 8 : 12), compact ? 58 : 74, "Cambria"));

        var signer = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, compact ? 76 : 98, 5, compact ? 18 : 26)
        };
        signer.Children.Add(T("Teklif Eden", compact ? 12.8 : 14.4, false, TextAlignment.Center, null, "Cambria"));
        signer.Children.Add(T("Adı Soyadı - Ticaret Unvanı - İmza - Mühür", compact ? 11.6 : 13.0, false, TextAlignment.Center, null, "Cambria"));
        body.Children.Add(signer);

        var note = T("NOT: Teklifiniz kapalı zarfta ve firma kaşesi zarfa basılı olarak teslim edilecektir.",
            compact ? 11.8 : 13.2, true, TextAlignment.Left, new Thickness(0, 0, 0, 0), "Cambria");
        note.HorizontalAlignment = HorizontalAlignment.Left;
        note.VerticalAlignment = VerticalAlignment.Bottom;
        Grid.SetRow(note, 1);
        layout.Children.Add(note);
        return page;
    }

    private static FixedPage BuildApprovalDocumentPage(ProcurementData data)
    {
        var products = ActiveProducts(data);
        var calc = ProcurementEngine.Calculate(data);
        var info = data.ApprovalDocument ?? new ApprovalDocumentInfo();
        var page = NewPage(A4PortraitWidth, A4PortraitHeight);
        var root = NewPageRoot(page, A4PortraitWidth, A4PortraitHeight);
        var body = new StackPanel { Orientation = Orientation.Vertical };
        root.Children.Add(body);

        body.Children.Add(T("ONAY BELGESİ", 15.0, true, TextAlignment.Center, new Thickness(0, 0, 0, 6)));

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(285) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        AddRow(header, 28); AddCell(header, 0, 0, "ALIM YAPAN İDARENİN ADI", true, 9.4, TextAlignment.Left); AddCell(header, 0, 1, data.Institution.FullName, false, 9.4, TextAlignment.Center, 1, 1, "Cambria", 60, 7.4);
        AddRow(header, 28); AddCell(header, 1, 0, "BELGE TARİH VE SAYISI", true, 9.4, TextAlignment.Left); AddCell(header, 1, 1, $"{DateText(data.Dates.ApprovalDocumentDate)}  /  {Safe(info.DocumentNumber)}", false, 9.4, TextAlignment.Center);
        body.Children.Add(header);

        var details = new Grid { Margin = new Thickness(0, 0, 0, 0) };
        details.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(285) });
        details.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        AddRow(details, 30); AddCell(details, 0, 0, "DOĞRUDAN TEMİN İLE İLGİLİ BİLGİLER", true, 10.2, TextAlignment.Center, 2, 1);
        var row = 1;
        void Detail(string label, string value, double height = 27)
        {
            AddRow(details, height); AddCell(details, row, 0, label, true, 8.8, TextAlignment.Left); AddCell(details, row, 1, value, false, 8.8, TextAlignment.Left, 1, 1, "Cambria", 88, 7.0); row++;
        }
        Detail("İŞİN TANIMI", data.JobTitle, 36);
        Detail("İŞİN NİTELİĞİ", data.JobTitle, 36);
        Detail("İŞİN MİKTARI", ApprovalQuantitySummary(products), 34);
        Detail("Yaklaşık Maliyet (KDV Hariç)", NumberParser.Money(calc.ApproximateCostTotal));
        Detail("Kullanılabilir Ödenek Tutarı", ApprovalMoneyText(info.AvailableBudgetAmount));
        Detail("Yatırım Proje Numarası (varsa)", info.InvestmentProjectNumber);
        Detail("Bütçe Tertibi (varsa)", info.BudgetCode);
        Detail("Avans Verilecekse Şartları", info.AdvanceConditions);
        Detail("İhale Usulü", string.IsNullOrWhiteSpace(info.TenderProcedure) ? "4734 S.K. 22/d Kapsamında" : info.TenderProcedure);
        Detail("İlanın Şekli ve Adedi", string.IsNullOrWhiteSpace(info.AnnouncementMethod) ? "İlansız" : info.AnnouncementMethod);
        Detail("Ön Yeterlik/İhale Dokümanı Satış Bedeli", info.PrequalificationDocumentSalePrice);
        Detail("Fiyat Farkı Ödenecekse Dayanağı", string.IsNullOrWhiteSpace(info.PriceDifferenceBasis) ? "Ödenmeyecektir" : info.PriceDifferenceBasis, 32);
        body.Children.Add(details);

        var notes = new Grid(); notes.ColumnDefinitions.Add(new ColumnDefinition());
        AddRow(notes, 28); AddCell(notes, 0, 0, "AÇIKLAMALAR", true, 9.8, TextAlignment.Center);
        AddRow(notes, 55); AddCell(notes, 1, 0, string.IsNullOrWhiteSpace(info.Notes) ? data.JobTitle : info.Notes, false, 8.8, TextAlignment.Left, 1, 1, "Cambria", 120, 7.0);
        body.Children.Add(notes);

        body.Children.Add(BuildApprovalCommissionTable("PİYASA ARAŞTIRMA GÖREVLİLERİ", data.MarketCommission));
        body.Children.Add(BuildApprovalCommissionTable("MUAYENE VE KABUL KOMİSYONU GÖREVLİLERİ", data.InspectionCommission));

        var approval = new Grid();
        approval.ColumnDefinitions.Add(new ColumnDefinition()); approval.ColumnDefinitions.Add(new ColumnDefinition());
        AddRow(approval, 28); AddCell(approval, 0, 0, "ONAY", true, 10.0, TextAlignment.Center, 2, 1);
        AddRow(approval, 112);
        var left = $"Yukarıda belirtilen mal/malzeme/hizmetin alınması hususunu onaylarınıza arz ederim\n\n{DateText(data.Dates.ApprovalDocumentDate)}\n\n{info.SubmittedByName}\n{info.SubmittedByTitle}";
        var rightTitle = !string.IsNullOrWhiteSpace(data.Institution.AuthorizedTitle) ? data.Institution.AuthorizedTitle : data.Institution.AuthorizedDuty;
        var right = $"Uygundur\n{DateText(data.Dates.ApprovalDocumentDate)}\nHarcama Yetkilisi\n\n{data.Institution.AuthorizedName}\n{rightTitle}";
        AddCell(approval, 1, 0, left, false, 8.8, TextAlignment.Center, 1, 1, "Cambria", 95, 7.2);
        AddCell(approval, 1, 1, right, false, 8.8, TextAlignment.Center, 1, 1, "Cambria", 80, 7.2);
        body.Children.Add(approval);
        return page;
    }

    private static Grid BuildApprovalCommissionTable(string title, IEnumerable<CommissionMember> source)
    {
        var members = source.Take(3).ToList();
        while (members.Count < 3) members.Add(new CommissionMember());
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
        AddRow(grid, 28); AddCell(grid, 0, 0, title, true, 9.6, TextAlignment.Center, 2, 1);
        AddRow(grid, 24); AddCell(grid, 1, 0, "Adı Soyadı", true, 8.6); AddCell(grid, 1, 1, "Unvanı", true, 8.6);
        for (var i = 0; i < 3; i++)
        {
            AddRow(grid, 25);
            AddCell(grid, i + 2, 0, $"{i + 1}. {members[i].Name}", false, 8.3, TextAlignment.Left);
            AddCell(grid, i + 2, 1, $"{CommissionRole(i)} ({members[i].Title})", false, 8.3, TextAlignment.Left);
        }
        return grid;
    }

    private static string ApprovalQuantitySummary(IReadOnlyCollection<ProductItem> products) =>
        products.Count == 0 ? string.Empty : string.Join("; ", products.Select(p => $"{p.Quantity} {p.Unit} {p.Name}"));

    private static string ApprovalMoneyText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return NumberParser.TryPositiveDecimal(value, out var parsed) ? NumberParser.Money(parsed) : value.Trim();
    }
    // Eski Excel formu görsel referanstır; yeni iş kuralı ise alımın tamamında tek kazanan firmadır.
    private static FixedPage BuildMarketResearchPage(ProcurementData data)
    {
        var products = ActiveProducts(data);
        var firms = ActiveFirms(data);
        var calc = ProcurementEngine.Calculate(data);
        var winner = firms.FirstOrDefault(f => f.Id == calc.WinningFirmId);
        var compact = products.Count >= 6 || firms.Count >= 5;
        var dense = products.Count >= 7;

        var page = NewPage(A4LandscapeWidth, A4LandscapeHeight);
        var root = NewPageRoot(page, A4LandscapeWidth, A4LandscapeHeight);
        var body = new StackPanel { Orientation = Orientation.Vertical };
        root.Children.Add(body);

        body.Children.Add(T("PİYASA FİYAT ARAŞTIRMASI TUTANAĞI", dense ? 15.2 : compact ? 16.6 : 18.2, true, TextAlignment.Center,
            new Thickness(0, 0, 0, dense ? 1 : compact ? 3 : 6)));

        var info = new Grid { Margin = new Thickness(0, 0, 0, dense ? 1 : compact ? 3 : 6) };
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 330 : 355) });
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        AddInfoRow(info, 0, "İdarenin Adı", data.Institution.FullName, dense ? 15 : compact ? 18 : 22, dense ? 8.9 : compact ? 10.2 : 12.7);
        AddInfoRow(info, 1, "Yapılan İş / Mal / Hizmetin Adı, Niteliği", data.JobTitle, dense ? 17 : compact ? 20 : 24, dense ? 8.9 : compact ? 10.2 : 12.7);
        AddInfoRow(info, 2, "Alım ve Yetkilendirilen Görevlilere İlişkin", string.Empty, dense ? 13 : compact ? 16 : 20, dense ? 8.6 : compact ? 9.8 : 12.2);
        AddInfoRow(info, 3, "Harcama Belgesi Tarih ve Sayısı", ApprovalLine(data), dense ? 18 : compact ? 22 : 26, dense ? 8.6 : compact ? 9.8 : 12.2);
        body.Children.Add(info);

        body.Children.Add(BuildAllFirmOffersTable(products, firms, compact, dense));
        body.Children.Add(new Border { Height = dense ? 2 : compact ? 4 : 7, Background = Brushes.Transparent });
        body.Children.Add(BuildWinningFirmTable(products, winner, calc, compact, dense));

        var footer = new Border
        {
            BorderBrush = Black,
            BorderThickness = new Thickness(1.0, 0, 1.0, 1.0),
            Padding = new Thickness(dense ? 7 : compact ? 10 : 18, dense ? 3 : compact ? 5 : 8,
                dense ? 7 : compact ? 10 : 18, dense ? 3 : compact ? 5 : 8)
        };
        var footerPanel = new StackPanel();
        footer.Child = footerPanel;

        var reportDate = DateText(data.Dates.MarketReportDate);
        var legal = $"4734 sayılı Kamu İhale Kanununun 22 nci Maddesi uyarınca doğrudan temin usulüyle yapılacak alımlara ilişkin yapılan piyasa araştırmasında firmalarca/kişilerce teklif edilen fiyatlar tarafımca/tarafımızca değerlendirilerek yukarıda adı ve adresi belirtilen firmadan alım yapılması uygun görülmüştür. {reportDate}";
        footerPanel.Children.Add(PlainText(legal, dense ? 8.3 : compact ? 9.8 : 12.2, TextAlignment.Justify,
            new Thickness(0, 0, 0, dense ? 1 : compact ? 2 : 5), dense ? 27 : compact ? 35 : 50));

        footerPanel.Children.Add(T("Piyasa Fiyat Araştırması Görevlisi / Görevlileri", dense ? 8.9 : compact ? 10.5 : 13.0, false,
            TextAlignment.Center, new Thickness(0, 0, 0, dense ? 0 : compact ? 1 : 4)));
        footerPanel.Children.Add(BuildMarketSignatureRow(data.MarketCommission, compact, dense));

        footerPanel.Children.Add(T(
            "* Piyasa fiyat araştırması yapılacak kişi / firma, yer sayısına ihale yetkilisi karar verebilecektir.\n" +
            "* Piyasa fiyat araştırması için görevlendirilecek personelin sayısına ihale yetkilisi karar verebilecektir.",
            dense ? 7.2 : compact ? 8.7 : 10.8, false, TextAlignment.Left,
            new Thickness(dense ? 6 : compact ? 10 : 22, dense ? 0 : 1, 0, dense ? 0 : compact ? 1 : 3)));

        var approval = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, dense ? 0 : compact ? 0 : 2, 0, 0) };
        approval.Children.Add(T("UYGUNDUR", dense ? 8.7 : compact ? 10.3 : 12.8, false, TextAlignment.Center));
        approval.Children.Add(T(DateText(data.Dates.MarketReportDate), dense ? 8.0 : compact ? 9.6 : 12.0, false, TextAlignment.Center));
        approval.Children.Add(T(data.Institution.AuthorizedName,
            FitFontSize(data.Institution.AuthorizedName, dense ? 8.0 : compact ? 9.6 : 12.0, 30, dense ? 6.8 : compact ? 7.8 : 9.2),
            false, TextAlignment.Center));
        if (!string.IsNullOrWhiteSpace(data.Institution.AuthorizedDuty))
            approval.Children.Add(T(data.Institution.AuthorizedDuty, dense ? 7.7 : compact ? 9.2 : 11.5, false, TextAlignment.Center));
        if (!string.IsNullOrWhiteSpace(data.Institution.AuthorizedTitle))
            approval.Children.Add(T(data.Institution.AuthorizedTitle,
                FitFontSize(data.Institution.AuthorizedTitle, dense ? 7.7 : compact ? 9.2 : 11.5, 34, dense ? 6.6 : compact ? 7.6 : 9.0),
                false, TextAlignment.Center));
        footerPanel.Children.Add(approval);

        body.Children.Add(footer);
        return page;
    }

    private static FixedPage BuildInspectionAcceptancePage(ProcurementData data)
    {
        var products = ActiveProducts(data);
        var firms = ActiveFirms(data);
        var calc = ProcurementEngine.Calculate(data);
        var winner = firms.FirstOrDefault(f => f.Id == calc.WinningFirmId);
        var compact = products.Count >= 7;

        var page = NewPage(A4PortraitWidth, A4PortraitHeight);
        var root = NewPageRoot(page, A4PortraitWidth, A4PortraitHeight);
        var body = new StackPanel { Orientation = Orientation.Vertical };
        root.Children.Add(body);

        body.Children.Add(T("T.C.", 16.0, true, TextAlignment.Center));
        var inspectionAuthority = data.Institution.TopAuthority.ToUpper(Tr);
        body.Children.Add(T(inspectionAuthority, FitFontSize(inspectionAuthority, 16.0, 48, 12.2), true, TextAlignment.Center));
        body.Children.Add(T(data.Institution.ShortName, FitFontSize(data.Institution.ShortName, 15.4, 46, 11.8), true, TextAlignment.Center));
        body.Children.Add(T(DateText(data.Dates.InspectionReportDate), 14.2, false, TextAlignment.Right,
            new Thickness(0, compact ? 22 : 36, 0, compact ? 18 : 28)));
        body.Children.Add(T("TESLİM ALMA, MUAYENE VE KABUL TUTANAĞI", 18.4, true, TextAlignment.Center,
            new Thickness(0, 0, 0, compact ? 18 : 28)));

        var winnerName = winner is null ? "........................................................" : FirmDisplayName(winner).Replace("\n", " ");
        var paragraph = $"{DateText(data.Dates.AssignmentDate)} tarih ve {Safe(data.Dates.AssignmentNo)} sayılı Müdürlük Makam Olur'u ve {DateText(data.Dates.ApprovalDate)} tarih ve HYS - {Safe(data.Dates.ApprovalNo)} sayılı Harcama Talimatı Onay Belgesi ile 4734 S.K. 22/d kapsamında {Safe(data.JobTitle)} doğrudan temin yoluyla satın alınmış olup, yüklenici firma \"{winnerName}\" tarafından, {DateText(data.Dates.DeliveryDate)} tarihinde eksiksiz olarak komisyonumuzca teslim alınmıştır.";
        var paragraphSize = FitFontSize(paragraph, compact ? 13.2 : 14.6, compact ? 430 : 500, compact ? 10.6 : 11.6);
        body.Children.Add(PlainText(paragraph, paragraphSize, TextAlignment.Justify,
            new Thickness(0, 0, 0, compact ? 18 : 28), compact ? 125 : 160));

        body.Children.Add(BuildInspectionItemsTable(products, compact));
        body.Children.Add(T("MUAYENE, TESLİM ALMA VE KABUL KOMİSYONU", 15.6, true, TextAlignment.Center,
            new Thickness(0, compact ? 24 : 40, 0, compact ? 16 : 24)));
        body.Children.Add(BuildInspectionSignatureTable(data.InspectionCommission));
        return page;
    }

    private static void AddInstitutionHeader(StackPanel body, ProcurementData data, bool compact)
    {
        body.Children.Add(T("T.C.", compact ? 14.2 : 16.0, true, TextAlignment.Center, null, "Cambria"));
        var authorityText = data.Institution.TopAuthority.ToUpper(Tr);
        body.Children.Add(T(authorityText,
            FitFontSize(authorityText, compact ? 14.2 : 16.0, 48, compact ? 11.2 : 12.2),
            true, TextAlignment.Center, null, "Cambria"));
        body.Children.Add(T(data.Institution.ShortName,
            FitFontSize(data.Institution.ShortName, compact ? 13.8 : 15.6, 46, compact ? 11.0 : 12.0),
            true, TextAlignment.Center, null, "Cambria"));
    }

    private static StackPanel BuildInstitutionContactBlock(ProcurementData data, bool compact, Thickness margin)
    {
        var panel = new StackPanel { Margin = margin };
        panel.Children.Add(T("Adres :", compact ? 12.0 : 13.5, false, TextAlignment.Left, null, "Cambria"));
        panel.Children.Add(T(data.Institution.FullName, compact ? 12.0 : 13.5, false, TextAlignment.Left, null, "Cambria"));
        panel.Children.Add(T(data.Institution.Address, compact ? 11.8 : 13.2, false, TextAlignment.Left, null, "Cambria"));
        panel.Children.Add(T("Tel: " + data.Institution.Phone, compact ? 11.8 : 13.2, false, TextAlignment.Left, null, "Cambria"));
        panel.Children.Add(T("E-Posta: " + data.Institution.Email, compact ? 11.8 : 13.2, false, TextAlignment.Left, null, "Cambria"));
        return panel;
    }

    private static Grid BuildSimpleSignatureRow(IEnumerable<CommissionMember> source, bool compact)
    {
        var members = source.Take(3).ToList();
        while (members.Count < 3) members.Add(new CommissionMember());
        var grid = new Grid { Margin = new Thickness(0, 0, 0, compact ? 4 : 8) };
        for (var i = 0; i < 3; i++) grid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var i = 0; i < 3; i++)
        {
            var p = new StackPanel { Margin = new Thickness(8, 0, 8, 0) };
            p.Children.Add(T(members[i].Name,
                FitFontSize(members[i].Name, compact ? 12.5 : 14.2, 26, compact ? 9.8 : 10.8),
                false, TextAlignment.Center, null, "Cambria"));
            p.Children.Add(T(i == 0 ? "Komisyon Başkanı" : "Üye", compact ? 11.8 : 13.4, false, TextAlignment.Center, null, "Cambria"));
            Grid.SetColumn(p, i);
            grid.Children.Add(p);
        }
        return grid;
    }

    private static Grid BuildLabeledSignatureRow(IEnumerable<CommissionMember> source, bool compact)
    {
        var members = source.Take(3).ToList();
        while (members.Count < 3) members.Add(new CommissionMember());
        var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
        for (var i = 0; i < 3; i++) grid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var i = 0; i < 3; i++)
        {
            var p = new StackPanel
            {
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = compact ? 210 : 245
            };
            var nameLine = "Adı Soyadı : " + members[i].Name;
            p.Children.Add(T(nameLine, FitFontSize(nameLine, compact ? 9.2 : 10.3, 30, compact ? 7.8 : 8.4), false, TextAlignment.Center));
            p.Children.Add(T("Unvanı: " + CommissionRole(i), compact ? 9.2 : 10.3, false, TextAlignment.Center));
            p.Children.Add(T("İmzası :", compact ? 9.2 : 10.3, false, TextAlignment.Center, new Thickness(0, compact ? 3 : 5, 0, 0)));
            Grid.SetColumn(p, i);
            grid.Children.Add(p);
        }
        return grid;
    }

    private static Grid BuildApproximateRequestItemsTable(List<ProductItem> products, bool compact)
    {
        var grid = NewOfferGrid(compact);

        AddRow(grid, compact ? 25 : 30);
        AddRow(grid, compact ? 38 : 46);
        AddCell(grid, 0, 0, "YAPILACAK İŞİN-HİZMETİN-MALIN VEYA MALZEMENİN", false,
            compact ? 12.2 : 13.8, TextAlignment.Center, 6, 1, "Cambria");
        AddCell(grid, 1, 0, "Sıra\nNo", false, compact ? 11.8 : 13.2, TextAlignment.Center, 1, 1, "Cambria");
        AddCell(grid, 1, 1, "CİNSİ-ÖZELLİĞİ", false, compact ? 12.0 : 13.5, TextAlignment.Center, 1, 1, "Cambria");
        AddCell(grid, 1, 2, "Birimi", false, compact ? 11.8 : 13.2, TextAlignment.Center, 1, 1, "Cambria");
        AddCell(grid, 1, 3, "Miktarı", false, compact ? 11.8 : 13.2, TextAlignment.Center, 1, 1, "Cambria");
        AddCell(grid, 1, 4, "Birim Fiyatı\nKDV HARİÇ", false, compact ? 11.3 : 12.7, TextAlignment.Center, 1, 1, "Cambria");
        AddCell(grid, 1, 5, "TUTARI", false, compact ? 11.8 : 13.2, TextAlignment.Center, 1, 1, "Cambria");

        for (var i = 0; i < products.Count; i++)
        {
            AddRow(grid, compact ? 27 : 34);
            var p = products[i];
            AddCell(grid, i + 2, 0, (i + 1).ToString(CultureInfo.InvariantCulture), false, compact ? 11.2 : 12.6, TextAlignment.Center, 1, 1, "Cambria");
            AddCell(grid, i + 2, 1, ProductDisplay(p), false, compact ? 11.2 : 12.6, TextAlignment.Center, 1, 1, "Cambria",
                compact ? 42 : 52, compact ? 8.8 : 9.8);
            AddCell(grid, i + 2, 2, p.Unit, false, compact ? 11.2 : 12.6, TextAlignment.Center, 1, 1, "Cambria");
            AddCell(grid, i + 2, 3, p.Quantity, false, compact ? 11.2 : 12.6, TextAlignment.Center, 1, 1, "Cambria");
            AddCell(grid, i + 2, 4, string.Empty, false, compact ? 11.2 : 12.6, TextAlignment.Center, 1, 1, "Cambria");
            AddCell(grid, i + 2, 5, string.Empty, false, compact ? 11.2 : 12.6, TextAlignment.Center, 1, 1, "Cambria");
        }

        var totalRow = products.Count + 2;
        AddRow(grid, compact ? 25 : 30);
        AddCell(grid, totalRow, 0, string.Empty, false, 10, TextAlignment.Center, 4, 1, "Cambria");
        AddCell(grid, totalRow, 4, "TOPLAM", false, compact ? 11.6 : 13.0, TextAlignment.Center, 1, 1, "Cambria");
        AddCell(grid, totalRow, 5, string.Empty, false, compact ? 11.6 : 13.0, TextAlignment.Center, 1, 1, "Cambria");
        return grid;
    }

    private static Grid BuildFirmOfferItemsTable(List<ProductItem> products, bool compact)
    {
        var grid = NewOfferGrid(compact);

        AddRow(grid, compact ? 25 : 30);
        AddRow(grid, compact ? 38 : 46);
        AddCell(grid, 0, 0, "YAPILACAK İŞİN-HİZMETİN-MALIN VEYA MALZEMENİN", false,
            compact ? 12.0 : 13.6, TextAlignment.Center, 4, 1, "Cambria");
        AddCell(grid, 0, 4, "Teklif Edilen (KDV Hariç)", false,
            compact ? 11.8 : 13.2, TextAlignment.Center, 2, 1, "Cambria");
        AddCell(grid, 1, 0, "Sıra\nNo", false, compact ? 11.6 : 13.0, TextAlignment.Center, 1, 1, "Cambria");
        AddCell(grid, 1, 1, "Cinsi/Özelliği", false, compact ? 11.8 : 13.2, TextAlignment.Center, 1, 1, "Cambria");
        AddCell(grid, 1, 2, "Birimi", false, compact ? 11.6 : 13.0, TextAlignment.Center, 1, 1, "Cambria");
        AddCell(grid, 1, 3, "Miktarı", false, compact ? 11.6 : 13.0, TextAlignment.Center, 1, 1, "Cambria");
        AddCell(grid, 1, 4, "Birim Fiyatı", false, compact ? 11.4 : 12.8, TextAlignment.Center, 1, 1, "Cambria");
        AddCell(grid, 1, 5, "Toplam Fiyatı", false, compact ? 11.4 : 12.8, TextAlignment.Center, 1, 1, "Cambria");

        for (var i = 0; i < products.Count; i++)
        {
            AddRow(grid, compact ? 27 : 34);
            var p = products[i];
            AddCell(grid, i + 2, 0, (i + 1).ToString(CultureInfo.InvariantCulture), false, compact ? 11.1 : 12.5, TextAlignment.Center, 1, 1, "Cambria");
            AddCell(grid, i + 2, 1, ProductDisplay(p), false, compact ? 11.1 : 12.5, TextAlignment.Center, 1, 1, "Cambria",
                compact ? 42 : 52, compact ? 8.8 : 9.8);
            AddCell(grid, i + 2, 2, p.Unit, false, compact ? 11.1 : 12.5, TextAlignment.Center, 1, 1, "Cambria");
            AddCell(grid, i + 2, 3, p.Quantity, false, compact ? 11.1 : 12.5, TextAlignment.Center, 1, 1, "Cambria");
            AddCell(grid, i + 2, 4, string.Empty, false, compact ? 11.1 : 12.5, TextAlignment.Center, 1, 1, "Cambria");
            AddCell(grid, i + 2, 5, string.Empty, false, compact ? 11.1 : 12.5, TextAlignment.Center, 1, 1, "Cambria");
        }

        var totalRow = products.Count + 2;
        AddRow(grid, compact ? 25 : 30);
        AddCell(grid, totalRow, 0, "KDV Hariç Teklif Edilen Toplam Fiyat:", false,
            compact ? 11.2 : 12.6, TextAlignment.Right, 5, 1, "Cambria");
        AddCell(grid, totalRow, 5, string.Empty, false, compact ? 11.2 : 12.6, TextAlignment.Center, 1, 1, "Cambria");

        AddRow(grid, compact ? 25 : 30);
        AddCell(grid, totalRow + 1, 0, "Yazıyla:", false,
            compact ? 11.2 : 12.6, TextAlignment.Right, 2, 1, "Cambria");
        AddCell(grid, totalRow + 1, 2, string.Empty, false,
            compact ? 11.2 : 12.6, TextAlignment.Left, 4, 1, "Cambria");
        return grid;
    }

    private static Grid NewOfferGrid(bool compact)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 72 : 82) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 72 : 82) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 100 : 110) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 100 : 110) });
        return grid;
    }

    private static void AddApproxInfoRow(Grid grid, int row, string label1, string value1, string label2, string value2, bool compact)
    {
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, MinHeight = compact ? 24 : 28 });
        var l1 = T(label1, compact ? 9.7 : 10.8, true);
        var v1 = T(value1, FitFontSize(value1, compact ? 9.7 : 10.8, compact ? 85 : 100, compact ? 8.0 : 9.0));
        Grid.SetRow(l1, row); Grid.SetColumn(l1, 0);
        Grid.SetRow(v1, row); Grid.SetColumn(v1, 1);
        grid.Children.Add(l1); grid.Children.Add(v1);
        if (!string.IsNullOrWhiteSpace(label2))
        {
            var l2 = T(label2, compact ? 9.7 : 10.8, true, TextAlignment.Right);
            var v2 = T(value2, compact ? 9.7 : 10.8, false, TextAlignment.Center);
            Grid.SetRow(l2, row); Grid.SetColumn(l2, 2);
            Grid.SetRow(v2, row); Grid.SetColumn(v2, 3);
            grid.Children.Add(l2); grid.Children.Add(v2);
        }
    }

    private static Grid BuildApproximateCostTable(List<ProductItem> products, List<Firm> firms, CalculationResult calc, bool compact)
    {
        const int firmSlots = 5;
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 215 : 235) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 62 : 68) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 60 : 66) });

        var used = 38 + (compact ? 215 : 235) + (compact ? 62 : 68) + (compact ? 60 : 66)
            + (compact ? 98 : 108) + (compact ? 110 : 120);
        var remaining = A4LandscapeWidth - (2 * Margin) - used;
        var firmWidth = remaining / firmSlots;
        for (var i = 0; i < firmSlots; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(firmWidth) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 98 : 108) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 110 : 120) });

        // Excel örneğindeki iki katmanlı başlık: üst bantta firma adları, alt bantta sütun adları.
        AddRow(grid, compact ? 42 : 52);
        AddCell(grid, 0, 0, "FİRMA ADI", true, compact ? 9.0 : 10.0, TextAlignment.Center, 4, 1);
        for (var f = 0; f < firmSlots; f++)
        {
            var name = f < firms.Count ? FirmDisplayName(firms[f]) : string.Empty;
            AddCell(grid, 0, f + 4, name, false, compact ? 8.0 : 9.0,
                TextAlignment.Center, 1, 1, "Cambria", compact ? 22 : 26, compact ? 6.6 : 7.0);
        }
        AddCell(grid, 0, 4 + firmSlots, string.Empty, false, 8.0);
        AddCell(grid, 0, 5 + firmSlots, string.Empty, false, 8.0);

        AddRow(grid, compact ? 43 : 52);
        AddCell(grid, 1, 0, "S.No", true, compact ? 8.8 : 9.8);
        AddCell(grid, 1, 1, "Mal / Hizmetin Adı", true, compact ? 9.0 : 10.0);
        AddCell(grid, 1, 2, "Mal / Hizmetin\nMiktarı", true, compact ? 8.5 : 9.5);
        AddCell(grid, 1, 3, "Ölçü\nBirimi", true, compact ? 8.5 : 9.5);
        for (var f = 0; f < firmSlots; f++)
            AddCell(grid, 1, f + 4, "Mal/Hizmet\nBirim Fiyat Teklifi", true, compact ? 7.8 : 8.8);
        AddCell(grid, 1, 4 + firmSlots, "Ortalama\nBirim Fiyat", true, compact ? 8.2 : 9.2);
        AddCell(grid, 1, 5 + firmSlots, "Toplam\nMaliyet Bedeli", true, compact ? 8.2 : 9.2);

        for (var i = 0; i < products.Count; i++)
        {
            AddRow(grid, compact ? 31 : 38);
            var p = products[i];
            var pc = calc.Products.FirstOrDefault(x => x.ProductId == p.Id);
            AddCell(grid, i + 2, 0, (i + 1).ToString(CultureInfo.InvariantCulture), true, compact ? 8.5 : 9.5);
            AddCell(grid, i + 2, 1, ProductDisplay(p), false, compact ? 8.5 : 9.5, TextAlignment.Center,
                1, 1, "Cambria", compact ? 34 : 42, compact ? 6.8 : 7.4);
            AddCell(grid, i + 2, 2, p.Quantity, false, compact ? 8.5 : 9.5);
            AddCell(grid, i + 2, 3, p.Unit, false, compact ? 8.5 : 9.5);
            for (var f = 0; f < firmSlots; f++)
            {
                decimal? price = null;
                if (f < firms.Count)
                    price = firms[f].Offers.FirstOrDefault(o => o.ProductId == p.Id)?.ApproximateUnitPrice;
                AddCell(grid, i + 2, f + 4, Price(price), false, compact ? 7.9 : 8.9);
            }
            AddCell(grid, i + 2, 4 + firmSlots, Price(pc?.ApproximateAverageUnitPrice), false, compact ? 8.0 : 9.0);
            AddCell(grid, i + 2, 5 + firmSlots, Price(pc?.ApproximateLineTotal), false, compact ? 8.0 : 9.0);
        }

        var totalRow = products.Count + 2;
        AddRow(grid, compact ? 26 : 31);
        AddCell(grid, totalRow, 0, string.Empty, false, 8.0, TextAlignment.Center, 4 + firmSlots, 1);
        AddCell(grid, totalRow, 4 + firmSlots, "TOPLAM", true, compact ? 9.0 : 10.0);
        AddCell(grid, totalRow, 5 + firmSlots, Price(calc.ApproximateCostTotal), true, compact ? 9.0 : 10.0);
        return grid;
    }

    private static Grid BuildAllFirmOffersTable(List<ProductItem> products, List<Firm> firms, bool compact, bool dense)
    {
        const int firmSlots = 5;
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 270 : 300) });
        var used = 34 + (compact ? 270 : 300);
        var remaining = A4LandscapeWidth - (2 * Margin) - used;
        var firmWidth = remaining / firmSlots;
        for (var i = 0; i < firmSlots; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(firmWidth) });

        AddRow(grid, dense ? 18 : compact ? 22 : 27);
        AddRow(grid, dense ? 24 : compact ? 30 : 38);
        AddCell(grid, 0, 0, string.Empty, true, dense ? 8.4 : compact ? 10.0 : 12.2, TextAlignment.Center, 1, 2);
        AddCell(grid, 0, 1, "Mal / Hizmet / Yapım İşi", true, dense ? 8.8 : compact ? 10.5 : 13.0, TextAlignment.Center, 1, 2);
        AddCell(grid, 0, 2, "Kişi / Firmalar ve KDV Hariç Tüm Ürünler İçin Verilen Nihai Toplam Teklifleri", true,
            dense ? 8.3 : compact ? 9.8 : 12.3, TextAlignment.Center, firmSlots, 1);

        for (var i = 0; i < firmSlots; i++)
        {
            var name = i < firms.Count ? FirmDisplayName(firms[i]) : string.Empty;
            AddCell(grid, 1, i + 2, name, false, dense ? 7.4 : compact ? 8.9 : 10.9,
                TextAlignment.Center, 1, 1, "Cambria", dense ? 20 : compact ? 23 : 27, dense ? 6.2 : compact ? 6.8 : 7.6);
        }

        var rowHeight = dense ? 18 : compact ? 23 : 30;
        for (var r = 0; r < products.Count; r++)
        {
            AddRow(grid, rowHeight);
            var p = products[r];
            AddCell(grid, r + 2, 0, (r + 1).ToString(CultureInfo.InvariantCulture), false, dense ? 7.8 : compact ? 9.4 : 11.5);
            AddCell(grid, r + 2, 1, ProductDisplay(p), false, dense ? 7.8 : compact ? 9.3 : 11.5, TextAlignment.Center,
                1, 1, "Cambria", dense ? 32 : compact ? 38 : 46, dense ? 6.2 : compact ? 7.0 : 8.2);
        }

        var span = Math.Max(1, products.Count);
        for (var f = 0; f < firmSlots; f++)
        {
            var total = f < firms.Count ? Price(CompleteFinalOfferTotal(firms[f], products)) : string.Empty;
            AddCell(grid, 2, f + 2, total, false, dense ? 7.5 : compact ? 9.0 : 11.0,
                TextAlignment.Center, 1, span, "Cambria", 18, dense ? 6.4 : compact ? 7.2 : 8.2);
        }

        return grid;
    }

    private static Grid BuildWinningFirmTable(List<ProductItem> products, Firm? winner, CalculationResult calc, bool compact, bool dense)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 270 : 300) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 205 : 225) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 125 : 145) });

        AddRow(grid, dense ? 18 : compact ? 22 : 27);
        AddRow(grid, dense ? 19 : compact ? 23 : 29);
        AddCell(grid, 0, 0, string.Empty, true, dense ? 8.3 : compact ? 10.0 : 12.0, TextAlignment.Center, 1, 2);
        AddCell(grid, 0, 1, "Mal / Hizmet / Yapım İşi", true, dense ? 8.7 : compact ? 10.4 : 12.8, TextAlignment.Center, 1, 2);
        AddCell(grid, 0, 2, "Uygun Görülen Kişi / Firma / Firmalar", true, dense ? 8.6 : compact ? 10.3 : 12.8, TextAlignment.Center, 3, 1);
        AddCell(grid, 1, 2, "Adı", true, dense ? 8.4 : compact ? 10.0 : 12.3);
        AddCell(grid, 1, 3, "Adresi", true, dense ? 8.4 : compact ? 10.0 : 12.3);
        AddCell(grid, 1, 4, "Teklif Ettiği Fiyat", true, dense ? 8.2 : compact ? 9.8 : 12.1);

        var rowHeight = dense ? 18 : compact ? 23 : 30;
        for (var r = 0; r < products.Count; r++)
        {
            AddRow(grid, rowHeight);
            var p = products[r];
            AddCell(grid, r + 2, 0, (r + 1).ToString(CultureInfo.InvariantCulture), false, dense ? 7.8 : compact ? 9.4 : 11.5);
            AddCell(grid, r + 2, 1, ProductDisplay(p), false, dense ? 7.8 : compact ? 9.3 : 11.5,
                TextAlignment.Center, 1, 1, "Cambria", dense ? 32 : compact ? 38 : 46, dense ? 6.2 : compact ? 7.0 : 8.2);
        }

        var span = Math.Max(1, products.Count);
        AddCell(grid, 2, 2, winner is null ? string.Empty : FirmDisplayName(winner), false, dense ? 7.2 : compact ? 8.8 : 10.8,
            TextAlignment.Center, 1, span, "Cambria", dense ? 24 : compact ? 28 : 34, dense ? 6.0 : compact ? 6.8 : 7.6);
        AddCell(grid, 2, 3, winner?.Address ?? string.Empty, false, dense ? 7.0 : compact ? 8.6 : 10.6,
            TextAlignment.Center, 1, span, "Cambria", dense ? 42 : compact ? 48 : 60, dense ? 5.9 : compact ? 6.6 : 7.4);
        AddCell(grid, 2, 4, Price(calc.MarketResultTotal), false, dense ? 7.4 : compact ? 9.0 : 11.0,
            TextAlignment.Center, 1, span, "Cambria", 18, dense ? 6.2 : compact ? 7.0 : 8.0);

        var totalRow = products.Count + 2;
        AddRow(grid, dense ? 18 : compact ? 21 : 26);
        AddCell(grid, totalRow, 0, "TOPLAM (KDV HARİÇ):", true, dense ? 8.5 : compact ? 10.0 : 12.5, TextAlignment.Right, 4, 1);
        AddCell(grid, totalRow, 4, Price(calc.MarketResultTotal), true, dense ? 8.6 : compact ? 10.1 : 12.6);
        return grid;
    }

    private static Grid BuildInspectionItemsTable(List<ProductItem> products, bool compact)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        AddRow(grid, 38);
        AddCell(grid, 0, 0, "S.NO", true, 14.0);
        AddCell(grid, 0, 1, "Cinsi/Özelliği", true, 14.0);
        AddCell(grid, 0, 2, "BİRİMİ", true, 14.0);
        AddCell(grid, 0, 3, "MİKTARI", true, 14.0);
        for (var i = 0; i < products.Count; i++)
        {
            AddRow(grid, compact ? 37 : 48);
            var p = products[i];
            AddCell(grid, i + 1, 0, (i + 1).ToString(CultureInfo.InvariantCulture), true, compact ? 12.8 : 14.0);
            AddCell(grid, i + 1, 1, ProductDisplay(p), false, compact ? 12.3 : 13.8,
                TextAlignment.Center, 1, 1, "Cambria", compact ? 36 : 44, compact ? 8.8 : 10.0);
            AddCell(grid, i + 1, 2, p.Unit, false, compact ? 12.5 : 14.0);
            AddCell(grid, i + 1, 3, p.Quantity, true, compact ? 12.5 : 14.0);
        }
        return grid;
    }

    private static Grid BuildMarketSignatureRow(IEnumerable<CommissionMember> source, bool compact, bool dense)
    {
        var members = source.Take(3).ToList();
        while (members.Count < 3) members.Add(new CommissionMember());
        var grid = new Grid { Margin = new Thickness(0, dense ? 0 : compact ? 0 : 1, 0, dense ? 0 : compact ? 0 : 2) };
        for (var i = 0; i < 3; i++) grid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var i = 0; i < 3; i++)
        {
            var sp = new StackPanel { Margin = new Thickness(8, 0, 8, 0) };
            sp.Children.Add(T(members[i].Name,
                FitFontSize(members[i].Name, dense ? 8.0 : compact ? 9.7 : 12.0, dense ? 24 : 27, dense ? 6.6 : compact ? 7.8 : 9.0),
                false, TextAlignment.Center));
            sp.Children.Add(T(CommissionRole(i), dense ? 7.6 : compact ? 9.2 : 11.4, false, TextAlignment.Center,
                new Thickness(0, dense ? 0 : 1, 0, dense ? 0 : compact ? 2 : 5)));
            sp.Children.Add(T("........................................................", dense ? 6.8 : compact ? 8.2 : 10.0, false, TextAlignment.Center));
            Grid.SetColumn(sp, i);
            grid.Children.Add(sp);
        }
        return grid;
    }

    private static Grid BuildInspectionSignatureTable(IEnumerable<CommissionMember> source)
    {
        var members = source.Take(3).ToList();
        while (members.Count < 3) members.Add(new CommissionMember());
        var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
        for (var i = 0; i < 3; i++) grid.ColumnDefinitions.Add(new ColumnDefinition());
        AddRow(grid, 38);
        AddRow(grid, 34);
        for (var i = 0; i < 3; i++)
        {
            AddCell(grid, 0, i, members[i].Name, false, 13.8,
                TextAlignment.Center, 1, 1, "Cambria", 28, 10.0);
            AddCell(grid, 1, i, CommissionRole(i), false, 13.2);
        }
        return grid;
    }

    private static void AddInfoRow(Grid grid, int row, string label, string value, double height, double fontSize)
    {
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, MinHeight = height });
        var l = T(label, FitFontSize(label, fontSize, 48, Math.Max(8.0, fontSize - 2.0)), true, TextAlignment.Left);
        var v = T(value, FitFontSize(value, fontSize, 92, Math.Max(8.0, fontSize - 2.2)), false, TextAlignment.Left);
        Grid.SetRow(l, row); Grid.SetColumn(l, 0); Grid.SetRow(v, row); Grid.SetColumn(v, 1);
        grid.Children.Add(l); grid.Children.Add(v);
    }

    private static FixedPage NewPage(double width, double height) => new()
    {
        Width = width,
        Height = height,
        Background = Brushes.White
    };

    private static Grid NewPageRoot(FixedPage page, double width, double height)
    {
        var root = new Grid
        {
            Width = width - (2 * Margin),
            Height = height - (2 * Margin),
            Background = Brushes.White
        };
        FixedPage.SetLeft(root, Margin);
        FixedPage.SetTop(root, Margin);
        page.Children.Add(root);
        return root;
    }

    private static TextBlock T(string? text, double size, bool bold = false, TextAlignment align = TextAlignment.Left, Thickness? margin = null, string fontFamily = "Cambria")
    {
        // R9 typography: official body/data stays Cambria; headings and emphasized labels use Bahnschrift.
        var resolvedFontFamily = bold && string.Equals(fontFamily, "Cambria", StringComparison.OrdinalIgnoreCase)
            ? "Bahnschrift"
            : fontFamily;
        var block = new TextBlock
        {
            Text = text ?? string.Empty,
            FontFamily = new FontFamily(resolvedFontFamily),
            FontSize = size,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            TextAlignment = align,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = margin ?? new Thickness(0),
            Foreground = Black
        };
        TextOptions.SetTextFormattingMode(block, TextFormattingMode.Ideal);
        TextOptions.SetTextHintingMode(block, TextHintingMode.Fixed);
        return block;
    }

    private static Border PlainText(string text, double size, TextAlignment alignment, Thickness margin, double minHeight, string fontFamily = "Cambria")
    {
        return new Border
        {
            Margin = margin,
            MinHeight = minHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Child = T(text, size, false, alignment, null, fontFamily)
        };
    }

    private static void AddRow(Grid grid, double height) => grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(height) });

    private static void AddCell(Grid grid, int row, int col, string? text, bool bold, double size,
        TextAlignment align = TextAlignment.Center, int colSpan = 1, int rowSpan = 1, string fontFamily = "Cambria",
        int fitChars = 0, double minFontSize = 7.0)
    {
        var effectiveSize = fitChars > 0 ? FitFontSize(text, size, fitChars, minFontSize) : size;
        var border = new Border
        {
            BorderBrush = Black,
            BorderThickness = MediumBorder,
            Padding = new Thickness(4, 1.5, 4, 1.5),
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Child = T(text, effectiveSize, bold, align, null, fontFamily)
        };
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        if (colSpan > 1) Grid.SetColumnSpan(border, colSpan);
        if (rowSpan > 1) Grid.SetRowSpan(border, rowSpan);
        grid.Children.Add(border);
    }

    private static string CommissionRole(int index) => index == 0 ? "Komisyon Başkanı" : "Üye";

    private static double FitFontSize(string? text, double baseSize, int softChars, double minSize)
    {
        if (string.IsNullOrWhiteSpace(text) || softChars <= 0) return baseSize;
        var lines = text.Replace("\r", string.Empty).Split('\n');
        var longest = lines.Length == 0 ? 0 : lines.Max(x => x.Trim().Length);
        var visualLoad = Math.Max(longest, (int)Math.Ceiling(text.Length / Math.Max(1.0, lines.Length)));
        if (visualLoad <= softChars) return baseSize;

        var ratio = Math.Sqrt(softChars / (double)visualLoad);
        return Math.Max(minSize, Math.Round(baseSize * ratio, 1));
    }

    private static List<ProductItem> ActiveProducts(ProcurementData data) =>
        data.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Take(8).ToList();

    private static List<Firm> ActiveFirms(ProcurementData data) =>
        data.Firms.Where(f => !string.IsNullOrWhiteSpace(f.Name)).Take(5).ToList();

    private static string ProductDisplay(ProductItem p)
    {
        if (string.IsNullOrWhiteSpace(p.Specification)) return p.Name;
        if (p.Name.Contains(p.Specification, StringComparison.OrdinalIgnoreCase)) return p.Name;
        return p.Name + "\n" + p.Specification;
    }

    private static string FirmDisplayName(Firm f)
    {
        if (string.IsNullOrWhiteSpace(f.ContactName)) return f.Name;
        if (f.Name.Contains(f.ContactName, StringComparison.OrdinalIgnoreCase)) return f.Name;
        return f.Name + "\n" + f.ContactName;
    }

    private static decimal? CompleteFinalOfferTotal(Firm firm, IReadOnlyCollection<ProductItem> products)
    {
        decimal total = 0m;
        foreach (var product in products)
        {
            if (!NumberParser.TryPositiveDecimal(product.Quantity, out var quantity)) return null;
            var finalUnitPrice = firm.Offers.FirstOrDefault(o => o.ProductId == product.Id)?.FinalUnitPrice;
            if (!finalUnitPrice.HasValue || finalUnitPrice.Value <= 0m) return null;
            total += quantity * finalUnitPrice.Value;
        }
        return total > 0m ? total : null;
    }

    private static string MoneyNoSymbol(decimal? value) => value is > 0 ? value.Value.ToString("N2", Tr) : string.Empty;
    private static string MoneyNoSymbol(decimal value) => value > 0 ? value.ToString("N2", Tr) : string.Empty;
    private static string Price(decimal? value) => value is > 0 ? value.Value.ToString("N2", Tr) + " ₺" : string.Empty;
    private static string Price(decimal value) => value > 0 ? value.ToString("N2", Tr) + " ₺" : string.Empty;
    private static string DateText(DateTime? date) => date.HasValue ? date.Value.ToString("dd.MM.yyyy", Tr) : ".... / ..... / ........";
    private static string Safe(string? value) => string.IsNullOrWhiteSpace(value) ? "........................" : value.Trim();
    private static string ApprovalLine(ProcurementData data) => $": {DateText(data.Dates.ApprovalDate)} tarih ve HYS - {Safe(data.Dates.ApprovalNo)} sayılı Harcama Talimatı Onay Belgesi";
}

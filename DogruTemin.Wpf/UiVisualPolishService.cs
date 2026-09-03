using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace DogruTemin.Wpf;

internal static class UiVisualPolishService
{
    private static readonly SolidColorBrush Gold = BrushFrom("#FFB41F");
    private static readonly SolidColorBrush Navy = BrushFrom("#031C38");
    private static readonly SolidColorBrush WarningDark = BrushFrom("#8A5A12");

    public static void Apply(MainWindow window)
    {
        ApplyVersionAndHeader(window);
        ApplyHomeCardLanguage(window);
        SimplifyHomeDashboard(window);
        NormalizeButtons(window);
        NormalizePanels(window);
        NormalizeDatePickers(window);
    }

    private static void ApplyVersionAndHeader(MainWindow window)
    {
        if (window.FindName("HeaderVersionText") is TextBlock headerVersion)
            headerVersion.Text = AppVersionInfo.DisplayVersion;

        // DoğruFatura finalinde olduğu gibi kullanıcıya teknik revizyon kartı yerine
        // yalnız çalışma/kayıt durumu gösterilir. Sürüm Hakkında ekranında kalır.
        var versionLabel = UiTree.Descendants<TextBlock>(window).FirstOrDefault(x => x.Text == "SÜRÜM");
        if (versionLabel is not null && UiTree.FindAncestor<Border>(versionLabel) is { } versionCard)
        {
            versionCard.Visibility = Visibility.Collapsed;
            // Sürüm kartı kalkınca ortada boş alan bırakma; kayıt durumu kartını DoğruFatura gibi dengeli tut.
            if (UiTree.FindAncestor<StackPanel>(versionCard) is { } statusPanel)
            {
                statusPanel.Margin = new Thickness(18, 0, 0, 0);
                if (UiTree.FindAncestor<Grid>(statusPanel) is { } headerGrid
                    && headerGrid.ColumnDefinitions.Count > 1)
                    headerGrid.ColumnDefinitions[1].Width = new GridLength(220);
            }
        }
    }

    private static void ApplyHomeCardLanguage(MainWindow window)
    {
        if (window.FindName("HomeView") is not FrameworkElement home) return;

        // Kahraman bilgi kartını DoğruFatura'nın premium kart diliyle eşitle.
        if (window.FindName("HomeJobTitleText") is TextBlock title
            && UiTree.FindAncestor<Border>(title) is { } hero)
        {
            if (window.TryFindResource("PremiumPanelBrush") is Brush panelBrush)
                hero.Background = panelBrush;
            if (window.TryFindResource("PremiumGoldBorderBrush") is Brush goldBorder)
                hero.BorderBrush = goldBorder;
            hero.BorderThickness = new Thickness(1.25);
            hero.CornerRadius = new CornerRadius(12);
            hero.Effect = null;
            hero.SnapsToDevicePixels = true;
            hero.UseLayoutRounding = true;
        }

        foreach (var border in UiTree.Descendants<Border>(home))
        {
            // Metinli kartlarda gölge yerine temiz sınır/kontrast kullanılır.
            if (UiTree.Descendants<TextBlock>(border).Any()) border.Effect = null;
            if (border.CornerRadius.TopLeft is >= 9 and <= 12)
                border.CornerRadius = new CornerRadius(10);
            border.SnapsToDevicePixels = true;
            border.UseLayoutRounding = true;
        }

        foreach (var text in UiTree.Descendants<TextBlock>(home))
        {
            TextOptions.SetTextFormattingMode(text, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(text, TextRenderingMode.ClearType);
            text.SnapsToDevicePixels = true;
        }
    }

    private static void SimplifyHomeDashboard(MainWindow window)
    {
        if (window.FindName("HomeView") is not FrameworkElement home) return;

        // Akıllı İş Akışı aynı kalem/firma/hazırlık bilgilerini daha işlevsel verdiği için
        // üstteki tekrar eden üç KPI kartını kaldır.
        var repeatedKpis = UiTree.Descendants<UniformGrid>(home)
            .FirstOrDefault(grid => grid.Columns == 3
                && ContainsLabel(grid, "MAL / HİZMET")
                && ContainsLabel(grid, "FİRMA / TEKLİF")
                && ContainsLabel(grid, "SÜREÇ HAZIRLIĞI"));
        Collapse(repeatedKpis);

        // Belge hazırlığı bilgisi Akıllı İş Akışı'nın 5. adımı ve Kontrol Merkezi'nde zaten bulunuyor.
        CollapseCardByTitle(home, "BELGE HAZIRLIĞI");

        // Son kayıt kahraman kartta; arşiv ve son PDF kendi ekranlarında mevcut.
        // Tekrar eden SON DURUM kartını kaldır, sonraki öneriyi tam genişliğe yay.
        var lastStatusTitle = UiTree.Descendants<TextBlock>(home).FirstOrDefault(x => x.Text == "SON DURUM");
        var lastStatusCard = lastStatusTitle is null ? null : UiTree.FindAncestor<Border>(lastStatusTitle);
        if (lastStatusCard is not null)
        {
            lastStatusCard.Visibility = Visibility.Collapsed;
            lastStatusCard.Margin = new Thickness(0);
        }

        var nextStepTitle = UiTree.Descendants<TextBlock>(home).FirstOrDefault(x => x.Text == "SIRADAKİ ÖNERİLEN ADIM");
        var nextStepCard = nextStepTitle is null ? null : UiTree.FindAncestor<Border>(nextStepTitle);
        if (nextStepCard is not null)
        {
            Grid.SetColumn(nextStepCard, 0);
            Grid.SetColumnSpan(nextStepCard, 3);
            nextStepCard.Margin = new Thickness(0);
        }
    }

    private static void CollapseCardByTitle(FrameworkElement root, string title)
    {
        var label = UiTree.Descendants<TextBlock>(root).FirstOrDefault(x => x.Text == title);
        if (label is not null)
            Collapse(UiTree.FindAncestor<Border>(label));
    }

    private static bool ContainsLabel(DependencyObject root, string text) =>
        UiTree.Descendants<TextBlock>(root).Any(x => x.Text == text);

    private static void Collapse(FrameworkElement? element)
    {
        if (element is null) return;
        element.Visibility = Visibility.Collapsed;
        element.Margin = new Thickness(0);
    }

    private static void NormalizeButtons(MainWindow window)
    {
        foreach (var button in UiTree.Descendants<Button>(window))
        {
            if (button.Content is string content && content is not ("—" or "×"))
            {
                button.MinHeight = 42;
                if (button.Height > 0 && button.Height < 42) button.Height = 42;
                if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)))
                    AutomationProperties.SetName(button, content.Replace("/", " "));
            }

            if (button.Background is SolidColorBrush background
                && background.Color == (Color)ColorConverter.ConvertFromString("#B7791F"))
            {
                button.Background = WarningDark;
                button.Foreground = Brushes.White;
            }

            if (button.Background is SolidColorBrush goldBackground
                && goldBackground.Color == (Color)ColorConverter.ConvertFromString("#FFB41F"))
                button.Foreground = Navy;
        }
    }

    private static void NormalizePanels(MainWindow window)
    {
        foreach (var border in UiTree.Descendants<Border>(window))
        {
            if (border.CornerRadius.TopLeft is >= 8 and <= 12
                && border.CornerRadius.TopRight is >= 8 and <= 12)
            {
                border.SnapsToDevicePixels = true;
                border.UseLayoutRounding = true;
            }
        }
    }

    private static void NormalizeDatePickers(MainWindow window)
    {
        foreach (var picker in UiTree.Descendants<LargeDatePicker>(window))
        {
            picker.Height = 42;
            picker.SnapsToDevicePixels = true;
            picker.UseLayoutRounding = true;
            if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(picker)))
                AutomationProperties.SetName(picker, picker.Name switch
                {
                    "AssignmentDatePicker" => "Komisyon görevlendirme tarihi",
                    "ApprovalDatePicker" => "Harcama belgesi tarihi",
                    "CostCalculationDatePicker" => "Yaklaşık maliyet hesap cetveli tarihi",
                    "MarketReportDatePicker" => "Piyasa fiyat araştırması tutanağı tarihi",
                    "InspectionReportDatePicker" => "Muayene ve kabul tutanağı tarihi",
                    "DeliveryDatePicker" => "Teslim alma kabul tarihi",
                    "OfferDatePicker" => "Firma teklif tarihi",
                    _ => "Tarih"
                });
        }
    }

    private static SolidColorBrush BrushFrom(string value) =>
        new((Color)ColorConverter.ConvertFromString(value));
}

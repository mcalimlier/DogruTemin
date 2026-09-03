$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

function Replace-RegexOnce {
    param(
        [string]$Text,
        [string]$Pattern,
        [string]$Replacement,
        [string]$Description
    )
    $regex = [regex]::new($Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $matches = $regex.Matches($Text)
    if ($matches.Count -ne 1) {
        throw "$Description için beklenen 1 eşleşme yerine $($matches.Count) eşleşme bulundu."
    }
    return $regex.Replace($Text, $Replacement, 1)
}

function Replace-LiteralOnce {
    param(
        [string]$Text,
        [string]$Old,
        [string]$New,
        [string]$Description
    )
    $first = $Text.IndexOf($Old, [StringComparison]::Ordinal)
    if ($first -lt 0) { throw "$Description bulunamadı." }
    $second = $Text.IndexOf($Old, $first + $Old.Length, [StringComparison]::Ordinal)
    if ($second -ge 0) { throw "$Description birden fazla bulundu." }
    return $Text.Substring(0, $first) + $New + $Text.Substring($first + $Old.Length)
}

$utf8 = New-Object System.Text.UTF8Encoding($false)
$xamlPath = Join-Path $PWD 'DogruTemin.Wpf\MainWindow.xaml'
$codePath = Join-Path $PWD 'DogruTemin.Wpf\MainWindow.xaml.cs'

# ---------------- ANA EKRAN XAML FİZİKSEL TEMİZLİĞİ ----------------
$xaml = [IO.File]::ReadAllText($xamlPath)

$xaml = Replace-RegexOnce $xaml '\s*<Border Background="#092746" CornerRadius="9" BorderBrush="#24496B" BorderThickness="1" Padding="14,8" Width="198">.*?<TextBlock Text="SÜRÜM".*?</Border>\s*' "`r`n" 'üst sürüm kartı'
$xaml = Replace-LiteralOnce $xaml '<ColumnDefinition Width="420"/>' '<ColumnDefinition Width="220"/>' 'üst orta kolon genişliği'
$xaml = Replace-LiteralOnce $xaml 'Padding="13,8" Margin="10,0,0,0" Width="188"' 'Padding="13,8" Margin="0" Width="188"' 'kayıt durumu kartı boşluğu'

$xaml = Replace-RegexOnce $xaml '\s*<!-- DoğruFatura bilgi kartı dili: açık KPI kartları \+ keskin koyu metin -->.*?</UniformGrid>\s*(?=\s*<!-- R19: Ana ekranın tüm kartları)' "`r`n`r`n                            " 'tekrarlı üçlü KPI kart satırı'

$xaml = Replace-RegexOnce $xaml '(</UniformGrid>\s*)<Border Style="\{StaticResource PremiumPanel\}" Padding="15,12" Margin="0,0,0,12">.*?<TextBlock Text="BELGE HAZIRLIĞI".*?</Border>\s*(?=\s*<!-- R19 premium son durum \+ önerilen adım -->)' '$1' 'tekrarlı belge hazırlığı kartı'

$nextStep = @'

                            <!-- Sıradaki önerilen adım -->
                            <Border Style="{StaticResource PremiumPanel}" Padding="16,12">
                                <Grid>
                                    <Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width="160"/></Grid.ColumnDefinitions>
                                    <StackPanel>
                                        <StackPanel Orientation="Horizontal">
                                            <Ellipse Width="8" Height="8" Fill="{StaticResource Gold500Brush}" VerticalAlignment="Center" Margin="0,0,8,0"/>
                                            <TextBlock Text="SIRADAKİ ÖNERİLEN ADIM" Foreground="{StaticResource Gold500Brush}" FontSize="11.5" FontWeight="Bold"/>
                                        </StackPanel>
                                        <TextBlock x:Name="HomeNextStepText" Text="İşlem bilgilerini tamamlayın." Foreground="White" FontSize="14" FontWeight="SemiBold" TextWrapping="Wrap" Margin="16,4,20,0"/>
                                    </StackPanel>
                                    <Button x:Name="HomeNextStepButton" Grid.Column="1" Style="{StaticResource ModernButton}" Content="EKRANI AÇ" Height="38" Width="150" HorizontalAlignment="Right" VerticalAlignment="Center" Padding="10,4" Click="HomeNextStepButton_Click"/>
                                </Grid>
                            </Border>
'@
$xaml = Replace-RegexOnce $xaml '\s*<!-- R19 premium son durum \+ önerilen adım -->\s*<Grid>.*?</Grid>\s*(?=\s*</StackPanel>\s*</ScrollViewer>)' $nextStep 'son durum/önerilen adım alt bloğu'
$xaml = $xaml.Replace('<!-- R19: Ana ekranın tüm kartları R18 premium görsel diliyle uyumlu hale getirildi -->', '<!-- Akıllı iş akışı -->')

# XAML iyi biçimlenmiş XML olarak kalmalı.
$null = [xml]$xaml
[IO.File]::WriteAllText($xamlPath, $xaml, $utf8)

# ---------------- CODE-BEHIND ÖLÜ KOD / BAĞ TEMİZLİĞİ ----------------
$code = [IO.File]::ReadAllText($codePath)
$code = Replace-LiteralOnce $code '    private string _lastPdfPath = string.Empty;' '' 'eski son PDF state alanı'
$code = Replace-LiteralOnce $code '        _lastPdfPath = FindMostRecentPdf() ?? string.Empty;' '' 'başlangıç son PDF taraması'
$code = Replace-LiteralOnce $code '        LoadSettingsEditor();' '' 'boş ayar editörü çağrısı'
$code = Replace-RegexOnce $code '\s*private void LoadSettingsEditor\(\)\s*\{\s*// Varsayılan birim ve varsayılan komisyon şablonları yeni arayüzde kullanılmaz\.\s*// Eski ayar dosyaları StorageService tarafından geriye dönük uyumluluk için okunmaya devam eder\.\s*\}\s*' "`r`n`r`n    " 'boş LoadSettingsEditor metodu'

$code = Replace-LiteralOnce $code '    private bool ConfirmEditorClear(string message) =>' '    private static bool ConfirmEditorClear(string message) =>' 'ConfirmEditorClear static iyileştirmesi'
$code = $code.Replace('StringComparison.CurrentCultureIgnoreCase', 'StringComparison.OrdinalIgnoreCase')

$code = Replace-LiteralOnce $code '    private void NavFirms_Click(object sender, RoutedEventArgs e) => ShowView("Firms");' '' 'eski NavFirms_Click olayı'
$oldNavOffers = @'
    private void NavOffers_Click(object sender, RoutedEventArgs e)
    {
        ShowView("Offers");
        PrepareOffersView();
    }
'@
$code = Replace-LiteralOnce $code $oldNavOffers '' 'eski NavOffers_Click olayı'
$code = Replace-LiteralOnce $code '    private void NavDates_Click(object sender, RoutedEventArgs e) => ShowView("Dates");' '' 'eski NavDates_Click olayı'

$simplifiedOutputStage = @'
    private void SetHomeOutputStage(bool preparationReady, bool finalReady, int preparationIssueCount, int finalIssueCount)
    {
        if (preparationReady && finalReady)
        {
            HomeStageOutputStatus.Text = "TAMAM";
            HomeStageOutputStatus.Foreground = new SolidColorBrush(Color.FromRgb(126, 226, 166));
            HomeStageOutputDetail.Text = "Ön hazırlık ve nihai işlem belgeleri üretime hazır.";
            return;
        }

        if (preparationReady || finalReady)
        {
            HomeStageOutputStatus.Text = "KISMİ";
            HomeStageOutputStatus.Foreground = new SolidColorBrush(Color.FromRgb(143, 183, 216));
        }
        else
        {
            HomeStageOutputStatus.Text = "EKSİK";
            HomeStageOutputStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 191, 54));
        }

        var preparationText = preparationReady ? "Ön hazırlık: hazır" : $"Ön hazırlık: {preparationIssueCount} eksik";
        var finalText = finalReady ? "Nihai: hazır" : $"Nihai: {finalIssueCount} eksik";
        HomeStageOutputDetail.Text = $"{preparationText} • {finalText}";
    }

'@
$code = Replace-RegexOnce $code '    private void SetHomeOutputStage\(bool preparationReady, bool finalReady, int preparationIssueCount, int finalIssueCount\)\s*\{.*?(?=    private void UpdateHomeActivitySummary\(\))' $simplifiedOutputStage 'belge kartına bağlı eski ana ekran kodu'

$simplifiedActivity = @'
    private void UpdateHomeActivitySummary()
    {
        if (HomeHeroLastSavedText is null) return;
        HomeHeroLastSavedText.Text = _lastSavedAt.HasValue
            ? _lastSavedAt.Value.ToString("dd.MM.yyyy • HH:mm", CultureInfo.InvariantCulture)
            : "Henüz kaydedilmedi";
    }

'@
$code = Replace-RegexOnce $code '    private void UpdateHomeActivitySummary\(\)\s*\{.*?(?=    private void UpdateDerivedState\(ProcurementData\? supplied = null\))' $simplifiedActivity 'son durum/son PDF kodu'

$statisticsYears = @'
    private void RefreshStatisticsYears()
    {
        var statisticsYearBox = StatisticsYearBox;
        if (statisticsYearBox is null) return;

        var selected = statisticsYearBox.SelectedItem?.ToString();
        var years = _storage.ListRecords()
            .Where(x => x.Dates.ApprovalDate.HasValue)
            .Select(x => x.Dates.ApprovalDate!.Value.Year)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();

        if (!years.Contains(DateTime.Now.Year)) years.Add(DateTime.Now.Year);
        years = years.Distinct().OrderByDescending(x => x).ToList();

        StatisticsYears.Clear();
        StatisticsYears.Add("Tüm Yıllar");
        foreach (var year in years) StatisticsYears.Add(year.ToString(CultureInfo.InvariantCulture));

        var target = !string.IsNullOrWhiteSpace(selected) && StatisticsYears.Contains(selected)
            ? selected
            : DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
        statisticsYearBox.SelectedItem = StatisticsYears.Contains(target) ? target : "Tüm Yıllar";
    }

'@
$code = Replace-RegexOnce $code '    private void RefreshStatisticsYears\(\)\s*\{.*?(?=    private void RefreshStatistics_Click\()' $statisticsYears 'istatistik yıl seçimi null güvenliği'

# Kültürden bağımsız dosya/ekran biçimleri için güvenli, hedefli iyileştirmeler.
$code = $code.Replace('selectedCoreIssues.Count.ToString();', 'selectedCoreIssues.Count.ToString(CultureInfo.InvariantCulture);')
$code = $code.Replace('selectedAdvisories.Count.ToString();', 'selectedAdvisories.Count.ToString(CultureInfo.InvariantCulture);')
$code = $code.Replace('draftIssues.Count.ToString();', 'draftIssues.Count.ToString(CultureInfo.InvariantCulture);')
$code = $code.Replace('dateWarnings.Count.ToString();', 'dateWarnings.Count.ToString(CultureInfo.InvariantCulture);')

[IO.File]::WriteAllText($codePath, $code, $utf8)

Write-Host 'RC1 fiziksel ana ekran ve ölü kod temizliği uygulandı.' -ForegroundColor Green

$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Read-Text([string]$Path) { [System.IO.File]::ReadAllText($Path) }
function Write-Text([string]$Path, [string]$Text) { [System.IO.File]::WriteAllText($Path, $Text, $Utf8NoBom) }
function Replace-Once([string]$Text, [string]$Old, [string]$New, [string]$Label) {
    $i = $Text.IndexOf($Old, [System.StringComparison]::Ordinal)
    if ($i -lt 0) { throw "Anchor bulunamadı: $Label" }
    if ($Text.IndexOf($Old, $i + $Old.Length, [System.StringComparison]::Ordinal) -ge 0) { throw "Anchor birden fazla: $Label" }
    return $Text.Substring(0, $i) + $New + $Text.Substring($i + $Old.Length)
}

# ---------------- MainWindow.xaml ----------------
$xamlPath = 'DogruTemin.Wpf/MainWindow.xaml'
$xaml = Read-Text $xamlPath

$oldNav = '<Button x:Name="NavFirmsOffers" Style="{StaticResource SubNavButton}" Click="NavFirmsOffers_Click"><StackPanel Orientation="Horizontal"><TextBlock FontFamily="Segoe MDL2 Assets" Text="&#xE731;" FontSize="17" Width="30"/><TextBlock Text="FİRMALAR/TEKLİFLER" VerticalAlignment="Center"/></StackPanel></Button>'
$newNav = $oldNav + "`r`n                        " + '<Button x:Name="NavApprovalDocument" Style="{StaticResource SubNavButton}" Click="NavApprovalDocument_Click"><StackPanel Orientation="Horizontal"><TextBlock FontFamily="Segoe MDL2 Assets" Text="&#xE8A5;" FontSize="17" Width="30"/><TextBlock Text="ONAY BELGESİ" VerticalAlignment="Center"/></StackPanel></Button>'
$xaml = Replace-Once $xaml $oldNav $newNav 'Onay Belgesi menü butonu'

$xaml = $xaml.Replace('Text="Komisyon Görevlendirme Tarihi"', 'Text="Kurul / Komisyon Onay Tarihi"')

$approvalView = @'
                    <!-- ONAY BELGESİ -->
                    <ScrollViewer x:Name="ApprovalDocumentView" Visibility="Collapsed" VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">
                        <StackPanel Margin="0,0,8,16">
                            <TextBlock Text="ONAY BELGESİ" Style="{StaticResource PageTitle}"/>
                            <TextBlock Text="Yalnız Onay Belgesi'ne özgü bilgileri girin. Kurum, mal/hizmet, yaklaşık maliyet, komisyonlar ve harcama yetkilisi mevcut işlemden otomatik alınır." Style="{StaticResource PageSubtitle}" Margin="2,0,0,10"/>

                            <Border Style="{StaticResource PremiumPanel}" Padding="18,16" Margin="0,0,0,12">
                                <StackPanel>
                                    <TextBlock Text="BELGE BİLGİLERİ" Style="{StaticResource PremiumSectionTitle}" FontSize="16" Margin="0,0,0,10"/>
                                    <Grid>
                                        <Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width="18"/><ColumnDefinition/></Grid.ColumnDefinitions>
                                        <StackPanel>
                                            <TextBlock Text="Onay Belgesi Tarihi" Style="{StaticResource PremiumFieldLabel}"/>
                                            <local:LargeDatePicker x:Name="ApprovalDocumentDatePicker" SelectedDateChanged="AnyDate_Changed"/>
                                        </StackPanel>
                                        <StackPanel Grid.Column="2">
                                            <TextBlock Text="Belge Sayısı" Style="{StaticResource PremiumFieldLabel}"/>
                                            <TextBox x:Name="ApprovalDocumentNumberBox" Style="{StaticResource PremiumFieldTextBox}" TextChanged="AnyGlobalField_TextChanged"/>
                                        </StackPanel>
                                    </Grid>
                                </StackPanel>
                            </Border>

                            <Border Style="{StaticResource PremiumPanel}" Padding="18,16" Margin="0,0,0,12">
                                <StackPanel>
                                    <TextBlock Text="ÖDENEK / İHALE BİLGİLERİ" Style="{StaticResource PremiumSectionTitle}" FontSize="16" Margin="0,0,0,10"/>
                                    <Grid>
                                        <Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width="18"/><ColumnDefinition/></Grid.ColumnDefinitions>
                                        <Grid.RowDefinitions><RowDefinition/><RowDefinition Height="10"/><RowDefinition/><RowDefinition Height="10"/><RowDefinition/><RowDefinition Height="10"/><RowDefinition/><RowDefinition Height="10"/><RowDefinition/></Grid.RowDefinitions>
                                        <StackPanel><TextBlock Text="Kullanılabilir Ödenek Tutarı" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalAvailableBudgetBox" Style="{StaticResource PremiumFieldTextBox}" TextChanged="AnyGlobalField_TextChanged"/></StackPanel>
                                        <StackPanel Grid.Column="2"><TextBlock Text="Yatırım Proje Numarası (varsa)" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalInvestmentProjectBox" Style="{StaticResource PremiumFieldTextBox}" TextChanged="AnyGlobalField_TextChanged"/></StackPanel>
                                        <StackPanel Grid.Row="2"><TextBlock Text="Bütçe Tertibi (varsa)" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalBudgetCodeBox" Style="{StaticResource PremiumFieldTextBox}" TextChanged="AnyGlobalField_TextChanged"/></StackPanel>
                                        <StackPanel Grid.Row="2" Grid.Column="2"><TextBlock Text="Avans Verilecekse Şartları" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalAdvanceConditionsBox" Style="{StaticResource PremiumFieldTextBox}" TextChanged="AnyGlobalField_TextChanged"/></StackPanel>
                                        <StackPanel Grid.Row="4"><TextBlock Text="İhale Usulü" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalTenderProcedureBox" Style="{StaticResource PremiumFieldTextBox}" TextChanged="AnyGlobalField_TextChanged"/></StackPanel>
                                        <StackPanel Grid.Row="4" Grid.Column="2"><TextBlock Text="İlanın Şekli ve Adedi" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalAnnouncementMethodBox" Style="{StaticResource PremiumFieldTextBox}" TextChanged="AnyGlobalField_TextChanged"/></StackPanel>
                                        <StackPanel Grid.Row="6"><TextBlock Text="Ön Yeterlik / İhale Dokümanı Satış Bedeli" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalPrequalificationSalePriceBox" Style="{StaticResource PremiumFieldTextBox}" TextChanged="AnyGlobalField_TextChanged"/></StackPanel>
                                        <StackPanel Grid.Row="6" Grid.Column="2"><TextBlock Text="Fiyat Farkı Ödenecekse Dayanağı" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalPriceDifferenceBasisBox" Style="{StaticResource PremiumFieldTextBox}" TextChanged="AnyGlobalField_TextChanged"/></StackPanel>
                                        <StackPanel Grid.Row="8" Grid.ColumnSpan="3"><TextBlock Text="Açıklamalar" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalNotesBox" Style="{StaticResource PremiumFieldTextBox}" MinHeight="72" AcceptsReturn="True" TextWrapping="Wrap" VerticalContentAlignment="Top" TextChanged="AnyGlobalField_TextChanged"/></StackPanel>
                                    </Grid>
                                </StackPanel>
                            </Border>

                            <Border Style="{StaticResource PremiumPanel}" Padding="18,16" Margin="0,0,0,12">
                                <StackPanel>
                                    <TextBlock Text="ONAYA SUNAN" Style="{StaticResource PremiumSectionTitle}" FontSize="16" Margin="0,0,0,10"/>
                                    <Grid>
                                        <Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width="18"/><ColumnDefinition/></Grid.ColumnDefinitions>
                                        <StackPanel><TextBlock Text="Ad Soyad" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalSubmittedByNameBox" Style="{StaticResource PremiumFieldTextBox}" TextChanged="AnyGlobalField_TextChanged"/></StackPanel>
                                        <StackPanel Grid.Column="2"><TextBlock Text="Unvanı" Style="{StaticResource PremiumFieldLabel}"/><TextBox x:Name="ApprovalSubmittedByTitleBox" Style="{StaticResource PremiumFieldTextBox}" TextChanged="AnyGlobalField_TextChanged"/></StackPanel>
                                    </Grid>
                                </StackPanel>
                            </Border>

                            <Border Background="#F4F8FC" BorderBrush="#C8D8E6" BorderThickness="1" CornerRadius="10" Padding="15,11">
                                <TextBlock Foreground="#31506C" FontSize="12" TextWrapping="Wrap" Text="Otomatik alınacak bilgiler: alım yapan idarenin adı, işin tanımı/niteliği, işin miktarı, KDV hariç yaklaşık maliyet, piyasa araştırma görevlileri, muayene ve kabul komisyonu ile harcama yetkilisi."/>
                            </Border>
                        </StackPanel>
                    </ScrollViewer>

'@
$xaml = Replace-Once $xaml '                    <!-- BELGE TARİHLERİ -->' ($approvalView + '                    <!-- BELGE TARİHLERİ -->') 'Onay Belgesi görünümü'
Write-Text $xamlPath $xaml

# ---------------- MainWindow.xaml.cs ----------------
$csPath = 'DogruTemin.Wpf/MainWindow.xaml.cs'
$cs = Read-Text $csPath

$cs = $cs.Replace('if (_currentView is not ("Institution" or "General" or "Firms" or "Offers" or "Dates" or "Output")) return;', 'if (_currentView is not ("Institution" or "General" or "Firms" or "Offers" or "ApprovalDocument" or "Dates" or "Output")) return;')

$oldInitDates = @'
        ApprovalDatePicker.SelectedDate = null;
        CostCalculationDatePicker.SelectedDate = null;
        MarketReportDatePicker.SelectedDate = null;
'@
$newInitDates = @'
        ApprovalDatePicker.SelectedDate = null;
        CostCalculationDatePicker.SelectedDate = null;
        ApprovalDocumentDatePicker.SelectedDate = null;
        MarketReportDatePicker.SelectedDate = null;
'@
$cs = Replace-Once $cs $oldInitDates $newInitDates 'boş durum Onay tarihi'

$oldInitFields = @'
        ApprovalNoBox.Text = string.Empty;
        var preservedInstitution = settings.Institution;
'@
$newInitFields = @'
        ApprovalNoBox.Text = string.Empty;
        ApprovalDocumentNumberBox.Text = string.Empty;
        ApprovalAvailableBudgetBox.Text = string.Empty;
        ApprovalInvestmentProjectBox.Text = string.Empty;
        ApprovalBudgetCodeBox.Text = string.Empty;
        ApprovalAdvanceConditionsBox.Text = string.Empty;
        ApprovalTenderProcedureBox.Text = "4734 S.K. 22/d Kapsamında";
        ApprovalAnnouncementMethodBox.Text = "İlansız";
        ApprovalPrequalificationSalePriceBox.Text = string.Empty;
        ApprovalPriceDifferenceBasisBox.Text = "Ödenmeyecektir";
        ApprovalNotesBox.Text = string.Empty;
        ApprovalSubmittedByNameBox.Text = string.Empty;
        ApprovalSubmittedByTitleBox.Text = string.Empty;
        var preservedInstitution = settings.Institution;
'@
$cs = Replace-Once $cs $oldInitFields $newInitFields 'boş durum Onay alanları'

$oldSeed = @'
        var approximateCostSheet = new OutputDocument { Name = "Yaklaşık Maliyet Hesap Cetveli", IsSelected = false, Status = "Uyarı", Note = "Nihai işlem belgesi" };
        var marketResearchReport = new OutputDocument { Name = "Piyasa Fiyat Araştırması Tutanağı", IsSelected = true, Status = "Uyarı", Note = "Nihai işlem belgesi" };
        var inspectionReport = new OutputDocument { Name = "Muayene ve Kabul Tutanağı", IsSelected = true, Status = "Uyarı", Note = "Nihai işlem belgesi" };
'@
$newSeed = @'
        var approximateCostSheet = new OutputDocument { Name = "Yaklaşık Maliyet Hesap Cetveli", IsSelected = false, Status = "Uyarı", Note = "Nihai işlem belgesi" };
        var approvalDocument = new OutputDocument { Name = "Onay Belgesi", IsSelected = true, Status = "Uyarı", Note = "Nihai işlem belgesi" };
        var marketResearchReport = new OutputDocument { Name = "Piyasa Fiyat Araştırması Tutanağı", IsSelected = true, Status = "Uyarı", Note = "Nihai işlem belgesi" };
        var inspectionReport = new OutputDocument { Name = "Muayene ve Kabul Tutanağı", IsSelected = true, Status = "Uyarı", Note = "Nihai işlem belgesi" };
'@
$cs = Replace-Once $cs $oldSeed $newSeed 'Onay Belgesi çıktı seed'
$cs = $cs.Replace('FinalOutputDocuments.Add(approximateCostSheet);`r`n        FinalOutputDocuments.Add(marketResearchReport);', 'FinalOutputDocuments.Add(approximateCostSheet);`r`n        FinalOutputDocuments.Add(approvalDocument);`r`n        FinalOutputDocuments.Add(marketResearchReport);')
$cs = $cs.Replace("FinalOutputDocuments.Add(approximateCostSheet);`n        FinalOutputDocuments.Add(marketResearchReport);", "FinalOutputDocuments.Add(approximateCostSheet);`n        FinalOutputDocuments.Add(approvalDocument);`n        FinalOutputDocuments.Add(marketResearchReport);")

$cs = $cs.Replace('DocumentsDatesView.Visibility = view is "Dates" or "Output" ? Visibility.Visible : Visibility.Collapsed;', 'DocumentsDatesView.Visibility = view is "Dates" or "Output" ? Visibility.Visible : Visibility.Collapsed;`r`n        ApprovalDocumentView.Visibility = view == "ApprovalDocument" ? Visibility.Visible : Visibility.Collapsed;')
$cs = $cs.Replace('var dataEntryView = view is "Institution" or "General" or "Firms" or "Offers" or "Dates" or "Output";', 'var dataEntryView = view is "Institution" or "General" or "Firms" or "Offers" or "ApprovalDocument" or "Dates" or "Output";')
$cs = $cs.Replace('NavFirmsOffers.Tag = view is "Firms" or "Offers" ? "Active" : null;', 'NavFirmsOffers.Tag = view is "Firms" or "Offers" ? "Active" : null;`r`n        NavApprovalDocument.Tag = view == "ApprovalDocument" ? "Active" : null;')

$navAnchor = @'
    private void NavDocumentsDates_Click(object sender, RoutedEventArgs e)
    {
        ShowView("Dates");
    }
'@
$navNew = @'
    private void NavApprovalDocument_Click(object sender, RoutedEventArgs e)
    {
        ShowView("ApprovalDocument");
    }

    private void NavDocumentsDates_Click(object sender, RoutedEventArgs e)
    {
        ShowView("Dates");
    }
'@
$cs = Replace-Once $cs $navAnchor $navNew 'Onay Belgesi nav handler'

$applyDateAnchor = @'
        CostCalculationDatePicker.SelectedDate = data.Dates.CostCalculationDate;
        // Eski kayıtlarda ayrı tutulan UYGUNDUR tarihi varsa tek Piyasa tarihi alanına taşınır.
'@
$applyDateNew = @'
        CostCalculationDatePicker.SelectedDate = data.Dates.CostCalculationDate;
        ApprovalDocumentDatePicker.SelectedDate = data.Dates.ApprovalDocumentDate;
        // Eski kayıtlarda ayrı tutulan UYGUNDUR tarihi varsa tek Piyasa tarihi alanına taşınır.
'@
$cs = Replace-Once $cs $applyDateAnchor $applyDateNew 'Onay tarihini UI yükle'

$institutionLoadAnchor = @'
        AuthorizedTitleBox.Text = data.Institution.AuthorizedTitle;
'@
$institutionLoadNew = @'
        AuthorizedTitleBox.Text = data.Institution.AuthorizedTitle;
        ApprovalDocumentNumberBox.Text = data.ApprovalDocument.DocumentNumber;
        ApprovalAvailableBudgetBox.Text = data.ApprovalDocument.AvailableBudgetAmount;
        ApprovalInvestmentProjectBox.Text = data.ApprovalDocument.InvestmentProjectNumber;
        ApprovalBudgetCodeBox.Text = data.ApprovalDocument.BudgetCode;
        ApprovalAdvanceConditionsBox.Text = data.ApprovalDocument.AdvanceConditions;
        ApprovalTenderProcedureBox.Text = string.IsNullOrWhiteSpace(data.ApprovalDocument.TenderProcedure) ? "4734 S.K. 22/d Kapsamında" : data.ApprovalDocument.TenderProcedure;
        ApprovalAnnouncementMethodBox.Text = string.IsNullOrWhiteSpace(data.ApprovalDocument.AnnouncementMethod) ? "İlansız" : data.ApprovalDocument.AnnouncementMethod;
        ApprovalPrequalificationSalePriceBox.Text = data.ApprovalDocument.PrequalificationDocumentSalePrice;
        ApprovalPriceDifferenceBasisBox.Text = string.IsNullOrWhiteSpace(data.ApprovalDocument.PriceDifferenceBasis) ? "Ödenmeyecektir" : data.ApprovalDocument.PriceDifferenceBasis;
        ApprovalNotesBox.Text = data.ApprovalDocument.Notes;
        ApprovalSubmittedByNameBox.Text = data.ApprovalDocument.SubmittedByName;
        ApprovalSubmittedByTitleBox.Text = data.ApprovalDocument.SubmittedByTitle;
'@
$cs = Replace-Once $cs $institutionLoadAnchor $institutionLoadNew 'Onay alanlarını UI yükle'

$buildDateAnchor = 'CostCalculationDate = CostCalculationDatePicker.SelectedDate, MarketReportDate = MarketReportDatePicker.SelectedDate,'
$buildDateNew = 'CostCalculationDate = CostCalculationDatePicker.SelectedDate, ApprovalDocumentDate = ApprovalDocumentDatePicker.SelectedDate, MarketReportDate = MarketReportDatePicker.SelectedDate,'
$cs = Replace-Once $cs $buildDateAnchor $buildDateNew 'Onay tarihini modele yaz'

$buildApprovalAnchor = @'
            PreferredWinnerFirmId = _preferredWinnerFirmId,
            AutoBackup = AutoBackupCheckBox.IsChecked == true
'@
$buildApprovalNew = @'
            ApprovalDocument = new DogruTemin.Core.Models.ApprovalDocumentInfo
            {
                DocumentNumber = ApprovalDocumentNumberBox.Text.Trim(),
                AvailableBudgetAmount = ApprovalAvailableBudgetBox.Text.Trim(),
                InvestmentProjectNumber = ApprovalInvestmentProjectBox.Text.Trim(),
                BudgetCode = ApprovalBudgetCodeBox.Text.Trim(),
                AdvanceConditions = ApprovalAdvanceConditionsBox.Text.Trim(),
                TenderProcedure = ApprovalTenderProcedureBox.Text.Trim(),
                AnnouncementMethod = ApprovalAnnouncementMethodBox.Text.Trim(),
                PrequalificationDocumentSalePrice = ApprovalPrequalificationSalePriceBox.Text.Trim(),
                PriceDifferenceBasis = ApprovalPriceDifferenceBasisBox.Text.Trim(),
                Notes = ApprovalNotesBox.Text.Trim(),
                SubmittedByName = ApprovalSubmittedByNameBox.Text.Trim(),
                SubmittedByTitle = ApprovalSubmittedByTitleBox.Text.Trim()
            },
            PreferredWinnerFirmId = _preferredWinnerFirmId,
            AutoBackup = AutoBackupCheckBox.IsChecked == true
'@
$cs = Replace-Once $cs $buildApprovalAnchor $buildApprovalNew 'Onay verisini modele yaz'

$cs = $cs.Replace('type is OutputDocumentType.ApproximateCostSheet`r`n            or OutputDocumentType.MarketResearchReport', 'type is OutputDocumentType.ApproximateCostSheet`r`n            or OutputDocumentType.ApprovalDocument`r`n            or OutputDocumentType.MarketResearchReport')
$cs = $cs.Replace("type is OutputDocumentType.ApproximateCostSheet`n            or OutputDocumentType.MarketResearchReport", "type is OutputDocumentType.ApproximateCostSheet`n            or OutputDocumentType.ApprovalDocument`n            or OutputDocumentType.MarketResearchReport")

$cs = $cs.Replace('OutputDocumentType.ApproximateCostSheet,`r`n            OutputDocumentType.MarketResearchReport,', 'OutputDocumentType.ApproximateCostSheet,`r`n            OutputDocumentType.ApprovalDocument,`r`n            OutputDocumentType.MarketResearchReport,')
$cs = $cs.Replace("OutputDocumentType.ApproximateCostSheet,`n            OutputDocumentType.MarketResearchReport,", "OutputDocumentType.ApproximateCostSheet,`n            OutputDocumentType.ApprovalDocument,`n            OutputDocumentType.MarketResearchReport,")

$docMapAnchor = '        "Yaklaşık Maliyet Hesap Cetveli" => OutputDocumentType.ApproximateCostSheet,'
$docMapNew = $docMapAnchor + "`r`n        " + '"Onay Belgesi" => OutputDocumentType.ApprovalDocument,'
$cs = Replace-Once $cs $docMapAnchor $docMapNew 'Onay belge tür eşlemesi'

$oldDatesDone = @'
        var datesDone = data.Dates.CostCalculationDate.HasValue
            && data.Dates.MarketReportDate.HasValue
            && data.Dates.InspectionReportDate.HasValue
            && data.Dates.DeliveryDate.HasValue
            && dateWarnings.Count == 0;
        SetHomeStage(HomeStageDatesStatus, HomeStageDatesDetail, datesDone,
            datesDone ? "4 belge tarihi hazır ve zorunlu sıra geçerli." : "Belge tarihlerini tamamlayın; yanlış sıra varsa uyarı verilir.");
'@
$newDatesDone = @'
        var datesDone = data.Dates.AssignmentDate.HasValue
            && data.Dates.CostCalculationDate.HasValue
            && data.Dates.ApprovalDocumentDate.HasValue
            && data.Dates.MarketReportDate.HasValue
            && data.Dates.InspectionReportDate.HasValue
            && data.Dates.DeliveryDate.HasValue
            && dateWarnings.Count == 0;
        SetHomeStage(HomeStageDatesStatus, HomeStageDatesDetail, datesDone,
            datesDone ? "Belge tarihleri hazır ve zorunlu kronoloji geçerli." : "Kurul/komisyon, yaklaşık maliyet, Onay Belgesi, piyasa ve muayene/kabul tarihlerini kontrol edin.");
'@
$cs = Replace-Once $cs $oldDatesDone $newDatesDone 'ana ekran tarih hazır durumu'

$nextAnchor = @'
        else if (!datesDone)
        {
            _homeNextView = "Dates";
            HomeNextStepText.Text = "Belge tarihlerini girin ve tarih sırasını kontrol edin.";
        }
'@
$nextNew = @'
        else if (!data.Dates.ApprovalDocumentDate.HasValue || string.IsNullOrWhiteSpace(data.ApprovalDocument.AvailableBudgetAmount)
            || string.IsNullOrWhiteSpace(data.ApprovalDocument.SubmittedByName) || string.IsNullOrWhiteSpace(data.ApprovalDocument.SubmittedByTitle))
        {
            _homeNextView = "ApprovalDocument";
            HomeNextStepText.Text = "Onay Belgesi'ne özgü bilgileri tamamlayın.";
        }
        else if (!datesDone)
        {
            _homeNextView = "Dates";
            HomeNextStepText.Text = "Belge tarihlerini girin ve tarih sırasını kontrol edin.";
        }
'@
$cs = Replace-Once $cs $nextAnchor $nextNew 'ana ekran Onay Belgesi sonraki adım'

$clearAnchor = @'
            case "Dates":
'@
$clearNew = @'
            case "ApprovalDocument":
                if (!ApprovalDocumentDatePicker.SelectedDate.HasValue
                    && string.IsNullOrWhiteSpace(ApprovalDocumentNumberBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalAvailableBudgetBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalInvestmentProjectBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalBudgetCodeBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalAdvanceConditionsBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalPrequalificationSalePriceBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalNotesBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalSubmittedByNameBox.Text)
                    && string.IsNullOrWhiteSpace(ApprovalSubmittedByTitleBox.Text)) return;
                if (!ConfirmEditorClear("Onay Belgesi'ne özgü giriş alanları temizlensin mi?")) return;
                _suppressDirtyTracking = true;
                try
                {
                    ApprovalDocumentDatePicker.SelectedDate = null;
                    ApprovalDocumentNumberBox.Text = string.Empty;
                    ApprovalAvailableBudgetBox.Text = string.Empty;
                    ApprovalInvestmentProjectBox.Text = string.Empty;
                    ApprovalBudgetCodeBox.Text = string.Empty;
                    ApprovalAdvanceConditionsBox.Text = string.Empty;
                    ApprovalTenderProcedureBox.Text = "4734 S.K. 22/d Kapsamında";
                    ApprovalAnnouncementMethodBox.Text = "İlansız";
                    ApprovalPrequalificationSalePriceBox.Text = string.Empty;
                    ApprovalPriceDifferenceBasisBox.Text = "Ödenmeyecektir";
                    ApprovalNotesBox.Text = string.Empty;
                    ApprovalSubmittedByNameBox.Text = string.Empty;
                    ApprovalSubmittedByTitleBox.Text = string.Empty;
                }
                finally { _suppressDirtyTracking = false; }
                MarkDirty();
                break;
            case "Dates":
'@
$cs = Replace-Once $cs $clearAnchor $clearNew 'Onay Belgesi temizle davranışı'

Write-Text $csPath $cs

# ---------------- StorageService.cs ----------------
$storagePath = 'DogruTemin.Core/Services/StorageService.cs'
$storage = Read-Text $storagePath
$storage = $storage.Replace('data.SchemaVersion = Math.Max(data.SchemaVersion, 3);', 'data.SchemaVersion = Math.Max(data.SchemaVersion, 4);')
$storage = $storage.Replace('data.Dates ??= new(); data.Institution ??= new();', 'data.Dates ??= new(); data.Institution ??= new(); data.ApprovalDocument ??= new();')
Write-Text $storagePath $storage

# ---------------- ValidationService.cs ----------------
$validationPath = 'DogruTemin.Core/Services/ValidationService.cs'
$validation = Read-Text $validationPath
$validation = $validation.Replace('RequireDate(data.Dates.CostCalculationDate, issues, "Yaklaşık Maliyet Hesap Cetveli tarihi");`r`n        RequireDate(data.Dates.MarketReportDate', 'RequireDate(data.Dates.CostCalculationDate, issues, "Yaklaşık Maliyet Hesap Cetveli tarihi");`r`n        RequireDate(data.Dates.ApprovalDocumentDate, issues, "Onay Belgesi tarihi");`r`n        RequireDate(data.Dates.MarketReportDate')
$validation = $validation.Replace("RequireDate(data.Dates.CostCalculationDate, issues, \"Yaklaşık Maliyet Hesap Cetveli tarihi\");`n        RequireDate(data.Dates.MarketReportDate", "RequireDate(data.Dates.CostCalculationDate, issues, \"Yaklaşık Maliyet Hesap Cetveli tarihi\");`n        RequireDate(data.Dates.ApprovalDocumentDate, issues, \"Onay Belgesi tarihi\");`n        RequireDate(data.Dates.MarketReportDate")

$oldDateComment = @'
        // DoğruTemin'in TEK zorunlu kronolojik tarih kuralı:
        // Yaklaşık Maliyet Hesap Cetveli < Piyasa Fiyat Araştırması Tutanağı < Muayene ve Kabul Tutanağı.
        // Bunların dışındaki tarihler kullanıcı kararına bağlıdır ve birbirleriyle kıyaslanmaz.
'@
$newDateComment = @'
        // Zorunlu kronoloji:
        // Kurul/Komisyon Onay <= Yaklaşık Maliyet < Onay Belgesi < Piyasa Fiyat Araştırması <= Muayene/Kabul.
        // Harcama belgesi ve teslim/kabul tarihi bu zincirin dışında ayrıca yönetilir.
'@
$validation = Replace-Once $validation $oldDateComment $newDateComment 'tarih kuralı açıklaması'

$oldDateFunc = @'
    private static void AddRequiredDateSequenceIssues(ProcessDates d, List<ValidationIssue> issues, ValidationSeverity severity)
    {
        // Eşit tarih de geçerli değildir; kullanıcı açıkça "<" sıralamasını istemiştir.
        if (d.CostCalculationDate.HasValue && d.MarketReportDate.HasValue &&
            d.CostCalculationDate.Value.Date >= d.MarketReportDate.Value.Date)
        {
            AddDateSequenceIssue(issues, severity,
                "Yaklaşık Maliyet Hesap Cetveli Tarihi, Piyasa Fiyat Araştırması Tutanağı tarihinden MUTLAKA önce olmalıdır.");
        }

        if (d.MarketReportDate.HasValue && d.InspectionReportDate.HasValue &&
            d.MarketReportDate.Value.Date >= d.InspectionReportDate.Value.Date)
        {
            AddDateSequenceIssue(issues, severity,
                "Piyasa Fiyat Araştırması Tutanağı tarihi, Muayene ve Kabul Tutanağı tarihinden MUTLAKA önce olmalıdır.");
        }
    }
'@
$newDateFunc = @'
    private static void AddRequiredDateSequenceIssues(ProcessDates d, List<ValidationIssue> issues, ValidationSeverity severity)
    {
        if (d.AssignmentDate.HasValue && d.CostCalculationDate.HasValue &&
            d.AssignmentDate.Value.Date > d.CostCalculationDate.Value.Date)
            AddDateSequenceIssue(issues, severity, "Kurul / Komisyon Onay Tarihi, Yaklaşık Maliyet Hesap Cetveli tarihinden sonra olamaz.");

        if (d.CostCalculationDate.HasValue && d.ApprovalDocumentDate.HasValue &&
            d.CostCalculationDate.Value.Date >= d.ApprovalDocumentDate.Value.Date)
            AddDateSequenceIssue(issues, severity, "Yaklaşık Maliyet Hesap Cetveli Tarihi, Onay Belgesi tarihinden MUTLAKA önce olmalıdır.");

        if (d.ApprovalDocumentDate.HasValue && d.MarketReportDate.HasValue &&
            d.ApprovalDocumentDate.Value.Date >= d.MarketReportDate.Value.Date)
            AddDateSequenceIssue(issues, severity, "Onay Belgesi tarihi, Piyasa Fiyat Araştırması Tutanağı tarihinden MUTLAKA önce olmalıdır.");

        if (d.MarketReportDate.HasValue && d.InspectionReportDate.HasValue &&
            d.MarketReportDate.Value.Date > d.InspectionReportDate.Value.Date)
            AddDateSequenceIssue(issues, severity, "Piyasa Fiyat Araştırması Tutanağı tarihi, Muayene ve Kabul Tutanağı tarihinden sonra olamaz.");
    }
'@
$validation = Replace-Once $validation $oldDateFunc $newDateFunc 'yeni tarih zinciri'

$caseAnchor = @'
            case OutputDocumentType.MarketResearchReport:
'@
$caseNew = @'
            case OutputDocumentType.ApprovalDocument:
                RequireDate(data.Dates.ApprovalDocumentDate, issues, "Onay Belgesi tarihi");
                RequireText(data.ApprovalDocument.AvailableBudgetAmount, issues, "Kullanılabilir ödenek tutarı");
                RequireText(data.ApprovalDocument.SubmittedByName, issues, "Onaya sunan ad soyad");
                RequireText(data.ApprovalDocument.SubmittedByTitle, issues, "Onaya sunan unvanı");
                RequireText(data.Institution.AuthorizedName, issues, "Harcama yetkilisi adı soyadı");
                RequireText(data.Institution.AuthorizedDuty, issues, "Harcama yetkilisi görevi");
                RequireCommission(data.MarketCommission, issues, "Piyasa araştırma görevlileri");
                RequireCommission(data.InspectionCommission, issues, "Muayene ve kabul komisyonu");
                RequireAtLeastThreeFirms(firms, issues);
                foreach (var firm in firms)
                    foreach (var product in products)
                        if (!(OfferFor(firm, product.Id)?.ApproximateUnitPrice > 0)) Critical(issues, "Offers", $"{firm.No}. firma / {product.No}. ürün yaklaşık fiyatı");
                break;
            case OutputDocumentType.MarketResearchReport:
'@
$validation = Replace-Once $validation $caseAnchor $caseNew 'Onay Belgesi doğrulaması'

$validation = $validation.Replace('data.Dates.ApprovalDate.HasValue,`r`n            !string.IsNullOrWhiteSpace(data.Dates.ApprovalNo)', 'data.Dates.ApprovalDate.HasValue,`r`n            !string.IsNullOrWhiteSpace(data.Dates.ApprovalNo),`r`n            data.Dates.ApprovalDocumentDate.HasValue,`r`n            !string.IsNullOrWhiteSpace(data.ApprovalDocument.AvailableBudgetAmount)')
$validation = $validation.Replace("data.Dates.ApprovalDate.HasValue,`n            !string.IsNullOrWhiteSpace(data.Dates.ApprovalNo)", "data.Dates.ApprovalDate.HasValue,`n            !string.IsNullOrWhiteSpace(data.Dates.ApprovalNo),`n            data.Dates.ApprovalDocumentDate.HasValue,`n            !string.IsNullOrWhiteSpace(data.ApprovalDocument.AvailableBudgetAmount)")
Write-Text $validationPath $validation

# ---------------- OfficialDocumentService.cs ----------------
$officialPath = 'DogruTemin.Wpf/Services/OfficialDocumentService.cs'
$official = Read-Text $officialPath
$official = $official.Replace('OutputDocumentType.ApproximateCostSheet => BuildApproximateCostSheetPage(data),', 'OutputDocumentType.ApproximateCostSheet => BuildApproximateCostSheetPage(data),`r`n                OutputDocumentType.ApprovalDocument => BuildApprovalDocumentPage(data),')
$official = $official.Replace('or OutputDocumentType.ApproximateCostSheet`r`n            or OutputDocumentType.FirmOfferForm', 'or OutputDocumentType.ApproximateCostSheet`r`n            or OutputDocumentType.ApprovalDocument`r`n            or OutputDocumentType.FirmOfferForm')
$official = $official.Replace("or OutputDocumentType.ApproximateCostSheet`n            or OutputDocumentType.FirmOfferForm", "or OutputDocumentType.ApproximateCostSheet`n            or OutputDocumentType.ApprovalDocument`n            or OutputDocumentType.FirmOfferForm")

$approvalPage = @'
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

'@
$official = Replace-Once $official '    // Eski Excel formu görsel referanstır; yeni iş kuralı ise alımın tamamında tek kazanan firmadır.' ($approvalPage + '    // Eski Excel formu görsel referanstır; yeni iş kuralı ise alımın tamamında tek kazanan firmadır.') 'Onay Belgesi resmi sayfa'
Write-Text $officialPath $official

# ---------------- UiAccessibilityService.cs ----------------
$accessPath = 'DogruTemin.Wpf/UiAccessibilityService.cs'
$access = Read-Text $accessPath
$access = $access.Replace('"AssignmentDatePicker", "ApprovalDatePicker", "CostCalculationDatePicker", "MarketReportDatePicker",', '"AssignmentDatePicker", "ApprovalDatePicker", "CostCalculationDatePicker", "ApprovalDocumentDatePicker", "MarketReportDatePicker",')
$access = $access.Replace('"InspectionReportDatePicker", "DeliveryDatePicker", "OfferDatePicker", "ArchiveSearchBox"', '"InspectionReportDatePicker", "DeliveryDatePicker", "ApprovalDocumentNumberBox", "ApprovalAvailableBudgetBox",`r`n        "ApprovalInvestmentProjectBox", "ApprovalBudgetCodeBox", "ApprovalAdvanceConditionsBox", "ApprovalTenderProcedureBox",`r`n        "ApprovalAnnouncementMethodBox", "ApprovalPrequalificationSalePriceBox", "ApprovalPriceDifferenceBasisBox",`r`n        "ApprovalNotesBox", "ApprovalSubmittedByNameBox", "ApprovalSubmittedByTitleBox", "OfferDatePicker", "ArchiveSearchBox"')
Write-Text $accessPath $access

# ---------------- ValidationTests.cs ----------------
$testsPath = 'DogruTemin.Tests/ValidationTests.cs'
$tests = Read-Text $testsPath
$oldTest = @'
    [Fact]
    public void MarketDateMustBeAfterCostCalculationDate()
    {
        var data = new ProcurementData
        {
            Dates = new ProcessDates
            {
                CostCalculationDate = new DateTime(2026, 9, 10),
                MarketReportDate = new DateTime(2026, 9, 10)
            }
        };

        var issues = ValidationService.DateLogicWarnings(data);

        Assert.Contains(issues, issue => issue.Message.Contains("MUTLAKA önce", StringComparison.Ordinal));
    }
'@
$newTest = @'
    [Fact]
    public void RequiredDateChainAllowsEqualityOnlyAtOuterLinks()
    {
        var data = new ProcurementData
        {
            Dates = new ProcessDates
            {
                AssignmentDate = new DateTime(2026, 9, 10),
                CostCalculationDate = new DateTime(2026, 9, 10),
                ApprovalDocumentDate = new DateTime(2026, 9, 11),
                MarketReportDate = new DateTime(2026, 9, 12),
                InspectionReportDate = new DateTime(2026, 9, 12)
            }
        };

        Assert.Empty(ValidationService.DateLogicWarnings(data));
    }

    [Fact]
    public void CostDateMustBeBeforeApprovalDocumentDate()
    {
        var data = new ProcurementData { Dates = new ProcessDates
        {
            CostCalculationDate = new DateTime(2026, 9, 10),
            ApprovalDocumentDate = new DateTime(2026, 9, 10)
        }};
        Assert.Contains(ValidationService.DateLogicWarnings(data), i => i.Message.Contains("Onay Belgesi", StringComparison.Ordinal));
    }

    [Fact]
    public void ApprovalDocumentDateMustBeBeforeMarketDate()
    {
        var data = new ProcurementData { Dates = new ProcessDates
        {
            ApprovalDocumentDate = new DateTime(2026, 9, 12),
            MarketReportDate = new DateTime(2026, 9, 12)
        }};
        Assert.Contains(ValidationService.DateLogicWarnings(data), i => i.Message.Contains("Piyasa Fiyat", StringComparison.Ordinal));
    }

    [Fact]
    public void CommissionApprovalCannotBeAfterCostDate()
    {
        var data = new ProcurementData { Dates = new ProcessDates
        {
            AssignmentDate = new DateTime(2026, 9, 11),
            CostCalculationDate = new DateTime(2026, 9, 10)
        }};
        Assert.NotEmpty(ValidationService.DateLogicWarnings(data));
    }

    [Fact]
    public void MarketAndInspectionDatesMayBeEqualButNotReverse()
    {
        var equal = new ProcurementData { Dates = new ProcessDates
        {
            MarketReportDate = new DateTime(2026, 9, 12),
            InspectionReportDate = new DateTime(2026, 9, 12)
        }};
        Assert.Empty(ValidationService.DateLogicWarnings(equal));

        equal.Dates.MarketReportDate = new DateTime(2026, 9, 13);
        Assert.NotEmpty(ValidationService.DateLogicWarnings(equal));
    }
'@
$tests = Replace-Once $tests $oldTest $newTest 'tarih zinciri testleri'
Write-Text $testsPath $tests

# XAML yapısal doğrulama
[xml](Get-Content $xamlPath -Raw) | Out-Null

# Derleme ve test kalite kapısı
& dotnet restore | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore başarısız' }
& dotnet build -c Release --no-restore | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Release build başarısız' }
& dotnet test -c Release --no-build | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Testler başarısız' }

# Değişiklikleri commit et
git config user.name 'mcalimlier'
git config user.email '123293878+mcalimlier@users.noreply.github.com'
git add DogruTemin.Core DogruTemin.Wpf DogruTemin.Tests
if (-not (git diff --cached --quiet)) {
    git commit -m 'Onay Belgesini ve yeni tarih kronolojisini ekle'
    git push
}

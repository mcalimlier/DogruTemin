using DogruTemin.Wpf.Models;
using ProcurementData = DogruTemin.Core.Models.ProcurementData;
using OutputDocumentType = DogruTemin.Core.Models.OutputDocumentType;
using FirmOfferCore = DogruTemin.Core.Models.FirmOffer;
using DogruTemin.Core.Services;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace DogruTemin.Wpf;

public partial class MainWindow
{
    private void ApplyDataToUi(ProcurementData data)
    {
        ProcurementLimits.EnsureCapacity(data);
        Firms.Clear();
        Products.Clear();
        InspectionCommission.Clear();
        MarketCommission.Clear();
        OfferRows.Clear();
        _offerEditingFirm = null;
        _offerEditorDirty = false;
        _suppressOfferSelection = true;
        OfferFirmSelector.SelectedItem = null;
        _suppressOfferSelection = false;

        foreach (var p in data.Products.Take(ProcurementLimits.MaxProducts))
            Products.Add(new ProductItem { Id = p.Id, No = p.No, Name = p.Name, Specification = p.Specification, Unit = p.Unit, Quantity = p.Quantity });
        foreach (var f in data.Firms.Take(5))
            Firms.Add(new Firm
            {
                Id = f.Id, No = f.No, Name = f.Name, TaxOffice = f.TaxOffice, TaxNo = f.TaxNo,
                ContactName = f.ContactName, Phone = PhoneForEntry(f.Phone), Email = f.Email, Address = f.Address, OfferDate = f.OfferDate,
                Offers = f.Offers.Select(o => new FirmOfferCore { ProductId = o.ProductId, ApproximateUnitPrice = o.ApproximateUnitPrice, FinalUnitPrice = o.FinalUnitPrice }).ToList()
            });
        foreach (var m in data.InspectionCommission.Take(3)) InspectionCommission.Add(new CommissionMember { No = m.No, Name = m.Name, Title = m.Title });
        foreach (var m in data.MarketCommission.Take(3)) MarketCommission.Add(new CommissionMember { No = m.No, Name = m.Name, Title = m.Title });

        JobTitleBox.Text = data.JobTitle;
        AssignmentDatePicker.SelectedDate = data.Dates.AssignmentDate;
        AssignmentNoBox.Text = data.Dates.AssignmentNo;
        ApprovalDatePicker.SelectedDate = data.Dates.ApprovalDate;
        ApprovalNoBox.Text = TextNormalizer.Hys(data.Dates.ApprovalNo);
        CostCalculationDatePicker.SelectedDate = data.Dates.CostCalculationDate;
        ApprovalDocumentDatePicker.SelectedDate = data.Dates.ApprovalDocumentDate;
        MarketReportDatePicker.SelectedDate = data.Dates.MarketReportDate ?? data.Dates.MarketApprovalDate;
        InspectionReportDatePicker.SelectedDate = data.Dates.InspectionReportDate;
        DeliveryDatePicker.SelectedDate = data.Dates.DeliveryDate;

        InstitutionTopAuthorityBox.Text = data.Institution.TopAuthority;
        InstitutionFullNameBox.Text = !string.IsNullOrWhiteSpace(data.Institution.FullName) ? data.Institution.FullName : data.Institution.ShortName;
        InstitutionProvinceBox.Text = data.Institution.Province;
        InstitutionAddressBox.Text = data.Institution.Address;
        InstitutionPhoneBox.Text = data.Institution.Phone;
        InstitutionEmailBox.Text = data.Institution.Email;
        AuthorizedNameBox.Text = data.Institution.AuthorizedName;
        AuthorizedDutyBox.Text = data.Institution.AuthorizedDuty;
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
        AutoBackupCheckBox.IsChecked = data.AutoBackup;
        _operationNumber = data.OperationNumber ?? string.Empty;
        _recordId = data.RecordId ?? string.Empty;
        _recordRevision = data.Revision;
        _lastOutput = data.LastOutput;
        _recordCreatedAt = data.CreatedAt == default ? DateTime.Now : data.CreatedAt;
        _preferredWinnerFirmId = data.PreferredWinnerFirmId ?? string.Empty;
        ReNumberFirms();
        ReNumberProducts();
    }

    private ProcurementData BuildDataFromUi()
    {
        return new ProcurementData
        {
            RecordId = _recordId,
            Revision = _recordRevision,
            LastOutput = _lastOutput,
            CreatedAt = _recordCreatedAt,
            OperationNumber = _operationNumber,
            JobTitle = JobTitleBox.Text.Trim(),
            Products = Products.Select(p => new DogruTemin.Core.Models.ProductItem
            {
                Id = p.Id, No = p.No, Name = p.Name.Trim(), Specification = p.Specification.Trim(), Unit = p.Unit, Quantity = p.Quantity.Trim()
            }).ToList(),
            Firms = Firms.Select(f => new DogruTemin.Core.Models.Firm
            {
                Id = f.Id, No = f.No, Name = f.Name.Trim(), TaxOffice = f.TaxOffice.Trim(), TaxNo = f.TaxNo.Trim(),
                ContactName = f.ContactName.Trim(), Phone = PhoneForStorage(f.Phone), Email = f.Email.Trim().ToLowerInvariant(), Address = f.Address.Trim(), OfferDate = f.OfferDate,
                Offers = f.Offers.Select(o => new FirmOfferCore { ProductId = o.ProductId, ApproximateUnitPrice = o.ApproximateUnitPrice, FinalUnitPrice = o.FinalUnitPrice }).ToList()
            }).ToList(),
            InspectionCommission = InspectionCommission.Take(3).Select(m => new DogruTemin.Core.Models.CommissionMember { No = m.No, Name = m.Name.Trim(), Title = m.Title.Trim() }).ToList(),
            MarketCommission = MarketCommission.Take(3).Select(m => new DogruTemin.Core.Models.CommissionMember { No = m.No, Name = m.Name.Trim(), Title = m.Title.Trim() }).ToList(),
            Dates = new DogruTemin.Core.Models.ProcessDates
            {
                AssignmentDate = AssignmentDatePicker.SelectedDate, AssignmentNo = AssignmentNoBox.Text.Trim(),
                ApprovalDate = ApprovalDatePicker.SelectedDate, ApprovalNo = TextNormalizer.Hys(ApprovalNoBox.Text),
                CostCalculationDate = CostCalculationDatePicker.SelectedDate, ApprovalDocumentDate = ApprovalDocumentDatePicker.SelectedDate, MarketReportDate = MarketReportDatePicker.SelectedDate,
                InspectionReportDate = InspectionReportDatePicker.SelectedDate,
                DeliveryDate = DeliveryDatePicker.SelectedDate,
                MarketApprovalDate = MarketReportDatePicker.SelectedDate,
                OfferDeadlineDate = null
            },
            Institution = new DogruTemin.Core.Models.InstitutionInfo
            {
                TopAuthority = InstitutionTopAuthorityBox.Text.Trim(), ShortName = InstitutionFullNameBox.Text.Trim(), FullName = InstitutionFullNameBox.Text.Trim(),
                Province = InstitutionProvinceBox.Text.Trim(), Address = InstitutionAddressBox.Text.Trim(), Phone = InstitutionPhoneBox.Text.Trim(),
                Email = InstitutionEmailBox.Text.Trim().ToLowerInvariant(), AuthorizedName = AuthorizedNameBox.Text.Trim(),
                AuthorizedDuty = AuthorizedDutyBox.Text.Trim(), AuthorizedTitle = AuthorizedTitleBox.Text.Trim()
            },
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
        };
    }

    private void UpdateHomeProcessMap(ProcurementData data)
    {
        var products = data.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Take(ProcurementLimits.MaxProducts).ToList();
        var firms = data.Firms.Where(f => !string.IsNullOrWhiteSpace(f.Name)).Take(5).ToList();

        var institutionDone = !string.IsNullOrWhiteSpace(data.Institution.TopAuthority)
            && !string.IsNullOrWhiteSpace(data.Institution.FullName)
            && !string.IsNullOrWhiteSpace(data.Institution.Province)
            && !string.IsNullOrWhiteSpace(data.Institution.Address)
            && ValidationService.ValidPhone(data.Institution.Phone)
            && ValidationService.ValidEmail(data.Institution.Email);

        var inspectionCommissionDone = data.InspectionCommission.Take(3).Count(m => !string.IsNullOrWhiteSpace(m.Name) && !string.IsNullOrWhiteSpace(m.Title)) == 3;
        var marketCommissionDone = data.MarketCommission.Take(3).Count(m => !string.IsNullOrWhiteSpace(m.Name) && !string.IsNullOrWhiteSpace(m.Title)) == 3;
        var commissionDone = inspectionCommissionDone && marketCommissionDone;
        var institutionCommissionDone = institutionDone && commissionDone;

        SetHomeCombinedStage(HomeStageCommissionStatus, HomeStageCommissionDetail, institutionDone, commissionDone,
            "Kurum bilgisi ve iki komisyon hazır.",
            institutionDone ? "Kurum hazır • komisyonları tamamlayın." : commissionDone ? "Komisyonlar hazır • kurum bilgisini tamamlayın." : "Kurum bilgisi ve iki komisyonu tamamlayın.");

        var productsValid = products.Count > 0 && products.All(p => p.Unit is "Adet" or "Miktar" && NumberParser.TryPositiveDecimal(p.Quantity, out _));
        var generalDone = !string.IsNullOrWhiteSpace(data.JobTitle) && productsValid && data.Dates.AssignmentDate.HasValue && !string.IsNullOrWhiteSpace(data.Dates.AssignmentNo) && data.Dates.ApprovalDate.HasValue && !string.IsNullOrWhiteSpace(data.Dates.ApprovalNo);
        SetHomeStage(HomeStageGeneralStatus, HomeStageGeneralDetail, generalDone,
            generalDone ? $"{products.Count} ürün ve temel işlem bilgileri hazır." : "İş başlığı, ürünler ve görevlendirme/harcama bilgilerini tamamlayın.");

        var firmsDone = firms.Count >= 3;
        var offersDone = firmsDone && products.Count > 0 && firms.All(f => products.All(p =>
        {
            var offer = f.Offers.FirstOrDefault(o => o.ProductId == p.Id);
            return offer?.ApproximateUnitPrice > 0m && offer.FinalUnitPrice > 0m;
        }));
        SetHomeCombinedStage(HomeStageFirmsStatus, HomeStageFirmsDetail, firmsDone, offersDone,
            $"{firms.Count} firma ve tüm teklifler hazır.",
            firmsDone ? $"{firms.Count} firma hazır • tekliflerde eksik var." : $"En az 3 firma gerekli; şu an {firms.Count} firma var.");

        var dateWarnings = ValidationService.DateLogicWarnings(data);
        var datesDone = data.Dates.AssignmentDate.HasValue && data.Dates.CostCalculationDate.HasValue && data.Dates.ApprovalDocumentDate.HasValue && data.Dates.MarketReportDate.HasValue && data.Dates.InspectionReportDate.HasValue && data.Dates.DeliveryDate.HasValue && dateWarnings.Count == 0;
        SetHomeStage(HomeStageDatesStatus, HomeStageDatesDetail, datesDone,
            datesDone ? "Belge tarihleri hazır ve zorunlu kronoloji geçerli." : "Kurul/komisyon, yaklaşık maliyet, Onay Belgesi, piyasa ve muayene/kabul tarihlerini kontrol edin.");

        var preparationTypes = new[] { OutputDocumentType.ApproximateRequest, OutputDocumentType.FirmOfferForm };
        var finalProcessTypes = new[] { OutputDocumentType.ApproximateCostSheet, OutputDocumentType.ApprovalDocument, OutputDocumentType.MarketResearchReport, OutputDocumentType.InspectionAcceptanceReport };
        var preparationIssues = DocumentReadinessService.GetExportCoreIssues(data, preparationTypes);
        var finalProcessIssues = DocumentReadinessService.GetExportCoreIssues(data, finalProcessTypes);
        var preparationReady = preparationIssues.Count == 0;
        var finalProcessReady = finalProcessIssues.Count == 0;
        var outputDone = preparationReady && finalProcessReady;
        SetHomeOutputStage(preparationReady, finalProcessReady, preparationIssues.Count, finalProcessIssues.Count);

        var firmsOffersDone = firmsDone && offersDone;
        var doneCount = new[] { institutionCommissionDone, generalDone, firmsOffersDone, datesDone, outputDone }.Count(x => x);
        HomeStageSummaryText.Text = $"{doneCount} / 5 TAMAM";

        if (!institutionCommissionDone)
        {
            _homeNextView = "Institution";
            HomeNextStepText.Text = "Kurum bilgisi ile piyasa ve muayene/kabul komisyonlarını tamamlayın.";
        }
        else if (!generalDone)
        {
            _homeNextView = "General";
            HomeNextStepText.Text = "İşlem, ürün ve görevlendirme/harcama bilgilerini tamamlayın.";
        }
        else if (!firmsDone)
        {
            _homeNextView = "Firms";
            HomeNextStepText.Text = "En az 3 firma ekleyin.";
        }
        else if (!offersDone)
        {
            _homeNextView = "Offers";
            HomeNextStepText.Text = "Firmaların yaklaşık ve nihai tekliflerini tamamlayın.";
        }
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
        else
        {
            _homeNextView = "Output";
            HomeNextStepText.Text = outputDone
                ? "Süreç hazır. Resmî belgeleri önizleyebilir, PDF kaydedebilir veya yazdırabilirsiniz."
                : "Çıktı ekranındaki kritik eksikleri giderin ve belgeleri önizleyin.";
        }
    }

    private static void SetHomeCombinedStage(TextBlock statusBlock, TextBlock detailBlock, bool firstReady, bool secondReady, string readyDetail, string incompleteDetail)
    {
        if (firstReady && secondReady)
        {
            statusBlock.Text = "TAMAM";
            statusBlock.Foreground = new SolidColorBrush(Color.FromRgb(126, 226, 166));
            detailBlock.Text = readyDetail;
            return;
        }
        if (firstReady || secondReady)
        {
            statusBlock.Text = "KISMİ";
            statusBlock.Foreground = new SolidColorBrush(Color.FromRgb(143, 183, 216));
        }
        else
        {
            statusBlock.Text = "EKSİK";
            statusBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 191, 54));
        }
        detailBlock.Text = incompleteDetail;
    }

    private static void SetHomeStage(TextBlock statusBlock, TextBlock detailBlock, bool completed, string detail)
    {
        statusBlock.Text = completed ? "TAMAM" : "EKSİK";
        statusBlock.Foreground = completed ? new SolidColorBrush(Color.FromRgb(126, 226, 166)) : new SolidColorBrush(Color.FromRgb(255, 191, 54));
        detailBlock.Text = detail;
    }

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

    private void UpdateHomeActivitySummary()
    {
        if (HomeHeroLastSavedText is null) return;
        HomeHeroLastSavedText.Text = _lastSavedAt.HasValue ? _lastSavedAt.Value.ToString("dd.MM.yyyy • HH:mm", CultureInfo.InvariantCulture) : "Henüz kaydedilmedi";
    }

    private void UpdateDerivedState(ProcurementData? supplied = null)
    {
        if (_suppressDirtyTracking && supplied is null) return;
        var data = supplied ?? BuildDataFromUi();
        var calc = ProcurementEngine.Calculate(data);
        var readiness = ValidationService.ReadinessPercent(data);
        var filledCommission = data.InspectionCommission.Count(m => !string.IsNullOrWhiteSpace(m.Name) && !string.IsNullOrWhiteSpace(m.Title))
            + data.MarketCommission.Count(m => !string.IsNullOrWhiteSpace(m.Name) && !string.IsNullOrWhiteSpace(m.Title));
        var firmCount = data.Firms.Count(f => !string.IsNullOrWhiteSpace(f.Name));
        var productCount = data.Products.Count(p => !string.IsNullOrWhiteSpace(p.Name));

        HomeProductCountText.Text = productCount.ToString(CultureInfo.InvariantCulture);
        HomeProductCountHintText.Text = productCount > 0 ? $"{productCount} / 10 kayıtlı" : "En az 1 gerekli";
        HomeFirmCountText.Text = firmCount.ToString(CultureInfo.InvariantCulture);
        HomeFirmCountHintText.Text = firmCount >= 3 ? $"{firmCount} firma hazır" : $"{firmCount} / en az 3";
        ActiveOperationText.Text = !string.IsNullOrWhiteSpace(data.JobTitle) ? data.JobTitle : (!string.IsNullOrWhiteSpace(data.OperationNumber) ? data.OperationNumber : "Yeni İşlem");
        HeaderReadinessText.Text = $"%{readiness}";
        UpdateReadinessRing(readiness);
        HomeJobTitleText.Text = string.IsNullOrWhiteSpace(data.JobTitle) ? "Henüz aktif işlem başlığı girilmedi" : data.JobTitle;
        HomeApprovalDateText.Text = data.Dates.ApprovalDate.HasValue ? $"Harcama belgesi tarihi: {data.Dates.ApprovalDate:dd.MM.yyyy}" : "Harcama belgesi tarihi: Henüz belirtilmedi";
        HomeOperationNumberText.Text = string.IsNullOrWhiteSpace(data.OperationNumber) ? "Henüz yok" : data.OperationNumber;
        HomeApproximateTotalText.Text = calc.ApproximateCostTotal > 0m ? NumberParser.Money(calc.ApproximateCostTotal) : "—";
        HomeFinalTotalText.Text = calc.MarketResultTotal > 0m ? NumberParser.Money(calc.MarketResultTotal) : "—";

        if (calc.ApproximateCostTotal > 0m && calc.MarketResultTotal > 0m)
        {
            var difference = calc.ApproximateCostTotal - calc.MarketResultTotal;
            var differencePercent = calc.ApproximateCostTotal == 0m ? 0m : difference / calc.ApproximateCostTotal * 100m;
            HomeDifferenceText.Text = NumberParser.Money(difference);
            HomeDifferenceText.Foreground = difference >= 0m ? new SolidColorBrush(Color.FromRgb(126, 226, 166)) : new SolidColorBrush(Color.FromRgb(255, 165, 165));
            HomeDifferenceHintText.Text = difference >= 0m ? $"Yaklaşık maliyete göre %{Math.Abs(differencePercent):0.0} düşük" : $"Yaklaşık maliyete göre %{Math.Abs(differencePercent):0.0} yüksek";
        }
        else
        {
            HomeDifferenceText.Text = "—";
            HomeDifferenceText.Foreground = new SolidColorBrush(Color.FromRgb(255, 226, 161));
            HomeDifferenceHintText.Text = "Hesaplama bekleniyor";
        }

        if (calc.HasWinnerTie && string.IsNullOrWhiteSpace(calc.WinningFirmId))
        {
            HomeWinnerText.Text = "Seçim gerekli";
            HomeWinnerHintText.Text = "Eşit en düşük teklif var";
        }
        else if (!string.IsNullOrWhiteSpace(calc.WinningFirmName))
        {
            HomeWinnerText.Text = calc.WinningFirmName;
            HomeWinnerHintText.Text = calc.MarketResultTotal > 0m ? NumberParser.Money(calc.MarketResultTotal) : "Nihai toplam hazır";
        }
        else
        {
            HomeWinnerText.Text = "Henüz yok";
            HomeWinnerHintText.Text = firmCount < 3 ? "En az 3 firma gerekli" : "Nihai teklifler bekleniyor";
        }

        var workflowIssues = ValidationService.ValidateWorkflow(data);
        var homeWarningCount = workflowIssues.Count;
        HomeWarningCountText.Text = $"{homeWarningCount} UYARI";
        UpdateHomeActivitySummary();
        HomeReadinessText.Text = $"%{readiness}";
        HomeReadinessBar.Width = 690.0 * readiness / 100.0;
        HomeReadinessHintText.Text = readiness >= 100 && homeWarningCount == 0
            ? "Temel hazırlık tamam. Akıllı iş akışı tüm aşamaların durumunu tek bakışta gösteriyor."
            : homeWarningCount > 0 ? $"{homeWarningCount} kontrol noktası dikkat istiyor. Akıllı iş akışı ilk eksik aşamayı gösteriyor." : "Süreç bilgileri girildikçe hazırlık durumu otomatik güncellenir.";
        UpdateHomeProcessMap(data);

        var selectedTypesForOutput = OutputDocuments.Where(d => d.IsSelected && OfficialDocumentService.IsImplemented(DocumentTypeFor(d.Name))).Select(d => DocumentTypeFor(d.Name)).ToList();
        var selectedCoreIssues = DocumentReadinessService.GetExportCoreIssues(data, selectedTypesForOutput);
        var selectedAdvisories = OutputDocuments.Where(d => d.IsSelected && OfficialDocumentService.IsImplemented(DocumentTypeFor(d.Name)))
            .SelectMany(d => ValidationService.ValidateDocument(data, DocumentTypeFor(d.Name))).Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().ToList();

        OutputCommissionText.Text = $"{filledCommission} / 6";
        OutputFirmText.Text = selectedTypesForOutput.Any(DocumentReadinessService.IsFinalProcessDocument) ? $"{firmCount} / EN AZ 3" : "GEREKMEZ";
        OutputProductText.Text = $"{productCount} / EN AZ 1";
        OutputCriticalText.Text = selectedCoreIssues.Count.ToString(CultureInfo.InvariantCulture);
        OutputWarningText.Text = selectedAdvisories.Count.ToString(CultureInfo.InvariantCulture);
        OutputReadinessText.Text = $"%{readiness}";
        OutputReadinessBar.Width = 390.0 * readiness / 100.0;
        RefreshControlCenter(data);

        foreach (var doc in OutputDocuments)
        {
            var type = DocumentTypeFor(doc.Name);
            if (!OfficialDocumentService.IsImplemented(type))
            {
                doc.Status = "Sırada";
                doc.Note = "Bu sürümde belge motoruna bağlı değil";
                continue;
            }
            var core = DocumentReadinessService.GetExportCoreIssues(data, new[] { type });
            var advisoryCount = ValidationService.ValidateDocument(data, type).Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().Count();
            doc.Status = core.Count == 0 ? "Hazır" : "Uyarı";
            doc.Note = core.Count == 0 ? (advisoryCount == 0 ? "Çıktı koşulları hazır" : $"Çıktı alınabilir • {advisoryCount} bilgi uyarısı") : core[0] + (core.Count > 1 ? $" (+{core.Count - 1})" : string.Empty);
        }
    }

    private void UpdateReadinessRing(int percent)
    {
        var p = Math.Clamp(percent, 0, 100);
        if (p <= 0)
        {
            HeaderReadinessArc.Data = Geometry.Empty;
            return;
        }
        var angle = p >= 100 ? 359.99 : 360.0 * p / 100.0;
        var radians = (angle - 90.0) * Math.PI / 180.0;
        var x = 24.0 + 20.0 * Math.Cos(radians);
        var y = 24.0 + 20.0 * Math.Sin(radians);
        var large = angle > 180.0 ? 1 : 0;
        HeaderReadinessArc.Data = Geometry.Parse(string.Format(CultureInfo.InvariantCulture, "M24,4 A20,20 0 {0} 1 {1:0.###},{2:0.###}", large, x, y));
    }

    private static OutputDocumentType DocumentTypeFor(string name) => name switch
    {
        "Yaklaşık Maliyet Talep Yazısı" => OutputDocumentType.ApproximateRequest,
        "Yaklaşık Maliyet Hesap Cetveli" => OutputDocumentType.ApproximateCostSheet,
        "Onay Belgesi" => OutputDocumentType.ApprovalDocument,
        "Firma Teklif Formu" => OutputDocumentType.FirmOfferForm,
        "Piyasa Fiyat Araştırması Tutanağı" => OutputDocumentType.MarketResearchReport,
        "Muayene ve Kabul Tutanağı" => OutputDocumentType.InspectionAcceptanceReport,
        _ => OutputDocumentType.ApproximateRequest
    };
}

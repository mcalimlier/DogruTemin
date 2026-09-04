namespace DogruTemin.Core.Models;

public class ApprovalDocumentInfo
{
    public string DocumentNumber { get; set; } = string.Empty;
    public string AvailableBudgetAmount { get; set; } = string.Empty;
    public string InvestmentProjectNumber { get; set; } = string.Empty;
    public string BudgetCode { get; set; } = string.Empty;
    public string AdvanceConditions { get; set; } = string.Empty;
    public string TenderProcedure { get; set; } = "4734 S.K. 22/d Kapsamında";
    public string AnnouncementMethod { get; set; } = "İlansız";
    public string PrequalificationDocumentSalePrice { get; set; } = string.Empty;
    public string PriceDifferenceBasis { get; set; } = "Ödenmeyecektir";
    public string Notes { get; set; } = string.Empty;
    public string SubmittedByName { get; set; } = string.Empty;
    public string SubmittedByTitle { get; set; } = string.Empty;
}

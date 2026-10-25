namespace DogruTemin.Core.Models;

public class ApplicationSettings
{
    public int SchemaVersion { get; set; } = 1;
    public InstitutionInfo Institution { get; set; } = new();
    public List<CommissionMember> DefaultInspectionCommission { get; set; } = new();
    public List<CommissionMember> DefaultMarketCommission { get; set; } = new();
    public string WindowSizePreference { get; set; } = "Auto";
    public string DefaultUnit { get; set; } = "Adet";
}

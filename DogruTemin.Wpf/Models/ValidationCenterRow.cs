namespace DogruTemin.Wpf.Models;

public sealed class ValidationCenterRow
{
    public string Severity { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TargetView { get; set; } = "General";
    public int SortOrder { get; set; }
}

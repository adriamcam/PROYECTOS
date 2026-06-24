namespace ITQS.SupportOperationsCenter.Models.Dashboard;

public sealed class AlertKpiCardModel
{
    public string Title { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string CssClass { get; set; } = string.Empty;
}

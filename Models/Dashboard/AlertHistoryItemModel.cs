namespace ITQS.SupportOperationsCenter.Models.Dashboard;

public sealed class AlertHistoryItemModel
{
    public long HistoryId { get; set; }
    public string KPIType { get; set; } = string.Empty;
    public long AlertId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
}
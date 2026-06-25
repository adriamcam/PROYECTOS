namespace ITQS.SupportOperationsCenter.Models.Dashboard;

public sealed class AssignedAlertGridItemModel
{
    public string GroupId { get; set; } = string.Empty;
    public string AlertName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public int Events { get; set; }
    public DateTime? LastEventAt { get; set; }
}
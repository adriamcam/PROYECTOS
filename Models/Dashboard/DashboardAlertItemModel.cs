namespace ITQS.SupportOperationsCenter.Models.Dashboard;

public sealed class DashboardAlertItemModel
{
    public string SourceType { get; set; } = string.Empty;
    public long Id { get; set; }

    public string ClientName { get; set; } = string.Empty;
    public string AlertName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;

    public int Events { get; set; }
    public DateTime? LastEventAt { get; set; }

    public string AlertStatus { get; set; } = "Activa";
    public bool Selected { get; set; }

    public string AssignedTo { get; set; } = string.Empty;
    public string AssignedEmail { get; set; } = string.Empty;
}

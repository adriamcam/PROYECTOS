namespace ITQS.SupportOperationsCenter.Models.Dashboard;

public sealed class AssignedAlertsDashboardModel
{
    public int TotalAssigned { get; set; }
    public int BackupAssigned { get; set; }
    public int ManagementAssigned { get; set; }
    public int Pending { get; set; }
    public int Resolved { get; set; }

    public List<string> Clients { get; set; } = new();
    public List<AssignedAlertGridItemModel> Alerts { get; set; } = new();
}
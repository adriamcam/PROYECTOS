namespace ITQS.SupportOperationsCenter.Models.Dashboard;

public sealed class AlertDashboardModel
{
    // KPI
    public int TotalAlerts { get; set; }
    public int BackupAlerts { get; set; }
    public int ManagementAlerts { get; set; }
    public int AssignedToMe { get; set; }
    public int Unassigned { get; set; }
    public int ResolvedToday { get; set; }

    // Widgets Dashboard
    public List<DashboardSeverityModel> Severities { get; set; } = new();

    public List<DashboardTopClientModel> TopClients { get; set; } = new();
}
namespace ITQS.SupportOperationsCenter.Models.Dashboard;

public sealed class AssignedAlertsDashboardModel
{
    // KPI Dashboard SOC / Overview
    public int TotalAlerts { get; set; }
    public int ManagementAlerts { get; set; }
    public int BackupAlerts { get; set; }
    public int UnassignedAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int NewToday { get; set; }

    // KPI Asignadas a mí
    public int TotalAssigned { get; set; }
    public int BackupAssigned { get; set; }
    public int ManagementAssigned { get; set; }
    public int Pending { get; set; }
    public int Resolved { get; set; }

    public List<string> Clients { get; set; } = new();

    public List<AssignedAlertGridItemModel> Alerts { get; set; } = new();
}
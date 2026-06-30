namespace ITQS.SupportOperationsCenter.Models.Maintenance;
public sealed class SqlMaintenanceDashboardModel
{
    public string DatabaseName { get; set; } = "REPORTES";
    public decimal TotalSpaceMb { get; set; }
    public long TotalRows { get; set; }
    public decimal EstimatedRecoverableMb { get; set; }
    public string HealthStatus { get; set; } = "Saludable";
    public string LastMaintenanceText { get; set; } = "Nunca";
    public int RetentionDays { get; set; } = 30;
    public List<SqlMaintenanceTableSummaryModel> Tables { get; set; } = new();
    public List<SqlMaintenanceHistoryModel> History { get; set; } = new();
    public SqlMaintenanceExecutionResultModel? LastExecution { get; set; }
}

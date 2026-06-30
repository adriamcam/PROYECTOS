namespace ITQS.SupportOperationsCenter.Models.Maintenance;
public sealed class SqlMaintenanceDashboardModel { public int RetentionDays { get; set; } = 30; public List<SqlMaintenanceTableSummaryModel> Tables { get; set; } = new(); public SqlMaintenanceExecutionResultModel? LastExecution { get; set; } }

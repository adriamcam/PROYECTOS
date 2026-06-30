using ITQS.SupportOperationsCenter.Models.Maintenance;
namespace ITQS.SupportOperationsCenter.Services.Interfaces;
public interface ISqlMaintenanceService
{
    Task<bool> CanAccessAsync(string userEmail, CancellationToken cancellationToken = default);
    Task<SqlMaintenanceDashboardModel> GetDashboardAsync(int retentionDays, CancellationToken cancellationToken = default);
    Task<SqlMaintenanceExecutionResultModel> CleanupAlertsManagementAsync(SqlMaintenanceRequestModel request, CancellationToken cancellationToken = default);
    Task<SqlMaintenanceExecutionResultModel> CleanupAzureAlertCloseQueueAsync(SqlMaintenanceRequestModel request, CancellationToken cancellationToken = default);
    Task<SqlMaintenanceExecutionResultModel> CleanupAlertasBackupAsync(SqlMaintenanceRequestModel request, CancellationToken cancellationToken = default);
}

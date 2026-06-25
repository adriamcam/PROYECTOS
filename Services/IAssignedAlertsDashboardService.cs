using ITQS.SupportOperationsCenter.Models.Common;
using ITQS.SupportOperationsCenter.Models.Dashboard;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface IAssignedAlertsDashboardService
{
    Task<OperationResult<AssignedAlertsDashboardModel>> GetDashboardAsync(
        string userEmail,
        CancellationToken cancellationToken = default);

    Task<List<DashboardAlertItemModel>> GetManagementAlertsAsync(
        CancellationToken cancellationToken = default);

    Task<List<DashboardAlertItemModel>> GetBackupAlertsAsync(
        CancellationToken cancellationToken = default);

    Task AssignManagementAlertAsync(
        long id,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default);

    Task AssignBackupAlertAsync(
        long id,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default);
}
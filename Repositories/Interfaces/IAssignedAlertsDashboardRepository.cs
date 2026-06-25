using ITQS.SupportOperationsCenter.Models.Dashboard;

namespace ITQS.SupportOperationsCenter.Repositories.Interfaces;

public interface IAssignedAlertsDashboardRepository
{
    Task<AssignedAlertsDashboardModel> GetDashboardAsync(
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
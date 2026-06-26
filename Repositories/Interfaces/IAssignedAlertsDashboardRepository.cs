using ITQS.SupportOperationsCenter.Models.Dashboard;

namespace ITQS.SupportOperationsCenter.Repositories.Interfaces;

public interface IAssignedAlertsDashboardRepository
{
    Task<AssignedAlertsDashboardModel> GetDashboardAsync(
        string userEmail,
        CancellationToken cancellationToken = default);

    Task<DashboardAlertPagedResultModel> GetManagementAlertsAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? clientName = null,
        CancellationToken cancellationToken = default);

    Task<DashboardAlertPagedResultModel> GetBackupAlertsAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? clientName = null,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetClientsAsync(
        string sourceType,
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

    Task AssignManagementAlertsAsync(
        List<long> ids,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default);

    Task AssignBackupAlertsAsync(
        List<long> ids,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default);
}
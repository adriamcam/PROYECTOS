using ITQS.SupportOperationsCenter.Models.Common;
using ITQS.SupportOperationsCenter.Models.Dashboard;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface IAssignedAlertsDashboardService
{
    Task<OperationResult<AssignedAlertsDashboardModel>> GetDashboardAsync(
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
        DashboardAlertItemModel alert,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default);

    Task AssignBackupAlertAsync(
        DashboardAlertItemModel alert,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default);

    Task AssignManagementAlertsAsync(
        List<DashboardAlertItemModel> alerts,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default);

    Task AssignBackupAlertsAsync(
        List<DashboardAlertItemModel> alerts,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default);
}
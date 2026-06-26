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

    Task<DashboardAlertPagedResultModel> GetAssignedAlertsAsync(
        string userEmail,
        int pageNumber,
        int pageSize,
        string? search = null,
        string? clientName = null,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetClientsAsync(
        string sourceType,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetAssignedClientsAsync(
        string userEmail,
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

    Task<AlertDetailModel?> GetAlertDetailAsync(
        DashboardAlertItemModel alert,
        CancellationToken cancellationToken = default);

    Task SaveAlertCommentAsync(
        AlertCommentRequestModel request,
        CancellationToken cancellationToken = default);

    Task CloseAlertAsync(
        AlertCommentRequestModel request,
        CancellationToken cancellationToken = default);
}
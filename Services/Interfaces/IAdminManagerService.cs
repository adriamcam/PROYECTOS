using ITQS.SupportOperationsCenter.Models.Dashboard;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface IAdminManagerService
{
    Task<bool> CanAccessAdminManagerAsync(
        string userEmail,
        CancellationToken cancellationToken = default);

    Task<AdminManagerDashboardModel> GetDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<AdminManagerAlertPagedResultModel> GetAlertsAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? clientName = null,
        string? assignedEmail = null,
        string? sourceType = null,
        string? severity = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetClientsAsync(
        CancellationToken cancellationToken = default);

    Task<List<AdminManagerAppUserModel>> GetUsersAsync(
        CancellationToken cancellationToken = default);

    Task<List<AdminManagerEngineerWorkloadModel>> GetEngineerWorkloadAsync(
        CancellationToken cancellationToken = default);

    Task<List<AdminManagerSeveritySummaryModel>> GetSeveritySummaryAsync(
        CancellationToken cancellationToken = default);

    Task<AdminManagerClosedHistoryPagedResultModel> GetClosedHistoryPagedAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? kpiType = null,
        string? userEmail = null,
        CancellationToken cancellationToken = default);

    Task<List<AdminManagerClosedHistoryModel>> GetClosedHistoryForExportAsync(
        string? search = null,
        string? kpiType = null,
        string? userEmail = null,
        CancellationToken cancellationToken = default);

    Task ReassignAlertsAsync(
        AdminManagerReassignRequestModel request,
        CancellationToken cancellationToken = default);

    Task CloseSeverityAsync(
        AdminManagerCloseSeverityRequestModel request,
        CancellationToken cancellationToken = default);

    Task<List<AdminManagerUserMaintenanceModel>> GetUserMaintenanceAsync(
        CancellationToken cancellationToken = default);

    Task SaveUserMaintenanceAsync(
        AdminManagerUserSaveRequestModel request,
        CancellationToken cancellationToken = default);
}

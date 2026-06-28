using ITQS.SupportOperationsCenter.Models.Dashboard;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface IAdminManagerService
{
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

    Task<List<AdminManagerClosedHistoryModel>> GetClosedHistoryAsync(
        int take = 100,
        CancellationToken cancellationToken = default);

    Task ReassignAlertsAsync(
        AdminManagerReassignRequestModel request,
        CancellationToken cancellationToken = default);

    Task CloseSeverityAsync(
        AdminManagerCloseSeverityRequestModel request,
        CancellationToken cancellationToken = default);
}

using ITQS.SupportOperationsCenter.Models.Common;
using ITQS.SupportOperationsCenter.Models.Dashboard;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class AssignedAlertsDashboardService : IAssignedAlertsDashboardService
{
    private readonly IAssignedAlertsDashboardRepository _repository;
    private readonly ILogger<AssignedAlertsDashboardService> _logger;

    public AssignedAlertsDashboardService(
        IAssignedAlertsDashboardRepository repository,
        ILogger<AssignedAlertsDashboardService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<OperationResult<AssignedAlertsDashboardModel>> GetDashboardAsync(
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dashboard = await _repository.GetDashboardAsync(userEmail, cancellationToken);

            return OperationResult<AssignedAlertsDashboardModel>.Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting Assigned Alerts Dashboard for {UserEmail}",
                userEmail);

            return OperationResult<AssignedAlertsDashboardModel>.Fail(
                "No fue posible cargar el dashboard.");
        }
    }

    public async Task<DashboardAlertPagedResultModel> GetManagementAlertsAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? clientName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetManagementAlertsAsync(
                pageNumber,
                pageSize,
                search,
                clientName,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Management alerts.");

            return new DashboardAlertPagedResultModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }

    public async Task<DashboardAlertPagedResultModel> GetBackupAlertsAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? clientName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetBackupAlertsAsync(
                pageNumber,
                pageSize,
                search,
                clientName,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Backup alerts.");

            return new DashboardAlertPagedResultModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }

    public async Task<List<string>> GetClientsAsync(
        string sourceType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetClientsAsync(
                sourceType,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading clients.");

            return new List<string>();
        }
    }

    public async Task AssignManagementAlertAsync(
        long id,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        await _repository.AssignManagementAlertAsync(
            id,
            userName,
            userEmail,
            cancellationToken);
    }

    public async Task AssignBackupAlertAsync(
        long id,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        await _repository.AssignBackupAlertAsync(
            id,
            userName,
            userEmail,
            cancellationToken);
    }

    public async Task AssignManagementAlertsAsync(
        List<long> ids,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        await _repository.AssignManagementAlertsAsync(
            ids,
            userName,
            userEmail,
            cancellationToken);
    }

    public async Task AssignBackupAlertsAsync(
        List<long> ids,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        await _repository.AssignBackupAlertsAsync(
            ids,
            userName,
            userEmail,
            cancellationToken);
    }
}
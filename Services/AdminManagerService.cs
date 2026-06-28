using ITQS.SupportOperationsCenter.Models.Dashboard;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class AdminManagerService : IAdminManagerService
{
    private readonly IAdminManagerRepository _repository;
    private readonly ILogger<AdminManagerService> _logger;

    public AdminManagerService(
        IAdminManagerRepository repository,
        ILogger<AdminManagerService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> CanAccessAdminManagerAsync(
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.CanAccessAdminManagerAsync(userEmail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Admin Manager access for {UserEmail}.", userEmail);
            return false;
        }
    }

    public async Task<AdminManagerDashboardModel> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetDashboardAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Admin Manager dashboard.");
            return new AdminManagerDashboardModel();
        }
    }

    public async Task<AdminManagerAlertPagedResultModel> GetAlertsAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? clientName = null,
        string? assignedEmail = null,
        string? sourceType = null,
        string? severity = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetAlertsAsync(
                pageNumber,
                pageSize,
                search,
                clientName,
                assignedEmail,
                sourceType,
                severity,
                status,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Admin Manager alerts.");

            return new AdminManagerAlertPagedResultModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }

    public async Task<List<string>> GetClientsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetClientsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Admin Manager clients.");
            return new List<string>();
        }
    }

    public async Task<List<AdminManagerAppUserModel>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetUsersAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Admin Manager users.");
            return new List<AdminManagerAppUserModel>();
        }
    }

    public async Task<List<AdminManagerEngineerWorkloadModel>> GetEngineerWorkloadAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetEngineerWorkloadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading engineer workload.");
            return new List<AdminManagerEngineerWorkloadModel>();
        }
    }

    public async Task<List<AdminManagerSeveritySummaryModel>> GetSeveritySummaryAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetSeveritySummaryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading severity summary.");
            return new List<AdminManagerSeveritySummaryModel>();
        }
    }

    public async Task<AdminManagerClosedHistoryPagedResultModel> GetClosedHistoryPagedAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? kpiType = null,
        string? userEmail = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetClosedHistoryPagedAsync(
                pageNumber,
                pageSize,
                search,
                kpiType,
                userEmail,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading paged closed alert history.");

            return new AdminManagerClosedHistoryPagedResultModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }

    public async Task<List<AdminManagerClosedHistoryModel>> GetClosedHistoryForExportAsync(
        string? search = null,
        string? kpiType = null,
        string? userEmail = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetClosedHistoryForExportAsync(
                search,
                kpiType,
                userEmail,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting closed alert history.");
            return new List<AdminManagerClosedHistoryModel>();
        }
    }

    public async Task ReassignAlertsAsync(
        AdminManagerReassignRequestModel request,
        CancellationToken cancellationToken = default)
    {
        await _repository.ReassignAlertsAsync(request, cancellationToken);
    }

    public async Task CloseSeverityAsync(
        AdminManagerCloseSeverityRequestModel request,
        CancellationToken cancellationToken = default)
    {
        await _repository.CloseSeverityAsync(request, cancellationToken);
    }

    public async Task<List<AdminManagerUserMaintenanceModel>> GetUserMaintenanceAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetUserMaintenanceAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Admin Manager user maintenance.");
            return new List<AdminManagerUserMaintenanceModel>();
        }
    }

    public async Task SaveUserMaintenanceAsync(
        AdminManagerUserSaveRequestModel request,
        CancellationToken cancellationToken = default)
    {
        await _repository.SaveUserMaintenanceAsync(request, cancellationToken);
    }
}

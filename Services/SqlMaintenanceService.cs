using ITQS.SupportOperationsCenter.Models.Maintenance;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class SqlMaintenanceService : ISqlMaintenanceService
{
    private readonly ISqlMaintenanceRepository _repository;
    private readonly ILogger<SqlMaintenanceService> _logger;

    public SqlMaintenanceService(
        ISqlMaintenanceRepository repository,
        ILogger<SqlMaintenanceService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> CanAccessAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.CanAccessAsync(userEmail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SQL Maintenance access.");
            return false;
        }
    }

    public async Task<SqlMaintenanceDashboardModel> GetDashboardAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetDashboardAsync(NormalizeRetentionDays(retentionDays), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading SQL Maintenance dashboard.");

            return new SqlMaintenanceDashboardModel
            {
                RetentionDays = NormalizeRetentionDays(retentionDays),
                HealthStatus = "Warning"
            };
        }
    }

    public Task<SqlMaintenanceExecutionResultModel> CleanupAlertsManagementAsync(
        SqlMaintenanceRequestModel request,
        CancellationToken cancellationToken = default)
    {
        request = Normalize(request);
        return Safe(() => _repository.CleanupAlertsManagementAsync(request, cancellationToken), request);
    }

    public Task<SqlMaintenanceExecutionResultModel> CleanupAzureAlertCloseQueueAsync(
        SqlMaintenanceRequestModel request,
        CancellationToken cancellationToken = default)
    {
        request = Normalize(request);
        return Safe(() => _repository.CleanupAzureAlertCloseQueueAsync(request, cancellationToken), request);
    }

    public Task<SqlMaintenanceExecutionResultModel> CleanupAlertasBackupAsync(
        SqlMaintenanceRequestModel request,
        CancellationToken cancellationToken = default)
    {
        request = Normalize(request);
        return Safe(() => _repository.CleanupAlertasBackupAsync(request, cancellationToken), request);
    }

    public Task<SqlMaintenanceExecutionResultModel> CleanupAlertUpdatesHistoryAsync(
        SqlMaintenanceRequestModel request,
        CancellationToken cancellationToken = default)
    {
        request = Normalize(request);
        return Safe(() => _repository.CleanupAlertUpdatesHistoryAsync(request, cancellationToken), request);
    }

    private async Task<SqlMaintenanceExecutionResultModel> Safe(
        Func<Task<SqlMaintenanceExecutionResultModel>> action,
        SqlMaintenanceRequestModel request)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing SQL maintenance action.");

            return new SqlMaintenanceExecutionResultModel
            {
                TableName = request.TableName,
                ActionName = request.ActionName,
                RetentionDays = request.RetentionDays,
                BatchSize = request.BatchSize,
                Succeeded = false,
                Message = ex.Message,
                StartedAt = DateTime.Now,
                FinishedAt = DateTime.Now,
                ExecutedBy = request.UserName,
                ExecutedByEmail = request.UserEmail
            };
        }
    }

    private static SqlMaintenanceRequestModel Normalize(SqlMaintenanceRequestModel request)
    {
        request.RetentionDays = NormalizeRetentionDays(request.RetentionDays);
        request.BatchSize = request.BatchSize <= 0 ? 5000 : Math.Min(request.BatchSize, 20000);
        request.UserEmail = request.UserEmail?.Trim() ?? string.Empty;
        request.UserName = request.UserName?.Trim() ?? string.Empty;

        return request;
    }

    private static int NormalizeRetentionDays(int retentionDays)
    {
        if (retentionDays < 30) return 30;
        if (retentionDays > 365) return 365;

        return retentionDays;
    }
}
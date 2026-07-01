using ITQS.SupportOperationsCenter.Models.Maintenance.SqlOperations;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class SqlOperationsDashboardService : ISqlOperationsDashboardService
{
    private readonly ISqlOperationsDashboardRepository _repository;
    private readonly ILogger<SqlOperationsDashboardService> _logger;

    public SqlOperationsDashboardService(
        ISqlOperationsDashboardRepository repository,
        ILogger<SqlOperationsDashboardService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> CanAccessAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        try { return await _repository.CanAccessAsync(userEmail, cancellationToken); }
        catch (Exception ex) { _logger.LogError(ex, "Error validating SQL Operations Dashboard access."); return false; }
    }

    public async Task<SqlOperationsDashboardModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        try { return await _repository.GetDashboardAsync(cancellationToken); }
        catch (Exception ex) { _logger.LogError(ex, "Error loading SQL Operations Dashboard."); return new SqlOperationsDashboardModel { HealthStatus = "Warning" }; }
    }
}

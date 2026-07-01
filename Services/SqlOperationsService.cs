using ITQS.SupportOperationsCenter.Models.Maintenance.SqlOperations;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class SqlOperationsService : ISqlOperationsService
{
    private readonly ISqlOperationsRepository _repository;
    private readonly ILogger<SqlOperationsService> _logger;

    public SqlOperationsService(ISqlOperationsRepository repository, ILogger<SqlOperationsService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> CanAccessAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        try { return await _repository.CanAccessAsync(userEmail, cancellationToken); }
        catch (Exception ex) { _logger.LogError(ex, "Error validating SQL operations access."); return false; }
    }

    public async Task<IReadOnlyList<SqlJobModel>> GetJobsAsync(CancellationToken cancellationToken = default)
    {
        try { return await _repository.GetJobsAsync(cancellationToken); }
        catch (Exception ex) { _logger.LogError(ex, "Error loading SQL jobs."); return Array.Empty<SqlJobModel>(); }
    }

    public async Task<IReadOnlyList<SqlBlockingModel>> GetBlockingAsync(CancellationToken cancellationToken = default)
    {
        try { return await _repository.GetBlockingAsync(cancellationToken); }
        catch (Exception ex) { _logger.LogError(ex, "Error loading SQL blocking."); return Array.Empty<SqlBlockingModel>(); }
    }

    public async Task<IReadOnlyList<SqlSessionModel>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        try { return await _repository.GetActiveSessionsAsync(cancellationToken); }
        catch (Exception ex) { _logger.LogError(ex, "Error loading SQL sessions."); return Array.Empty<SqlSessionModel>(); }
    }

    public async Task<IReadOnlyList<SqlSlowQueryModel>> GetSlowQueriesAsync(CancellationToken cancellationToken = default)
    {
        try { return await _repository.GetSlowQueriesAsync(cancellationToken); }
        catch (Exception ex) { _logger.LogError(ex, "Error loading SQL slow queries."); return Array.Empty<SqlSlowQueryModel>(); }
    }
}

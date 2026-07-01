using ITQS.SupportOperationsCenter.Models.Maintenance.SqlOperations;
namespace ITQS.SupportOperationsCenter.Repositories.Interfaces;
public interface ISqlOperationsRepository
{
    Task<bool> CanAccessAsync(string userEmail, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SqlJobModel>> GetJobsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SqlBlockingModel>> GetBlockingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SqlSessionModel>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SqlSlowQueryModel>> GetSlowQueriesAsync(CancellationToken cancellationToken = default);
}

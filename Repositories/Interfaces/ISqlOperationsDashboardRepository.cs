using ITQS.SupportOperationsCenter.Models.Maintenance.SqlOperations;

namespace ITQS.SupportOperationsCenter.Repositories.Interfaces;

public interface ISqlOperationsDashboardRepository
{
    Task<bool> CanAccessAsync(string userEmail, CancellationToken cancellationToken = default);
    Task<SqlOperationsDashboardModel> GetDashboardAsync(CancellationToken cancellationToken = default);
}

using ITQS.SupportOperationsCenter.Models.Maintenance.SqlOperations;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface ISqlOperationsDashboardService
{
    Task<bool> CanAccessAsync(string userEmail, CancellationToken cancellationToken = default);
    Task<SqlOperationsDashboardModel> GetDashboardAsync(CancellationToken cancellationToken = default);
}

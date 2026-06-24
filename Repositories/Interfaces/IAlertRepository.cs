using ITQS.SupportOperationsCenter.Models.Dashboard;

namespace ITQS.SupportOperationsCenter.Repositories.Interfaces;

public interface IAlertRepository
{
    Task<AlertDashboardModel> GetDashboardAsync(string userEmail, CancellationToken cancellationToken = default);
}

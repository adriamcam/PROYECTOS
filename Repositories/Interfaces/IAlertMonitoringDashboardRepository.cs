using ITQS.SupportOperationsCenter.Models.Dashboard;

namespace ITQS.SupportOperationsCenter.Repositories.Interfaces;

public interface IAlertMonitoringDashboardRepository
{
    Task<AlertMonitoringDashboardModel> GetDashboardAsync(
        CancellationToken cancellationToken = default);
}

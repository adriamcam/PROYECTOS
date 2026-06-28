using ITQS.SupportOperationsCenter.Models.Dashboard;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface IAlertMonitoringDashboardService
{
    Task<AlertMonitoringDashboardModel> GetDashboardAsync(
        CancellationToken cancellationToken = default);
}

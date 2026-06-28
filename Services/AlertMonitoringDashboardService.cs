using ITQS.SupportOperationsCenter.Models.Dashboard;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class AlertMonitoringDashboardService : IAlertMonitoringDashboardService
{
    private readonly IAlertMonitoringDashboardRepository _repository;
    private readonly ILogger<AlertMonitoringDashboardService> _logger;

    public AlertMonitoringDashboardService(
        IAlertMonitoringDashboardRepository repository,
        ILogger<AlertMonitoringDashboardService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AlertMonitoringDashboardModel> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetDashboardAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading alert monitoring dashboard.");
            return new AlertMonitoringDashboardModel();
        }
    }
}

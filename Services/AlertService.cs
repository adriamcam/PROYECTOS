using ITQS.SupportOperationsCenter.Models.Common;
using ITQS.SupportOperationsCenter.Models.Dashboard;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class AlertService : IAlertService
{
    private readonly IAlertRepository _alertRepository;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        IAlertRepository alertRepository,
        ILogger<AlertService> logger)
    {
        _alertRepository = alertRepository;
        _logger = logger;
    }

    public async Task<OperationResult<AlertDashboardModel>> GetDashboardAsync(
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                userEmail = "wcambronero@itqscr.com";
            }

            var dashboard = await _alertRepository.GetDashboardAsync(userEmail, cancellationToken);

            return OperationResult<AlertDashboardModel>.Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert dashboard data.");
            return OperationResult<AlertDashboardModel>.Fail("No fue posible cargar los KPI de alertas.");
        }
    }
}

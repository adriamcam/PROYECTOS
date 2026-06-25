using ITQS.SupportOperationsCenter.Models.Common;
using ITQS.SupportOperationsCenter.Models.Dashboard;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class AssignedAlertsDashboardService : IAssignedAlertsDashboardService
{
    private readonly IAssignedAlertsDashboardRepository _repository;
    private readonly ILogger<AssignedAlertsDashboardService> _logger;

    public AssignedAlertsDashboardService(
        IAssignedAlertsDashboardRepository repository,
        ILogger<AssignedAlertsDashboardService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<OperationResult<AssignedAlertsDashboardModel>> GetDashboardAsync(
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dashboard = await _repository.GetDashboardAsync(userEmail, cancellationToken);
            return OperationResult<AssignedAlertsDashboardModel>.Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting Assigned Alerts Dashboard for {UserEmail}",
                userEmail);

            return OperationResult<AssignedAlertsDashboardModel>.Fail(
                "No fue posible cargar el dashboard de alertas asignadas.");
        }
    }
}
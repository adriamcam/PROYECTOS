using ITQS.SupportOperationsCenter.Models.Common;
using ITQS.SupportOperationsCenter.Models.Dashboard;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface IAlertService
{
    Task<OperationResult<AlertDashboardModel>> GetDashboardAsync(
        string userEmail,
        CancellationToken cancellationToken = default);
}

using ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Models;

namespace ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Services;

public interface IReservedInstanceService
{
    Task<ReservedInstanceDashboard> GetDashboardAsync();

    Task<List<ReservedInstanceResource>> GetResourcesAsync();

    Task<List<ReservedInstanceChange>> GetChangesAsync();

    Task<List<ReservedInstanceResourceHistory>> GetResourceHistoryAsync(string resourceKey);
}

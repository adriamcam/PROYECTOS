using ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Models;

namespace ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Services;

public interface IReservedInstanceService
{
    Task<ReservedInstanceDashboard> GetDashboardAsync();

    Task<List<ReservedInstanceResource>> GetResourcesAsync();
    Task<List<ReservedInstanceChangeHistory>> GetChangeHistoryAsync(string resourceKey);
    Task SaveManualAnalysisNoteAsync(string resourceKey, string? note, string updatedBy);
}



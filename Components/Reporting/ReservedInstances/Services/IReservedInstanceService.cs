using ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Models;

namespace ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Services;

public interface IReservedInstanceService
{
    Task<ReservedInstanceDashboard> GetDashboardAsync();
    Task<List<ReservedInstanceResource>> GetResourcesAsync();
    Task<List<ReservedInstanceChangeHistory>> GetChangeHistoryAsync(string resourceKey);
    Task SaveManualAnalysisNoteAsync(string resourceKey, string? note, string updatedBy);
    Task<RICoverageKpi> GetCoverageKpisAsync();
    Task<List<RICoverageResource>> GetCoverageResourcesAsync();

    Task SaveVMCoverageNoteAsync(
        string resourceKey,
        string? note,
        string updatedBy);}


using ITQS.SupportOperationsCenter.Components.Reporting.PALM.Models;

namespace ITQS.SupportOperationsCenter.Components.Reporting.PALM.Services;

public interface IPalmService
{
    Task<PalmReportData> GetReportAsync();

    Task<PalmDashboard> GetDashboardAsync();

    Task<IReadOnlyList<PalmResource>> GetResultsAsync();

    Task<IReadOnlyList<PalmResource>> GetRequiresActionAsync();

    Task<PalmRun?> GetLatestRunAsync();

    Task<IReadOnlyList<PalmRun>> GetRunHistoryAsync();
}

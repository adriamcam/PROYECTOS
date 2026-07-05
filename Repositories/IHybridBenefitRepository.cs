using ITQS.SupportOperationsCenter.Components.Reporting.HybridBenefit.Models;

namespace ITQS.SupportOperationsCenter.Repositories;

public interface IHybridBenefitRepository
{
    Task<HybridBenefitDashboard> GetDashboardAsync();
    Task<HybridBenefitKpi> GetKpisAsync();
    Task<List<HybridBenefitTopCustomer>> GetTopCustomersAsync();
    Task<List<HybridBenefitDistribution>> GetDistributionAsync();
    Task<List<HybridBenefitResource>> GetResourcesAsync();
    Task<List<HybridBenefitChange>> GetChangesAsync();
    Task<List<HybridBenefitResourceHistory>> GetResourceHistoryAsync(string resourceKey);
}


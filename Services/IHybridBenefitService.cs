using ITQS.SupportOperationsCenter.Components.Reporting.HybridBenefit.Models;

namespace ITQS.SupportOperationsCenter.Services;

public interface IHybridBenefitService
{
    Task<HybridBenefitDashboard> GetDashboardAsync();
}

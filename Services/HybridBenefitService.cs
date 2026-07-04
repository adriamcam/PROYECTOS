using ITQS.SupportOperationsCenter.Components.Reporting.HybridBenefit.Models;
using ITQS.SupportOperationsCenter.Repositories;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class HybridBenefitService : IHybridBenefitService
{
    private readonly IHybridBenefitRepository _repository;

    public HybridBenefitService(IHybridBenefitRepository repository)
    {
        _repository = repository;
    }

    public Task<HybridBenefitDashboard> GetDashboardAsync()
    {
        return _repository.GetDashboardAsync();
    }
}

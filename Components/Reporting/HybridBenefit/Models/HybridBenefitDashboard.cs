namespace ITQS.SupportOperationsCenter.Components.Reporting.HybridBenefit.Models;

public class HybridBenefitDashboard
{
    public HybridBenefitKpi Kpis { get; set; } = new();

    public List<HybridBenefitTopCustomer> TopCustomers { get; set; } = new();

    public List<HybridBenefitDistribution> Distribution { get; set; } = new();

    public List<HybridBenefitResource> Resources { get; set; } = new();

    public List<HybridBenefitChange> Changes { get; set; } = new();
}

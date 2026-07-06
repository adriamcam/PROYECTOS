namespace ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Models;

public sealed class ReservedInstanceDashboard
{
    public ReservedInstanceKpi Kpis { get; set; } = new();
    public List<ReservedInstanceDistribution> Distribution { get; set; } = new();
    public List<ReservedInstanceTopCustomer> TopCustomers { get; set; } = new();
    public List<ReservedInstanceResource> Resources { get; set; } = new();
    public List<ReservedInstanceChange> Changes { get; set; } = new();
}

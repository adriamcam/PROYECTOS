namespace ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Models;

public sealed class ReservedInstanceTopCustomer
{
    public string Customer { get; set; } = "";
    public int TotalReservations { get; set; }
    public int Issues { get; set; }
    public decimal AvgUtilization { get; set; }
}

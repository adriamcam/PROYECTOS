namespace ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Models;

public sealed class ReservedInstanceKpi
{
    public int TotalResources { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalSubscriptions { get; set; }
    public int TotalReservations { get; set; }
    public int HealthyCount { get; set; }
    public int SkuMismatchCount { get; set; }
    public int DateMismatchCount { get; set; }
    public int LowUtilizationCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int ExpiredCount { get; set; }
    public decimal AvgUtilization { get; set; }
    public decimal TotalMonthlySavings { get; set; }
    public decimal TotalYearlySavings { get; set; }
    public DateTime? LastScanDate { get; set; }
}

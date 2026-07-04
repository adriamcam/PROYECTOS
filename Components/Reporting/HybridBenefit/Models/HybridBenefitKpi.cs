namespace ITQS.SupportOperationsCenter.Components.Reporting.HybridBenefit.Models;

public class HybridBenefitKpi
{
    public int TotalResources { get; set; }

    public int TotalCustomers { get; set; }

    public int TotalSubscriptions { get; set; }

    public int WindowsCount { get; set; }

    public int SqlCount { get; set; }

    public int TagCount { get; set; }

    public int MissingAhubTagCount { get; set; }

    public int MissingSqlAhubTagCount { get; set; }

    public int ChangeCount { get; set; }

    public DateTime LastScanDate { get; set; }
}


namespace ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Models;

public sealed class RICoverageResource
{
    public long Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string TenantId { get; set; } = "";
    public string SubscriptionName { get; set; } = "";
    public string SubscriptionId { get; set; } = "";
    public string ResourceGroupName { get; set; } = "";
    public string VMName { get; set; } = "";
    public string ResourceId { get; set; } = "";
    public string Location { get; set; } = "";
    public string AvailabilityZone { get; set; } = "";
    public string PowerState { get; set; } = "";
    public string VMSize { get; set; } = "";
    public string VMFamily { get; set; } = "";
    public int? VCPUs { get; set; }
    public decimal? MemoryGB { get; set; }
    public string OSType { get; set; } = "";
    public string? IRTag { get; set; }
    public bool HasIRTag { get; set; }
    public DateTime? IRTagExpirationDate { get; set; }
    public string? IntendedReservationId { get; set; }
    public string? IntendedReservationName { get; set; }
    public string? IntendedReservedSku { get; set; }
    public long CompatibleReservationCount { get; set; }
    public long CompatibleReservationQuantity { get; set; }
    public long EligibleVMCount { get; set; }
    public long RunningVMCount { get; set; }
    public long ReservationCount { get; set; }
    public long ReservedQuantity { get; set; }
    public long CoverageGap { get; set; }
    public decimal CoveragePercent { get; set; }
    public string CoverageStatusCode { get; set; } = "";
    public string CoverageStatus { get; set; } = "";
    public string CoverageConfidence { get; set; } = "";
    public int CoverageScore { get; set; }
    public string CoverageReason { get; set; } = "";
    public string CoverageRecommendation { get; set; } = "";
    public string? CompatibleReservationNames { get; set; }
    public string? Tags { get; set; }
    public DateTime? LastSeenAt { get; set; }
}

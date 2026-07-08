namespace ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Models;

public sealed class ReservedInstanceResource
{
    public string ResourceKey { get; set; } = "";
    public string Customer { get; set; } = "";
    public string TenantId { get; set; } = "";
    public string Subscription { get; set; } = "";
    public string SubscriptionId { get; set; } = "";
    public string ServerName { get; set; } = "";
    public string ResourceName { get; set; } = "";

    public string ReservationId { get; set; } = "";
    public string ReservationName { get; set; } = "";
    public string ReservationOrderId { get; set; } = "";
    public string ReservationTerm { get; set; } = "";
    public string AppliedScope { get; set; } = "";

    public DateTime? FechaReserva { get; set; }
    public DateTime? FechaVM { get; set; }
    public string FechaVal { get; set; } = "";

    public string SKUReserva { get; set; } = "";
    public string SKUVM { get; set; } = "";
    public string SKUVal { get; set; } = "";

    public string StatusVM { get; set; } = "";
    public decimal? Utilization30d { get; set; }
    public string UtilizationTrend { get; set; } = "";

    public string Region { get; set; } = "";
    public string VMSize { get; set; } = "";
    public int? vCPUs { get; set; }
    public decimal? MemoryGB { get; set; }

    public int? DaysRemaining { get; set; }
    public bool ExpiresSoon { get; set; }

    public decimal? SavingsMonthly { get; set; }
    public decimal? SavingsYearly { get; set; }

    public string OperationalStatus { get; set; } = "";
    public string Issue { get; set; } = "";
    public string LastChangedBy { get; set; } = "";
    public DateTime? LastChangeDate { get; set; }
    public bool? Ri1YearAvailable { get; set; }
    public bool? Ri3YearAvailable { get; set; }
    public string? RetirementStatus { get; set; }
    public string? LifecycleRecommendation { get; set; }
    public DateTime? LifecycleLastUpdatedAt { get; set; }

    public string? CauseCode { get; set; }
    public string? CauseDescription { get; set; }
    public string? AnalysisRecommendation { get; set; }
    public string? AnalysisStatus { get; set; }
    public string? ServiceType { get; set; }
    public string? PriorityLevel { get; set; }
    public string? OptimizationCategory { get; set; }
    public string? ConfidenceLevel { get; set; }
    public string? AnalysisNote { get; set; }
    public decimal? Utilization7d { get; set; }
    public decimal? EstimatedUsedHours7d { get; set; }
    public decimal? EstimatedUsedHours30d { get; set; }
    public int? MatchingResourceCount { get; set; }
    public int? RunningResourceCount { get; set; }
    public string? LastMatchedResourceName { get; set; }
    public string? LastMatchedResourceSku { get; set; }
    public string? LastMatchedResourceRegion { get; set; }
    public string? LastMatchedResourceType { get; set; }
    public string? LastMatchedResourceGroup { get; set; }
    public string? RelatedResourceName { get; set; }
    public DateTime? AnalysisUpdatedAt { get; set; }

    public DateTime? LastScanDate { get; set; }
}





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
    public DateTime? LastScanDate { get; set; }
}



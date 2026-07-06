namespace ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstancess.Models;

public class ReservedInstanceResource
{
    public string ResourceKey { get; set; } = "";

    public string Customer { get; set; } = "";

    public string Subscription { get; set; } = "";

    public string ResourceGroup { get; set; } = "";

    public string ResourceName { get; set; } = "";

    public bool HasWindowsAHUB { get; set; }

    public bool HasSqlAHUB { get; set; }

    public bool HasTagHB { get; set; }

    public bool HasTagHBSQL { get; set; }

    public string OperationalStatus { get; set; } = "";

    public string ChangeType { get; set; } = "";

    public string Severity { get; set; } = "";

    public DateTime? ChangeDate { get; set; }

    public DateTime? FirstSeenDate { get; set; }

    public DateTime? LastSeenDate { get; set; }

    public DateTime? LastScanDate { get; set; }

    public string HealthStatus { get; set; } = "";

    public int ActionOccurrenceCount { get; set; }

    public string RecurrenceStatus { get; set; } = "";

    public string LastChangedBy { get; set; } = "";

    public string ResourceType { get; set; } = "";
    public string OperatingSystem { get; set; } = "";
    public string OSType { get; set; } = "";
    public string Publisher { get; set; } = "";
    public string Offer { get; set; } = "";
    public string Sku { get; set; } = "";
    public string VMSize { get; set; } = "";
    public int? vCPUs { get; set; }
    public decimal? MemoryGB { get; set; }
    public string Region { get; set; } = "";

    public string SkuDisplay =>
        string.IsNullOrWhiteSpace(VMSize)
            ? "-"
            : $"{VMSize} ({vCPUs ?? 0} vCPUs, {MemoryGB ?? 0:N0} GiB memory)";

    public bool RequiresAction =>
        (HasWindowsAHUB && !HasTagHB)
        || (HasSqlAHUB && !HasTagHBSQL);
}










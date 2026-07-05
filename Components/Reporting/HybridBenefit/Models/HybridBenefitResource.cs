namespace ITQS.SupportOperationsCenter.Components.Reporting.HybridBenefit.Models;

public class HybridBenefitResource
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

    public bool RequiresAction =>
        (HasWindowsAHUB && !HasTagHB)
        || (HasSqlAHUB && !HasTagHBSQL);
}


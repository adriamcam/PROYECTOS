namespace ITQS.SupportOperationsCenter.Components.Reporting.HybridBenefit.Models;

public class HybridBenefitResource
{
    public int Id { get; set; }

    public string Customer { get; set; } = "";

    public string TenantId { get; set; } = "";

    public string SubscriptionId { get; set; } = "";

    public string Subscription { get; set; } = "";

    public string ResourceGroup { get; set; } = "";

    public string ResourceName { get; set; } = "";

    public string HybridBenefit { get; set; } = "";

    public string? LicenseType { get; set; }

    public string? SQLLicenseType { get; set; }

    public string? TagHB { get; set; }

    public string? TagHBSQL { get; set; }

    public string? AHUB_Status { get; set; }

    public string? ChangeType { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? Time { get; set; }

    public DateTime ScanDate { get; set; }
}


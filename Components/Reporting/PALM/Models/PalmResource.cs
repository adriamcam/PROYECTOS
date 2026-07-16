namespace ITQS.SupportOperationsCenter.Components.Reporting.PALM.Models;

public sealed class PalmResource
{
    public long Id { get; set; }

    public Guid? RunId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public string? SubscriptionName { get; set; }

    public Guid? SubscriptionId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? PartnerIdBefore { get; set; }

    public string? PartnerIdCurrent { get; set; }

    public string? PartnerIdTarget { get; set; }

    public string PartnerValidationStatus { get; set; } = string.Empty;

    public int VisibleSubscriptions { get; set; }

    public bool RequiresAction { get; set; }

    public string? ClientId { get; set; }

    public string? ErrorMessage { get; set; }

    public string? Detail { get; set; }

    public DateTime ScanDate { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models;

public sealed class ClientWorkspaceCustomer
{
    public Guid TenantId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerNamePortal { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string PartnerTenant { get; set; } = string.Empty;

    public string GdapStatus { get; set; } = string.Empty;

    public int SubscriptionCount { get; set; }

    public DateTime? LastOperationalUpdate { get; set; }

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(CustomerNamePortal)
            ? CustomerNamePortal
            : CustomerName;
}

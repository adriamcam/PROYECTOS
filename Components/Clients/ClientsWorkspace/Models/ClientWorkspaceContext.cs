namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models;

public sealed class ClientWorkspaceContext
{
    public Guid? TenantId { get; set; }

    public Guid? SubscriptionId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerNamePortal { get; set; } = string.Empty;

    public string SubscriptionName { get; set; } = string.Empty;

    public string SubscriptionState { get; set; } = string.Empty;

    public string PartnerTenant { get; set; } = string.Empty;

    public string GdapStatus { get; set; } = string.Empty;

    public bool HasCustomer => TenantId.HasValue;

    public bool HasSubscription => SubscriptionId.HasValue;

    public void Clear()
    {
        TenantId = null;
        SubscriptionId = null;
        CustomerName = string.Empty;
        CustomerNamePortal = string.Empty;
        SubscriptionName = string.Empty;
        SubscriptionState = string.Empty;
        PartnerTenant = string.Empty;
        GdapStatus = string.Empty;
    }
}

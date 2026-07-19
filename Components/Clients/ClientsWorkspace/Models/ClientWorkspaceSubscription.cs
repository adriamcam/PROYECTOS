namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models;

public sealed class ClientWorkspaceSubscription
{
    public Guid TenantId { get; set; }

    public Guid SubscriptionId { get; set; }

    public string SubscriptionName { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public DateTime? LastSeenAt { get; set; }

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(SubscriptionName)
            ? SubscriptionName
            : SubscriptionId.ToString();
}

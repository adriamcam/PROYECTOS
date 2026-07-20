namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models;

public sealed class ClientSecurityScore
{
    public decimal Score { get; set; }

    public int SubscriptionCount { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

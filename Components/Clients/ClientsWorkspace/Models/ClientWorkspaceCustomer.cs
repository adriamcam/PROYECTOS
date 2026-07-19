namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models;

public sealed class ClientWorkspaceCustomer
{
    public Guid TenantId { get; set; }

    /// <summary>
    /// Nombre comercial obtenido desde dbo.CRM_Customers.AccountNameCRM.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Nombre histórico proveniente de dbo.ITQS_Customers.
    /// Se conserva por compatibilidad con otros componentes.
    /// </summary>
    public string CustomerNamePortal { get; set; } = string.Empty;

    /// <summary>
    /// Nombre corto mostrado como Suscripción.
    /// Se obtiene principalmente de dbo.CRM_Customers.CustomerDomainPc.
    /// </summary>
    public string SubscriptionName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string PartnerTenant { get; set; } = string.Empty;

    public string GdapStatus { get; set; } = string.Empty;

    public int SubscriptionCount { get; set; }

    public DateTime? LastOperationalUpdate { get; set; }

    /// <summary>
    /// Nombre mostrado en el selector de clientes.
    /// </summary>
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(CustomerName)
            ? CustomerName
            : "Cliente sin nombre";
}

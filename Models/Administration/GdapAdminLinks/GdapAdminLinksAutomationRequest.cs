namespace ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

public sealed class GdapAdminLinksAutomationRequest
{
    public int CustomerId { get; set; }
    public string PartnerTenant { get; set; } = string.Empty;
    public string CustomerTenantId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int DaysThreshold { get; set; } = 30;
    public string RequestedBy { get; set; } = string.Empty;
}

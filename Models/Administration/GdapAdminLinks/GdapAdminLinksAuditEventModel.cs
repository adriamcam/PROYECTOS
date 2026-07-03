namespace ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

public sealed class GdapAdminLinksAuditEventModel
{
    public long Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerTenantId { get; set; } = string.Empty;
    public string PartnerTenant { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExecutedBy { get; set; } = string.Empty;
    public string ApprovalUrl { get; set; } = string.Empty;
}

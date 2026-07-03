namespace ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

public sealed class GdapAdminLinksSaveCustomerRequest
{
    public int Id { get; set; }
    public string CustomerTenantId { get; set; } = string.Empty;
    public string PrimaryContactName { get; set; } = string.Empty;
    public string PrimaryEmail { get; set; } = string.Empty;
    public string CCEmails { get; set; } = string.Empty;
    public bool AutoSendEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public string ExcludeReason { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

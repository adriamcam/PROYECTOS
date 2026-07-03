namespace ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

public sealed class GdapAdminLinksFilterModel
{
    public string Search { get; set; } = string.Empty;
    public string PartnerTenant { get; set; } = string.Empty;
    public string StatusFound { get; set; } = string.Empty;
    public string ActiveFilter { get; set; } = string.Empty;
    public string EmailFilter { get; set; } = string.Empty;
}

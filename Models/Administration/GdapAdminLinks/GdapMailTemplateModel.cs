namespace ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

public sealed class GdapMailTemplateModel
{
    public int Id { get; set; }
    public string TemplateKey { get; set; } = "GDAP_APPROVAL_RENEWAL";
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

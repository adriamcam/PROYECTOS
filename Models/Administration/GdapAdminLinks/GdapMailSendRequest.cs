namespace ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

public sealed class GdapMailSendRequest
{
    public int CustomerId { get; set; }
    public int TemplateId { get; set; }
    public string SentBy { get; set; } = string.Empty;
    public bool OpenInOutlookOnly { get; set; }
}

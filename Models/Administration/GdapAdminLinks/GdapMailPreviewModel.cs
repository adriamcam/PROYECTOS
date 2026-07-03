namespace ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

public sealed class GdapMailPreviewModel
{
    public int CustomerId { get; set; }
    public int TemplateId { get; set; }
    public string To { get; set; } = string.Empty;
    public string Cc { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string MailToUrl { get; set; } = string.Empty;
}

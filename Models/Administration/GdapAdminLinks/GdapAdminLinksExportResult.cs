namespace ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

public sealed class GdapAdminLinksExportResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv";
    public string Base64Content { get; set; } = string.Empty;
    public int TotalRows { get; set; }
}

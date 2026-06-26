namespace ITQS.SupportOperationsCenter.Models.Dashboard;

public sealed class AlertCommentRequestModel
{
    public string SourceType { get; set; } = string.Empty; // Management / Backup

    public long AlertId { get; set; }

    public string ClientName { get; set; } = string.Empty;
    public string AlertName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;

    public string Status { get; set; } = "InProgress"; // InProgress / Closed

    public string Comment { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
}
namespace ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

public sealed class GdapNotificationLogModel
{
    public int Id { get; set; }
    public string CustomerTenantId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PartnerTenant { get; set; } = string.Empty;
    public string NotificationCase { get; set; } = string.Empty;
    public string NotificationStage { get; set; } = string.Empty;
    public int? DaysToExpire { get; set; }
    public DateTime? ActiveEndDate { get; set; }
    public string ApprovalPendingLink { get; set; } = string.Empty;
    public string SentTo { get; set; } = string.Empty;
    public string SentCc { get; set; } = string.Empty;
    public string FlowName { get; set; } = string.Empty;
    public string FlowRunId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string ResolutionStatus { get; set; } = string.Empty;
    public string ResolutionReason { get; set; } = string.Empty;
}
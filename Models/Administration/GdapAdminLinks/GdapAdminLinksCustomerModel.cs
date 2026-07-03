namespace ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

public sealed class GdapAdminLinksCustomerModel
{
    public int Id { get; set; }
    public DateTime ExecutionDate { get; set; }
    public string PartnerTenant { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerTenantId { get; set; } = string.Empty;
    public string HasGdap { get; set; } = string.Empty;
    public int RelationshipQty { get; set; }
    public string StatusFound { get; set; } = string.Empty;
    public DateTime? ActiveEndDate { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string ApprovalPendingLink { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string PrimaryContactName { get; set; } = string.Empty;
    public string PrimaryEmail { get; set; } = string.Empty;
    public string CCEmails { get; set; } = string.Empty;
    public bool AutoSendEmail { get; set; }
    public string ExcludeReason { get; set; } = string.Empty;
    public DateTime? LastEmailSentAt { get; set; }
    public string LastEmailSentBy { get; set; } = string.Empty;
    public string SendMailStatus { get; set; } = string.Empty;
    public int SendMailAttempts { get; set; }
    public string LastAutomationStatus { get; set; } = string.Empty;
    public string LastAutomationMessage { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public int? DaysToExpire { get; set; }

public bool EnableGDAPAutomation { get; set; } = true;
public string GDAPAutomationReason { get; set; } = string.Empty;

    public bool CanSendEmail =>
        IsActive &&
        !string.IsNullOrWhiteSpace(PrimaryEmail) &&
        !string.IsNullOrWhiteSpace(ApprovalPendingLink) &&
        StatusFound.Contains("approvalPending", StringComparison.OrdinalIgnoreCase);
}


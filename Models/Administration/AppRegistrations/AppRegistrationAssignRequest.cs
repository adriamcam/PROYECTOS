namespace ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;

public sealed class AppRegistrationAssignRequest
{
    public long AppRegistrationId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string SubscriptionName { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string CredentialType { get; set; } = string.Empty;
    public Guid? KeyId { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DaysToExpire { get; set; }
    public string Priority { get; set; } = "Alta";
    public string AssignedTo { get; set; } = string.Empty;
    public string AssignedEmail { get; set; } = string.Empty;
    public string AssignedBy { get; set; } = string.Empty;
    public DateTime? RequiredDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

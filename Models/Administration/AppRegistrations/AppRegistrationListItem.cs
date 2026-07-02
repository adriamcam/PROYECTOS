namespace ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;

public class AppRegistrationListItem
{
    public long Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string SubscriptionName { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string CredentialType { get; set; } = string.Empty;
    public Guid? KeyId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DaysToExpire { get; set; }
    public bool IsExpired { get; set; }
    public DateTime ScanDate { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public int HealthPercent { get; set; }
}

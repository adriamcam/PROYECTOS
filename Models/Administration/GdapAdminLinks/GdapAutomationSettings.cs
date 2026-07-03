namespace ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

public sealed class GdapAutomationSettings
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceGroupName { get; set; } = string.Empty;
    public string AutomationAccountName { get; set; } = string.Empty;
    public string RunbookName { get; set; } = "ITQS-SOC-GENERA-GDAP-PC-CLIENTES";
    public int DefaultDaysThreshold { get; set; } = 30;
    public string ApiVersion { get; set; } = "2023-11-01";
}

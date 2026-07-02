namespace ITQS.SupportOperationsCenter.Models.Administration.Customers;

public sealed class AutomationRunbookSettings
{
    public string SubscriptionId { get; set; } = "7515e871-2a0a-40ae-a52b-339cce86c58b";
    public string ResourceGroupName { get; set; } = "rg-automation";
    public string AutomationAccountName { get; set; } = "AUTOMATIONMONITOR";
    public string ValidateConnectionsRunbookName { get; set; } = "ITQS-SOC-VALIDATE-CONNECTIONS-CLIENTES";
    public string ApiVersion { get; set; } = "2024-10-23";
}

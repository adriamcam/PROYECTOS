namespace ITQS.SupportOperationsCenter.Models.Administration.Customers;

public sealed class CustomerConnectionRunbookRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string SecretName { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

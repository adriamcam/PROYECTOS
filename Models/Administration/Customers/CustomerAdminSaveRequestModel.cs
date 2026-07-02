namespace ITQS.SupportOperationsCenter.Models.Administration.Customers;

public sealed class CustomerAdminSaveRequestModel
{
    public Guid? TenantId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerNamePortal { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string SecretName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = "SupportCloud";
    public string Notes { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

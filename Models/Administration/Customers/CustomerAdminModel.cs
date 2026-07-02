namespace ITQS.SupportOperationsCenter.Models.Administration.Customers;

public sealed class CustomerAdminModel
{
    public Guid TenantId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerNamePortal { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string SecretName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

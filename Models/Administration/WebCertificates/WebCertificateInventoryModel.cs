namespace ITQS.SupportOperationsCenter.Models.Administration.WebCertificates;

public sealed class WebCertificateInventoryModel
{
    public long CurrentId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string SubscriptionName { get; set; } = string.Empty;

    public string ResourceGroup { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;

    public string CertName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string SslState { get; set; } = string.Empty;

    public string Thumbprint { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;

    public DateTime? NotBefore { get; set; }
    public DateTime? NotAfter { get; set; }
    public int? DaysToExpire { get; set; }

    public string Status { get; set; } = string.Empty;
    public int StatusPriority { get; set; }

    public bool HasExpiryDate { get; set; }
    public bool IsExpired { get; set; }
    public bool IsCritical { get; set; }
    public bool IsWarning { get; set; }
    public bool IsHealthy { get; set; }
    public bool IsUnknown { get; set; }

    public bool IsActive { get; set; }
    public string UsageStatus { get; set; } = string.Empty;

    public string KeyVaultName { get; set; } = string.Empty;
    public string AppServiceSiteName { get; set; } = string.Empty;
    public string ApplicationGatewayName { get; set; } = string.Empty;

    public DateTime LastScanAt { get; set; }
    public bool IsPresent { get; set; }

    public string BindingType { get; set; } = string.Empty;
    public string CertificateType { get; set; } = string.Empty;
    public string DomainProvider { get; set; } = string.Empty;

    public bool IsManagedCertificate { get; set; }
    public bool UsesKeyVault { get; set; }

    public string KeyVaultSecretName { get; set; } = string.Empty;
}

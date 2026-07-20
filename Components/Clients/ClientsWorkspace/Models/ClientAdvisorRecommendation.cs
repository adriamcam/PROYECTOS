namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models;

public sealed class ClientAdvisorRecommendation
{
    public long Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public Guid SubscriptionId { get; set; }

    public string SubscriptionName { get; set; } = string.Empty;

    public string ResourceGroup { get; set; } = string.Empty;

    public string AffectedResource { get; set; } = string.Empty;

    public string ResourceName { get; set; } = string.Empty;

    public string AzureCategory { get; set; } = "Sin categoría";

    public string AdvisorType { get; set; } = string.Empty;

    public string Impact { get; set; } = "Sin clasificar";

    public string Currency { get; set; } = "USD";

    public decimal AnnualSavingsAmount { get; set; }

    public string RecommendationSpanish { get; set; } =
        "Recomendación de Azure Advisor";

    public string DescriptionSpanish { get; set; } = string.Empty;

    public string RemediationAzure { get; set; } = string.Empty;

    public string CostClassification { get; set; } = string.Empty;

    public bool RequiresMaintenanceWindow { get; set; }

    public int ImplementationMinutes { get; set; }

    public decimal ImplementationHours { get; set; }

    public string Complexity { get; set; } = "Sin clasificar";

    public string MaintenanceWindowText { get; set; } = string.Empty;

    public DateTime? AdvisorUpdatedAt { get; set; }
}

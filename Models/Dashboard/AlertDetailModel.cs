namespace ITQS.SupportOperationsCenter.Models.Dashboard;

public sealed class AlertDetailModel
{
    // Información general
    public string SourceType { get; set; } = string.Empty;
    public long AlertId { get; set; }

    public string ClientName { get; set; } = string.Empty;
    public string SubscriptionName { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;

    // Alerta
    public string AlertName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string AlertStatus { get; set; } = string.Empty;
    public string AlertState { get; set; } = string.Empty;

    // Recurso
    public string ResourceName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    // Información técnica
    public string Details { get; set; } = string.Empty;
    public string ErrorDetail { get; set; } = string.Empty;

    // Eventos
    public int Events { get; set; }

    public DateTime? AlertTime { get; set; }
    public DateTime? FirstOccurrence { get; set; }
    public DateTime? LastOccurrence { get; set; }

    // Asignación
    public string AssignedTo { get; set; } = string.Empty;
    public string AssignedEmail { get; set; } = string.Empty;

    // Resolución
    public string ResolutionNotes { get; set; } = string.Empty;

    // Historial del popup
    public List<AlertHistoryItemModel> History { get; set; } = new();
}
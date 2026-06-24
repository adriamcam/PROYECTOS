namespace ITQS.SupportOperationsCenter.Models;

public sealed class AlertDashboardDto
{
    public int TotalActive { get; set; }
    public int AssignedToMe { get; set; }
    public int Unassigned { get; set; }
    public int Critical { get; set; }
    public int High { get; set; }
    public int PendingClose { get; set; }
}

public sealed class AlertListItemDto
{
    public long Id { get; set; }
    public string? AlertId { get; set; }
    public string? CustomerName { get; set; }
    public string? SubscriptionName { get; set; }
    public string? AlertName { get; set; }
    public string? KPIType { get; set; }
    public string? ResourceName { get; set; }
    public string? Severity { get; set; }
    public int Events { get; set; }
    public string? AssignedTo { get; set; }
    public string? AssignedEmail { get; set; }
    public DateTime? LastInsertedAt { get; set; }
}

public sealed class AlertHistoryDto
{
    public long Id { get; set; }
    public long AlertRecordId { get; set; }
    public string? Action { get; set; }
    public string? UserEmail { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AlertDetailResultDto
{
    public AlertListItemDto? Alert { get; set; }
    public List<AlertHistoryDto> History { get; set; } = new();
}

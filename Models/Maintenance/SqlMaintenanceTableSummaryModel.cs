namespace ITQS.SupportOperationsCenter.Models.Maintenance;
public sealed class SqlMaintenanceTableSummaryModel
{
    public string TableName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long TotalRows { get; set; }
    public long ActiveRows { get; set; }
    public long ClosedRows { get; set; }
    public long PendingRows { get; set; }
    public long ProcessedRows { get; set; }
    public long NotFoundRows { get; set; }
    public long RetryRows { get; set; }
    public long ErrorRows { get; set; }
    public long EligibleToDelete { get; set; }
    public decimal TableSizeMb { get; set; }
    public decimal EstimatedRecoverableMb { get; set; }
    public bool IsProtected { get; set; }
    public string LastCleanupText { get; set; } = "Nunca";
    public string RetentionRule { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

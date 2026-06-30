namespace ITQS.SupportOperationsCenter.Models.Maintenance;
public sealed class SqlMaintenanceHistoryModel
{
    public long Id { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public long RowsAffected { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;
    public string ExecutedByEmail { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
}

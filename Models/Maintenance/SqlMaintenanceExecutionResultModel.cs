namespace ITQS.SupportOperationsCenter.Models.Maintenance;
public sealed class SqlMaintenanceExecutionResultModel
{
    public string TableName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public int RetentionDays { get; set; }
    public int BatchSize { get; set; }
    public long RowsAffected { get; set; }
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;
    public string ExecutedByEmail { get; set; } = string.Empty;
    public string DurationText { get { var d = FinishedAt - StartedAt; if (d.TotalSeconds < 1) return "< 1 segundo"; if (d.TotalMinutes < 1) return $"{d.TotalSeconds:N0} segundos"; return $"{d.TotalMinutes:N1} minutos"; } }
}

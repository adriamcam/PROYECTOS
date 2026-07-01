namespace ITQS.SupportOperationsCenter.Models.Maintenance.SqlOperations;
public sealed class SqlSlowQueryModel
{
    public long ExecutionCount { get; set; }
    public decimal AvgDurationMs { get; set; }
    public decimal AvgCpuMs { get; set; }
    public decimal AvgReads { get; set; }
    public decimal TotalDurationMs { get; set; }
    public string LastExecutionTime { get; set; } = string.Empty;
    public string SqlText { get; set; } = string.Empty;
}

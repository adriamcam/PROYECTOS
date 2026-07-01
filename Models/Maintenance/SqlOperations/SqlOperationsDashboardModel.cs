namespace ITQS.SupportOperationsCenter.Models.Maintenance.SqlOperations;

public sealed class SqlOperationsDashboardModel
{
    public string DatabaseName { get; set; } = string.Empty;
    public string HealthStatus { get; set; } = "Saludable";
    public decimal TotalSpaceMb { get; set; }
    public long TotalRows { get; set; }
    public decimal EstimatedRecoverableMb { get; set; }
    public string LastMaintenanceText { get; set; } = "Nunca";
    public int TotalJobs { get; set; }
    public int FailedJobs { get; set; }
    public int ActiveBlocks { get; set; }
    public int ActiveSessions { get; set; }
    public int RunningSessions { get; set; }
    public int SleepingSessions { get; set; }
    public int SlowQueries { get; set; }
    public decimal MaxAvgDurationMs { get; set; }
    public long TotalExecutions { get; set; }
}

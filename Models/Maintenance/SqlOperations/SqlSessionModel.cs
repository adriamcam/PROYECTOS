namespace ITQS.SupportOperationsCenter.Models.Maintenance.SqlOperations;
public sealed class SqlSessionModel
{
    public int SessionId { get; set; }
    public string LoginName { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public int CpuTimeMs { get; set; }
    public long Reads { get; set; }
    public long Writes { get; set; }
    public int OpenTransactions { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string SqlText { get; set; } = string.Empty;
}

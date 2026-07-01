namespace ITQS.SupportOperationsCenter.Models.Maintenance.SqlOperations;
public sealed class SqlBlockingModel
{
    public int SessionId { get; set; }
    public int BlockingSessionId { get; set; }
    public string LoginName { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string WaitType { get; set; } = string.Empty;
    public long WaitTimeMs { get; set; }
    public string Command { get; set; } = string.Empty;
    public string SqlText { get; set; } = string.Empty;
}

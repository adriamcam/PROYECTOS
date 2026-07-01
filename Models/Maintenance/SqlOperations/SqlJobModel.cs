namespace ITQS.SupportOperationsCenter.Models.Maintenance.SqlOperations;
public sealed class SqlJobModel
{
    public string JobName { get; set; } = string.Empty;
    public string Enabled { get; set; } = string.Empty;
    public string LastRunStatus { get; set; } = string.Empty;
    public string LastRunDateTime { get; set; } = string.Empty;
    public string LastRunDuration { get; set; } = string.Empty;
    public string NextRunDateTime { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
}

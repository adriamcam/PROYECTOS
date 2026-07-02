namespace ITQS.SupportOperationsCenter.Models.Administration.Customers;

public sealed class CustomerConnectionJobStatusResult
{
    public bool Found { get; set; }
    public string JobId { get; set; } = string.Empty;
    public string RunbookName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusDetails { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTimeOffset? CreationTime { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public DateTimeOffset? LastModifiedTime { get; set; }

    public bool IsFinal =>
        Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
        Status.Equals("Failed", StringComparison.OrdinalIgnoreCase) ||
        Status.Equals("Stopped", StringComparison.OrdinalIgnoreCase) ||
        Status.Equals("Suspended", StringComparison.OrdinalIgnoreCase);
}

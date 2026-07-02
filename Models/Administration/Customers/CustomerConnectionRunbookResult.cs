namespace ITQS.SupportOperationsCenter.Models.Administration.Customers;

public sealed class CustomerConnectionRunbookResult
{
    public bool Started { get; set; }
    public string Status { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string RunbookName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

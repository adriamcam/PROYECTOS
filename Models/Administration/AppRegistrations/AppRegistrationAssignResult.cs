namespace ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;

public sealed class AppRegistrationAssignResult
{
    public bool Success { get; set; }
    public long TaskId { get; set; }
    public bool EmailSent { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

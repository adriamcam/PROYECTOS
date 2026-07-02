namespace ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;

public sealed class AppRegistrationFilterModel
{
    public string Search { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CredentialType { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
}

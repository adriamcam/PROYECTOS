namespace ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;

public sealed class AppRegistrationNotificationSettings
{
    public string FromUser { get; set; } = string.Empty;
    public string PortalUrl { get; set; } = "https://itqssupportoperationscenter-cudchsf9cwcfaeee.eastus2-01.azurewebsites.net";
    public bool Enabled { get; set; } = true;
	public string TenantId { get; set; } = string.Empty;
public string ClientId { get; set; } = string.Empty;
public string ClientSecretName { get; set; } = string.Empty;
}

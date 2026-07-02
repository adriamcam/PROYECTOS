namespace ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;

public sealed class AppRegistrationNotificationSettings
{
    public string FromUser { get; set; } = "soporteitqs@itqscr.com";
    public string PortalUrl { get; set; } = "https://itqssupportoperationscenter-cudchsf9cwcfaeee.eastus2-01.azurewebsites.net";
    public bool Enabled { get; set; } = true;
}

namespace ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

public sealed class GdapMailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string FromDisplayName { get; set; } = "ITQS Servicios Administrados";
    public string SupportEmail { get; set; } = "soporteitqs@itqscr.com";
    public string SupportPhone { get; set; } = string.Empty;
    public string UsernameSecretName { get; set; } = string.Empty;
    public string PasswordSecretName { get; set; } = string.Empty;
}

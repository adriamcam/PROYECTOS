using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;
using ITQS.SupportOperationsCenter.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class AppRegistrationNotificationService : IAppRegistrationNotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SecretClient _secretClient;
    private readonly AppRegistrationNotificationSettings _settings;
    private readonly ILogger<AppRegistrationNotificationService> _logger;

    public AppRegistrationNotificationService(
        IHttpClientFactory httpClientFactory,
        SecretClient secretClient,
        IOptions<AppRegistrationNotificationSettings> options,
        ILogger<AppRegistrationNotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _secretClient = secretClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAssignmentEmailAsync(AppRegistrationAssignRequest request, long taskId)
    {
        if (!_settings.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(_settings.TenantId))
            throw new InvalidOperationException("AppRegistrationNotifications:TenantId no está configurado.");

        if (string.IsNullOrWhiteSpace(_settings.ClientId))
            throw new InvalidOperationException("AppRegistrationNotifications:ClientId no está configurado.");

        if (string.IsNullOrWhiteSpace(_settings.ClientSecretName))
            throw new InvalidOperationException("AppRegistrationNotifications:ClientSecretName no está configurado.");

        if (string.IsNullOrWhiteSpace(_settings.FromUser))
            throw new InvalidOperationException("AppRegistrationNotifications:FromUser no está configurado.");

      AccessToken token;

try
{
    var secret = await _secretClient.GetSecretAsync(_settings.ClientSecretName);

    var credential = new ClientSecretCredential(
        _settings.TenantId,
        _settings.ClientId,
        secret.Value.Value);

    token = await credential.GetTokenAsync(
        new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));
}
catch (Exception ex)
{
    throw new InvalidOperationException(
        "Error autenticando con la App Registration configurada para notificaciones Graph. " +
        ex.ToString());
}

        var credential = new ClientSecretCredential(
            _settings.TenantId,
            _settings.ClientId,
            secret.Value.Value);

        var token = await credential.GetTokenAsync(
            new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        var payload = new
        {
            message = new
            {
                subject = $"[SOC] Solicitud App Registration ITQS - {request.CustomerName}",
                body = new
                {
                    contentType = "HTML",
                    content = BuildHtml(request, taskId)
                },
                toRecipients = new[]
                {
                    new
                    {
                        emailAddress = new
                        {
                            address = request.AssignedEmail
                        }
                    }
                }
            },
            saveToSentItems = true
        };

        var url = $"https://graph.microsoft.com/v1.0/users/{_settings.FromUser}/sendMail";

        var response = await client.PostAsJsonAsync(url, payload);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Error enviando correo con Microsoft Graph. HTTP {(int)response.StatusCode}: {body}");
        }
    }

    private string BuildHtml(AppRegistrationAssignRequest request, long taskId)
    {
        var portalUrl = string.IsNullOrWhiteSpace(_settings.PortalUrl) ? "#" : _settings.PortalUrl;

        return $@"
<html>
<body style='font-family:Segoe UI,Arial,sans-serif;background:#f4f7fb;padding:24px;color:#0f172a;'>
  <div style='max-width:760px;margin:auto;background:white;border:1px solid #dbe3ef;border-radius:18px;overflow:hidden;box-shadow:0 18px 40px rgba(15,23,42,.08);'>
    <div style='background:#0b1f3a;color:white;padding:24px;'>
      <h2 style='margin:0;'>Nueva solicitud App Registration ITQS</h2>
      <p style='margin:8px 0 0;color:#cbd5e1;'>Tarea #{taskId} asignada desde Support Operations Center</p>
    </div>

    <div style='padding:24px;'>
      <h3 style='margin-top:0;'>Información de la solicitud</h3>

      <table style='width:100%;border-collapse:collapse;'>
        <tr><td style='padding:8px;font-weight:700;'>Cliente</td><td style='padding:8px;'>{request.CustomerName}</td></tr>
        <tr><td style='padding:8px;font-weight:700;'>TenantId</td><td style='padding:8px;font-family:Consolas;'>{request.TenantId}</td></tr>
        <tr><td style='padding:8px;font-weight:700;'>App Registration</td><td style='padding:8px;'>{request.AppName}</td></tr>
        <tr><td style='padding:8px;font-weight:700;'>ClientId</td><td style='padding:8px;font-family:Consolas;'>{request.ClientId}</td></tr>
        <tr><td style='padding:8px;font-weight:700;'>Tipo</td><td style='padding:8px;'>{request.CredentialType}</td></tr>
        <tr><td style='padding:8px;font-weight:700;'>Expira</td><td style='padding:8px;'>{request.EndDate:dd/MM/yyyy HH:mm}</td></tr>
        <tr><td style='padding:8px;font-weight:700;'>Días restantes</td><td style='padding:8px;'>{request.DaysToExpire}</td></tr>
        <tr><td style='padding:8px;font-weight:700;'>Prioridad</td><td style='padding:8px;'>{request.Priority}</td></tr>
        <tr><td style='padding:8px;font-weight:700;'>Fecha requerida</td><td style='padding:8px;'>{request.RequiredDate:dd/MM/yyyy}</td></tr>
        <tr><td style='padding:8px;font-weight:700;'>Asignado por</td><td style='padding:8px;'>{request.AssignedBy}</td></tr>
      </table>

      <h3>Observaciones</h3>
      <div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:16px;'>
        {System.Net.WebUtility.HtmlEncode(request.Notes)}
      </div>

      <p style='margin-top:24px;'>
        <a href='{portalUrl}' style='background:#2563eb;color:white;text-decoration:none;padding:12px 18px;border-radius:10px;font-weight:700;display:inline-block;'>
          Abrir Support Operations Center
        </a>
      </p>
    </div>
  </div>
</body>
</html>";
    }
}
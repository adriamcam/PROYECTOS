using System.Net;
using System.Net.Mail;
using Azure.Security.KeyVault.Secrets;
using ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;
using ITQS.SupportOperationsCenter.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class GdapMailSenderService : IGdapMailSenderService
{
    private readonly GdapMailSettings _settings;
    private readonly SecretClient _secretClient;

    public GdapMailSenderService(
        IOptions<GdapMailSettings> options,
        SecretClient secretClient)
    {
        _settings = options.Value;
        _secretClient = secretClient;
    }

    public async Task SendAsync(GdapMailPreviewModel message)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
            throw new InvalidOperationException("GdapMail:SmtpHost no está configurado.");

        if (string.IsNullOrWhiteSpace(_settings.FromEmail))
            throw new InvalidOperationException("GdapMail:FromEmail no está configurado.");

        if (string.IsNullOrWhiteSpace(message.To))
            throw new InvalidOperationException("El correo destino está vacío.");

        using var mail = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromDisplayName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true
        };

        foreach (var to in SplitEmails(message.To))
            mail.To.Add(to);

        foreach (var cc in SplitEmails(message.Cc))
            mail.CC.Add(cc);

        using var smtp = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_settings.UsernameSecretName) &&
            !string.IsNullOrWhiteSpace(_settings.PasswordSecretName))
        {
            var user = await _secretClient.GetSecretAsync(_settings.UsernameSecretName);
            var password = await _secretClient.GetSecretAsync(_settings.PasswordSecretName);
            smtp.Credentials = new NetworkCredential(user.Value.Value, password.Value.Value);
        }

        //await smtp.SendMailAsync(mail);
    }

    private static IEnumerable<string> SplitEmails(string value)
        => (value ?? string.Empty)
            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x));
}

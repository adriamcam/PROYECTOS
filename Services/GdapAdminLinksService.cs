using ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class GdapAdminLinksService : IGdapAdminLinksService
{
    private readonly IGdapAdminLinksRepository _repository;
    private readonly IGdapAutomationRunnerService _automationRunner;
    private readonly IGdapMailSenderService _mailSender;
    private readonly GdapAutomationSettings _automationSettings;
    private readonly GdapMailSettings _mailSettings;
    private readonly ILogger<GdapAdminLinksService> _logger;

    public GdapAdminLinksService(
        IGdapAdminLinksRepository repository,
        IGdapAutomationRunnerService automationRunner,
        IGdapMailSenderService mailSender,
        IOptions<GdapAutomationSettings> automationOptions,
        IOptions<GdapMailSettings> mailOptions,
        ILogger<GdapAdminLinksService> logger)
    {
        _repository = repository;
        _automationRunner = automationRunner;
        _mailSender = mailSender;
        _automationSettings = automationOptions.Value;
        _mailSettings = mailOptions.Value;
        _logger = logger;
    }

    public Task<GdapAdminLinksDashboardModel> GetDashboardAsync()
        => _repository.GetDashboardAsync();

    public Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetCustomersAsync(GdapAdminLinksFilterModel filters)
        => _repository.GetCustomersAsync(filters);

    public Task<GdapAdminLinksCustomerModel?> GetCustomerAsync(int id)
        => _repository.GetCustomerAsync(id);

    public Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetPendingEmailsAsync()
        => _repository.GetPendingEmailsAsync();

    public Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetExpiringSoonAsync()
        => _repository.GetExpiringSoonAsync();


    public Task<IReadOnlyList<GdapAdminLinksAuditEventModel>> GetAuditEventsAsync(int? customerId = null)
        => _repository.GetAuditEventsAsync(customerId);

    public Task<IReadOnlyList<GdapAdminLinksReportModel>> GetReportByPartnerAsync()
        => _repository.GetReportByPartnerAsync();

    public async Task<GdapAdminLinksExportResult> ExportCustomersCsvAsync(GdapAdminLinksFilterModel filters)
    {
        var customers = await _repository.GetCustomersAsync(filters);
        var sb = new StringBuilder();
        sb.AppendLine("PartnerTenant,CustomerName,CustomerTenantId,StatusFound,ActiveEndDate,DaysToExpire,PrimaryEmail,ApprovalPendingLink,IsActive,LastEmailSentAt,LastAutomationStatus");

        foreach (var item in customers)
        {
            sb.AppendLine(string.Join(',', new[]
            {
                Csv(item.PartnerTenant),
                Csv(item.CustomerName),
                Csv(item.CustomerTenantId),
                Csv(item.StatusFound),
                Csv(item.ActiveEndDate?.ToString("yyyy-MM-dd") ?? string.Empty),
                Csv(item.DaysToExpire?.ToString() ?? string.Empty),
                Csv(item.PrimaryEmail),
                Csv(item.ApprovalPendingLink),
                Csv(item.IsActive ? "1" : "0"),
                Csv(item.LastEmailSentAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty),
                Csv(item.LastAutomationStatus)
            }));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return new GdapAdminLinksExportResult
        {
            FileName = $"AdminLinksGDAP_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            ContentType = "text/csv",
            Base64Content = Convert.ToBase64String(bytes),
            TotalRows = customers.Count
        };
    }

    public async Task<GdapAdminLinksActionResult> UpdateCustomerAsync(GdapAdminLinksSaveCustomerRequest request)
    {
        try
        {
            if (request.Id <= 0)
                throw new InvalidOperationException("Cliente inválido.");

            if (!string.IsNullOrWhiteSpace(request.PrimaryEmail) && !request.PrimaryEmail.Contains('@'))
                throw new InvalidOperationException("El correo principal no tiene un formato válido.");

            await _repository.UpdateCustomerAsync(request);
            await _repository.RegisterHistoryAsync(request.Id, "Cliente actualizado", "Se actualizaron los datos de contacto/configuración del cliente.", request.UpdatedBy);

            return new GdapAdminLinksActionResult
            {
                Success = true,
                Message = "Cliente actualizado correctamente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando cliente GDAP {Id}", request.Id);
            return new GdapAdminLinksActionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Message = "No fue posible actualizar el cliente."
            };
        }
    }

    public async Task<GdapAdminLinksActionResult> DisableCustomerAsync(int id, string updatedBy, string reason)
    {
        try
        {
            await _repository.SetCustomerActiveAsync(id, false, updatedBy, reason);
            await _repository.RegisterHistoryAsync(id, "Cliente desactivado", reason, updatedBy);
            return new GdapAdminLinksActionResult { Success = true, Message = "Cliente desactivado correctamente." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error desactivando cliente GDAP {Id}", id);
            return new GdapAdminLinksActionResult { Success = false, ErrorMessage = ex.Message, Message = "No fue posible desactivar el cliente." };
        }
    }

    public async Task<GdapAdminLinksActionResult> EnableCustomerAsync(int id, string updatedBy)
    {
        try
        {
            await _repository.SetCustomerActiveAsync(id, true, updatedBy, string.Empty);
            await _repository.RegisterHistoryAsync(id, "Cliente reactivado", "Cliente habilitado nuevamente para procesamiento GDAP.", updatedBy);
            return new GdapAdminLinksActionResult { Success = true, Message = "Cliente reactivado correctamente." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivando cliente GDAP {Id}", id);
            return new GdapAdminLinksActionResult { Success = false, ErrorMessage = ex.Message, Message = "No fue posible reactivar el cliente." };
        }
    }

    public async Task<GdapAdminLinksActionResult> SetGdapAutomationStatusAsync(int id, bool enabled, string updatedBy, string reason)
{
    try
    {
        await _repository.SetGdapAutomationStatusAsync(id, enabled, updatedBy, reason);

        await _repository.RegisterHistoryAsync(
            id,
            enabled
                ? "Automation GDAP habilitada"
                : "Automation GDAP deshabilitada",
            enabled
                ? "Cliente habilitado para generar GDAP."
                : reason,
            updatedBy);

        return new GdapAdminLinksActionResult
        {
            Success = true,
            Message = enabled
                ? "Automation GDAP habilitada."
                : "Automation GDAP deshabilitada."
        };
    }
    catch(Exception ex)
    {
        _logger.LogError(ex,"Error actualizando EnableGDAPAutomation");

        return new GdapAdminLinksActionResult
        {
            Success=false,
            ErrorMessage=ex.Message,
            Message="No fue posible actualizar el cliente."
        };
    }
}

public async Task<GdapAdminLinksActionResult> ExecuteAutomationAsync(int id, string requestedBy)
    {
        try
        {
            var customer = await _repository.GetCustomerAsync(id);
            if (customer is null)
                throw new InvalidOperationException("No se encontró el cliente seleccionado.");

            if (!customer.IsActive)
                throw new InvalidOperationException("El cliente está desactivado. Reactívelo antes de ejecutar la Automation.");

            if (string.IsNullOrWhiteSpace(customer.CustomerTenantId))
                throw new InvalidOperationException("El cliente no tiene CustomerTenantId configurado.");

            if (string.IsNullOrWhiteSpace(customer.PartnerTenant))
                throw new InvalidOperationException("El cliente no tiene PartnerTenant configurado.");

            var request = new GdapAdminLinksAutomationRequest
            {
                CustomerId = customer.Id,
                PartnerTenant = customer.PartnerTenant,
                CustomerName = customer.CustomerName,
                CustomerTenantId = customer.CustomerTenantId,
                DaysThreshold = _automationSettings.DefaultDaysThreshold <= 0 ? 30 : _automationSettings.DefaultDaysThreshold,
                RequestedBy = requestedBy
            };

            await _repository.MarkAutomationStartedAsync(request, string.Empty);
            var result = await _automationRunner.StartRunbookForCustomerAsync(request);
            await _repository.MarkAutomationFinishedAsync(request, result);

            if (!result.Success)
            {
                return new GdapAdminLinksActionResult
                {
                    Success = false,
                    ErrorMessage = result.ErrorMessage,
                    Message = result.Message
                };
            }

            return new GdapAdminLinksActionResult
            {
                Success = true,
                Message = result.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando Automation GDAP para cliente {Id}", id);
            return new GdapAdminLinksActionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Message = "No fue posible ejecutar la Automation GDAP."
            };
        }
    }

    public async Task<GdapAdminLinksActionResult> SendExpirationReminderEmailsAsync(int daysToExpire, int templateId, string sentBy)
    {
        try
        {
            if (daysToExpire != 7 && daysToExpire != 15 && daysToExpire != 30)
                throw new InvalidOperationException("Solo se permiten recordatorios de vencimiento de 7, 15 o 30 días.");

            if (string.IsNullOrWhiteSpace(sentBy))
                throw new InvalidOperationException("No se pudo identificar el usuario que envía los correos.");

            var customers = await _repository.GetExpirationEmailQueueAsync(daysToExpire);

            if (customers.Count == 0)
            {
                return new GdapAdminLinksActionResult
                {
                    Success = true,
                    Message = $"No hay correos pendientes para clientes con vencimiento en {daysToExpire} días. Recuerda: solo se envía si el cliente tiene approvalPending, Approval URL y correo configurado."
                };
            }

            var sent = 0;
            var failed = 0;
            var errors = new List<string>();

            foreach (var customer in customers)
            {
                try
                {
                    var preview = await PreviewEmailAsync(customer.Id, templateId);
                    await _mailSender.SendAsync(preview);
                    await _repository.MarkEmailSentAsync(customer.Id, sentBy);
                    await _repository.RegisterHistoryAsync(
                        customer.Id,
                        $"Correo GDAP {daysToExpire} días",
                        $"Correo preventivo enviado a {preview.To}. GDAP vence en {customer.DaysToExpire} días.",
                        sentBy,
                        customer.ApprovalPendingLink);
                    sent++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"{customer.CustomerName}: {ex.Message}");
                    try
                    {
                        await _repository.MarkEmailFailedAsync(customer.Id, sentBy, ex.Message);
                    }
                    catch (Exception markEx)
                    {
                        _logger.LogWarning(markEx, "No se pudo registrar error de correo preventivo GDAP para cliente {CustomerId}", customer.Id);
                    }
                }
            }

            return new GdapAdminLinksActionResult
            {
                Success = failed == 0,
                Message = failed == 0
                    ? $"Correos preventivos de {daysToExpire} días enviados correctamente: {sent}."
                    : $"Correos enviados: {sent}. Errores: {failed}.",
                ErrorMessage = failed == 0 ? string.Empty : string.Join(" | ", errors.Take(5))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando correos preventivos GDAP para {DaysToExpire} días", daysToExpire);
            return new GdapAdminLinksActionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Message = "No fue posible enviar los correos preventivos GDAP."
            };
        }
    }


    public Task<IReadOnlyList<GdapMailTemplateModel>> GetMailTemplatesAsync()
        => _repository.GetMailTemplatesAsync();

    public async Task<GdapMailPreviewModel> PreviewEmailAsync(int customerId, int templateId)
    {
        var customer = await _repository.GetCustomerAsync(customerId)
            ?? throw new InvalidOperationException("No se encontró el cliente seleccionado.");

        if (!customer.CanSendEmail)
            throw new InvalidOperationException("El cliente no cumple las condiciones para enviar correo: approvalPending, Approval URL, activo y correo configurado.");

        var template = templateId > 0
            ? await _repository.GetMailTemplateAsync(templateId)
            : await _repository.GetDefaultMailTemplateAsync();

        if (template is null)
            throw new InvalidOperationException("No existe una plantilla de correo activa para GDAP.");

        var subject = RenderTemplate(template.Subject, customer);
        var body = RenderTemplate(template.HtmlBody, customer);
        var mailTo = BuildMailTo(customer.PrimaryEmail, customer.CCEmails, subject, StripHtml(body));

        return new GdapMailPreviewModel
        {
            CustomerId = customer.Id,
            TemplateId = template.Id,
            To = customer.PrimaryEmail,
            Cc = customer.CCEmails,
            Subject = subject,
            HtmlBody = body,
            MailToUrl = mailTo
        };
    }

    public async Task<GdapAdminLinksActionResult> SendEmailAsync(GdapMailSendRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SentBy))
                throw new InvalidOperationException("No se pudo identificar el usuario que envía el correo.");

            var preview = await PreviewEmailAsync(request.CustomerId, request.TemplateId);

            if (request.OpenInOutlookOnly)
            {
                return new GdapAdminLinksActionResult
                {
                    Success = true,
                    Message = preview.MailToUrl
                };
            }

            await _mailSender.SendAsync(preview);
            await _repository.MarkEmailSentAsync(request.CustomerId, request.SentBy);
            await _repository.RegisterHistoryAsync(request.CustomerId, "Correo GDAP enviado", $"Correo enviado a {preview.To}.", request.SentBy, null);

            return new GdapAdminLinksActionResult
            {
                Success = true,
                Message = "Correo GDAP enviado correctamente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando correo GDAP para cliente {CustomerId}", request.CustomerId);
            try
            {
                await _repository.MarkEmailFailedAsync(request.CustomerId, request.SentBy, ex.Message);
            }
            catch (Exception markEx)
            {
                _logger.LogWarning(markEx, "No se pudo marcar error de correo GDAP para cliente {CustomerId}", request.CustomerId);
            }

            return new GdapAdminLinksActionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Message = "No fue posible enviar el correo GDAP."
            };
        }
    }

    public async Task<GdapAdminLinksActionResult> SaveMailTemplateAsync(GdapMailTemplateModel template, string updatedBy)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(template.Name))
                throw new InvalidOperationException("El nombre de la plantilla es requerido.");

            if (string.IsNullOrWhiteSpace(template.Subject))
                throw new InvalidOperationException("El asunto de la plantilla es requerido.");

            if (string.IsNullOrWhiteSpace(template.HtmlBody))
                throw new InvalidOperationException("El cuerpo HTML de la plantilla es requerido.");

            template.UpdatedBy = updatedBy;
            if (string.IsNullOrWhiteSpace(template.CreatedBy))
                template.CreatedBy = updatedBy;

            await _repository.SaveMailTemplateAsync(template);

            return new GdapAdminLinksActionResult
            {
                Success = true,
                Message = "Plantilla de correo guardada correctamente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando plantilla de correo GDAP {TemplateId}", template.Id);
            return new GdapAdminLinksActionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Message = "No fue posible guardar la plantilla."
            };
        }
    }

    private static string Csv(string value)
    {
        value ??= string.Empty;
        value = value.Replace("\r", " ").Replace("\n", " ");
        if (value.Contains(',') || value.Contains('"'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private string RenderTemplate(string value, GdapAdminLinksCustomerModel customer)
    {
        var result = value ?? string.Empty;
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["{{CustomerName}}"] = customer.CustomerName,
            ["{{PartnerTenant}}"] = customer.PartnerTenant,
            ["{{ApprovalUrl}}"] = customer.ApprovalPendingLink,
            ["{{PrimaryContactName}}"] = string.IsNullOrWhiteSpace(customer.PrimaryContactName) ? customer.CustomerName : customer.PrimaryContactName,
            ["{{ExecutionDate}}"] = customer.ExecutionDate.ToString("dd/MM/yyyy HH:mm"),
            ["{{ExpirationDate}}"] = customer.ActiveEndDate?.ToString("dd/MM/yyyy") ?? string.Empty,
            ["{{SupportEmail}}"] = _mailSettings.SupportEmail,
            ["{{SupportPhone}}"] = _mailSettings.SupportPhone
        };

        foreach (var item in replacements)
            result = result.Replace(item.Key, item.Value);

        return result;
    }

    private static string BuildMailTo(string to, string cc, string subject, string body)
    {
        var url = $"mailto:{Uri.EscapeDataString(to)}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
        if (!string.IsNullOrWhiteSpace(cc))
            url += $"&cc={Uri.EscapeDataString(cc)}";
        return url;
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = System.Text.RegularExpressions.Regex.Replace(html, "<br\\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, "</p>", "\n\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty);
        return WebUtility.HtmlDecode(text).Trim();
    }

}


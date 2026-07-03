using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;
using ITQS.SupportOperationsCenter.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class GdapAutomationRunnerService : IGdapAutomationRunnerService
{
    private readonly GdapAutomationSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GdapAutomationRunnerService> _logger;

    public GdapAutomationRunnerService(
        IOptions<GdapAutomationSettings> options,
        IHttpClientFactory httpClientFactory,
        ILogger<GdapAutomationRunnerService> logger)
    {
        _settings = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<GdapAdminLinksAutomationResult> StartRunbookForCustomerAsync(GdapAdminLinksAutomationRequest request)
    {
        try
        {
            ValidateSettings();

            if (string.IsNullOrWhiteSpace(request.CustomerTenantId))
                throw new InvalidOperationException("El CustomerTenantId es requerido para ejecutar la Automation.");

            if (string.IsNullOrWhiteSpace(request.PartnerTenant))
                throw new InvalidOperationException("El PartnerTenant es requerido para ejecutar la Automation.");

            var jobId = Guid.NewGuid().ToString();
            var tokenCredential = new DefaultAzureCredential();
            var token = await tokenCredential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://management.azure.com/.default" }));

            var url = $"https://management.azure.com/subscriptions/{_settings.SubscriptionId}" +
                      $"/resourceGroups/{_settings.ResourceGroupName}" +
                      $"/providers/Microsoft.Automation/automationAccounts/{_settings.AutomationAccountName}" +
                      $"/jobs/{jobId}?api-version={_settings.ApiVersion}";

            var body = new
            {
                properties = new
                {
                    runbook = new { name = _settings.RunbookName },
                    parameters = new Dictionary<string, string>
                    {
                        ["DaysThreshold"] = (request.DaysThreshold <= 0 ? _settings.DefaultDaysThreshold : request.DaysThreshold).ToString(),
                        ["EnableGdapAutoCreation"] = "true",
                        ["TestMode"] = "false",
                        ["EnableSingleCustomerTest"] = "true",
                        ["TestPartnerTenantName"] = request.PartnerTenant,
                        ["TestCustomerTenantId"] = request.CustomerTenantId,
                        ["SearchCustomersWithoutValidGdap"] = "true"
                    }
                }
            };

            var client = _httpClientFactory.CreateClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Put, url);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error ejecutando Runbook GDAP. Status={Status}. Body={Body}", response.StatusCode, responseContent);
                return new GdapAdminLinksAutomationResult
                {
                    Success = false,
                    JobId = jobId,
                    Status = response.StatusCode.ToString(),
                    ErrorMessage = responseContent,
                    Message = "No fue posible iniciar la Automation GDAP."
                };
            }

            return new GdapAdminLinksAutomationResult
            {
                Success = true,
                JobId = jobId,
                Status = "Started",
                Message = $"Automation iniciada correctamente. JobId: {jobId}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error iniciando Automation GDAP para {CustomerTenantId}", request.CustomerTenantId);
            return new GdapAdminLinksAutomationResult
            {
                Success = false,
                Status = "Failed",
                ErrorMessage = ex.Message,
                Message = "No fue posible iniciar la Automation GDAP."
            };
        }
    }


    public async Task<GdapAdminLinksAutomationResult> StartCustomerSyncForCustomerAsync(GdapAdminLinksAutomationRequest request)
    {
        try
        {
            ValidateSettings();

            var runbookName = "ITQS-SOC-GDAP-CUSTOMER-SYNC";
            var jobId = Guid.NewGuid().ToString();

            var tokenCredential = new DefaultAzureCredential();
            var token = await tokenCredential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://management.azure.com/.default" }));

            var url = $"https://management.azure.com/subscriptions/{_settings.SubscriptionId}" +
                      $"/resourceGroups/{_settings.ResourceGroupName}" +
                      $"/providers/Microsoft.Automation/automationAccounts/{_settings.AutomationAccountName}" +
                      $"/jobs/{jobId}?api-version={_settings.ApiVersion}";

            var body = new
            {
                properties = new
                {
                    runbook = new { name = runbookName },
                    parameters = new Dictionary<string, string>
                    {
                        ["EnableSingleCustomerTest"] = "true",
                        ["TestPartnerTenantName"] = request.PartnerTenant,
                        ["TestCustomerTenantId"] = request.CustomerTenantId
                    }
                }
            };

            var client = _httpClientFactory.CreateClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Put, url);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new GdapAdminLinksAutomationResult
                {
                    Success = false,
                    JobId = jobId,
                    Status = response.StatusCode.ToString(),
                    ErrorMessage = responseContent,
                    Message = "No fue posible iniciar la sincronización del cliente."
                };
            }

            return new GdapAdminLinksAutomationResult
            {
                Success = true,
                JobId = jobId,
                Status = "Started",
                Message = $"Sincronización iniciada correctamente. JobId: {jobId}"
            };
        }
        catch (Exception ex)
        {
            return new GdapAdminLinksAutomationResult
            {
                Success = false,
                Status = "Failed",
                ErrorMessage = ex.Message,
                Message = "No fue posible iniciar la sincronización del cliente."
            };
        }
    }
    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.SubscriptionId))
            throw new InvalidOperationException("Falta configurar GdapAutomation:SubscriptionId.");

        if (string.IsNullOrWhiteSpace(_settings.ResourceGroupName))
            throw new InvalidOperationException("Falta configurar GdapAutomation:ResourceGroupName.");

        if (string.IsNullOrWhiteSpace(_settings.AutomationAccountName))
            throw new InvalidOperationException("Falta configurar GdapAutomation:AutomationAccountName.");

        if (string.IsNullOrWhiteSpace(_settings.RunbookName))
            throw new InvalidOperationException("Falta configurar GdapAutomation:RunbookName.");
    }
}


using System.Net.Http.Headers;
using System.Net.Http.Json;
using Azure.Core;
using Azure.Identity;
using ITQS.SupportOperationsCenter.Models.Administration.Customers;
using ITQS.SupportOperationsCenter.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class CustomerConnectionRunbookService : ICustomerConnectionRunbookService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AutomationRunbookSettings _settings;
    private readonly ILogger<CustomerConnectionRunbookService> _logger;

    public CustomerConnectionRunbookService(
        IHttpClientFactory httpClientFactory,
        IOptions<AutomationRunbookSettings> options,
        ILogger<CustomerConnectionRunbookService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<CustomerConnectionRunbookResult> StartValidationAsync(
        CustomerConnectionRunbookRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.TenantId == Guid.Empty)
                throw new InvalidOperationException("TenantId inválido.");

            var jobId = Guid.NewGuid().ToString();

            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                cancellationToken);

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

            var url =
                $"https://management.azure.com/subscriptions/{_settings.SubscriptionId}" +
                $"/resourceGroups/{_settings.ResourceGroupName}" +
                $"/providers/Microsoft.Automation/automationAccounts/{_settings.AutomationAccountName}" +
                $"/jobs/{jobId}?api-version={_settings.ApiVersion}";

            var body = new
            {
                properties = new
                {
                    runbook = new { name = _settings.ValidateConnectionsRunbookName },
                    parameters = new Dictionary<string, string>
                    {
                        ["TenantId"] = request.TenantId.ToString(),
                        ["CustomerName"] = request.CustomerName ?? string.Empty,
                        ["ClientId"] = request.ClientId ?? string.Empty,
                        ["SecretName"] = request.SecretName ?? string.Empty,
                        ["RequestedBy"] = request.RequestedBy ?? string.Empty,
                        ["TestMode"] = "true",
                        ["RunPALAdmin"] = "true",
                        ["RunMFA"] = "true",
                        ["RunAppReg"] = "true",
                        ["FailOnAnyError"] = "false",
                        ["PartnerIdExpected"] = "2191153"
                    }
                }
            };

            var response = await client.PutAsJsonAsync(url, body, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new CustomerConnectionRunbookResult
                {
                    Started = false,
                    Status = "Error",
                    JobId = jobId,
                    RunbookName = _settings.ValidateConnectionsRunbookName,
                    Message = "No fue posible iniciar el runbook.",
                    ErrorMessage = $"HTTP {(int)response.StatusCode}: {responseBody}"
                };
            }

            return new CustomerConnectionRunbookResult
            {
                Started = true,
                Status = "Started",
                JobId = jobId,
                RunbookName = _settings.ValidateConnectionsRunbookName,
                Message = $"Runbook iniciado correctamente. JobId: {jobId}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error iniciando runbook de validación para TenantId {TenantId}", request.TenantId);

            return new CustomerConnectionRunbookResult
            {
                Started = false,
                Status = "Error",
                RunbookName = _settings.ValidateConnectionsRunbookName,
                Message = "No fue posible iniciar el runbook.",
                ErrorMessage = ex.Message
            };
        }
    }
}

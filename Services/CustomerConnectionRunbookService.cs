using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

            var client = await CreateAuthorizedClientAsync(cancellationToken);

            var url = BuildJobUrl(jobId);

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

    public async Task<CustomerConnectionJobStatusResult> GetJobStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jobId))
                throw new InvalidOperationException("JobId inválido.");

            var client = await CreateAuthorizedClientAsync(cancellationToken);
            var response = await client.GetAsync(BuildJobUrl(jobId), cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new CustomerConnectionJobStatusResult
                {
                    Found = false,
                    JobId = jobId,
                    RunbookName = _settings.ValidateConnectionsRunbookName,
                    Status = "Error",
                    ErrorMessage = $"HTTP {(int)response.StatusCode}: {responseBody}",
                    Message = "No fue posible consultar el estado del job."
                };
            }

            using var json = JsonDocument.Parse(responseBody);
            var root = json.RootElement;
            var properties = root.GetProperty("properties");

            var result = new CustomerConnectionJobStatusResult
            {
                Found = true,
                JobId = jobId,
                RunbookName = _settings.ValidateConnectionsRunbookName,
                Status = GetString(properties, "status"),
                StatusDetails = GetString(properties, "statusDetails"),
                CreationTime = GetDate(properties, "creationTime"),
                StartTime = GetDate(properties, "startTime"),
                EndTime = GetDate(properties, "endTime"),
                LastModifiedTime = GetDate(properties, "lastModifiedTime")
            };

            result.Message = result.IsFinal
                ? $"Estado final del job: {result.Status}"
                : $"Estado actual del job: {result.Status}";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando estado del job {JobId}", jobId);

            return new CustomerConnectionJobStatusResult
            {
                Found = false,
                JobId = jobId,
                RunbookName = _settings.ValidateConnectionsRunbookName,
                Status = "Error",
                ErrorMessage = ex.Message,
                Message = "Error consultando estado del job."
            };
        }
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(CancellationToken cancellationToken)
    {
        var credential = new DefaultAzureCredential();

        var token = await credential.GetTokenAsync(
            new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
            cancellationToken);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        return client;
    }

    private string BuildJobUrl(string jobId)
    {
        return
            $"https://management.azure.com/subscriptions/{_settings.SubscriptionId}" +
            $"/resourceGroups/{_settings.ResourceGroupName}" +
            $"/providers/Microsoft.Automation/automationAccounts/{_settings.AutomationAccountName}" +
            $"/jobs/{jobId}?api-version={_settings.ApiVersion}";
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.ToString()
            : string.Empty;
    }

    private static DateTimeOffset? GetDate(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;

        return DateTimeOffset.TryParse(value.ToString(), out var parsed)
            ? parsed
            : null;
    }
}

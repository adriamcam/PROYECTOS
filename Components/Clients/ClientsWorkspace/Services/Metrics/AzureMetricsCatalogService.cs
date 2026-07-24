using Dapper;
using ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Metrics;
using ITQS.SupportOperationsCenter.Data;

namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Services.Metrics;

public sealed class AzureMetricsCatalogService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AzureMetricsCatalogService(
        ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<AzureMetricsModule>>
        GetAvailableModulesAsync(
            string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return [];
        }

        const string sql = """
            SELECT
                CustomerName,
                TenantId,
                SubscriptionId,
                SubscriptionName,
                ModuleKey,
                ResourceType,
                DisplayName,
                ResourceCount,
                IsEnabled,
                DisplayOrder
            FROM dbo.vw_ClientMetricsCatalog
            WHERE TRY_CONVERT(
                    uniqueidentifier,
                    SubscriptionId
                  ) = TRY_CONVERT(
                    uniqueidentifier,
                    @SubscriptionId
                  )
            ORDER BY
                DisplayOrder,
                DisplayName;
            """;

        using var connection =
            _connectionFactory.CreateConnection();

        connection.Open();

        var modules =
            await connection.QueryAsync<AzureMetricsModule>(
                sql,
                new
                {
                    SubscriptionId = subscriptionId.Trim()
                });

        return modules.ToList();
    }
}

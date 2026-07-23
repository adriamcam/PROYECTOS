using Dapper;
using ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Infrastructure;
using ITQS.SupportOperationsCenter.Data;

namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Services.Infrastructure;

public sealed class VirtualMachineService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public VirtualMachineService(
        ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<VirtualMachineModel>> GetVirtualMachinesAsync(
        string customerName,
        string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return [];
        }

        const string sql = """
            SELECT *
            FROM dbo.vw_VMInfrastructure
            WHERE SubscriptionId = @SubscriptionId
            ORDER BY VMName;
            """;

        using var connection =
            _connectionFactory.CreateConnection();

        connection.Open();

        var result =
            await connection.QueryAsync<VirtualMachineModel>(
                sql,
                new
                {
                    SubscriptionId = subscriptionId.Trim()
                });

        return result.ToList();
    }
}

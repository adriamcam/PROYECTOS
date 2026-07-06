using System.Data;
using Dapper;
using ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Models;
using ITQS.SupportOperationsCenter.Data;

namespace ITQS.SupportOperationsCenter.Components.Reporting.ReservedInstances.Services;

public sealed class ReservedInstanceService : IReservedInstanceService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ReservedInstanceService(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ReservedInstanceDashboard> GetDashboardAsync()
    {
        using var conn = _connectionFactory.CreateConnection();

        var kpis = await conn.QueryFirstOrDefaultAsync<ReservedInstanceKpi>(
            "dbo.sp_RI_GetKPIs",
            commandType: CommandType.StoredProcedure,
            commandTimeout: 120
        ) ?? new ReservedInstanceKpi();

        var distribution = (await conn.QueryAsync<ReservedInstanceDistribution>(
            "dbo.sp_RI_GetDistribution",
            commandType: CommandType.StoredProcedure,
            commandTimeout: 120
        )).ToList();

        var topCustomers = (await conn.QueryAsync<ReservedInstanceTopCustomer>(
            "dbo.sp_RI_GetTopCustomers",
            commandType: CommandType.StoredProcedure,
            commandTimeout: 120
        )).ToList();

        var resources = (await conn.QueryAsync<ReservedInstanceResource>(
            "dbo.sp_RI_GetResourcesOperational",
            commandType: CommandType.StoredProcedure,
            commandTimeout: 120
        )).ToList();

        return new ReservedInstanceDashboard
        {
            Kpis = kpis,
            Distribution = distribution,
            TopCustomers = topCustomers,
            Resources = resources
        };
    }

    public async Task<List<ReservedInstanceResource>> GetResourcesAsync()
    {
        using var conn = _connectionFactory.CreateConnection();

        var result = await conn.QueryAsync<ReservedInstanceResource>(
            "dbo.sp_RI_GetResourcesOperational",
            commandType: CommandType.StoredProcedure,
            commandTimeout: 120
        );

        return result.ToList();
    }
    public async Task<List<ReservedInstanceChangeHistory>> GetChangeHistoryAsync(string resourceKey)
    {
        using var conn = _connectionFactory.CreateConnection();

        var result = await conn.QueryAsync<ReservedInstanceChangeHistory>(
            "dbo.sp_RI_GetChangeHistory",
            new { ResourceKey = resourceKey },
            commandType: CommandType.StoredProcedure,
            commandTimeout: 120
        );

        return result.ToList();
    }
}


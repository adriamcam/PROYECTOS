using System.Data;
using Dapper;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Components.Reporting.HybridBenefit.Models;

namespace ITQS.SupportOperationsCenter.Repositories;

public sealed class HybridBenefitRepository : IHybridBenefitRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public HybridBenefitRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HybridBenefitDashboard> GetDashboardAsync()
    {
        return new HybridBenefitDashboard
        {
            Kpis = await GetKpisAsync(),
            TopCustomers = await GetTopCustomersAsync(),
            Distribution = await GetDistributionAsync(),
            Resources = await GetResourcesAsync(),
            Changes = await GetChangesAsync()
        };
    }

    public async Task<HybridBenefitKpi> GetKpisAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();

        var result = await conn.QueryFirstOrDefaultAsync<HybridBenefitKpi>(
            "dbo.sp_HB_GetKPIs",
            commandType: CommandType.StoredProcedure);

        return result ?? new HybridBenefitKpi();
    }

    public async Task<List<HybridBenefitTopCustomer>> GetTopCustomersAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();

        var result = await conn.QueryAsync<HybridBenefitTopCustomer>(
            "dbo.sp_HB_GetTopCustomers",
            commandType: CommandType.StoredProcedure);

        return result.ToList();
    }

    public async Task<List<HybridBenefitDistribution>> GetDistributionAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();

        var result = await conn.QueryAsync<HybridBenefitDistribution>(
            "dbo.sp_HB_GetDistribution",
            commandType: CommandType.StoredProcedure);

        return result.ToList();
    }

    public async Task<List<HybridBenefitResource>> GetResourcesAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();

        var result = await conn.QueryAsync<HybridBenefitResource>(
            "dbo.sp_HB_GetResourcesOperational",
            commandType: CommandType.StoredProcedure);

        var resources = result.ToList();

        foreach (var item in resources)
        {
            if (item.RequiresAction)
            {
                item.HealthStatus = "Action Required";
            }
            else if (item.ChangeType == "New Resource")
            {
                item.HealthStatus = "New";
            }
            else
            {
                item.HealthStatus = "Healthy";
            }
        }

        return resources;
    }

    public async Task<List<HybridBenefitResourceHistory>> GetResourceHistoryAsync(string resourceKey)
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();

        var result = await conn.QueryAsync<HybridBenefitResourceHistory>(
            "dbo.sp_HB_GetResourceHistory",
            new { ResourceKey = resourceKey },
            commandType: CommandType.StoredProcedure);

        return result.ToList();
    }

    public async Task<List<HybridBenefitChange>> GetChangesAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();

        var result = await conn.QueryAsync<HybridBenefitChange>(
            "dbo.sp_HB_GetChanges",
            commandType: CommandType.StoredProcedure);

        return result.ToList();
    }
}

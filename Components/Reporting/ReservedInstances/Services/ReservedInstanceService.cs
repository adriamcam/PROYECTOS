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
            "dbo.sp_RI_GetKPIs", commandType: CommandType.StoredProcedure, commandTimeout: 120
        ) ?? new ReservedInstanceKpi();

        var distribution = (await conn.QueryAsync<ReservedInstanceDistribution>(
            "dbo.sp_RI_GetDistribution", commandType: CommandType.StoredProcedure, commandTimeout: 120
        )).ToList();

        var topCustomers = (await conn.QueryAsync<ReservedInstanceTopCustomer>(
            "dbo.sp_RI_GetTopCustomers", commandType: CommandType.StoredProcedure, commandTimeout: 120
        )).ToList();

        var resources = (await conn.QueryAsync<ReservedInstanceResource>(
            "dbo.sp_RI_GetResourcesOperational", commandType: CommandType.StoredProcedure, commandTimeout: 120
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
            "dbo.sp_RI_GetResourcesOperational", commandType: CommandType.StoredProcedure, commandTimeout: 120
        );
        return result.ToList();
    }

    public async Task SaveManualAnalysisNoteAsync(string resourceKey, string? note, string updatedBy)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            "dbo.sp_RI_SaveManualAnalysisNote",
            new { ResourceKey = resourceKey, ManualAnalysisNote = note, UpdatedBy = updatedBy },
            commandType: CommandType.StoredProcedure,
            commandTimeout: 120
        );
    }


    public async Task SaveVMCoverageNoteAsync(
        string resourceKey,
        string? note,
        string updatedBy)
    {
        using var conn = _connectionFactory.CreateConnection();

        await conn.ExecuteAsync(
            "dbo.sp_RI_SaveVMCoverageNote",
            new
            {
                ResourceKey = resourceKey,
                ManualAnalysisNote = note,
                UpdatedBy = updatedBy
            },
            commandType: CommandType.StoredProcedure,
            commandTimeout: 120
        );
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

    public async Task<RICoverageKpi> GetCoverageKpisAsync()
    {
        using var conn = _connectionFactory.CreateConnection();

        const string sql = @"
SELECT TOP 1
    TotalVMs,
    CoveredAligned,
    CoveredCompatible,
    ProbablyCoveredNoTag,
    TaggedSkuChanged,
    AmbiguousCoverage,
    InsufficientCoverage,
    TagIRVigenteSinReserva,
    TagIRVencida,
    TagIREliminada,
    TagIRNoInterpretable,
    Uncovered,
    AverageCoverageScore
FROM dbo.vw_RI_VMCoverageKpi;";

        return await conn.QueryFirstOrDefaultAsync<RICoverageKpi>(
            sql, commandType: CommandType.Text, commandTimeout: 120
        ) ?? new RICoverageKpi();
    }

    public async Task<List<RICoverageResource>> GetCoverageResourcesAsync()
    {
        using var conn = _connectionFactory.CreateConnection();

        const string sql = @"
SELECT
    Id,
    CustomerName,
    CAST(TenantId AS nvarchar(100)) AS TenantId,
    SubscriptionName,
    SubscriptionId,
    ResourceGroupName,
    VMName,
    ResourceId,
    Location,
    AvailabilityZone,
    PowerState,
    VMSize,
    VMFamily,
    VCPUs,
    MemoryGB,
    OSType,
    IRTag,
    HasIRTag,
    IRTagExpirationDate,
    IntendedReservationId,
    IntendedReservationName,
    IntendedReservedSku,
    CompatibleReservationCount,
    CompatibleReservationQuantity,
    EligibleVMCount,
    RunningVMCount,
    ReservationCount,
    ReservedQuantity,
    CoverageGap,
    CoveragePercent,
    CoverageStatusCode,
    CoverageStatus,
    CoverageConfidence,
    CoverageScore,
    CoverageReason,
    CoverageRecommendation,
    CompatibleReservationNames,
    Tags,
    LastSeenAt
FROM dbo.vw_RI_VMCoverageRelevant
ORDER BY CoverageScore ASC, CustomerName, SubscriptionName, VMName;";

        var result = await conn.QueryAsync<RICoverageResource>(
            sql, commandType: CommandType.Text, commandTimeout: 120
        );

        return result.ToList();
    }
}



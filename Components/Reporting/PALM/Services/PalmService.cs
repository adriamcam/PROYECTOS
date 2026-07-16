using Dapper;
using Microsoft.Data.SqlClient;
using ITQS.SupportOperationsCenter.Components.Reporting.PALM.Models;

namespace ITQS.SupportOperationsCenter.Components.Reporting.PALM.Services;

public sealed class PalmService : IPalmService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PalmService> _logger;

    public PalmService(
        IConfiguration configuration,
        ILogger<PalmService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private SqlConnection CreateConnection()
    {
        var connectionString =
            _configuration.GetConnectionString("ReportesDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No se encontró la cadena de conexión " +
                "'ConnectionStrings:ReportesDb'.");
        }

        return new SqlConnection(connectionString);
    }

    public async Task<PalmReportData> GetReportAsync()
    {
        const string sql = """
        SELECT
            TotalCustomers,
            TotalOK,
            TotalNOK,
            TotalRefreshed,
            PartnerIdCorrect,
            WithoutPartnerId,
            DifferentPartnerId,
            RequiresAction,
            TotalVisibleSubscriptions,
            CompliancePercent,
            FirstScanDate,
            LastScanDate,
            LastUpdatedAt
        FROM dbo.vw_PALM_Dashboard;

        SELECT
            RunId,
            StartedAt,
            FinishedAt,
            DurationSeconds,
            TotalCustomers,
            TotalSubscriptions,
            TotalOK,
            TotalNOK,
            SuccessPercent,
            Status,
            Detail,
            CreatedAt
        FROM dbo.vw_PALM_LatestRun;

        SELECT
            Id,
            RunId,
            CustomerName,
            TenantId,
            SubscriptionName,
            SubscriptionId,
            Status,
            Action,
            CONVERT(varchar(50), PartnerIdBefore) AS PartnerIdBefore,
            CONVERT(varchar(50), PartnerIdCurrent) AS PartnerIdCurrent,
            CONVERT(varchar(50), PartnerIdTarget) AS PartnerIdTarget,
            PartnerValidationStatus,
            ISNULL(VisibleSubscriptions, 0) AS VisibleSubscriptions,
            CONVERT(bit, RequiresAction) AS RequiresAction,
            ClientId,
            ErrorMessage,
            Detail,
            ScanDate,
            UpdatedAt
        FROM dbo.vw_PALM_Results
        ORDER BY
            RequiresAction DESC,
            CustomerName;

        SELECT
            Id,
            RunId,
            CustomerName,
            TenantId,
            SubscriptionName,
            SubscriptionId,
            Status,
            Action,
            CONVERT(varchar(50), PartnerIdBefore) AS PartnerIdBefore,
            CONVERT(varchar(50), PartnerIdCurrent) AS PartnerIdCurrent,
            CONVERT(varchar(50), PartnerIdTarget) AS PartnerIdTarget,
            PartnerValidationStatus,
            ISNULL(VisibleSubscriptions, 0) AS VisibleSubscriptions,
            CONVERT(bit, RequiresAction) AS RequiresAction,
            ClientId,
            ErrorMessage,
            Detail,
            ScanDate,
            UpdatedAt
        FROM dbo.vw_PALM_RequiresAction
        ORDER BY CustomerName;

        SELECT
            RunId,
            StartedAt,
            FinishedAt,
            DurationSeconds,
            TotalCustomers,
            TotalSubscriptions,
            TotalOK,
            TotalNOK,
            SuccessPercent,
            Status,
            Detail,
            CreatedAt
        FROM dbo.vw_PALM_RunHistory
        ORDER BY StartedAt DESC;
        """;

        try
        {
            await using var connection = CreateConnection();

            await connection.OpenAsync();

            using var multi = await connection.QueryMultipleAsync(
                sql,
                commandTimeout: 120);

            var dashboard =
                await multi.ReadFirstOrDefaultAsync<PalmDashboard>()
                ?? new PalmDashboard();

            var latestRun =
                await multi.ReadFirstOrDefaultAsync<PalmRun>();

            var results =
                (await multi.ReadAsync<PalmResource>())
                .ToList();

            var requiresAction =
                (await multi.ReadAsync<PalmResource>())
                .ToList();

            var runHistory =
                (await multi.ReadAsync<PalmRun>())
                .ToList();

            return new PalmReportData
            {
                Dashboard = dashboard,
                LatestRun = latestRun,
                Results = results,
                RequiresAction = requiresAction,
                RunHistory = runHistory
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cargando el Reporte PALM.");

            throw;
        }
    }

    public async Task<PalmDashboard> GetDashboardAsync()
    {
        const string sql = """
        SELECT
            TotalCustomers,
            TotalOK,
            TotalNOK,
            TotalRefreshed,
            PartnerIdCorrect,
            WithoutPartnerId,
            DifferentPartnerId,
            RequiresAction,
            TotalVisibleSubscriptions,
            CompliancePercent,
            FirstScanDate,
            LastScanDate,
            LastUpdatedAt
        FROM dbo.vw_PALM_Dashboard;
        """;

        await using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<PalmDashboard>(
            sql,
            commandTimeout: 120)
            ?? new PalmDashboard();
    }

    public async Task<IReadOnlyList<PalmResource>> GetResultsAsync()
    {
        const string sql = """
        SELECT
            Id,
            RunId,
            CustomerName,
            TenantId,
            SubscriptionName,
            SubscriptionId,
            Status,
            Action,
            CONVERT(varchar(50), PartnerIdBefore) AS PartnerIdBefore,
            CONVERT(varchar(50), PartnerIdCurrent) AS PartnerIdCurrent,
            CONVERT(varchar(50), PartnerIdTarget) AS PartnerIdTarget,
            PartnerValidationStatus,
            ISNULL(VisibleSubscriptions, 0) AS VisibleSubscriptions,
            CONVERT(bit, RequiresAction) AS RequiresAction,
            ClientId,
            ErrorMessage,
            Detail,
            ScanDate,
            UpdatedAt
        FROM dbo.vw_PALM_Results
        ORDER BY
            RequiresAction DESC,
            CustomerName;
        """;

        await using var connection = CreateConnection();

        var rows = await connection.QueryAsync<PalmResource>(
            sql,
            commandTimeout: 120);

        return rows.ToList();
    }

    public async Task<IReadOnlyList<PalmResource>>
        GetRequiresActionAsync()
    {
        const string sql = """
        SELECT
            Id,
            RunId,
            CustomerName,
            TenantId,
            SubscriptionName,
            SubscriptionId,
            Status,
            Action,
            CONVERT(varchar(50), PartnerIdBefore) AS PartnerIdBefore,
            CONVERT(varchar(50), PartnerIdCurrent) AS PartnerIdCurrent,
            CONVERT(varchar(50), PartnerIdTarget) AS PartnerIdTarget,
            PartnerValidationStatus,
            ISNULL(VisibleSubscriptions, 0) AS VisibleSubscriptions,
            CONVERT(bit, RequiresAction) AS RequiresAction,
            ClientId,
            ErrorMessage,
            Detail,
            ScanDate,
            UpdatedAt
        FROM dbo.vw_PALM_RequiresAction
        ORDER BY CustomerName;
        """;

        await using var connection = CreateConnection();

        var rows = await connection.QueryAsync<PalmResource>(
            sql,
            commandTimeout: 120);

        return rows.ToList();
    }

    public async Task<PalmRun?> GetLatestRunAsync()
    {
        const string sql = """
        SELECT
            RunId,
            StartedAt,
            FinishedAt,
            DurationSeconds,
            TotalCustomers,
            TotalSubscriptions,
            TotalOK,
            TotalNOK,
            SuccessPercent,
            Status,
            Detail,
            CreatedAt
        FROM dbo.vw_PALM_LatestRun;
        """;

        await using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<PalmRun>(
            sql,
            commandTimeout: 120);
    }

    public async Task<IReadOnlyList<PalmRun>> GetRunHistoryAsync()
    {
        const string sql = """
        SELECT
            RunId,
            StartedAt,
            FinishedAt,
            DurationSeconds,
            TotalCustomers,
            TotalSubscriptions,
            TotalOK,
            TotalNOK,
            SuccessPercent,
            Status,
            Detail,
            CreatedAt
        FROM dbo.vw_PALM_RunHistory
        ORDER BY StartedAt DESC;
        """;

        await using var connection = CreateConnection();

        var rows = await connection.QueryAsync<PalmRun>(
            sql,
            commandTimeout: 120);

        return rows.ToList();
    }
}

using Dapper;
using ITQS.SupportOperationsCenter.Components.Reporting.PALM.Models;
using ITQS.SupportOperationsCenter.Data;

namespace ITQS.SupportOperationsCenter.Components.Reporting.PALM.Services;

public sealed class PalmService : IPalmService
{
    private static readonly HashSet<Guid> InternalTenantIds =
    [
        Guid.Parse("dae6c85d-d6cf-4c0c-88c0-c9ebe847f4de"),
        Guid.Parse("cea8e8e7-5cdf-446c-8a5a-b97daea0d9f0"),
        Guid.Parse("fd548a39-0203-4765-bb62-bd2727d07f04")
    ];

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<PalmService> _logger;

    public PalmService(
        ISqlConnectionFactory connectionFactory,
        ILogger<PalmService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
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
            CAST(
                CASE
                    WHEN ISNULL(TotalOK, 0) + ISNULL(TotalNOK, 0) = 0
                        THEN 0
                    ELSE
                        ISNULL(TotalOK, 0) * 100.0
                        / (ISNULL(TotalOK, 0) + ISNULL(TotalNOK, 0))
                END
                AS decimal(10,2)
            ) AS SuccessPercent,
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
            CAST(
                CASE
                    WHEN ISNULL(TotalOK, 0) + ISNULL(TotalNOK, 0) = 0
                        THEN 0
                    ELSE
                        ISNULL(TotalOK, 0) * 100.0
                        / (ISNULL(TotalOK, 0) + ISNULL(TotalNOK, 0))
                END
                AS decimal(10,2)
            ) AS SuccessPercent,
            Status,
            Detail,
            CreatedAt
        FROM dbo.vw_PALM_RunHistory
        ORDER BY StartedAt DESC;
        """;

        try
        {
            using var connection = _connectionFactory.CreateConnection();

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

            results = results
                .Where(resource => !IsInternalTenant(resource))
                .ToList();

            requiresAction = requiresAction
                .Where(resource => !IsInternalTenant(resource))
                .ToList();

            RecalculateDashboard(
                dashboard,
                results,
                requiresAction);

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

        using var connection = _connectionFactory.CreateConnection();

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

        using var connection = _connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<PalmResource>(
            sql,
            commandTimeout: 120);

        return rows.ToList();
    }

    public async Task<IReadOnlyList<PalmResource>> GetRequiresActionAsync()
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

        using var connection = _connectionFactory.CreateConnection();

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
            CAST(
                CASE
                    WHEN ISNULL(TotalOK, 0) + ISNULL(TotalNOK, 0) = 0
                        THEN 0
                    ELSE
                        ISNULL(TotalOK, 0) * 100.0
                        / (ISNULL(TotalOK, 0) + ISNULL(TotalNOK, 0))
                END
                AS decimal(10,2)
            ) AS SuccessPercent,
            Status,
            Detail,
            CreatedAt
        FROM dbo.vw_PALM_LatestRun;
        """;

        using var connection = _connectionFactory.CreateConnection();

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
            CAST(
                CASE
                    WHEN ISNULL(TotalOK, 0) + ISNULL(TotalNOK, 0) = 0
                        THEN 0
                    ELSE
                        ISNULL(TotalOK, 0) * 100.0
                        / (ISNULL(TotalOK, 0) + ISNULL(TotalNOK, 0))
                END
                AS decimal(10,2)
            ) AS SuccessPercent,
            Status,
            Detail,
            CreatedAt
        FROM dbo.vw_PALM_RunHistory
        ORDER BY StartedAt DESC;
        """;

        using var connection = _connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<PalmRun>(
            sql,
            commandTimeout: 120);

        return rows.ToList();
    }

    private static bool IsInternalTenant(PalmResource resource)
    {
        return resource.TenantId != Guid.Empty &&
               InternalTenantIds.Contains(resource.TenantId);
    }

    private static void RecalculateDashboard(
        PalmDashboard dashboard,
        IReadOnlyCollection<PalmResource> results,
        IReadOnlyCollection<PalmResource> requiresAction)
    {
        dashboard.TotalCustomers = results
            .Select(resource => resource.TenantId)
            .Where(tenantId => tenantId != Guid.Empty)
            .Distinct()
            .LongCount();

        dashboard.TotalOK = results.Count(resource =>
            string.Equals(
                resource.Status,
                "OK",
                StringComparison.OrdinalIgnoreCase));

        dashboard.TotalNOK = results.Count(resource =>
            string.Equals(
                resource.Status,
                "NOK",
                StringComparison.OrdinalIgnoreCase));

        dashboard.TotalRefreshed = results.Count(resource =>
            string.Equals(
                resource.Action,
                "Refreshed",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                resource.Action,
                "Updated",
                StringComparison.OrdinalIgnoreCase));

        dashboard.PartnerIdCorrect = results.Count(resource =>
            IsValidation(
                resource.PartnerValidationStatus,
                "PARTNER_ID_CORRECTO",
                "CORRECT",
                "OK",
                "VALID"));

        dashboard.WithoutPartnerId = results.Count(resource =>
            IsValidation(
                resource.PartnerValidationStatus,
                "SIN_PARTNER_ID",
                "MISSING",
                "WITHOUTPARTNERID"));

        dashboard.DifferentPartnerId = results.Count(resource =>
            IsValidation(
                resource.PartnerValidationStatus,
                "PARTNER_ID_DIFERENTE",
                "DIFFERENT",
                "MISMATCH"));

        dashboard.RequiresAction = requiresAction.Count;

        dashboard.TotalVisibleSubscriptions = results.Sum(resource =>
            Math.Max(resource.VisibleSubscriptions, 0));

        var evaluated =
            dashboard.PartnerIdCorrect +
            dashboard.WithoutPartnerId +
            dashboard.DifferentPartnerId;

        dashboard.CompliancePercent = evaluated == 0
            ? 0m
            : Math.Round(
                dashboard.PartnerIdCorrect * 100m / evaluated,
                2);
    }

    private static bool IsValidation(
        string? value,
        params string[] expectedValues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return expectedValues.Any(expected =>
            string.Equals(
                value.Trim(),
                expected,
                StringComparison.OrdinalIgnoreCase));
    }
}


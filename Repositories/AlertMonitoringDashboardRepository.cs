using Dapper;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Models.Dashboard;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;

namespace ITQS.SupportOperationsCenter.Repositories;

public sealed class AlertMonitoringDashboardRepository : IAlertMonitoringDashboardRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<AlertMonitoringDashboardRepository> _logger;

    public AlertMonitoringDashboardRepository(
        ISqlConnectionFactory connectionFactory,
        ILogger<AlertMonitoringDashboardRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<AlertMonitoringDashboardModel> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
SET NOCOUNT ON;

IF OBJECT_ID('tempdb..#ActiveAlerts') IS NOT NULL DROP TABLE #ActiveAlerts;
IF OBJECT_ID('tempdb..#ClosedAlerts') IS NOT NULL DROP TABLE #ClosedAlerts;
IF OBJECT_ID('tempdb..#LastSevenDays') IS NOT NULL DROP TABLE #LastSevenDays;

CREATE TABLE #ActiveAlerts
(
    SourceType nvarchar(50) NOT NULL,
    ClientName nvarchar(500) NOT NULL,
    AlertName nvarchar(1000) NOT NULL,
    Severity nvarchar(100) NOT NULL,
    ResourceName nvarchar(1000) NOT NULL,
    AssignedEmail nvarchar(320) NULL,
    EventDate date NULL,
    LastEventAt datetime2 NULL
);

INSERT INTO #ActiveAlerts
(
    SourceType,
    ClientName,
    AlertName,
    Severity,
    ResourceName,
    AssignedEmail,
    EventDate,
    LastEventAt
)
SELECT
    SourceType = 'Management',
    ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
    AlertName = ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
    Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
    ResourceName = ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso'),
    AssignedEmail = ISNULL(AssignedEmail, ''),
    EventDate = CAST(ISNULL(UpdatedAt, InsertedAt) AS date),
    LastEventAt = ISNULL(UpdatedAt, InsertedAt)
FROM dbo.AlertsManagement
WHERE ISNULL(Active, 0) = 1;

INSERT INTO #ActiveAlerts
(
    SourceType,
    ClientName,
    AlertName,
    Severity,
    ResourceName,
    AssignedEmail,
    EventDate,
    LastEventAt
)
SELECT
    SourceType = 'Backup',
    ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
    AlertName = ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
    Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
    ResourceName = COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso'),
    AssignedEmail = ISNULL(AssignedEmail, ''),
    EventDate = CAST(ISNULL(UpdatedAt, InsertedAt) AS date),
    LastEventAt = ISNULL(UpdatedAt, InsertedAt)
FROM dbo.AlertasBackup
WHERE ISNULL(Active, 0) = 1;

CREATE TABLE #ClosedAlerts
(
    ClosedDate date NULL,
    UpdatedAt datetime2 NULL,
    KPIType nvarchar(100) NULL,
    AlertId bigint NULL,
    Comment nvarchar(max) NULL,
    UpdatedBy nvarchar(300) NULL,
    UserEmail nvarchar(320) NULL,
    ClientName nvarchar(500) NULL,
    AlertName nvarchar(1000) NULL,
    ResourceName nvarchar(1000) NULL,
    Severity nvarchar(100) NULL
);

INSERT INTO #ClosedAlerts
(
    ClosedDate,
    UpdatedAt,
    KPIType,
    AlertId,
    Comment,
    UpdatedBy,
    UserEmail,
    ClientName,
    AlertName,
    ResourceName,
    Severity
)
SELECT
    ClosedDate = CAST(H.UpdatedAt AS date),
    H.UpdatedAt,
    H.KPIType,
    AlertId = CAST(ISNULL(H.AlertId, 0) AS bigint),
    H.Comment,
    H.UpdatedBy,
    H.UserEmail,
    ClientName = ISNULL(COALESCE(AM.SubscriptionName, AB.SubscriptionName), ''),
    AlertName = ISNULL(COALESCE(AM.AlertName, AB.AlertRule, H.Alert_norm), ''),
    ResourceName = ISNULL(COALESCE(AM.TargetResourceName, AB.ResourceName, AB.VMName, AB.ProtectedItem, H.Res_norm), ''),
    Severity = ISNULL(COALESCE(AM.Severity, AB.Severity), '')
FROM dbo.AlertUpdatesHistory H
LEFT JOIN dbo.AlertsManagement AM
    ON H.KPIType = 'Management'
   AND H.AlertId = AM.Id
LEFT JOIN dbo.AlertasBackup AB
    ON H.KPIType = 'Backup'
   AND H.AlertId = AB.Id
WHERE LOWER(ISNULL(H.Status, '')) IN ('closed', 'close');

CREATE TABLE #LastSevenDays
(
    TrendDate date NOT NULL
);

DECLARE @StartDate date = DATEADD(day, -6, CAST(GETDATE() AS date));
DECLARE @EndDate date = CAST(GETDATE() AS date);

WHILE @StartDate <= @EndDate
BEGIN
    INSERT INTO #LastSevenDays(TrendDate)
    VALUES (@StartDate);

    SET @StartDate = DATEADD(day, 1, @StartDate);
END;

SELECT
    AffectedClients = COUNT(DISTINCT ClientName),
    AffectedResources = COUNT(DISTINCT ResourceName),
    CriticalAlerts = ISNULL(SUM(CASE WHEN UPPER(Severity) IN ('CRITICAL', 'HIGH', 'SEV0', 'SEV1') THEN 1 ELSE 0 END), 0),
    EngineersWithLoad = COUNT(DISTINCT NULLIF(AssignedEmail, '')),
    ResolvedToday = ISNULL((SELECT COUNT(1) FROM #ClosedAlerts WHERE ClosedDate = CAST(GETDATE() AS date)), 0),
    AverageResolutionHours =
        ISNULL
        (
            (
                SELECT CAST(AVG(CAST(DATEDIFF(minute, COALESCE(AM.InsertedAt, AB.InsertedAt, H.UpdatedAt), H.UpdatedAt) AS decimal(18,2))) / 60.0 AS decimal(18,2))
                FROM dbo.AlertUpdatesHistory H
                LEFT JOIN dbo.AlertsManagement AM
                    ON H.KPIType = 'Management'
                   AND H.AlertId = AM.Id
                LEFT JOIN dbo.AlertasBackup AB
                    ON H.KPIType = 'Backup'
                   AND H.AlertId = AB.Id
                WHERE LOWER(ISNULL(H.Status, '')) IN ('closed', 'close')
                  AND H.UpdatedAt >= DATEADD(day, -7, SYSDATETIME())
                  AND DATEDIFF(minute, COALESCE(AM.InsertedAt, AB.InsertedAt, H.UpdatedAt), H.UpdatedAt) >= 0
            ),
            0
        )
FROM #ActiveAlerts;

SELECT
    SourceType,
    TotalAlerts = COUNT(1),
    Percentage =
        CAST
        (
            CASE
                WHEN SUM(COUNT(1)) OVER() = 0 THEN 0
                ELSE COUNT(1) * 100.0 / SUM(COUNT(1)) OVER()
            END AS decimal(18,2)
        )
FROM #ActiveAlerts
GROUP BY SourceType
ORDER BY TotalAlerts DESC;

SELECT
    SeverityGroup,
    TotalAlerts = COUNT(1),
    Percentage =
        CAST
        (
            CASE
                WHEN SUM(COUNT(1)) OVER() = 0 THEN 0
                ELSE COUNT(1) * 100.0 / SUM(COUNT(1)) OVER()
            END AS decimal(18,2)
        )
FROM
(
    SELECT
        SeverityGroup =
            CASE
                WHEN UPPER(Severity) IN ('CRITICAL', 'SEV0') THEN 'Sev0 / Critical'
                WHEN UPPER(Severity) IN ('HIGH', 'SEV1') THEN 'Sev1 / High'
                WHEN UPPER(Severity) IN ('MEDIUM', 'WARNING', 'SEV2') THEN 'Sev2 / Medium'
                WHEN UPPER(Severity) IN ('LOW', 'SEV3', 'SEV4') THEN 'Sev3+ / Low'
                ELSE 'Informational'
            END
    FROM #ActiveAlerts
) S
GROUP BY SeverityGroup
ORDER BY
    CASE SeverityGroup
        WHEN 'Sev0 / Critical' THEN 1
        WHEN 'Sev1 / High' THEN 2
        WHEN 'Sev2 / Medium' THEN 3
        WHEN 'Sev3+ / Low' THEN 4
        ELSE 5
    END;

SELECT
    D.TrendDate,
    NewAlerts = ISNULL(N.NewAlerts, 0),
    CriticalAlerts = ISNULL(C.CriticalAlerts, 0),
    ClosedAlerts = ISNULL(CL.ClosedAlerts, 0)
FROM #LastSevenDays D
OUTER APPLY
(
    SELECT COUNT(1) AS NewAlerts
    FROM #ActiveAlerts A
    WHERE A.EventDate = D.TrendDate
) N
OUTER APPLY
(
    SELECT COUNT(1) AS CriticalAlerts
    FROM #ActiveAlerts A
    WHERE A.EventDate = D.TrendDate
      AND UPPER(A.Severity) IN ('CRITICAL', 'HIGH', 'SEV0', 'SEV1')
) C
OUTER APPLY
(
    SELECT COUNT(1) AS ClosedAlerts
    FROM #ClosedAlerts X
    WHERE X.ClosedDate = D.TrendDate
) CL
ORDER BY D.TrendDate;

SELECT TOP (10)
    ClientName,
    TotalAlerts = COUNT(1),
    CriticalAlerts = ISNULL(SUM(CASE WHEN UPPER(Severity) IN ('CRITICAL', 'HIGH', 'SEV0', 'SEV1') THEN 1 ELSE 0 END), 0)
FROM #ActiveAlerts
GROUP BY ClientName
ORDER BY TotalAlerts DESC, CriticalAlerts DESC;

SELECT TOP (10)
    AlertName,
    SourceType,
    TotalAlerts = COUNT(1)
FROM #ActiveAlerts
GROUP BY AlertName, SourceType
ORDER BY TotalAlerts DESC;

SELECT TOP (10)
    ActivityDate,
    ActivityType,
    ClientName,
    AlertName,
    ResourceName,
    Severity,
    UpdatedBy
FROM
(
    SELECT TOP (10)
        ActivityDate = LastEventAt,
        ActivityType = 'Alerta activa',
        ClientName,
        AlertName,
        ResourceName,
        Severity,
        UpdatedBy = ''
    FROM #ActiveAlerts
    ORDER BY LastEventAt DESC

    UNION ALL

    SELECT TOP (10)
        ActivityDate = UpdatedAt,
        ActivityType = 'Cierre',
        ClientName,
        AlertName,
        ResourceName,
        Severity,
        UpdatedBy = ISNULL(UpdatedBy, '')
    FROM #ClosedAlerts
    ORDER BY UpdatedAt DESC
) X
ORDER BY ActivityDate DESC;
";

        using var multi = await connection.QueryMultipleAsync(sql);

        var dashboard = new AlertMonitoringDashboardModel
        {
            Kpis = await multi.ReadFirstOrDefaultAsync<AlertMonitoringKpiModel>() ?? new AlertMonitoringKpiModel(),
            SourceDistribution = (await multi.ReadAsync<AlertMonitoringSourceDistributionModel>()).ToList(),
            SeverityDistribution = (await multi.ReadAsync<AlertMonitoringSeverityDistributionModel>()).ToList(),
            Trends = (await multi.ReadAsync<AlertMonitoringTrendModel>()).ToList(),
            TopClients = (await multi.ReadAsync<AlertMonitoringTopClientModel>()).ToList(),
            TopAlerts = (await multi.ReadAsync<AlertMonitoringTopAlertModel>()).ToList(),
            RecentActivity = (await multi.ReadAsync<AlertMonitoringRecentActivityModel>()).ToList()
        };

        return dashboard;
    }
}
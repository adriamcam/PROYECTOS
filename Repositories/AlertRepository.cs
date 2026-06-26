using Dapper;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Models.Dashboard;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using System.Data;

namespace ITQS.SupportOperationsCenter.Repositories;

public sealed class AlertRepository : IAlertRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<AlertRepository> _logger;

    public AlertRepository(
        ISqlConnectionFactory connectionFactory,
        ILogger<AlertRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<AlertDashboardModel> GetDashboardAsync(
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
DECLARE @Today date = CAST(GETDATE() AS date);

;WITH Management AS
(
    SELECT
        COUNT(1) AS ManagementAlerts,
        SUM(CASE WHEN Active = 1 AND AssignedEmail = @UserEmail THEN 1 ELSE 0 END) AS AssignedToMe,
        SUM(CASE WHEN Active = 1 AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '') THEN 1 ELSE 0 END) AS Unassigned,
        SUM(CASE WHEN Active = 0 AND CAST(ISNULL(UpdatedAt, ResolveTime) AS date) = @Today THEN 1 ELSE 0 END) AS ResolvedToday
    FROM dbo.AlertsManagement
    WHERE Active = 1
       OR (Active = 0 AND CAST(ISNULL(UpdatedAt, ResolveTime) AS date) = @Today)
),
BackupAlertsCte AS
(
    SELECT
        COUNT(1) AS BackupAlerts,
        SUM(CASE WHEN Active = 1 AND AssignedEmail = @UserEmail THEN 1 ELSE 0 END) AS AssignedToMe,
        SUM(CASE WHEN Active = 1 AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '') THEN 1 ELSE 0 END) AS Unassigned
    FROM dbo.AlertasBackup
    WHERE Active = 1
)
SELECT
    ISNULL(m.ManagementAlerts, 0) + ISNULL(b.BackupAlerts, 0) AS TotalAlerts,
    ISNULL(b.BackupAlerts, 0) AS BackupAlerts,
    ISNULL(m.ManagementAlerts, 0) AS ManagementAlerts,
    ISNULL(m.AssignedToMe, 0) + ISNULL(b.AssignedToMe, 0) AS AssignedToMe,
    ISNULL(m.Unassigned, 0) + ISNULL(b.Unassigned, 0) AS Unassigned,
    ISNULL(m.ResolvedToday, 0) AS ResolvedToday
FROM Management m
CROSS JOIN BackupAlertsCte b;

;WITH AllAlerts AS
(
    SELECT Severity
    FROM dbo.AlertsManagement
    WHERE Active = 1

    UNION ALL

    SELECT Severity
    FROM dbo.AlertasBackup
    WHERE Active = 1
)
SELECT
    Severity =
        CASE
            WHEN UPPER(ISNULL(Severity, '')) IN ('CRITICAL', 'SEV0') THEN 'Critical / Sev0'
            WHEN UPPER(ISNULL(Severity, '')) IN ('HIGH', 'SEV1') THEN 'High / Sev1'
            WHEN UPPER(ISNULL(Severity, '')) IN ('MEDIUM', 'WARNING', 'SEV2') THEN 'Medium / Sev2'
            ELSE 'Low / Sev3+'
        END,
    Total = COUNT(1)
FROM AllAlerts
GROUP BY
    CASE
        WHEN UPPER(ISNULL(Severity, '')) IN ('CRITICAL', 'SEV0') THEN 'Critical / Sev0'
        WHEN UPPER(ISNULL(Severity, '')) IN ('HIGH', 'SEV1') THEN 'High / Sev1'
        WHEN UPPER(ISNULL(Severity, '')) IN ('MEDIUM', 'WARNING', 'SEV2') THEN 'Medium / Sev2'
        ELSE 'Low / Sev3+'
    END
ORDER BY
    CASE
        WHEN CASE
                WHEN UPPER(ISNULL(Severity, '')) IN ('CRITICAL', 'SEV0') THEN 'Critical / Sev0'
                WHEN UPPER(ISNULL(Severity, '')) IN ('HIGH', 'SEV1') THEN 'High / Sev1'
                WHEN UPPER(ISNULL(Severity, '')) IN ('MEDIUM', 'WARNING', 'SEV2') THEN 'Medium / Sev2'
                ELSE 'Low / Sev3+'
             END = 'Critical / Sev0' THEN 1
        WHEN CASE
                WHEN UPPER(ISNULL(Severity, '')) IN ('CRITICAL', 'SEV0') THEN 'Critical / Sev0'
                WHEN UPPER(ISNULL(Severity, '')) IN ('HIGH', 'SEV1') THEN 'High / Sev1'
                WHEN UPPER(ISNULL(Severity, '')) IN ('MEDIUM', 'WARNING', 'SEV2') THEN 'Medium / Sev2'
                ELSE 'Low / Sev3+'
             END = 'High / Sev1' THEN 2
        WHEN CASE
                WHEN UPPER(ISNULL(Severity, '')) IN ('CRITICAL', 'SEV0') THEN 'Critical / Sev0'
                WHEN UPPER(ISNULL(Severity, '')) IN ('HIGH', 'SEV1') THEN 'High / Sev1'
                WHEN UPPER(ISNULL(Severity, '')) IN ('MEDIUM', 'WARNING', 'SEV2') THEN 'Medium / Sev2'
                ELSE 'Low / Sev3+'
             END = 'Medium / Sev2' THEN 3
        ELSE 4
    END;

;WITH AllAlerts AS
(
    SELECT ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente')
    FROM dbo.AlertsManagement
    WHERE Active = 1

    UNION ALL

    SELECT ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente')
    FROM dbo.AlertasBackup
    WHERE Active = 1
)
SELECT TOP (10)
    ClientName,
    Total = COUNT(1)
FROM AllAlerts
GROUP BY ClientName
ORDER BY COUNT(1) DESC;
";

        try
        {
            using var connection = _connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                sql,
                new { UserEmail = userEmail },
                commandType: CommandType.Text,
                cancellationToken: cancellationToken);

            using var multi = await connection.QueryMultipleAsync(command);

            var dashboard =
                await multi.ReadSingleOrDefaultAsync<AlertDashboardModel>()
                ?? new AlertDashboardModel();

            dashboard.Severities =
                (await multi.ReadAsync<DashboardSeverityModel>()).ToList();

            dashboard.TopClients =
                (await multi.ReadAsync<DashboardTopClientModel>()).ToList();

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error loading alert dashboard KPI data for user {UserEmail}",
                userEmail);

            throw;
        }
    }
}
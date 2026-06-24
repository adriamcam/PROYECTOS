using Dapper;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Models.Dashboard;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;

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
        SUM(CASE 
                WHEN Active = 1 
                 AND AssignedEmail = @UserEmail 
                THEN 1 ELSE 0 
            END) AS AssignedToMe,
        SUM(CASE 
                WHEN Active = 1 
                 AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '') 
                THEN 1 ELSE 0 
            END) AS Unassigned,
        SUM(CASE 
                WHEN Active = 0 
                 AND CAST(ISNULL(UpdatedAt, ResolveTime) AS date) = @Today
                THEN 1 ELSE 0 
            END) AS ResolvedToday
    FROM dbo.AlertsManagement
    WHERE Active = 1
       OR (
            Active = 0 
            AND CAST(ISNULL(UpdatedAt, ResolveTime) AS date) = @Today
          )
),
Backup AS
(
    SELECT
        COUNT(1) AS BackupAlerts
    FROM dbo.AlertsBackup
    WHERE Active = 1
)
SELECT
    ISNULL(m.ManagementAlerts, 0) + ISNULL(b.BackupAlerts, 0) AS TotalAlerts,
    ISNULL(b.BackupAlerts, 0) AS BackupAlerts,
    ISNULL(m.ManagementAlerts, 0) AS ManagementAlerts,
    ISNULL(m.AssignedToMe, 0) AS AssignedToMe,
    ISNULL(m.Unassigned, 0) AS Unassigned,
    ISNULL(m.ResolvedToday, 0) AS ResolvedToday
FROM Management m
CROSS JOIN Backup b;
";

        try
        {
            using var connection = _connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                sql,
                new { UserEmail = userEmail },
                cancellationToken: cancellationToken);

            var result = await connection.QuerySingleOrDefaultAsync<AlertDashboardModel>(command);

            return result ?? new AlertDashboardModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading alert dashboard KPI data for user {UserEmail}", userEmail);
            throw;
        }
    }
}

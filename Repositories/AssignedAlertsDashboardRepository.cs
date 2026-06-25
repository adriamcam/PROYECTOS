using Dapper;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Models.Dashboard;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;

namespace ITQS.SupportOperationsCenter.Repositories;

public sealed class AssignedAlertsDashboardRepository : IAssignedAlertsDashboardRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<AssignedAlertsDashboardRepository> _logger;

    public AssignedAlertsDashboardRepository(
        ISqlConnectionFactory connectionFactory,
        ILogger<AssignedAlertsDashboardRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<AssignedAlertsDashboardModel> GetDashboardAsync(
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        return new AssignedAlertsDashboardModel();
    }

    public async Task<List<DashboardAlertItemModel>> GetManagementAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
SELECT
    'Management'          AS SourceType,
    Id,
    SubscriptionName      AS ClientName,
    AlertName,
    Severity,
    'Management'          AS AlertType,
    TargetResourceName    AS ResourceName,
    Events,
    UpdatedAt             AS LastEventAt,
    AssignedTo,
    AssignedEmail
FROM dbo.AlertsManagement
WHERE Active = 1
ORDER BY UpdatedAt DESC;";

        var result = await connection.QueryAsync<DashboardAlertItemModel>(sql);

        return result.ToList();
    }

    public async Task<List<DashboardAlertItemModel>> GetBackupAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
SELECT
    'Backup'          AS SourceType,
    Id,
    SubscriptionName  AS ClientName,
    AlertRule         AS AlertName,
    Severity,
    'Backup'          AS AlertType,
    VMName            AS ResourceName,
    1                 AS Events,
    UpdatedAt         AS LastEventAt,
    AssignedTo,
    AssignedEmail
FROM dbo.AlertasBackup
WHERE Active = 1
ORDER BY UpdatedAt DESC;";

        var result = await connection.QueryAsync<DashboardAlertItemModel>(sql);

        return result.ToList();
    }

    public async Task AssignManagementAlertAsync(
        long id,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
UPDATE dbo.AlertsManagement
SET AssignedTo = @userName,
    AssignedEmail = @userEmail,
    UpdatedAt = GETDATE()
WHERE Id = @id;";

        await connection.ExecuteAsync(sql, new
        {
            id,
            userName,
            userEmail
        });
    }

    public async Task AssignBackupAlertAsync(
        long id,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
UPDATE dbo.AlertsBackups
SET AssignedTo = @userName,
    AssignedEmail = @userEmail,
    UpdatedAt = GETDATE()
WHERE Id = @id;";

        await connection.ExecuteAsync(sql, new
        {
            id,
            userName,
            userEmail
        });
    }
}
using Dapper;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Models.Dashboard;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using System.Data;

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
        const string sql = @"
SELECT
    TotalAssigned = 0,
    BackupAssigned = 0,
    ManagementAssigned = 0,
    Pending = 0,
    Resolved = 0;
";

        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new { UserEmail = userEmail },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<AssignedAlertsDashboardModel>(command);
    }

    public async Task<List<DashboardAlertItemModel>> GetManagementAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT TOP (100)
    SourceType = 'Management',
    Id = CAST(Id AS bigint),
    ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
    AlertName = ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
    Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
    AlertType = 'Management',
    ResourceName = ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso'),
    Events = 1,
    LastEventAt = COALESCE(UpdatedAt, InsertedAt, AlertTime),
    AssignedTo = ISNULL(AssignedTo, ''),
    AssignedEmail = ISNULL(AssignedEmail, '')
FROM dbo.AlertsManagement
WHERE ISNULL(Active, 0) = 1
ORDER BY COALESCE(UpdatedAt, InsertedAt, AlertTime) DESC;
";

        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            commandType: CommandType.Text,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<DashboardAlertItemModel>(command);
        return rows.ToList();
    }

    public async Task<List<DashboardAlertItemModel>> GetBackupAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT TOP (100)
    SourceType = 'Backup',
    Id = CAST(Id AS bigint),
    ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
    AlertName = ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
    Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
    AlertType = 'Backup',
    ResourceName = COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso'),
    Events = 1,
    LastEventAt = COALESCE(UpdatedAt, InsertedAt, AlertTime),
    AssignedTo = ISNULL(AssignedTo, ''),
    AssignedEmail = ISNULL(AssignedEmail, '')
FROM dbo.AlertasBackup
WHERE ISNULL(Active, 0) = 1
ORDER BY COALESCE(UpdatedAt, InsertedAt, AlertTime) DESC;
";

        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            commandType: CommandType.Text,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<DashboardAlertItemModel>(command);
        return rows.ToList();
    }

    public async Task AssignManagementAlertAsync(
        long id,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE dbo.AlertsManagement
SET
    AssignedTo = @UserName,
    AssignedEmail = @UserEmail,
    UpdatedAt = GETDATE()
WHERE Id = @Id
  AND ISNULL(Active, 0) = 1
  AND ISNULL(AssignedEmail, '') = '';
";

        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new { Id = id, UserName = userName, UserEmail = userEmail },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    public async Task AssignBackupAlertAsync(
        long id,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE dbo.AlertasBackup
SET
    AssignedTo = @UserName,
    AssignedEmail = @UserEmail,
    UpdatedAt = GETDATE()
WHERE Id = @Id
  AND ISNULL(Active, 0) = 1
  AND ISNULL(AssignedEmail, '') = '';
";

        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new { Id = id, UserName = userName, UserEmail = userEmail },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }
}
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

    public Task<AssignedAlertsDashboardModel> GetDashboardAsync(
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AssignedAlertsDashboardModel());
    }

    public async Task<List<DashboardAlertItemModel>> GetManagementAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
SELECT TOP (200)
    SourceType = 'Management',
    Id = MIN(CAST(Id AS bigint)),
    ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
    AlertName = ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
    Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
    AlertType = 'Management',
    ResourceName = ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso'),
    Events = COUNT(1),
    LastEventAt = MAX(COALESCE(UpdatedAt, InsertedAt, AlertTime)),
    AssignedTo = ISNULL(MAX(AssignedTo), ''),
    AssignedEmail = ISNULL(MAX(AssignedEmail), '')
FROM dbo.AlertsManagement
WHERE ISNULL(Active, 0) = 1
GROUP BY
    ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
    ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
    ISNULL(NULLIF(Severity, ''), 'Unknown'),
    ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso')
ORDER BY
    MAX(COALESCE(UpdatedAt, InsertedAt, AlertTime)) DESC,
    COUNT(1) DESC;";

        var result = await connection.QueryAsync<DashboardAlertItemModel>(sql);
        return result.ToList();
    }

    public async Task<List<DashboardAlertItemModel>> GetBackupAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
SELECT TOP (200)
    SourceType = 'Backup',
    Id = MIN(CAST(Id AS bigint)),
    ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
    AlertName = ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
    Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
    AlertType = 'Backup',
    ResourceName = COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso'),
    Events = COUNT(1),
    LastEventAt = MAX(COALESCE(UpdatedAt, InsertedAt, AlertTime)),
    AssignedTo = ISNULL(MAX(AssignedTo), ''),
    AssignedEmail = ISNULL(MAX(AssignedEmail), '')
FROM dbo.AlertasBackup
WHERE ISNULL(Active, 0) = 1
GROUP BY
    ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
    ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
    ISNULL(NULLIF(Severity, ''), 'Unknown'),
    COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso')
ORDER BY
    MAX(COALESCE(UpdatedAt, InsertedAt, AlertTime)) DESC,
    COUNT(1) DESC;";

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
DECLARE @SubscriptionName nvarchar(255);
DECLARE @AlertName nvarchar(500);
DECLARE @Severity nvarchar(100);
DECLARE @ResourceName nvarchar(500);

SELECT
    @SubscriptionName = SubscriptionName,
    @AlertName = AlertName,
    @Severity = Severity,
    @ResourceName = TargetResourceName
FROM dbo.AlertsManagement
WHERE Id = @Id;

UPDATE dbo.AlertsManagement
SET
    AssignedTo = @UserName,
    AssignedEmail = @UserEmail,
    UpdatedAt = GETDATE()
WHERE ISNULL(Active, 0) = 1
  AND ISNULL(AssignedEmail, '') = ''
  AND ISNULL(SubscriptionName, '') = ISNULL(@SubscriptionName, '')
  AND ISNULL(AlertName, '') = ISNULL(@AlertName, '')
  AND ISNULL(Severity, '') = ISNULL(@Severity, '')
  AND ISNULL(TargetResourceName, '') = ISNULL(@ResourceName, '');";

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            UserName = userName,
            UserEmail = userEmail
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
DECLARE @SubscriptionName nvarchar(255);
DECLARE @AlertRule nvarchar(500);
DECLARE @Severity nvarchar(100);
DECLARE @ResourceName nvarchar(500);

SELECT
    @SubscriptionName = SubscriptionName,
    @AlertRule = AlertRule,
    @Severity = Severity,
    @ResourceName = COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''))
FROM dbo.AlertasBackup
WHERE Id = @Id;

UPDATE dbo.AlertasBackup
SET
    AssignedTo = @UserName,
    AssignedEmail = @UserEmail,
    UpdatedAt = GETDATE()
WHERE ISNULL(Active, 0) = 1
  AND ISNULL(AssignedEmail, '') = ''
  AND ISNULL(SubscriptionName, '') = ISNULL(@SubscriptionName, '')
  AND ISNULL(AlertRule, '') = ISNULL(@AlertRule, '')
  AND ISNULL(Severity, '') = ISNULL(@Severity, '')
  AND ISNULL(COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, '')), '') = ISNULL(@ResourceName, '');";

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            UserName = userName,
            UserEmail = userEmail
        });
    }
}
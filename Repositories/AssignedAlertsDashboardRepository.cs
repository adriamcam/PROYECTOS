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

        const string sql = @"
SELECT
(
    SELECT COUNT(1) FROM dbo.AlertsManagement WHERE ISNULL(Active, 0) = 1
)
+
(
    SELECT COUNT(1) FROM dbo.AlertasBackup WHERE ISNULL(Active, 0) = 1
) AS TotalAlerts,

(
    SELECT COUNT(1) FROM dbo.AlertsManagement WHERE ISNULL(Active, 0) = 1
) AS ManagementAlerts,

(
    SELECT COUNT(1) FROM dbo.AlertasBackup WHERE ISNULL(Active, 0) = 1
) AS BackupAlerts,

(
    SELECT COUNT(1)
    FROM dbo.AlertsManagement
    WHERE ISNULL(Active, 0) = 1
      AND ISNULL(AssignedEmail, '') = ''
)
+
(
    SELECT COUNT(1)
    FROM dbo.AlertasBackup
    WHERE ISNULL(Active, 0) = 1
      AND ISNULL(AssignedEmail, '') = ''
) AS UnassignedAlerts,

(
    SELECT COUNT(1)
    FROM dbo.AlertsManagement
    WHERE ISNULL(Active, 0) = 1
      AND UPPER(ISNULL(Severity, '')) IN ('CRITICAL', 'HIGH', 'SEV0', 'SEV1')
)
+
(
    SELECT COUNT(1)
    FROM dbo.AlertasBackup
    WHERE ISNULL(Active, 0) = 1
      AND UPPER(ISNULL(Severity, '')) IN ('CRITICAL', 'HIGH', 'SEV0', 'SEV1')
) AS CriticalAlerts,

(
    SELECT COUNT(1)
    FROM dbo.AlertsManagement
    WHERE CAST(ISNULL(UpdatedAt, GETDATE()) AS date) = CAST(GETDATE() AS date)
)
+
(
    SELECT COUNT(1)
    FROM dbo.AlertasBackup
    WHERE CAST(ISNULL(UpdatedAt, GETDATE()) AS date) = CAST(GETDATE() AS date)
) AS NewToday;
";

        var dashboard = await connection.QueryFirstOrDefaultAsync<AssignedAlertsDashboardModel>(sql);
        return dashboard ?? new AssignedAlertsDashboardModel();
    }

    public async Task<List<string>> GetClientsAsync(
        string sourceType,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = sourceType == "Backup"
            ? @"
SELECT DISTINCT
    ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') AS ClientName
FROM dbo.AlertasBackup
WHERE ISNULL(Active, 0) = 1
  AND ISNULL(AssignedEmail, '') = ''
ORDER BY ClientName;"
            : @"
SELECT DISTINCT
    ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') AS ClientName
FROM dbo.AlertsManagement
WHERE ISNULL(Active, 0) = 1
  AND ISNULL(AssignedEmail, '') = ''
ORDER BY ClientName;";

        var result = await connection.QueryAsync<string>(sql);
        return result.ToList();
    }

    public async Task<DashboardAlertPagedResultModel> GetManagementAlertsAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? clientName = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 25 : pageSize;

        var offset = (pageNumber - 1) * pageSize;
        var searchValue = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
        var clientValue = string.IsNullOrWhiteSpace(clientName) ? null : clientName.Trim();

        const string countSql = @"
;WITH GroupedAlerts AS
(
    SELECT
        ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        AlertName = ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
        Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
        ResourceName = ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso')
    FROM dbo.AlertsManagement
    WHERE ISNULL(Active, 0) = 1
      AND ISNULL(AssignedEmail, '') = ''
      AND (
            @ClientName IS NULL
            OR ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') = @ClientName
          )
      AND (
            @Search IS NULL
            OR SubscriptionName LIKE @Search
            OR AlertName LIKE @Search
            OR Severity LIKE @Search
            OR TargetResourceName LIKE @Search
          )
    GROUP BY
        ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
        ISNULL(NULLIF(Severity, ''), 'Unknown'),
        ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso')
)
SELECT COUNT(1)
FROM GroupedAlerts;
";

        const string dataSql = @"
;WITH GroupedAlerts AS
(
    SELECT
        SourceType = 'Management',
        Id = MIN(CAST(Id AS bigint)),
        ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        AlertName = ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
        Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
        AlertType = 'Management',
        ResourceName = ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso'),
        Events = COUNT(1),
        LastEventAt = MAX(UpdatedAt),
        AssignedTo = '',
        AssignedEmail = ''
    FROM dbo.AlertsManagement
    WHERE ISNULL(Active, 0) = 1
      AND ISNULL(AssignedEmail, '') = ''
      AND (
            @ClientName IS NULL
            OR ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') = @ClientName
          )
      AND (
            @Search IS NULL
            OR SubscriptionName LIKE @Search
            OR AlertName LIKE @Search
            OR Severity LIKE @Search
            OR TargetResourceName LIKE @Search
          )
    GROUP BY
        ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
        ISNULL(NULLIF(Severity, ''), 'Unknown'),
        ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso')
)
SELECT
    SourceType,
    Id,
    ClientName,
    AlertName,
    Severity,
    AlertType,
    ResourceName,
    Events,
    LastEventAt,
    AssignedTo,
    AssignedEmail
FROM GroupedAlerts
ORDER BY
    LastEventAt DESC,
    Events DESC
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;
";

        var parameters = new
        {
            Search = searchValue,
            ClientName = clientValue,
            Offset = offset,
            PageSize = pageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await connection.QueryAsync<DashboardAlertItemModel>(dataSql, parameters);

        return new DashboardAlertPagedResultModel
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<DashboardAlertPagedResultModel> GetBackupAlertsAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? clientName = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 25 : pageSize;

        var offset = (pageNumber - 1) * pageSize;
        var searchValue = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
        var clientValue = string.IsNullOrWhiteSpace(clientName) ? null : clientName.Trim();

        const string countSql = @"
;WITH GroupedAlerts AS
(
    SELECT
        ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        AlertName = ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
        Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
        ResourceName = COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso')
    FROM dbo.AlertasBackup
    WHERE ISNULL(Active, 0) = 1
      AND ISNULL(AssignedEmail, '') = ''
      AND (
            @ClientName IS NULL
            OR ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') = @ClientName
          )
      AND (
            @Search IS NULL
            OR SubscriptionName LIKE @Search
            OR AlertRule LIKE @Search
            OR Severity LIKE @Search
            OR ResourceName LIKE @Search
            OR VMName LIKE @Search
            OR ProtectedItem LIKE @Search
          )
    GROUP BY
        ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
        ISNULL(NULLIF(Severity, ''), 'Unknown'),
        COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso')
)
SELECT COUNT(1)
FROM GroupedAlerts;
";

        const string dataSql = @"
;WITH GroupedAlerts AS
(
    SELECT
        SourceType = 'Backup',
        Id = MIN(CAST(Id AS bigint)),
        ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        AlertName = ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
        Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
        AlertType = 'Backup',
        ResourceName = COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso'),
        Events = COUNT(1),
        LastEventAt = MAX(UpdatedAt),
        AssignedTo = '',
        AssignedEmail = ''
    FROM dbo.AlertasBackup
    WHERE ISNULL(Active, 0) = 1
      AND ISNULL(AssignedEmail, '') = ''
      AND (
            @ClientName IS NULL
            OR ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') = @ClientName
          )
      AND (
            @Search IS NULL
            OR SubscriptionName LIKE @Search
            OR AlertRule LIKE @Search
            OR Severity LIKE @Search
            OR ResourceName LIKE @Search
            OR VMName LIKE @Search
            OR ProtectedItem LIKE @Search
          )
    GROUP BY
        ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
        ISNULL(NULLIF(Severity, ''), 'Unknown'),
        COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso')
)
SELECT
    SourceType,
    Id,
    ClientName,
    AlertName,
    Severity,
    AlertType,
    ResourceName,
    Events,
    LastEventAt,
    AssignedTo,
    AssignedEmail
FROM GroupedAlerts
ORDER BY
    LastEventAt DESC,
    Events DESC
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;
";

        var parameters = new
        {
            Search = searchValue,
            ClientName = clientValue,
            Offset = offset,
            PageSize = pageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await connection.QueryAsync<DashboardAlertItemModel>(dataSql, parameters);

        return new DashboardAlertPagedResultModel
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task AssignManagementAlertAsync(
        long id,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        await AssignManagementAlertsAsync(
            new List<long> { id },
            userName,
            userEmail,
            cancellationToken);
    }

    public async Task AssignBackupAlertAsync(
        long id,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        await AssignBackupAlertsAsync(
            new List<long> { id },
            userName,
            userEmail,
            cancellationToken);
    }

    public async Task AssignManagementAlertsAsync(
        List<long> ids,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        if (ids is null || ids.Count == 0)
        {
            return;
        }

        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
UPDATE dbo.AlertsManagement
SET
    AssignedTo = @UserName,
    AssignedEmail = @UserEmail,
    UpdatedAt = GETDATE()
WHERE ISNULL(Active, 0) = 1
  AND Id IN @Ids;";

        await connection.ExecuteAsync(sql, new
        {
            Ids = ids,
            UserName = userName,
            UserEmail = userEmail
        });
    }

    public async Task AssignBackupAlertsAsync(
        List<long> ids,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        if (ids is null || ids.Count == 0)
        {
            return;
        }

        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
UPDATE dbo.AlertasBackup
SET
    AssignedTo = @UserName,
    AssignedEmail = @UserEmail,
    UpdatedAt = GETDATE()
WHERE ISNULL(Active, 0) = 1
  AND Id IN @Ids;";

        await connection.ExecuteAsync(sql, new
        {
            Ids = ids,
            UserName = userName,
            UserEmail = userEmail
        });
    }
}
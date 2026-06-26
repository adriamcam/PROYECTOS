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
        DashboardAlertItemModel alert,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        await AssignManagementAlertsAsync(
            new List<DashboardAlertItemModel> { alert },
            userName,
            userEmail,
            cancellationToken);
    }

    public async Task AssignBackupAlertAsync(
        DashboardAlertItemModel alert,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        await AssignBackupAlertsAsync(
            new List<DashboardAlertItemModel> { alert },
            userName,
            userEmail,
            cancellationToken);
    }

    public async Task AssignManagementAlertsAsync(
        List<DashboardAlertItemModel> alerts,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        if (alerts is null || alerts.Count == 0)
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
  AND ISNULL(AssignedEmail, '') = ''
  AND ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') = @ClientName
  AND ISNULL(NULLIF(AlertName, ''), 'Sin nombre') = @AlertName
  AND ISNULL(NULLIF(Severity, ''), 'Unknown') = @Severity
  AND ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso') = @ResourceName;";

        foreach (var alert in alerts)
        {
            await connection.ExecuteAsync(sql, new
            {
                UserName = userName,
                UserEmail = userEmail,
                alert.ClientName,
                alert.AlertName,
                alert.Severity,
                alert.ResourceName
            });
        }
    }

    public async Task AssignBackupAlertsAsync(
        List<DashboardAlertItemModel> alerts,
        string userName,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        if (alerts is null || alerts.Count == 0)
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
  AND ISNULL(AssignedEmail, '') = ''
  AND ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') = @ClientName
  AND ISNULL(NULLIF(AlertRule, ''), 'Sin nombre') = @AlertName
  AND ISNULL(NULLIF(Severity, ''), 'Unknown') = @Severity
  AND COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso') = @ResourceName;";

        foreach (var alert in alerts)
        {
            await connection.ExecuteAsync(sql, new
            {
                UserName = userName,
                UserEmail = userEmail,
                alert.ClientName,
                alert.AlertName,
                alert.Severity,
                alert.ResourceName
            });
        }
    }
	//========================================================
// DETALLE DE ALERTA
//========================================================

public async Task<AlertDetailModel?> GetAlertDetailAsync(
    DashboardAlertItemModel alert,
    CancellationToken cancellationToken = default)
{
    using var connection = _connectionFactory.CreateConnection();

    if (alert.SourceType == "Backup")
    {
        const string sql = @"
SELECT TOP 1
    SourceType = 'Backup',
    AlertId = CAST(B.Id AS bigint),
    ClientName = ISNULL(B.SubscriptionName, ''),
    SubscriptionName = ISNULL(B.SubscriptionName, ''),
    SubscriptionId = ISNULL(B.SubscriptionId, ''),
    TenantId = ISNULL(B.TenantId, ''),
    AlertName = ISNULL(B.AlertRule, ''),
    Severity = ISNULL(B.Severity, ''),
    AlertStatus = CASE WHEN ISNULL(B.Active, 0) = 1 THEN 'InProgress' ELSE 'Closed' END,
    AlertState = '',
    ResourceName = COALESCE(NULLIF(B.ResourceName, ''), NULLIF(B.VMName, ''), NULLIF(B.ProtectedItem, ''), ''),
    ResourceType = 'Backup',
    ResourceId = '',
    ResourceGroup = ISNULL(B.ResourceGroup, ''),
    Location = '',
    Details = '',
    ErrorDetail = ISNULL(B.ErrorDetail, ''),
    Events = @Events,
    AlertTime = B.AlertTime,
    FirstOccurrence = B.InsertedAt,
    LastOccurrence = B.UpdatedAt,
    AssignedTo = ISNULL(B.AssignedTo, ''),
    AssignedEmail = ISNULL(B.AssignedEmail, ''),
    ResolutionNotes = ISNULL(B.ResolutionNotes, '')
FROM dbo.AlertasBackup B
WHERE ISNULL(B.SubscriptionName, '') = @ClientName
  AND ISNULL(B.AlertRule, '') = @AlertName
  AND ISNULL(B.Severity, '') = @Severity
  AND COALESCE(NULLIF(B.ResourceName, ''), NULLIF(B.VMName, ''), NULLIF(B.ProtectedItem, ''), '') = @ResourceName
ORDER BY ISNULL(B.UpdatedAt, B.InsertedAt) DESC;

SELECT
    HistoryId,
    KPIType,
    AlertId = CAST(ISNULL(AlertId, 0) AS bigint),
    Comment = ISNULL(Comment, ''),
    Status = ISNULL(Status, ''),
    UpdatedBy = ISNULL(UpdatedBy, ''),
    UserEmail = ISNULL(UserEmail, ''),
    UpdatedAt
FROM dbo.AlertUpdatesHistory
WHERE KPIType = 'Backup'
  AND ISNULL(Res_nom, '') = @ResourceName
  AND ISNULL(Alert_nom, '') = @AlertName
ORDER BY UpdatedAt DESC;
";

        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            alert.ClientName,
            alert.AlertName,
            alert.Severity,
            alert.ResourceName,
            alert.Events
        });

        var detail = await multi.ReadFirstOrDefaultAsync<AlertDetailModel>();

        if (detail is not null)
        {
            detail.History = (await multi.ReadAsync<AlertHistoryItemModel>()).ToList();
        }

        return detail;
    }
    else
    {
        const string sql = @"
SELECT TOP 1
    SourceType = 'Management',
    AlertId = CAST(A.Id AS bigint),
    ClientName = ISNULL(A.SubscriptionName, ''),
    SubscriptionName = ISNULL(A.SubscriptionName, ''),
    SubscriptionId = ISNULL(A.SubscriptionId, ''),
    TenantId = ISNULL(A.TenantId, ''),
    AlertName = ISNULL(A.AlertName, ''),
    Severity = ISNULL(A.Severity, ''),
    AlertStatus = ISNULL(A.AlertStatus, CASE WHEN ISNULL(A.Active, 0) = 1 THEN 'InProgress' ELSE 'Closed' END),
    AlertState = ISNULL(A.AlertState, ''),
    ResourceName = ISNULL(A.TargetResourceName, ''),
    ResourceType = ISNULL(I.Type, ''),
    ResourceId = ISNULL(I.ResourceId, ''),
    ResourceGroup = ISNULL(A.ResourceGroup, ''),
    Location = ISNULL(I.Location, ''),
    Details = ISNULL(A.Details, ''),
    ErrorDetail = '',
    Events = @Events,
    AlertTime = A.AlertTime,
    FirstOccurrence = A.InsertedAt,
    LastOccurrence = A.UpdatedAt,
    AssignedTo = ISNULL(A.AssignedTo, ''),
    AssignedEmail = ISNULL(A.AssignedEmail, ''),
    ResolutionNotes = ISNULL(A.ResolutionNotes, '')
FROM dbo.AlertsManagement A
LEFT JOIN dbo.ITQS_CustomerInventory_Current I
    ON I.SubscriptionId = A.SubscriptionId
   AND I.Name = A.TargetResourceName
WHERE ISNULL(A.SubscriptionName, '') = @ClientName
  AND ISNULL(A.AlertName, '') = @AlertName
  AND ISNULL(A.Severity, '') = @Severity
  AND ISNULL(A.TargetResourceName, '') = @ResourceName
ORDER BY ISNULL(A.UpdatedAt, A.InsertedAt) DESC;

SELECT
    HistoryId,
    KPIType,
    AlertId = CAST(ISNULL(AlertId, 0) AS bigint),
    Comment = ISNULL(Comment, ''),
    Status = ISNULL(Status, ''),
    UpdatedBy = ISNULL(UpdatedBy, ''),
    UserEmail = ISNULL(UserEmail, ''),
    UpdatedAt
FROM dbo.AlertUpdatesHistory
WHERE KPIType = 'Management'
  AND ISNULL(Res_nom, '') = @ResourceName
  AND ISNULL(Alert_nom, '') = @AlertName
ORDER BY UpdatedAt DESC;
";

        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            alert.ClientName,
            alert.AlertName,
            alert.Severity,
            alert.ResourceName,
            alert.Events
        });

        var detail = await multi.ReadFirstOrDefaultAsync<AlertDetailModel>();

        if (detail is not null)
        {
            detail.History = (await multi.ReadAsync<AlertHistoryItemModel>()).ToList();
        }

        return detail;
    }
}

//========================================================
// COMENTARIOS
//========================================================

public async Task SaveAlertCommentAsync(
    AlertCommentRequestModel request,
    CancellationToken cancellationToken = default)
{
    using var connection = _connectionFactory.CreateConnection();

    const string sql = @"
INSERT INTO dbo.AlertUpdatesHistory
(
    KPIType,
    AlertId,
    Comment,
    Status,
    UpdatedBy,
    UpdatedAt,
    Res_nom,
    Alert_nom,
    UserEmail
)
VALUES
(
    @KPIType,
    @AlertId,
    @Comment,
    @Status,
    @UpdatedBy,
    GETDATE(),
    @ResourceName,
    @AlertName,
    @UserEmail
);";

    await connection.ExecuteAsync(sql, new
    {
        KPIType = request.SourceType,
        request.AlertId,
        request.Comment,
        request.Status,
        UpdatedBy = request.UpdatedBy,
        request.ResourceName,
        request.AlertName,
        request.UserEmail
    });
}
//========================================================
// CERRAR ALERTA
//========================================================

public async Task CloseAlertAsync(
    AlertCommentRequestModel request,
    CancellationToken cancellationToken = default)
{
    using var connection = _connectionFactory.CreateConnection();

    if (request.SourceType == "Backup")
    {
        const string sql = @"
UPDATE dbo.AlertasBackup
SET
    Active = 0,
    ResolveTime = GETDATE(),
    ResolutionNotes = @Comment,
    LastUpdatedBy = @UpdatedBy,
    UpdatedAt = SYSDATETIME()
WHERE ISNULL(Active, 0) = 1
  AND ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') = @ClientName
  AND ISNULL(NULLIF(AlertRule, ''), 'Sin nombre') = @AlertName
  AND ISNULL(NULLIF(Severity, ''), 'Unknown') = @Severity
  AND COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso') = @ResourceName;

INSERT INTO dbo.AlertUpdatesHistory
(
    KPIType,
    AlertId,
    Comment,
    Status,
    UpdatedBy,
    UpdatedAt,
    Res_nom,
    Alert_nom,
    UserEmail
)
VALUES
(
    @KPIType,
    @AlertId,
    @Comment,
    'Closed',
    @UpdatedBy,
    GETDATE(),
    @ResourceName,
    @AlertName,
    @UserEmail
);";

        await connection.ExecuteAsync(sql, new
        {
            KPIType = request.SourceType,
            request.AlertId,
            request.ClientName,
            request.AlertName,
            request.Severity,
            request.ResourceName,
            request.Comment,
            request.UpdatedBy,
            request.UserEmail
        });
    }
    else
    {
        const string sql = @"
UPDATE dbo.AlertsManagement
SET
    Active = 0,
    AlertStatus = 'Closed',
    ResolveTime = GETDATE(),
    ResolutionNotes = @Comment,
    LastUpdatedBy = @UpdatedBy,
    UpdatedAt = SYSDATETIME()
WHERE ISNULL(Active, 0) = 1
  AND ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') = @ClientName
  AND ISNULL(NULLIF(AlertName, ''), 'Sin nombre') = @AlertName
  AND ISNULL(NULLIF(Severity, ''), 'Unknown') = @Severity
  AND ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso') = @ResourceName;

INSERT INTO dbo.AlertUpdatesHistory
(
    KPIType,
    AlertId,
    Comment,
    Status,
    UpdatedBy,
    UpdatedAt,
    Res_nom,
    Alert_nom,
    UserEmail
)
VALUES
(
    @KPIType,
    @AlertId,
    @Comment,
    'Closed',
    @UpdatedBy,
    GETDATE(),
    @ResourceName,
    @AlertName,
    @UserEmail
);";

        await connection.ExecuteAsync(sql, new
        {
            KPIType = request.SourceType,
            request.AlertId,
            request.ClientName,
            request.AlertName,
            request.Severity,
            request.ResourceName,
            request.Comment,
            request.UpdatedBy,
            request.UserEmail
        });
    }
}
}

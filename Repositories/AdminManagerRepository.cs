using Dapper;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Models.Dashboard;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;

namespace ITQS.SupportOperationsCenter.Repositories;

public sealed class AdminManagerRepository : IAdminManagerRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<AdminManagerRepository> _logger;

    public AdminManagerRepository(
        ISqlConnectionFactory connectionFactory,
        ILogger<AdminManagerRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<AdminManagerDashboardModel> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
;WITH AssignedBase AS
(
    SELECT
        SourceType = 'Management',
        ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        AlertName = ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
        Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
        ResourceName = ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso'),
        AssignedEmail = ISNULL(AssignedEmail, ''),
        AlertStatus =
            CASE
                WHEN LOWER(ISNULL(AlertStatus, '')) IN ('inprogress', 'in progress') THEN 'InProgress'
                ELSE 'Activa'
            END,
        Events = COUNT(1)
    FROM dbo.AlertsManagement
    WHERE ISNULL(Active, 0) = 1
      AND ISNULL(AssignedEmail, '') <> ''
    GROUP BY
        ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
        ISNULL(NULLIF(Severity, ''), 'Unknown'),
        ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso'),
        ISNULL(AssignedEmail, ''),
        CASE
            WHEN LOWER(ISNULL(AlertStatus, '')) IN ('inprogress', 'in progress') THEN 'InProgress'
            ELSE 'Activa'
        END

    UNION ALL

    SELECT
        SourceType = 'Backup',
        ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        AlertName = ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
        Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
        ResourceName = COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso'),
        AssignedEmail = ISNULL(AssignedEmail, ''),
        AlertStatus = 'Activa',
        Events = COUNT(1)
    FROM dbo.AlertasBackup
    WHERE ISNULL(Active, 0) = 1
      AND ISNULL(AssignedEmail, '') <> ''
    GROUP BY
        ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
        ISNULL(NULLIF(Severity, ''), 'Unknown'),
        COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso'),
        ISNULL(AssignedEmail, '')
),
UnassignedCloseable AS
(
    SELECT
        SourceType = 'Management',
        ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        AlertName = ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
        Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
        ResourceName = ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso'),
        Events = COUNT(1)
    FROM dbo.AlertsManagement
    WHERE ISNULL(Active, 0) = 1
      AND ISNULL(AssignedEmail, '') = ''
      AND LOWER(ISNULL(AlertStatus, '')) NOT IN ('inprogress', 'in progress')
    GROUP BY
        ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
        ISNULL(NULLIF(Severity, ''), 'Unknown'),
        ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso')

    UNION ALL

    SELECT
        SourceType = 'Backup',
        ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        AlertName = ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
        Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
        ResourceName = COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso'),
        Events = COUNT(1)
    FROM dbo.AlertasBackup
    WHERE ISNULL(Active, 0) = 1
      AND ISNULL(AssignedEmail, '') = ''
    GROUP BY
        ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
        ISNULL(NULLIF(Severity, ''), 'Unknown'),
        COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso')
)
SELECT
    TotalAssignedGroups = ISNULL((SELECT COUNT(1) FROM AssignedBase), 0),
    TotalAssignedEvents = ISNULL((SELECT SUM(Events) FROM AssignedBase), 0),
    ActiveGroups = ISNULL((SELECT COUNT(1) FROM AssignedBase WHERE AlertStatus = 'Activa'), 0),
    InProgressGroups = ISNULL((SELECT COUNT(1) FROM AssignedBase WHERE AlertStatus = 'InProgress'), 0),
    ClosedToday = ISNULL((
        SELECT COUNT(1)
        FROM dbo.AlertUpdatesHistory
        WHERE LOWER(ISNULL(Status, '')) IN ('closed', 'close')
          AND CAST(UpdatedAt AS date) = CAST(GETDATE() AS date)
    ), 0),
    ClosedMonth = ISNULL((
        SELECT COUNT(1)
        FROM dbo.AlertUpdatesHistory
        WHERE LOWER(ISNULL(Status, '')) IN ('closed', 'close')
          AND YEAR(UpdatedAt) = YEAR(GETDATE())
          AND MONTH(UpdatedAt) = MONTH(GETDATE())
    ), 0),
    UsersWithAlerts = ISNULL((SELECT COUNT(DISTINCT AssignedEmail) FROM AssignedBase), 0),
    UnassignedCloseableGroups = ISNULL((SELECT COUNT(1) FROM UnassignedCloseable), 0),
    CriticalGroups = ISNULL((
        SELECT COUNT(1)
        FROM AssignedBase
        WHERE UPPER(Severity) IN ('CRITICAL', 'HIGH', 'SEV0', 'SEV1')
    ), 0);
";

        return await connection.QueryFirstOrDefaultAsync<AdminManagerDashboardModel>(sql)
               ?? new AdminManagerDashboardModel();
    }

    public async Task<AdminManagerAlertPagedResultModel> GetAlertsAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? clientName = null,
        string? assignedEmail = null,
        string? sourceType = null,
        string? severity = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 25 : pageSize;

        var offset = (pageNumber - 1) * pageSize;
        var searchValue = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
        var clientValue = string.IsNullOrWhiteSpace(clientName) ? null : clientName.Trim();
        var assignedValue = string.IsNullOrWhiteSpace(assignedEmail) ? null : assignedEmail.Trim();
        var sourceValue = string.IsNullOrWhiteSpace(sourceType) || sourceType == "All" ? null : sourceType.Trim();
        var severityValue = string.IsNullOrWhiteSpace(severity) || severity == "All" ? null : severity.Trim();
        var statusValue = string.IsNullOrWhiteSpace(status) || status == "All" ? null : status.Trim();

        const string baseSql = @"
;WITH RawAlerts AS
(
    SELECT
        SourceType = 'Management',
        Id = CAST(A.Id AS bigint),
        ClientName = ISNULL(NULLIF(A.SubscriptionName, ''), 'Sin cliente'),
        AlertName = ISNULL(NULLIF(A.AlertName, ''), 'Sin nombre'),
        Severity = ISNULL(NULLIF(A.Severity, ''), 'Unknown'),
        AlertType = 'Management',
        ResourceName = ISNULL(NULLIF(A.TargetResourceName, ''), 'Sin recurso'),
        LastEventAt = ISNULL(A.UpdatedAt, A.InsertedAt),
        AssignedTo = ISNULL(A.AssignedTo, ''),
        AssignedEmail = ISNULL(A.AssignedEmail, ''),
        AlertStatus =
            CASE
                WHEN LOWER(ISNULL(H.Status, '')) IN ('inprogress', 'in progress', 'update_note') THEN 'InProgress'
                WHEN LOWER(ISNULL(A.AlertStatus, '')) IN ('inprogress', 'in progress') THEN 'InProgress'
                ELSE 'Activa'
            END
    FROM dbo.AlertsManagement A
    OUTER APPLY
    (
        SELECT TOP 1 Status
        FROM dbo.AlertUpdatesHistory H
        WHERE H.KPIType = 'Management'
          AND H.AlertId = CAST(A.Id AS bigint)
        ORDER BY H.UpdatedAt DESC
    ) H
    WHERE ISNULL(A.Active, 0) = 1
      AND ISNULL(A.AssignedEmail, '') <> ''

    UNION ALL

    SELECT
        SourceType = 'Backup',
        Id = CAST(B.Id AS bigint),
        ClientName = ISNULL(NULLIF(B.SubscriptionName, ''), 'Sin cliente'),
        AlertName = ISNULL(NULLIF(B.AlertRule, ''), 'Sin nombre'),
        Severity = ISNULL(NULLIF(B.Severity, ''), 'Unknown'),
        AlertType = 'Backup',
        ResourceName = COALESCE(NULLIF(B.ResourceName, ''), NULLIF(B.VMName, ''), NULLIF(B.ProtectedItem, ''), 'Sin recurso'),
        LastEventAt = ISNULL(B.UpdatedAt, B.InsertedAt),
        AssignedTo = ISNULL(B.AssignedTo, ''),
        AssignedEmail = ISNULL(B.AssignedEmail, ''),
        AlertStatus =
            CASE
                WHEN LOWER(ISNULL(H.Status, '')) IN ('inprogress', 'in progress', 'update_note') THEN 'InProgress'
                ELSE 'Activa'
            END
    FROM dbo.AlertasBackup B
    OUTER APPLY
    (
        SELECT TOP 1 Status
        FROM dbo.AlertUpdatesHistory H
        WHERE H.KPIType = 'Backup'
          AND H.AlertId = CAST(B.Id AS bigint)
        ORDER BY H.UpdatedAt DESC
    ) H
    WHERE ISNULL(B.Active, 0) = 1
      AND ISNULL(B.AssignedEmail, '') <> ''
),
GroupedAlerts AS
(
    SELECT
        SourceType,
        Id = MIN(Id),
        ClientName,
        AlertName,
        Severity,
        AlertType,
        ResourceName,
        Events = COUNT(1),
        LastEventAt = MAX(LastEventAt),
        AlertStatus =
            CASE
                WHEN SUM(CASE WHEN AlertStatus = 'InProgress' THEN 1 ELSE 0 END) > 0 THEN 'InProgress'
                ELSE 'Activa'
            END,
        AssignedTo = MAX(AssignedTo),
        AssignedEmail = MAX(AssignedEmail)
    FROM RawAlerts
    WHERE
        (@Search IS NULL OR ClientName LIKE @Search OR AlertName LIKE @Search OR Severity LIKE @Search OR ResourceName LIKE @Search OR AssignedTo LIKE @Search OR AssignedEmail LIKE @Search)
        AND (@ClientName IS NULL OR ClientName = @ClientName)
        AND (@AssignedEmail IS NULL OR LOWER(AssignedEmail) = LOWER(@AssignedEmail))
        AND (@SourceType IS NULL OR SourceType = @SourceType)
        AND (@Severity IS NULL OR Severity = @Severity)
    GROUP BY
        SourceType,
        ClientName,
        AlertName,
        Severity,
        AlertType,
        ResourceName,
        AssignedEmail
)
";

        var countSql = baseSql + @"
SELECT COUNT(1)
FROM GroupedAlerts
WHERE (@Status IS NULL OR AlertStatus = @Status);
";

        var dataSql = baseSql + @"
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
    AlertStatus,
    AssignedTo,
    AssignedEmail
FROM GroupedAlerts
WHERE (@Status IS NULL OR AlertStatus = @Status)
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
            AssignedEmail = assignedValue,
            SourceType = sourceValue,
            Severity = severityValue,
            Status = statusValue,
            Offset = offset,
            PageSize = pageSize
        };

        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await connection.QueryAsync<AdminManagerAlertItemModel>(dataSql, parameters);

        return new AdminManagerAlertPagedResultModel
        {
            Items = items.ToList(),
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<string>> GetClientsAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
SELECT DISTINCT ClientName
FROM
(
    SELECT ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente')
    FROM dbo.AlertsManagement
    WHERE ISNULL(Active, 0) = 1

    UNION ALL

    SELECT ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente')
    FROM dbo.AlertasBackup
    WHERE ISNULL(Active, 0) = 1
) X
ORDER BY ClientName;
";

        var result = await connection.QueryAsync<string>(sql);
        return result.ToList();
    }

    public async Task<List<AdminManagerAppUserModel>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
SELECT
    Id,
    UserEmail = ISNULL(UserEmail, ''),
    DisplayName = ISNULL(DisplayName, ''),
    EffectiveRole = ISNULL(EffectiveRole, ''),
    IsActive = CAST(ISNULL(IsActive, 0) AS bit)
FROM dbo.ITQS_AppUsers
WHERE ISNULL(IsActive, 0) = 1
ORDER BY DisplayName;
";

        var users = await connection.QueryAsync<AdminManagerAppUserModel>(sql);
        return users.ToList();
    }

    public async Task<List<AdminManagerEngineerWorkloadModel>> GetEngineerWorkloadAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
;WITH AssignedGroups AS
(
    SELECT
        AssignedEmail = ISNULL(AssignedEmail, ''),
        AlertStatus =
            CASE
                WHEN LOWER(ISNULL(AlertStatus, '')) IN ('inprogress', 'in progress') THEN 'InProgress'
                ELSE 'Activa'
            END,
        Events = COUNT(1)
    FROM dbo.AlertsManagement
    WHERE ISNULL(Active, 0) = 1
      AND ISNULL(AssignedEmail, '') <> ''
    GROUP BY
        ISNULL(AssignedEmail, ''),
        ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
        ISNULL(NULLIF(Severity, ''), 'Unknown'),
        ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso'),
        CASE
            WHEN LOWER(ISNULL(AlertStatus, '')) IN ('inprogress', 'in progress') THEN 'InProgress'
            ELSE 'Activa'
        END

    UNION ALL

    SELECT
        AssignedEmail = ISNULL(AssignedEmail, ''),
        AlertStatus = 'Activa',
        Events = COUNT(1)
    FROM dbo.AlertasBackup
    WHERE ISNULL(Active, 0) = 1
      AND ISNULL(AssignedEmail, '') <> ''
    GROUP BY
        ISNULL(AssignedEmail, ''),
        ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
        ISNULL(NULLIF(Severity, ''), 'Unknown'),
        COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso')
),
ClosedToday AS
(
    SELECT
        UserEmail = ISNULL(UserEmail, ''),
        ClosedToday = COUNT(1)
    FROM dbo.AlertUpdatesHistory
    WHERE LOWER(ISNULL(Status, '')) IN ('closed', 'close')
      AND CAST(UpdatedAt AS date) = CAST(GETDATE() AS date)
    GROUP BY ISNULL(UserEmail, '')
)
SELECT
    UserEmail = U.UserEmail,
    DisplayName = U.DisplayName,
    ActiveGroups = SUM(CASE WHEN G.AlertStatus = 'Activa' THEN 1 ELSE 0 END),
    InProgressGroups = SUM(CASE WHEN G.AlertStatus = 'InProgress' THEN 1 ELSE 0 END),
    TotalEvents = ISNULL(SUM(G.Events), 0),
    ClosedToday = ISNULL(MAX(C.ClosedToday), 0)
FROM dbo.ITQS_AppUsers U
LEFT JOIN AssignedGroups G
    ON LOWER(G.AssignedEmail) = LOWER(U.UserEmail)
LEFT JOIN ClosedToday C
    ON LOWER(C.UserEmail) = LOWER(U.UserEmail)
WHERE ISNULL(U.IsActive, 0) = 1
GROUP BY
    U.UserEmail,
    U.DisplayName
ORDER BY
    ISNULL(SUM(G.Events), 0) DESC,
    U.DisplayName;
";

        var result = await connection.QueryAsync<AdminManagerEngineerWorkloadModel>(sql);
        return result.ToList();
    }

    public async Task<List<AdminManagerSeveritySummaryModel>> GetSeveritySummaryAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
;WITH Closeable AS
(
    SELECT
        SourceType = 'Management',
        Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
        Events = COUNT(1)
    FROM dbo.AlertsManagement
    WHERE ISNULL(Active, 0) = 1
      AND ISNULL(AssignedEmail, '') = ''
      AND LOWER(ISNULL(AlertStatus, '')) NOT IN ('inprogress', 'in progress')
    GROUP BY
        ISNULL(NULLIF(Severity, ''), 'Unknown'),
        ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
        ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso')

    UNION ALL

    SELECT
        SourceType = 'Backup',
        Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
        Events = COUNT(1)
    FROM dbo.AlertasBackup
    WHERE ISNULL(Active, 0) = 1
      AND ISNULL(AssignedEmail, '') = ''
    GROUP BY
        ISNULL(NULLIF(Severity, ''), 'Unknown'),
        ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
        COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso')
)
SELECT
    SourceType,
    Severity,
    Groups = COUNT(1),
    Events = SUM(Events),
    CloseableGroups = COUNT(1),
    CloseableEvents = SUM(Events)
FROM Closeable
GROUP BY
    SourceType,
    Severity
ORDER BY
    CASE UPPER(Severity)
        WHEN 'SEV0' THEN 1
        WHEN 'CRITICAL' THEN 2
        WHEN 'SEV1' THEN 3
        WHEN 'HIGH' THEN 4
        WHEN 'SEV2' THEN 5
        WHEN 'WARNING' THEN 6
        WHEN 'MEDIUM' THEN 7
        WHEN 'SEV3' THEN 8
        WHEN 'INFO' THEN 9
        ELSE 99
    END,
    SourceType;
";

        var result = await connection.QueryAsync<AdminManagerSeveritySummaryModel>(sql);
        return result.ToList();
    }

    public async Task<List<AdminManagerClosedHistoryModel>> GetClosedHistoryAsync(
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        take = take <= 0 ? 100 : take;

        const string sql = @"
SELECT TOP (@Take)
    HistoryId = H.HistoryId,
    KPIType = ISNULL(H.KPIType, ''),
    AlertId = CAST(ISNULL(H.AlertId, 0) AS bigint),
    ClientName = ISNULL(COALESCE(AM.SubscriptionName, AB.SubscriptionName), ''),
    AlertName = ISNULL(COALESCE(AM.AlertName, AB.AlertRule, H.Alert_norm), ''),
    ResourceName = ISNULL(COALESCE(AM.TargetResourceName, AB.ResourceName, AB.VMName, AB.ProtectedItem, H.Res_norm), ''),
    Severity = ISNULL(COALESCE(AM.Severity, AB.Severity), ''),
    ClosedBy = ISNULL(H.UpdatedBy, ''),
    UserEmail = ISNULL(H.UserEmail, ''),
    Comment = ISNULL(H.Comment, ''),
    ClosedAt = H.UpdatedAt
FROM dbo.AlertUpdatesHistory H
LEFT JOIN dbo.AlertsManagement AM
    ON H.KPIType = 'Management'
   AND H.AlertId = AM.Id
LEFT JOIN dbo.AlertasBackup AB
    ON H.KPIType = 'Backup'
   AND H.AlertId = AB.Id
WHERE LOWER(ISNULL(H.Status, '')) IN ('closed', 'close')
ORDER BY H.UpdatedAt DESC;
";

        var result = await connection.QueryAsync<AdminManagerClosedHistoryModel>(sql, new { Take = take });
        return result.ToList();
    }

    public async Task ReassignAlertsAsync(
        AdminManagerReassignRequestModel request,
        CancellationToken cancellationToken = default)
    {
        if (request.Alerts is null || request.Alerts.Count == 0)
        {
            return;
        }

        using var connection = _connectionFactory.CreateConnection();

        foreach (var alert in request.Alerts)
        {
            if (alert.SourceType == "Backup")
            {
                const string backupSql = @"
DECLARE @Affected TABLE
(
    AlertRecordId bigint NOT NULL
);

INSERT INTO @Affected (AlertRecordId)
SELECT B.Id
FROM dbo.AlertasBackup B
WHERE ISNULL(B.Active, 0) = 1
  AND LOWER(ISNULL(B.AssignedEmail, '')) = LOWER(@CurrentAssignedEmail)
  AND ISNULL(NULLIF(B.SubscriptionName, ''), 'Sin cliente') = @ClientName
  AND ISNULL(NULLIF(B.AlertRule, ''), 'Sin nombre') = @AlertName
  AND ISNULL(NULLIF(B.Severity, ''), 'Unknown') = @Severity
  AND COALESCE(NULLIF(B.ResourceName, ''), NULLIF(B.VMName, ''), NULLIF(B.ProtectedItem, ''), 'Sin recurso') = @ResourceName;

INSERT INTO dbo.AlertUpdatesHistory
(
    KPIType,
    AlertId,
    Comment,
    Status,
    UpdatedBy,
    UpdatedAt,
    Res_norm,
    Alert_norm,
    UserEmail
)
SELECT
    'Backup',
    A.AlertRecordId,
    @Comment,
    'REASSIGN',
    @RequestedBy,
    SYSDATETIME(),
    @ResourceName,
    @AlertName,
    @RequestedByEmail
FROM @Affected A;

UPDATE B
SET
    AssignedTo = @NewAssignedTo,
    AssignedEmail = @NewAssignedEmail,
    UpdatedAt = SYSDATETIME(),
    LastUpdatedBy = @RequestedBy
FROM dbo.AlertasBackup B
INNER JOIN @Affected A
    ON A.AlertRecordId = B.Id;
";

                await connection.ExecuteAsync(backupSql, new
                {
                    alert.ClientName,
                    alert.AlertName,
                    alert.Severity,
                    alert.ResourceName,
                    CurrentAssignedEmail = alert.AssignedEmail,
                    request.NewAssignedTo,
                    request.NewAssignedEmail,
                    request.RequestedBy,
                    request.RequestedByEmail,
                    Comment = string.IsNullOrWhiteSpace(request.Comment)
                        ? $"Reasignada de {alert.AssignedTo} a {request.NewAssignedTo}."
                        : request.Comment
                });
            }
            else
            {
                const string managementSql = @"
DECLARE @Affected TABLE
(
    AlertRecordId bigint NOT NULL
);

INSERT INTO @Affected (AlertRecordId)
SELECT A.Id
FROM dbo.AlertsManagement A
WHERE ISNULL(A.Active, 0) = 1
  AND LOWER(ISNULL(A.AssignedEmail, '')) = LOWER(@CurrentAssignedEmail)
  AND ISNULL(NULLIF(A.SubscriptionName, ''), 'Sin cliente') = @ClientName
  AND ISNULL(NULLIF(A.AlertName, ''), 'Sin nombre') = @AlertName
  AND ISNULL(NULLIF(A.Severity, ''), 'Unknown') = @Severity
  AND ISNULL(NULLIF(A.TargetResourceName, ''), 'Sin recurso') = @ResourceName;

INSERT INTO dbo.AlertUpdatesHistory
(
    KPIType,
    AlertId,
    Comment,
    Status,
    UpdatedBy,
    UpdatedAt,
    Res_norm,
    Alert_norm,
    UserEmail
)
SELECT
    'Management',
    A.AlertRecordId,
    @Comment,
    'REASSIGN',
    @RequestedBy,
    SYSDATETIME(),
    @ResourceName,
    @AlertName,
    @RequestedByEmail
FROM @Affected A;

UPDATE M
SET
    AssignedTo = @NewAssignedTo,
    AssignedEmail = @NewAssignedEmail,
    UpdatedAt = SYSDATETIME(),
    LastUpdatedBy = @RequestedBy
FROM dbo.AlertsManagement M
INNER JOIN @Affected A
    ON A.AlertRecordId = M.Id;
";

                await connection.ExecuteAsync(managementSql, new
                {
                    alert.ClientName,
                    alert.AlertName,
                    alert.Severity,
                    alert.ResourceName,
                    CurrentAssignedEmail = alert.AssignedEmail,
                    request.NewAssignedTo,
                    request.NewAssignedEmail,
                    request.RequestedBy,
                    request.RequestedByEmail,
                    Comment = string.IsNullOrWhiteSpace(request.Comment)
                        ? $"Reasignada de {alert.AssignedTo} a {request.NewAssignedTo}."
                        : request.Comment
                });
            }
        }
    }

    public async Task CloseSeverityAsync(
        AdminManagerCloseSeverityRequestModel request,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sourceType = string.IsNullOrWhiteSpace(request.SourceType) || request.SourceType == "All"
            ? "All"
            : request.SourceType;

        if (sourceType is "All" or "Management")
        {
            const string managementSql = @"
DECLARE @ClosedAlerts TABLE
(
    AlertRecordId bigint NOT NULL,
    AzureAlertId nvarchar(1000) NULL,
    TenantId nvarchar(100) NULL,
    SubscriptionId nvarchar(100) NULL,
    AlertName nvarchar(500) NULL,
    ResourceName nvarchar(500) NULL
);

INSERT INTO @ClosedAlerts
(
    AlertRecordId,
    AzureAlertId,
    TenantId,
    SubscriptionId,
    AlertName,
    ResourceName
)
SELECT
    A.Id,
    A.AlertId,
    A.TenantId,
    A.SubscriptionId,
    A.AlertName,
    A.TargetResourceName
FROM dbo.AlertsManagement A
WHERE ISNULL(A.Active, 0) = 1
  AND ISNULL(A.AssignedEmail, '') = ''
  AND ISNULL(NULLIF(A.Severity, ''), 'Unknown') = @Severity
  AND LOWER(ISNULL(A.AlertStatus, '')) NOT IN ('inprogress', 'in progress');

INSERT INTO dbo.AlertUpdatesHistory
(
    KPIType,
    AlertId,
    Comment,
    Status,
    UpdatedBy,
    UpdatedAt,
    Res_norm,
    Alert_norm,
    UserEmail
)
SELECT
    'Management',
    C.AlertRecordId,
    @Comment,
    'Closed',
    @RequestedBy,
    SYSDATETIME(),
    C.ResourceName,
    C.AlertName,
    @RequestedByEmail
FROM @ClosedAlerts C;

INSERT INTO dbo.AzureAlertCloseQueue
(
    KPIType,
    SourceTable,
    AlertRecordId,
    AzureAlertId,
    TenantId,
    SubscriptionId,
    UserEmail,
    UserName,
    Comment,
    RequestedAt,
    Status,
    RetryCount
)
SELECT
    'Management',
    'AlertsManagement',
    C.AlertRecordId,
    C.AzureAlertId,
    C.TenantId,
    C.SubscriptionId,
    @RequestedByEmail,
    @RequestedBy,
    @Comment,
    SYSDATETIME(),
    'Pending',
    0
FROM @ClosedAlerts C;

UPDATE A
SET
    Active = 0,
    AlertStatus = 'Closed',
    ResolveTime = SYSDATETIME(),
    ResolutionNotes = @Comment,
    LastUpdatedBy = @RequestedBy,
    UpdatedAt = SYSDATETIME()
FROM dbo.AlertsManagement A
INNER JOIN @ClosedAlerts C
    ON C.AlertRecordId = A.Id;
";

            await connection.ExecuteAsync(managementSql, new
            {
                request.Severity,
                request.RequestedBy,
                request.RequestedByEmail,
                Comment = string.IsNullOrWhiteSpace(request.Comment)
                    ? $"Cierre masivo por severidad {request.Severity} desde Admin / Manager View."
                    : request.Comment
            });
        }

        if (sourceType is "All" or "Backup")
        {
            const string backupSql = @"
DECLARE @ClosedAlerts TABLE
(
    AlertRecordId bigint NOT NULL,
    AzureAlertId nvarchar(1000) NULL,
    TenantId nvarchar(100) NULL,
    SubscriptionId nvarchar(100) NULL,
    AlertName nvarchar(500) NULL,
    ResourceName nvarchar(500) NULL
);

INSERT INTO @ClosedAlerts
(
    AlertRecordId,
    AzureAlertId,
    TenantId,
    SubscriptionId,
    AlertName,
    ResourceName
)
SELECT
    B.Id,
    NULL,
    B.TenantId,
    B.SubscriptionId,
    B.AlertRule,
    COALESCE(NULLIF(B.ResourceName, ''), NULLIF(B.VMName, ''), NULLIF(B.ProtectedItem, ''), '')
FROM dbo.AlertasBackup B
WHERE ISNULL(B.Active, 0) = 1
  AND ISNULL(B.AssignedEmail, '') = ''
  AND ISNULL(NULLIF(B.Severity, ''), 'Unknown') = @Severity;

INSERT INTO dbo.AlertUpdatesHistory
(
    KPIType,
    AlertId,
    Comment,
    Status,
    UpdatedBy,
    UpdatedAt,
    Res_norm,
    Alert_norm,
    UserEmail
)
SELECT
    'Backup',
    C.AlertRecordId,
    @Comment,
    'Closed',
    @RequestedBy,
    SYSDATETIME(),
    C.ResourceName,
    C.AlertName,
    @RequestedByEmail
FROM @ClosedAlerts C;

INSERT INTO dbo.AzureAlertCloseQueue
(
    KPIType,
    SourceTable,
    AlertRecordId,
    AzureAlertId,
    TenantId,
    SubscriptionId,
    UserEmail,
    UserName,
    Comment,
    RequestedAt,
    Status,
    RetryCount
)
SELECT
    'Backup',
    'AlertasBackup',
    C.AlertRecordId,
    C.AzureAlertId,
    C.TenantId,
    C.SubscriptionId,
    @RequestedByEmail,
    @RequestedBy,
    @Comment,
    SYSDATETIME(),
    'Pending',
    0
FROM @ClosedAlerts C;

UPDATE B
SET
    Active = 0,
    ResolveTime = SYSDATETIME(),
    ResolutionNotes = @Comment,
    LastUpdatedBy = @RequestedBy,
    UpdatedAt = SYSDATETIME()
FROM dbo.AlertasBackup B
INNER JOIN @ClosedAlerts C
    ON C.AlertRecordId = B.Id;
";

            await connection.ExecuteAsync(backupSql, new
            {
                request.Severity,
                request.RequestedBy,
                request.RequestedByEmail,
                Comment = string.IsNullOrWhiteSpace(request.Comment)
                    ? $"Cierre masivo por severidad {request.Severity} desde Admin / Manager View."
                    : request.Comment
            });
        }
    }
}

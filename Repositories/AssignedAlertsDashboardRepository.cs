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
DECLARE @TodayStart datetime2 = CONVERT(date, SYSDATETIME());
DECLARE @TomorrowStart datetime2 = DATEADD(day, 1, @TodayStart);

;WITH Management AS
(
    SELECT
        Total = COUNT_BIG(1),
        Critical = SUM(CASE WHEN UPPER(ISNULL(Severity, '')) IN ('CRITICAL', 'HIGH', 'SEV0', 'SEV1') THEN 1 ELSE 0 END),
        NewToday = SUM(CASE WHEN ISNULL(UpdatedAt, InsertedAt) >= @TodayStart AND ISNULL(UpdatedAt, InsertedAt) < @TomorrowStart THEN 1 ELSE 0 END)
    FROM dbo.AlertsManagement
    WHERE Active = 1
      AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '')
),
Backup AS
(
    SELECT
        Total = COUNT_BIG(1),
        Critical = SUM(CASE WHEN UPPER(ISNULL(Severity, '')) IN ('CRITICAL', 'HIGH', 'SEV0', 'SEV1') THEN 1 ELSE 0 END),
        NewToday = SUM(CASE WHEN ISNULL(UpdatedAt, InsertedAt) >= @TodayStart AND ISNULL(UpdatedAt, InsertedAt) < @TomorrowStart THEN 1 ELSE 0 END)
    FROM dbo.AlertasBackup
    WHERE Active = 1
      AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '')
)
SELECT
    TotalAlerts = CAST(ISNULL(M.Total, 0) + ISNULL(B.Total, 0) AS int),
    ManagementAlerts = CAST(ISNULL(M.Total, 0) AS int),
    BackupAlerts = CAST(ISNULL(B.Total, 0) AS int),
    UnassignedAlerts = CAST(ISNULL(M.Total, 0) + ISNULL(B.Total, 0) AS int),
    CriticalAlerts = CAST(ISNULL(M.Critical, 0) + ISNULL(B.Critical, 0) AS int),
    NewToday = CAST(ISNULL(M.NewToday, 0) + ISNULL(B.NewToday, 0) AS int)
FROM Management M
CROSS JOIN Backup B;
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
WHERE Active = 1
  AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '')
ORDER BY ClientName;"
            : @"
SELECT DISTINCT
    ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') AS ClientName
FROM dbo.AlertsManagement
WHERE Active = 1
  AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '')
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

        const string sql = @"
;WITH BaseAlerts AS
(
    SELECT
        Id,
        SubscriptionName,
        AlertName,
        Severity,
        TargetResourceName,
        UpdatedAt,
        InsertedAt
    FROM dbo.AlertsManagement
    WHERE Active = 1
      AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '')
      AND (@ClientName IS NULL OR ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') = @ClientName)
      AND (
            @Search IS NULL
            OR SubscriptionName LIKE @Search
            OR AlertName LIKE @Search
            OR Severity LIKE @Search
            OR TargetResourceName LIKE @Search
          )
),
GroupedAlerts AS
(
    SELECT
        SourceType = 'Management',
        Id = MIN(CAST(Id AS bigint)),
        ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        AlertName = ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
        Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
        AlertType = 'Management',
        ResourceName = ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso'),
        Events = COUNT_BIG(1),
        LastEventAt = MAX(ISNULL(UpdatedAt, InsertedAt)),
        AssignedTo = '',
        AssignedEmail = '',
        AlertStatus = 'Activa'
    FROM BaseAlerts
    GROUP BY
        ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
        ISNULL(NULLIF(Severity, ''), 'Unknown'),
        ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso')
)
SELECT COUNT(1) FROM GroupedAlerts;

;WITH BaseAlerts AS
(
    SELECT
        Id,
        SubscriptionName,
        AlertName,
        Severity,
        TargetResourceName,
        UpdatedAt,
        InsertedAt
    FROM dbo.AlertsManagement
    WHERE Active = 1
      AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '')
      AND (@ClientName IS NULL OR ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') = @ClientName)
      AND (
            @Search IS NULL
            OR SubscriptionName LIKE @Search
            OR AlertName LIKE @Search
            OR Severity LIKE @Search
            OR TargetResourceName LIKE @Search
          )
),
GroupedAlerts AS
(
    SELECT
        SourceType = 'Management',
        Id = MIN(CAST(Id AS bigint)),
        ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        AlertName = ISNULL(NULLIF(AlertName, ''), 'Sin nombre'),
        Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
        AlertType = 'Management',
        ResourceName = ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso'),
        Events = COUNT_BIG(1),
        LastEventAt = MAX(ISNULL(UpdatedAt, InsertedAt)),
        AssignedTo = '',
        AssignedEmail = '',
        AlertStatus = 'Activa'
    FROM BaseAlerts
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
    Events = CAST(Events AS int),
    LastEventAt,
    AlertStatus,
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

        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        var totalCount = await multi.ReadFirstOrDefaultAsync<int>();
        var items = await multi.ReadAsync<DashboardAlertItemModel>();

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

        const string sql = @"
;WITH BaseAlerts AS
(
    SELECT
        Id,
        SubscriptionName,
        AlertRule,
        Severity,
        ResourceName,
        VMName,
        ProtectedItem,
        UpdatedAt,
        InsertedAt
    FROM dbo.AlertasBackup
    WHERE Active = 1
      AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '')
      AND (@ClientName IS NULL OR ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') = @ClientName)
      AND (
            @Search IS NULL
            OR SubscriptionName LIKE @Search
            OR AlertRule LIKE @Search
            OR Severity LIKE @Search
            OR ResourceName LIKE @Search
            OR VMName LIKE @Search
            OR ProtectedItem LIKE @Search
          )
),
GroupedAlerts AS
(
    SELECT
        SourceType = 'Backup',
        Id = MIN(CAST(Id AS bigint)),
        ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        AlertName = ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
        Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
        AlertType = 'Backup',
        ResourceName = COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso'),
        Events = COUNT_BIG(1),
        LastEventAt = MAX(ISNULL(UpdatedAt, InsertedAt)),
        AssignedTo = '',
        AssignedEmail = '',
        AlertStatus = 'Activa'
    FROM BaseAlerts
    GROUP BY
        ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
        ISNULL(NULLIF(Severity, ''), 'Unknown'),
        COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso')
)
SELECT COUNT(1) FROM GroupedAlerts;

;WITH BaseAlerts AS
(
    SELECT
        Id,
        SubscriptionName,
        AlertRule,
        Severity,
        ResourceName,
        VMName,
        ProtectedItem,
        UpdatedAt,
        InsertedAt
    FROM dbo.AlertasBackup
    WHERE Active = 1
      AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '')
      AND (@ClientName IS NULL OR ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') = @ClientName)
      AND (
            @Search IS NULL
            OR SubscriptionName LIKE @Search
            OR AlertRule LIKE @Search
            OR Severity LIKE @Search
            OR ResourceName LIKE @Search
            OR VMName LIKE @Search
            OR ProtectedItem LIKE @Search
          )
),
GroupedAlerts AS
(
    SELECT
        SourceType = 'Backup',
        Id = MIN(CAST(Id AS bigint)),
        ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente'),
        AlertName = ISNULL(NULLIF(AlertRule, ''), 'Sin nombre'),
        Severity = ISNULL(NULLIF(Severity, ''), 'Unknown'),
        AlertType = 'Backup',
        ResourceName = COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso'),
        Events = COUNT_BIG(1),
        LastEventAt = MAX(ISNULL(UpdatedAt, InsertedAt)),
        AssignedTo = '',
        AssignedEmail = '',
        AlertStatus = 'Activa'
    FROM BaseAlerts
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
    Events = CAST(Events AS int),
    LastEventAt,
    AlertStatus,
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

        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        var totalCount = await multi.ReadFirstOrDefaultAsync<int>();
        var items = await multi.ReadAsync<DashboardAlertItemModel>();

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
    UpdatedAt = SYSDATETIME()
WHERE Active = 1
  AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '')
  AND ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') = @ClientName
  AND ISNULL(NULLIF(AlertName, ''), 'Sin nombre') = @AlertName
  AND ISNULL(NULLIF(Severity, ''), 'Unknown') = @Severity
  AND ISNULL(NULLIF(TargetResourceName, ''), 'Sin recurso') = @ResourceName;";

        using var transaction = connection.BeginTransaction();

        try
        {
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
                }, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
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
    UpdatedAt = SYSDATETIME()
WHERE Active = 1
  AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '')
  AND ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente') = @ClientName
  AND ISNULL(NULLIF(AlertRule, ''), 'Sin nombre') = @AlertName
  AND ISNULL(NULLIF(Severity, ''), 'Unknown') = @Severity
  AND COALESCE(NULLIF(ResourceName, ''), NULLIF(VMName, ''), NULLIF(ProtectedItem, ''), 'Sin recurso') = @ResourceName;";

        using var transaction = connection.BeginTransaction();

        try
        {
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
                }, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<AlertDetailModel?> GetAlertDetailAsync(
        DashboardAlertItemModel alert,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        if (alert.SourceType == "Backup")
        {
            const string sql = @"
;WITH LastHistory AS
(
    SELECT
        H.AlertId,
        H.Status,
        rn = ROW_NUMBER() OVER (PARTITION BY H.KPIType, H.AlertId ORDER BY H.UpdatedAt DESC)
    FROM dbo.AlertUpdatesHistory H
    WHERE H.KPIType = 'Backup'
      AND H.AlertId = @AlertId
)
SELECT TOP 1
    SourceType = 'Backup',
    AlertId = CAST(B.Id AS bigint),
    ClientName = ISNULL(B.SubscriptionName, ''),
    SubscriptionName = ISNULL(B.SubscriptionName, ''),
    SubscriptionId = ISNULL(B.SubscriptionId, ''),
    TenantId = ISNULL(B.TenantId, ''),
    AlertName = ISNULL(B.AlertRule, ''),
    Severity = ISNULL(B.Severity, ''),
    AlertStatus =
        CASE
            WHEN B.Active = 0 THEN 'Closed'
            WHEN LOWER(ISNULL(H.Status, '')) IN ('closed', 'close') THEN 'Closed'
            WHEN LOWER(ISNULL(H.Status, '')) IN ('inprogress', 'in progress', 'update_note') THEN 'InProgress'
            ELSE 'Activa'
        END,
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
    LastOccurrence = ISNULL(B.UpdatedAt, B.InsertedAt),
    AssignedTo = ISNULL(B.AssignedTo, ''),
    AssignedEmail = ISNULL(B.AssignedEmail, ''),
    ResolutionNotes = ISNULL(B.ResolutionNotes, '')
FROM dbo.AlertasBackup B
LEFT JOIN LastHistory H
    ON H.AlertId = B.Id
   AND H.rn = 1
WHERE B.Id = @AlertId;

SELECT
    HistoryId,
    KPIType,
    AlertId = CAST(ISNULL(AlertId, 0) AS bigint),
    Comment = ISNULL(Comment, ''),
    Status =
        CASE
            WHEN LOWER(ISNULL(Status, '')) IN ('closed', 'close') THEN 'Closed'
            WHEN LOWER(ISNULL(Status, '')) IN ('inprogress', 'in progress', 'update_note') THEN 'InProgress'
            ELSE ISNULL(Status, '')
        END,
    UpdatedBy = ISNULL(UpdatedBy, ''),
    UserEmail = ISNULL(UserEmail, ''),
    UpdatedAt
FROM dbo.AlertUpdatesHistory
WHERE KPIType = 'Backup'
  AND AlertId = @AlertId
ORDER BY UpdatedAt DESC;
";

            using var multi = await connection.QueryMultipleAsync(sql, new
            {
                AlertId = alert.Id,
                alert.Events
            });

            var detail = await multi.ReadFirstOrDefaultAsync<AlertDetailModel>();

            if (detail is not null)
            {
                detail.History = (await multi.ReadAsync<AlertHistoryItemModel>()).ToList();
            }

            return detail;
        }

        const string managementSql = @"
;WITH LastHistory AS
(
    SELECT
        H.AlertId,
        H.Status,
        rn = ROW_NUMBER() OVER (PARTITION BY H.KPIType, H.AlertId ORDER BY H.UpdatedAt DESC)
    FROM dbo.AlertUpdatesHistory H
    WHERE H.KPIType = 'Management'
      AND H.AlertId = @AlertId
)
SELECT TOP 1
    SourceType = 'Management',
    AlertId = CAST(A.Id AS bigint),
    ClientName = ISNULL(A.SubscriptionName, ''),
    SubscriptionName = ISNULL(A.SubscriptionName, ''),
    SubscriptionId = ISNULL(A.SubscriptionId, ''),
    TenantId = ISNULL(A.TenantId, ''),
    AlertName = ISNULL(A.AlertName, ''),
    Severity = ISNULL(A.Severity, ''),
    AlertStatus =
        CASE
            WHEN A.Active = 0 THEN 'Closed'
            WHEN LOWER(ISNULL(H.Status, '')) IN ('closed', 'close') THEN 'Closed'
            WHEN LOWER(ISNULL(A.AlertStatus, '')) IN ('closed', 'close') THEN 'Closed'
            WHEN LOWER(ISNULL(H.Status, '')) IN ('inprogress', 'in progress', 'update_note') THEN 'InProgress'
            WHEN LOWER(ISNULL(A.AlertStatus, '')) IN ('inprogress', 'in progress') THEN 'InProgress'
            ELSE 'Activa'
        END,
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
    LastOccurrence = ISNULL(A.UpdatedAt, A.InsertedAt),
    AssignedTo = ISNULL(A.AssignedTo, ''),
    AssignedEmail = ISNULL(A.AssignedEmail, ''),
    ResolutionNotes = ISNULL(A.ResolutionNotes, '')
FROM dbo.AlertsManagement A
LEFT JOIN dbo.ITQS_CustomerInventory_Current I
    ON I.SubscriptionId = A.SubscriptionId
   AND I.Name = A.TargetResourceName
LEFT JOIN LastHistory H
    ON H.AlertId = A.Id
   AND H.rn = 1
WHERE A.Id = @AlertId;

SELECT
    HistoryId,
    KPIType,
    AlertId = CAST(ISNULL(AlertId, 0) AS bigint),
    Comment = ISNULL(Comment, ''),
    Status =
        CASE
            WHEN LOWER(ISNULL(Status, '')) IN ('closed', 'close') THEN 'Closed'
            WHEN LOWER(ISNULL(Status, '')) IN ('inprogress', 'in progress', 'update_note') THEN 'InProgress'
            ELSE ISNULL(Status, '')
        END,
    UpdatedBy = ISNULL(UpdatedBy, ''),
    UserEmail = ISNULL(UserEmail, ''),
    UpdatedAt
FROM dbo.AlertUpdatesHistory
WHERE KPIType = 'Management'
  AND AlertId = @AlertId
ORDER BY UpdatedAt DESC;
";

        using var managementMulti = await connection.QueryMultipleAsync(managementSql, new
        {
            AlertId = alert.Id,
            alert.Events
        });

        var managementDetail = await managementMulti.ReadFirstOrDefaultAsync<AlertDetailModel>();

        if (managementDetail is not null)
        {
            managementDetail.History = (await managementMulti.ReadAsync<AlertHistoryItemModel>()).ToList();
        }

        return managementDetail;
    }

    public async Task SaveAlertCommentAsync(
        AlertCommentRequestModel request,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        string sql;

        if (request.SourceType == "Backup")
        {
            sql = @"
UPDATE dbo.AlertasBackup
SET
    UpdatedAt = SYSDATETIME(),
    LastUpdatedBy = @UpdatedBy
WHERE Id = @AlertId;

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
VALUES
(
    @KPIType,
    @AlertId,
    @Comment,
    @Status,
    @UpdatedBy,
    SYSDATETIME(),
    @ResourceName,
    @AlertName,
    @UserEmail
);";
        }
        else
        {
            sql = @"
UPDATE dbo.AlertsManagement
SET
    AlertStatus = @Status,
    UpdatedAt = SYSDATETIME(),
    LastUpdatedBy = @UpdatedBy
WHERE Id = @AlertId;

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
VALUES
(
    @KPIType,
    @AlertId,
    @Comment,
    @Status,
    @UpdatedBy,
    SYSDATETIME(),
    @ResourceName,
    @AlertName,
    @UserEmail
);";
        }

        try
        {
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
            }, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task CloseAlertAsync(
        AlertCommentRequestModel request,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        string sql;

        if (request.SourceType == "Backup")
        {
            sql = @"
DECLARE @ClosedAlerts TABLE
(
    AlertRecordId bigint NOT NULL,
    AzureAlertId nvarchar(1000) NULL,
    TenantId nvarchar(100) NULL,
    SubscriptionId nvarchar(100) NULL
);

INSERT INTO @ClosedAlerts
(
    AlertRecordId,
    AzureAlertId,
    TenantId,
    SubscriptionId
)
SELECT
    B.Id,
    B.AzureAlertId,
    B.TenantId,
    B.SubscriptionId
FROM dbo.AlertasBackup B
WHERE B.Active = 1
  AND B.AssignedEmail = @UserEmail
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
    @KPIType,
    C.AlertRecordId,
    @Comment,
    'Closed',
    @UpdatedBy,
    SYSDATETIME(),
    @ResourceName,
    @AlertName,
    @UserEmail
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
    @KPIType,
    'AlertasBackup',
    C.AlertRecordId,
    C.AzureAlertId,
    C.TenantId,
    C.SubscriptionId,
    @UserEmail,
    @UpdatedBy,
    @Comment,
    SYSDATETIME(),
    'Pending',
    0
FROM @ClosedAlerts C
WHERE C.AzureAlertId IS NOT NULL
  AND LTRIM(RTRIM(C.AzureAlertId)) <> ''
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.AzureAlertCloseQueue Q
      WHERE Q.AzureAlertId = C.AzureAlertId
        AND Q.Status IN ('Pending', 'Processing', 'Processed', 'NotFound')
  );

UPDATE B
SET
    B.Active = 0,
    B.ResolveTime = SYSDATETIME(),
    B.ResolutionNotes = @Comment,
    B.LastUpdatedBy = @UpdatedBy,
    B.UpdatedAt = SYSDATETIME()
FROM dbo.AlertasBackup B
INNER JOIN @ClosedAlerts C
    ON C.AlertRecordId = B.Id;";
        }
        else
        {
            sql = @"
DECLARE @ClosedAlerts TABLE
(
    AlertRecordId bigint NOT NULL,
    AzureAlertId nvarchar(1000) NULL,
    TenantId nvarchar(100) NULL,
    SubscriptionId nvarchar(100) NULL
);

INSERT INTO @ClosedAlerts
(
    AlertRecordId,
    AzureAlertId,
    TenantId,
    SubscriptionId
)
SELECT
    A.Id,
    A.AlertId,
    A.TenantId,
    A.SubscriptionId
FROM dbo.AlertsManagement A
WHERE A.Active = 1
  AND A.AssignedEmail = @UserEmail
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
    @KPIType,
    C.AlertRecordId,
    @Comment,
    'Closed',
    @UpdatedBy,
    SYSDATETIME(),
    @ResourceName,
    @AlertName,
    @UserEmail
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
    @KPIType,
    'AlertsManagement',
    C.AlertRecordId,
    C.AzureAlertId,
    C.TenantId,
    C.SubscriptionId,
    @UserEmail,
    @UpdatedBy,
    @Comment,
    SYSDATETIME(),
    'Pending',
    0
FROM @ClosedAlerts C
WHERE C.AzureAlertId IS NOT NULL
  AND LTRIM(RTRIM(C.AzureAlertId)) <> ''
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.AzureAlertCloseQueue Q
      WHERE Q.AzureAlertId = C.AzureAlertId
        AND Q.Status IN ('Pending', 'Processing', 'Processed', 'NotFound')
  );

UPDATE A
SET
    A.Active = 0,
    A.AlertStatus = 'Closed',
    A.ResolveTime = SYSDATETIME(),
    A.ResolutionNotes = @Comment,
    A.LastUpdatedBy = @UpdatedBy,
    A.UpdatedAt = SYSDATETIME()
FROM dbo.AlertsManagement A
INNER JOIN @ClosedAlerts C
    ON C.AlertRecordId = A.Id;";
        }

        try
        {
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
            }, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<DashboardAlertPagedResultModel> GetAssignedAlertsAsync(
        string userEmail,
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

        const string sql = @"
;WITH ManagementBase AS
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
        BaseStatus =
            CASE
                WHEN LOWER(ISNULL(A.AlertStatus, '')) IN ('closed', 'close') THEN 'Closed'
                WHEN LOWER(ISNULL(A.AlertStatus, '')) IN ('inprogress', 'in progress') THEN 'InProgress'
                ELSE 'Activa'
            END
    FROM dbo.AlertsManagement A
    WHERE A.Active = 1
      AND A.AssignedEmail = @UserEmail
),
BackupBase AS
(
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
        BaseStatus = 'Activa'
    FROM dbo.AlertasBackup B
    WHERE B.Active = 1
      AND B.AssignedEmail = @UserEmail
),
AssignedAlerts AS
(
    SELECT * FROM ManagementBase
    UNION ALL
    SELECT * FROM BackupBase
),
LatestHistory AS
(
    SELECT
        H.KPIType,
        H.AlertId,
        H.Status,
        rn = ROW_NUMBER() OVER (PARTITION BY H.KPIType, H.AlertId ORDER BY H.UpdatedAt DESC)
    FROM dbo.AlertUpdatesHistory H
    INNER JOIN AssignedAlerts A
        ON A.SourceType = H.KPIType
       AND A.Id = H.AlertId
),
PreparedAlerts AS
(
    SELECT
        A.SourceType,
        A.Id,
        A.ClientName,
        A.AlertName,
        A.Severity,
        A.AlertType,
        A.ResourceName,
        A.LastEventAt,
        A.AssignedTo,
        A.AssignedEmail,
        AlertStatus =
            CASE
                WHEN LOWER(ISNULL(H.Status, '')) IN ('closed', 'close') THEN 'Closed'
                WHEN A.BaseStatus = 'Closed' THEN 'Closed'
                WHEN LOWER(ISNULL(H.Status, '')) IN ('inprogress', 'in progress', 'update_note') THEN 'InProgress'
                WHEN A.BaseStatus = 'InProgress' THEN 'InProgress'
                ELSE 'Activa'
            END
    FROM AssignedAlerts A
    LEFT JOIN LatestHistory H
        ON H.KPIType = A.SourceType
       AND H.AlertId = A.Id
       AND H.rn = 1
    WHERE
        (@ClientName IS NULL OR A.ClientName = @ClientName)
        AND
        (
            @Search IS NULL
            OR A.ClientName LIKE @Search
            OR A.AlertName LIKE @Search
            OR A.Severity LIKE @Search
            OR A.ResourceName LIKE @Search
            OR A.SourceType LIKE @Search
        )
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
        Events = COUNT_BIG(1),
        LastEventAt = MAX(LastEventAt),
        AlertStatus =
            CASE
                WHEN SUM(CASE WHEN AlertStatus = 'InProgress' THEN 1 ELSE 0 END) > 0 THEN 'InProgress'
                ELSE 'Activa'
            END,
        AssignedTo = MAX(AssignedTo),
        AssignedEmail = MAX(AssignedEmail)
    FROM PreparedAlerts
    GROUP BY
        SourceType,
        ClientName,
        AlertName,
        Severity,
        AlertType,
        ResourceName
)
SELECT COUNT(1) FROM GroupedAlerts;

;WITH ManagementBase AS
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
        BaseStatus =
            CASE
                WHEN LOWER(ISNULL(A.AlertStatus, '')) IN ('closed', 'close') THEN 'Closed'
                WHEN LOWER(ISNULL(A.AlertStatus, '')) IN ('inprogress', 'in progress') THEN 'InProgress'
                ELSE 'Activa'
            END
    FROM dbo.AlertsManagement A
    WHERE A.Active = 1
      AND A.AssignedEmail = @UserEmail
),
BackupBase AS
(
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
        BaseStatus = 'Activa'
    FROM dbo.AlertasBackup B
    WHERE B.Active = 1
      AND B.AssignedEmail = @UserEmail
),
AssignedAlerts AS
(
    SELECT * FROM ManagementBase
    UNION ALL
    SELECT * FROM BackupBase
),
LatestHistory AS
(
    SELECT
        H.KPIType,
        H.AlertId,
        H.Status,
        rn = ROW_NUMBER() OVER (PARTITION BY H.KPIType, H.AlertId ORDER BY H.UpdatedAt DESC)
    FROM dbo.AlertUpdatesHistory H
    INNER JOIN AssignedAlerts A
        ON A.SourceType = H.KPIType
       AND A.Id = H.AlertId
),
PreparedAlerts AS
(
    SELECT
        A.SourceType,
        A.Id,
        A.ClientName,
        A.AlertName,
        A.Severity,
        A.AlertType,
        A.ResourceName,
        A.LastEventAt,
        A.AssignedTo,
        A.AssignedEmail,
        AlertStatus =
            CASE
                WHEN LOWER(ISNULL(H.Status, '')) IN ('closed', 'close') THEN 'Closed'
                WHEN A.BaseStatus = 'Closed' THEN 'Closed'
                WHEN LOWER(ISNULL(H.Status, '')) IN ('inprogress', 'in progress', 'update_note') THEN 'InProgress'
                WHEN A.BaseStatus = 'InProgress' THEN 'InProgress'
                ELSE 'Activa'
            END
    FROM AssignedAlerts A
    LEFT JOIN LatestHistory H
        ON H.KPIType = A.SourceType
       AND H.AlertId = A.Id
       AND H.rn = 1
    WHERE
        (@ClientName IS NULL OR A.ClientName = @ClientName)
        AND
        (
            @Search IS NULL
            OR A.ClientName LIKE @Search
            OR A.AlertName LIKE @Search
            OR A.Severity LIKE @Search
            OR A.ResourceName LIKE @Search
            OR A.SourceType LIKE @Search
        )
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
        Events = COUNT_BIG(1),
        LastEventAt = MAX(LastEventAt),
        AlertStatus =
            CASE
                WHEN SUM(CASE WHEN AlertStatus = 'InProgress' THEN 1 ELSE 0 END) > 0 THEN 'InProgress'
                ELSE 'Activa'
            END,
        AssignedTo = MAX(AssignedTo),
        AssignedEmail = MAX(AssignedEmail)
    FROM PreparedAlerts
    GROUP BY
        SourceType,
        ClientName,
        AlertName,
        Severity,
        AlertType,
        ResourceName
)
SELECT
    SourceType,
    Id,
    ClientName,
    AlertName,
    Severity,
    AlertType,
    ResourceName,
    Events = CAST(Events AS int),
    LastEventAt,
    AlertStatus,
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
            UserEmail = userEmail,
            Search = searchValue,
            ClientName = clientValue,
            Offset = offset,
            PageSize = pageSize
        };

        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        var totalCount = await multi.ReadFirstOrDefaultAsync<int>();
        var items = await multi.ReadAsync<DashboardAlertItemModel>();

        return new DashboardAlertPagedResultModel
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<string>> GetAssignedClientsAsync(
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
SELECT DISTINCT ClientName
FROM
(
    SELECT
        ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente')
    FROM dbo.AlertsManagement
    WHERE Active = 1
      AND AssignedEmail = @UserEmail

    UNION ALL

    SELECT
        ClientName = ISNULL(NULLIF(SubscriptionName, ''), 'Sin cliente')
    FROM dbo.AlertasBackup
    WHERE Active = 1
      AND AssignedEmail = @UserEmail
) X
ORDER BY ClientName;
";

        var result = await connection.QueryAsync<string>(
            sql,
            new { UserEmail = userEmail });

        return result.ToList();
    }
}

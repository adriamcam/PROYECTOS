USE REPORTES;
GO

CREATE OR ALTER PROCEDURE dbo.sp_App_GetAlertDashboard
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        TotalActive = SUM(CASE WHEN Active = 1 THEN 1 ELSE 0 END),
        AssignedToMe = SUM(CASE WHEN Active = 1 AND AssignedEmail = @UserEmail THEN 1 ELSE 0 END),
        Unassigned = SUM(CASE WHEN Active = 1 AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '') THEN 1 ELSE 0 END),
        Critical = SUM(CASE WHEN Active = 1 AND Severity IN ('Sev0','Sev1') THEN 1 ELSE 0 END),
        High = SUM(CASE WHEN Active = 1 AND Severity = 'Sev2' THEN 1 ELSE 0 END),
        PendingClose = (
            SELECT COUNT(1)
            FROM dbo.AzureAlertCloseQueue q
            WHERE q.Status = 'Pending'
        )
    FROM dbo.AlertsManagement;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_App_GetAssignedAlerts
    @UserEmail NVARCHAR(256),
    @PageNumber INT = 1,
    @PageSize INT = 50,
    @Search NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @PageSize = CASE WHEN @PageSize > 50 THEN 50 WHEN @PageSize < 1 THEN 50 ELSE @PageSize END;
    SET @PageNumber = CASE WHEN @PageNumber < 1 THEN 1 ELSE @PageNumber END;

    SELECT
        Id,
        AlertId,
        CustomerName,
        SubscriptionName,
        AlertName,
        KPIType,
        ResourceName,
        Severity,
        Events,
        AssignedTo,
        AssignedEmail,
        LastInsertedAt
    FROM dbo.AlertsManagement
    WHERE Active = 1
      AND AssignedEmail = @UserEmail
      AND (
            @Search IS NULL OR @Search = ''
            OR CustomerName LIKE '%' + @Search + '%'
            OR SubscriptionName LIKE '%' + @Search + '%'
            OR AlertName LIKE '%' + @Search + '%'
            OR ResourceName LIKE '%' + @Search + '%'
          )
    ORDER BY LastInsertedAt DESC, Id DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_App_GetUnassignedAlerts
    @PageNumber INT = 1,
    @PageSize INT = 50,
    @Search NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @PageSize = CASE WHEN @PageSize > 50 THEN 50 WHEN @PageSize < 1 THEN 50 ELSE @PageSize END;
    SET @PageNumber = CASE WHEN @PageNumber < 1 THEN 1 ELSE @PageNumber END;

    SELECT
        Id,
        AlertId,
        CustomerName,
        SubscriptionName,
        AlertName,
        KPIType,
        ResourceName,
        Severity,
        Events,
        AssignedTo,
        AssignedEmail,
        LastInsertedAt
    FROM dbo.AlertsManagement
    WHERE Active = 1
      AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '')
      AND (
            @Search IS NULL OR @Search = ''
            OR CustomerName LIKE '%' + @Search + '%'
            OR SubscriptionName LIKE '%' + @Search + '%'
            OR AlertName LIKE '%' + @Search + '%'
            OR ResourceName LIKE '%' + @Search + '%'
          )
    ORDER BY LastInsertedAt DESC, Id DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_App_GetAlertDetail
    @Id BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        AlertId,
        CustomerName,
        SubscriptionName,
        AlertName,
        KPIType,
        ResourceName,
        Severity,
        Events,
        AssignedTo,
        AssignedEmail,
        LastInsertedAt
    FROM dbo.AlertsManagement
    WHERE Id = @Id;

    SELECT TOP (50)
        Id,
        AlertRecordId,
        Action,
        UserEmail,
        Comment,
        CreatedAt
    FROM dbo.AlertUpdatesHistory
    WHERE AlertRecordId = @Id
    ORDER BY CreatedAt DESC, Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_App_AssignAlert
    @Id BIGINT,
    @UserName NVARCHAR(256),
    @UserEmail NVARCHAR(256),
    @Comment NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.AlertsManagement
    SET AssignedTo = @UserName,
        AssignedEmail = @UserEmail,
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id
      AND Active = 1;

    INSERT INTO dbo.AlertUpdatesHistory
    (
        AlertRecordId,
        Action,
        UserEmail,
        Comment,
        CreatedAt
    )
    VALUES
    (
        @Id,
        'Assigned',
        @UserEmail,
        ISNULL(@Comment, 'Asignada desde ITQS SOC'),
        SYSUTCDATETIME()
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_App_CloseAlert
    @Id BIGINT,
    @UserName NVARCHAR(256),
    @UserEmail NVARCHAR(256),
    @Comment NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.AlertsManagement
    SET Active = 0,
        ResolveTime = SYSUTCDATETIME(),
        ResolutionNotes = ISNULL(@Comment, 'Cierre desde ITQS SOC'),
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id;

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
        Status
    )
    SELECT
        m.KPIType,
        'AlertsManagement',
        m.Id,
        m.AlertId,
        m.TenantId,
        m.SubscriptionId,
        @UserEmail,
        @UserName,
        ISNULL(@Comment, 'Cierre desde ITQS SOC'),
        'Pending'
    FROM dbo.AlertsManagement m
    WHERE m.Id = @Id
      AND m.AlertId IS NOT NULL
      AND LTRIM(RTRIM(m.AlertId)) <> ''
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.AzureAlertCloseQueue q
          WHERE q.SourceTable = 'AlertsManagement'
            AND q.AlertRecordId = m.Id
            AND q.Status = 'Pending'
      );

    INSERT INTO dbo.AlertUpdatesHistory
    (
        AlertRecordId,
        Action,
        UserEmail,
        Comment,
        CreatedAt
    )
    VALUES
    (
        @Id,
        'Closed',
        @UserEmail,
        ISNULL(@Comment, 'Cierre desde ITQS SOC'),
        SYSUTCDATETIME()
    );
END;
GO

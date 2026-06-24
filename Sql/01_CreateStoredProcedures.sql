USE [REPORTES];
GO

CREATE OR ALTER PROCEDURE dbo.sp_App_GetAlertDashboard
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        TotalActive = COUNT_BIG(1),
        AssignedToMe = SUM(CASE WHEN AssignedEmail = @UserEmail THEN 1 ELSE 0 END),
        Unassigned = SUM(CASE WHEN AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '' THEN 1 ELSE 0 END),
        Critical = SUM(CASE WHEN Severity IN ('Sev0','Critical','Crítica','Critica','4') THEN 1 ELSE 0 END),
        High = SUM(CASE WHEN Severity IN ('Sev1','High','Alta','3') THEN 1 ELSE 0 END),
        MediumLow = SUM(CASE WHEN Severity NOT IN ('Sev0','Critical','Crítica','Critica','4','Sev1','High','Alta','3') OR Severity IS NULL THEN 1 ELSE 0 END),
        PendingClose = (
            SELECT COUNT_BIG(1)
            FROM dbo.AzureAlertCloseQueue q
            WHERE q.Status = 'Pending'
        )
    FROM dbo.AlertsManagement
    WHERE Active = 1;
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
    SET @PageNumber = CASE WHEN @PageNumber < 1 THEN 1 ELSE @PageNumber END;
    SET @PageSize = CASE WHEN @PageSize < 1 THEN 50 WHEN @PageSize > 50 THEN 50 ELSE @PageSize END;

    ;WITH src AS
    (
        SELECT
            v.Id,
            v.KPIType,
            v.AlertId,
            v.AlertName,
            v.CustomerName,
            v.SubscriptionName,
            v.ResourceName,
            v.Severity,
            v.Events,
            v.AssignedTo,
            v.AssignedEmail,
            v.LastInsertedAt,
            TotalRows = COUNT(1) OVER()
        FROM dbo.vw_AllAssignedAlerts_SoT_Norm_v3 v
        WHERE v.Active = 1
          AND v.AssignedEmail = @UserEmail
          AND (
                @Search IS NULL
                OR v.CustomerName LIKE '%' + @Search + '%'
                OR v.SubscriptionName LIKE '%' + @Search + '%'
                OR v.ResourceName LIKE '%' + @Search + '%'
                OR v.AlertName LIKE '%' + @Search + '%'
              )
    )
    SELECT
        Id, KPIType, AlertId, AlertName, CustomerName, SubscriptionName,
        ResourceName, Severity, Events, AssignedTo, AssignedEmail, LastInsertedAt, TotalRows
    FROM src
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
    SET @PageNumber = CASE WHEN @PageNumber < 1 THEN 1 ELSE @PageNumber END;
    SET @PageSize = CASE WHEN @PageSize < 1 THEN 50 WHEN @PageSize > 50 THEN 50 ELSE @PageSize END;

    ;WITH src AS
    (
        SELECT
            v.Id,
            v.KPIType,
            v.AlertId,
            v.AlertName,
            v.CustomerName,
            v.SubscriptionName,
            v.ResourceName,
            v.Severity,
            v.Events,
            v.AssignedTo,
            v.AssignedEmail,
            v.LastInsertedAt,
            TotalRows = COUNT(1) OVER()
        FROM dbo.vw_Dashboard_Unassigned_SoT_Norm_v2 v
        WHERE v.Active = 1
          AND (v.AssignedEmail IS NULL OR LTRIM(RTRIM(v.AssignedEmail)) = '')
          AND (
                @Search IS NULL
                OR v.CustomerName LIKE '%' + @Search + '%'
                OR v.SubscriptionName LIKE '%' + @Search + '%'
                OR v.ResourceName LIKE '%' + @Search + '%'
                OR v.AlertName LIKE '%' + @Search + '%'
              )
    )
    SELECT
        Id, KPIType, AlertId, AlertName, CustomerName, SubscriptionName,
        ResourceName, Severity, Events, AssignedTo, AssignedEmail, LastInsertedAt, TotalRows
    FROM src
    ORDER BY LastInsertedAt DESC, Id DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_App_GetAlertDetail
    @AlertRecordId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.Id,
        m.KPIType,
        m.AlertId,
        m.AlertName,
        m.CustomerName,
        m.TenantId,
        m.SubscriptionId,
        m.SubscriptionName,
        m.ResourceName,
        m.Region,
        m.Severity,
        m.Events,
        m.Active,
        m.AssignedTo,
        m.AssignedEmail,
        m.ResolutionNotes,
        m.LastInsertedAt,
        m.UpdatedAt,
        m.ResolveTime
    FROM dbo.AlertsManagement m
    WHERE m.Id = @AlertRecordId;

    SELECT TOP (100)
        h.Id,
        h.AlertRecordId,
        h.Action,
        h.UserEmail,
        h.UserName,
        h.Comment,
        h.CreatedAt
    FROM dbo.AlertUpdatesHistory h
    WHERE h.AlertRecordId = @AlertRecordId
    ORDER BY h.CreatedAt DESC, h.Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_App_AssignAlert
    @AlertRecordId BIGINT,
    @UserName NVARCHAR(256),
    @UserEmail NVARCHAR(256),
    @Comment NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE dbo.AlertsManagement
       SET AssignedTo = @UserName,
           AssignedEmail = @UserEmail,
           UpdatedAt = SYSUTCDATETIME()
     WHERE Id = @AlertRecordId
       AND Active = 1;

    INSERT INTO dbo.AlertUpdatesHistory
    (
        AlertRecordId,
        Action,
        UserEmail,
        UserName,
        Comment,
        CreatedAt
    )
    VALUES
    (
        @AlertRecordId,
        'Assigned',
        @UserEmail,
        @UserName,
        ISNULL(@Comment, 'Asignada desde ITQS Support Operations Center'),
        SYSUTCDATETIME()
    );

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_App_CloseAlert
    @AlertRecordId BIGINT,
    @UserName NVARCHAR(256),
    @UserEmail NVARCHAR(256),
    @Comment NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @FinalComment NVARCHAR(1000) = ISNULL(@Comment, 'Cliente Notificado-Monitoreo | Cierre desde ITQS Support Operations Center');

    BEGIN TRANSACTION;

    UPDATE dbo.AlertsManagement
       SET Active = 0,
           AssignedTo = @UserName,
           AssignedEmail = @UserEmail,
           ResolutionNotes = @FinalComment,
           ResolveTime = SYSUTCDATETIME(),
           UpdatedAt = SYSUTCDATETIME()
     WHERE Id = @AlertRecordId
       AND Active = 1;

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
        @FinalComment,
        'Pending'
    FROM dbo.AlertsManagement m
    WHERE m.Id = @AlertRecordId
      AND m.AlertId IS NOT NULL
      AND LTRIM(RTRIM(m.AlertId)) <> ''
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.AzureAlertCloseQueue q
          WHERE q.SourceTable = 'AlertsManagement'
            AND q.AlertRecordId = m.Id
            AND q.AzureAlertId = m.AlertId
            AND q.Status IN ('Pending','Processing')
      );

    INSERT INTO dbo.AlertUpdatesHistory
    (
        AlertRecordId,
        Action,
        UserEmail,
        UserName,
        Comment,
        CreatedAt
    )
    VALUES
    (
        @AlertRecordId,
        'Closed',
        @UserEmail,
        @UserName,
        @FinalComment,
        SYSUTCDATETIME()
    );

    COMMIT TRANSACTION;
END;
GO

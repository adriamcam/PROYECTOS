CREATE OR ALTER PROCEDURE dbo.sp_HB_DetectChanges
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentRunId UNIQUEIDENTIFIER;
    DECLARE @PreviousRunId UNIQUEIDENTIFIER;

    SELECT TOP 1 @CurrentRunId = SnapshotRunId
    FROM dbo.ReporteBeneficioHibridoHistory
    GROUP BY SnapshotRunId
    ORDER BY MAX(SnapshotDate) DESC;

    SELECT TOP 1 @PreviousRunId = SnapshotRunId
    FROM dbo.ReporteBeneficioHibridoHistory
    WHERE SnapshotRunId <> @CurrentRunId
    GROUP BY SnapshotRunId
    ORDER BY MAX(SnapshotDate) DESC;

    IF @CurrentRunId IS NULL
        RETURN;

    ;WITH CurrentSnap AS
    (
        SELECT *
        FROM dbo.ReporteBeneficioHibridoHistory
        WHERE SnapshotRunId = @CurrentRunId
    ),
    PreviousSnap AS
    (
        SELECT *
        FROM dbo.ReporteBeneficioHibridoHistory
        WHERE SnapshotRunId = @PreviousRunId
    )

    INSERT INTO dbo.ReporteBeneficioHibridoChanges
    (
        SnapshotRunId,
        ResourceKey,
        Customer,
        TenantId,
        SubscriptionId,
        Subscription,
        ResourceGroup,
        ResourceName,
        ChangeType,
        OldValue,
        NewValue,
        ChangeDate
    )

    SELECT
        @CurrentRunId,
        C.ResourceKey,
        C.Customer,
        C.TenantId,
        C.SubscriptionId,
        C.Subscription,
        C.ResourceGroup,
        C.ResourceName,
        'New Resource',
        NULL,
        'Detected',
        C.SnapshotDate
    FROM CurrentSnap C
    LEFT JOIN PreviousSnap P
        ON P.ResourceKey = C.ResourceKey
    WHERE P.ResourceKey IS NULL

    UNION ALL

    SELECT
        @CurrentRunId,
        P.ResourceKey,
        P.Customer,
        P.TenantId,
        P.SubscriptionId,
        P.Subscription,
        P.ResourceGroup,
        P.ResourceName,
        'Resource Deleted',
        'Detected',
        NULL,
        SYSUTCDATETIME()
    FROM PreviousSnap P
    LEFT JOIN CurrentSnap C
        ON C.ResourceKey = P.ResourceKey
    WHERE C.ResourceKey IS NULL

    UNION ALL

    SELECT
        @CurrentRunId,
        C.ResourceKey,
        C.Customer,
        C.TenantId,
        C.SubscriptionId,
        C.Subscription,
        C.ResourceGroup,
        C.ResourceName,
        'Windows AHUB Added',
        '0',
        '1',
        C.SnapshotDate
    FROM CurrentSnap C
    JOIN PreviousSnap P ON P.ResourceKey = C.ResourceKey
    WHERE P.HasWindowsAHUB = 0
      AND C.HasWindowsAHUB = 1

    UNION ALL

    SELECT
        @CurrentRunId,
        C.ResourceKey,
        C.Customer,
        C.TenantId,
        C.SubscriptionId,
        C.Subscription,
        C.ResourceGroup,
        C.ResourceName,
        'Windows AHUB Removed',
        '1',
        '0',
        C.SnapshotDate
    FROM CurrentSnap C
    JOIN PreviousSnap P ON P.ResourceKey = C.ResourceKey
    WHERE P.HasWindowsAHUB = 1
      AND C.HasWindowsAHUB = 0

    UNION ALL

    SELECT
        @CurrentRunId,
        C.ResourceKey,
        C.Customer,
        C.TenantId,
        C.SubscriptionId,
        C.Subscription,
        C.ResourceGroup,
        C.ResourceName,
        'SQL AHUB Added',
        '0',
        '1',
        C.SnapshotDate
    FROM CurrentSnap C
    JOIN PreviousSnap P ON P.ResourceKey = C.ResourceKey
    WHERE P.HasSqlAHUB = 0
      AND C.HasSqlAHUB = 1

    UNION ALL

    SELECT
        @CurrentRunId,
        C.ResourceKey,
        C.Customer,
        C.TenantId,
        C.SubscriptionId,
        C.Subscription,
        C.ResourceGroup,
        C.ResourceName,
        'SQL AHUB Removed',
        '1',
        '0',
        C.SnapshotDate
    FROM CurrentSnap C
    JOIN PreviousSnap P ON P.ResourceKey = C.ResourceKey
    WHERE P.HasSqlAHUB = 1
      AND C.HasSqlAHUB = 0

    UNION ALL

    SELECT
        @CurrentRunId,
        C.ResourceKey,
        C.Customer,
        C.TenantId,
        C.SubscriptionId,
        C.Subscription,
        C.ResourceGroup,
        C.ResourceName,
        'TagHB Added',
        ISNULL(P.TagHB,''),
        ISNULL(C.TagHB,''),
        C.SnapshotDate
    FROM CurrentSnap C
    JOIN PreviousSnap P ON P.ResourceKey = C.ResourceKey
    WHERE ISNULL(LTRIM(RTRIM(P.TagHB)),'') = ''
      AND ISNULL(LTRIM(RTRIM(C.TagHB)),'') <> ''

    UNION ALL

    SELECT
        @CurrentRunId,
        C.ResourceKey,
        C.Customer,
        C.TenantId,
        C.SubscriptionId,
        C.Subscription,
        C.ResourceGroup,
        C.ResourceName,
        'TagHB Removed',
        ISNULL(P.TagHB,''),
        ISNULL(C.TagHB,''),
        C.SnapshotDate
    FROM CurrentSnap C
    JOIN PreviousSnap P ON P.ResourceKey = C.ResourceKey
    WHERE ISNULL(LTRIM(RTRIM(P.TagHB)),'') <> ''
      AND ISNULL(LTRIM(RTRIM(C.TagHB)),'') = ''

    UNION ALL

    SELECT
        @CurrentRunId,
        C.ResourceKey,
        C.Customer,
        C.TenantId,
        C.SubscriptionId,
        C.Subscription,
        C.ResourceGroup,
        C.ResourceName,
        'TagHBSQL Added',
        ISNULL(P.TagHBSQL,''),
        ISNULL(C.TagHBSQL,''),
        C.SnapshotDate
    FROM CurrentSnap C
    JOIN PreviousSnap P ON P.ResourceKey = C.ResourceKey
    WHERE ISNULL(LTRIM(RTRIM(P.TagHBSQL)),'') = ''
      AND ISNULL(LTRIM(RTRIM(C.TagHBSQL)),'') <> ''

    UNION ALL

    SELECT
        @CurrentRunId,
        C.ResourceKey,
        C.Customer,
        C.TenantId,
        C.SubscriptionId,
        C.Subscription,
        C.ResourceGroup,
        C.ResourceName,
        'TagHBSQL Removed',
        ISNULL(P.TagHBSQL,''),
        ISNULL(C.TagHBSQL,''),
        C.SnapshotDate
    FROM CurrentSnap C
    JOIN PreviousSnap P ON P.ResourceKey = C.ResourceKey
    WHERE ISNULL(LTRIM(RTRIM(P.TagHBSQL)),'') <> ''
      AND ISNULL(LTRIM(RTRIM(C.TagHBSQL)),'') = '';
END;
GO
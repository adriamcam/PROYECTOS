CREATE OR ALTER VIEW dbo.vw_HB_ResourcesOperational
AS
WITH LastRun AS
(
    SELECT TOP 1 SnapshotRunId
    FROM dbo.ReporteBeneficioHibridoHistory
    GROUP BY SnapshotRunId
    ORDER BY MAX(SnapshotDate) DESC
),
LastSnapshot AS
(
    SELECT H.*
    FROM dbo.ReporteBeneficioHibridoHistory H
    INNER JOIN LastRun R ON R.SnapshotRunId = H.SnapshotRunId
),
LastChange AS
(
    SELECT *
    FROM
    (
        SELECT
            C.*,
            ROW_NUMBER() OVER
            (
                PARTITION BY C.ResourceKey
                ORDER BY C.ChangeDate DESC
            ) AS rn
        FROM dbo.ReporteBeneficioHibridoChanges C
    ) X
    WHERE rn = 1
)
SELECT
    S.ResourceKey,
    S.Customer,
    S.TenantId,
    S.SubscriptionId,
    S.Subscription,
    S.ResourceGroup,
    S.ResourceName,

    S.HasWindowsAHUB,
    S.HasSqlAHUB,
    S.HasTagHB,
    S.HasTagHBSQL,
    S.TagHB,
    S.TagHBSQL,

    CASE
        WHEN C.IsNewResource = 1 THEN 'New Resource'
        WHEN C.ChangeType LIKE '%Removed%' THEN 'Changed'
        WHEN S.HasWindowsAHUB = 1 AND S.HasTagHB = 0 THEN 'Missing Windows Tag'
        WHEN S.HasSqlAHUB = 1 AND S.HasTagHBSQL = 0 THEN 'Missing SQL Tag'
        ELSE 'Healthy'
    END AS OperationalStatus,

    C.ChangeType,
    C.Severity,
    C.OldValue,
    C.NewValue,
    C.ChangeDate,

    S.FirstSeenDate,
    S.LastSeenDate,
    S.SnapshotDate AS LastScanDate
FROM LastSnapshot S
LEFT JOIN LastChange C
    ON C.ResourceKey = S.ResourceKey;
GO
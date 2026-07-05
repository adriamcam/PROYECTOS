CREATE OR ALTER PROCEDURE dbo.sp_HB_CreateSnapshot
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RunId UNIQUEIDENTIFIER = NEWID();

    ;WITH Recursos AS
    (
        SELECT

            LOWER(CONCAT(
                ISNULL(SubscriptionId,''),'|',
                ISNULL(ResourceGroup,''),'|',
                ISNULL(ResourceName,'')
            )) AS ResourceKey,

            MAX(Customer) Customer,
            MAX(TenantId) TenantId,
            MAX(SubscriptionId) SubscriptionId,
            MAX(Subscription) Subscription,
            MAX(ResourceGroup) ResourceGroup,
            MAX(ResourceName) ResourceName,

            MAX(
                CASE
                    WHEN HybridBenefit='Azure Hybrid Benefit'
                    THEN 1
                    ELSE 0
                END
            ) AS HasWindowsAHUB,

            MAX(
                CASE
                    WHEN SQLLicenseType='AHUB'
                      OR HybridBenefit='Azure Hybrid Benefit (SQL)'
                    THEN 1
                    ELSE 0
                END
            ) AS HasSqlAHUB,

            MAX(
                CASE
                    WHEN TagHB IS NOT NULL
                     AND LTRIM(RTRIM(TagHB))<>''
                    THEN 1
                    ELSE 0
                END
            ) AS HasTagHB,

            MAX(
                CASE
                    WHEN TagHBSQL IS NOT NULL
                     AND LTRIM(RTRIM(TagHBSQL))<>''
                    THEN 1
                    ELSE 0
                END
            ) AS HasTagHBSQL,

            MAX(TagHB) TagHB,
            MAX(TagHBSQL) TagHBSQL,

            MAX(ScanDate) ScanDate

        FROM dbo.ReporteBeneficioHibrido

        GROUP BY

            LOWER(CONCAT(
                ISNULL(SubscriptionId,''),'|',
                ISNULL(ResourceGroup,''),'|',
                ISNULL(ResourceName,'')
            ))
    )

    INSERT INTO dbo.ReporteBeneficioHibridoHistory
    (

        SnapshotRunId,

        ResourceKey,

        Customer,

        TenantId,

        SubscriptionId,

        Subscription,

        ResourceGroup,

        ResourceName,

        HasWindowsAHUB,

        HasSqlAHUB,

        HasTagHB,

        HasTagHBSQL,

        TagHB,

        TagHBSQL,

        FirstSeenDate,

        LastSeenDate,

        SnapshotDate

    )

    SELECT

        @RunId,

        ResourceKey,

        Customer,

        TenantId,

        SubscriptionId,

        Subscription,

        ResourceGroup,

        ResourceName,

        HasWindowsAHUB,

        HasSqlAHUB,

        HasTagHB,

        HasTagHBSQL,

        TagHB,

        TagHBSQL,

        ScanDate,

        ScanDate,

        ScanDate

    FROM Recursos;

END;
GO
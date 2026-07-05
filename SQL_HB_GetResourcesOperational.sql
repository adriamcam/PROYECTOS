CREATE OR ALTER PROCEDURE dbo.sp_HB_GetResourcesOperational
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
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
        OperationalStatus,
        ChangeType,
        Severity,
        OldValue,
        NewValue,
        ChangeDate,
        FirstSeenDate,
        LastSeenDate,
        LastScanDate
    FROM dbo.vw_HB_ResourcesOperational
    ORDER BY
        CASE OperationalStatus
            WHEN 'Missing Windows Tag' THEN 1
            WHEN 'Missing SQL Tag' THEN 2
            WHEN 'Changed' THEN 3
            WHEN 'New Resource' THEN 4
            ELSE 5
        END,
        Customer,
        ResourceName;
END;
GO
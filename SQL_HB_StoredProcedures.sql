CREATE OR ALTER PROCEDURE dbo.sp_HB_GetKPIs
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(*) AS TotalResources,
        COUNT(DISTINCT Customer) AS TotalCustomers,
        COUNT(DISTINCT SubscriptionId) AS TotalSubscriptions,

        SUM(CASE 
            WHEN HybridBenefit = 'Azure Hybrid Benefit'
             AND LicenseType IN ('Windows_Server','Windows_Client')
            THEN 1 ELSE 0 
        END) AS WindowsCount,

        SUM(CASE 
            WHEN HybridBenefit = 'Azure Hybrid Benefit (SQL)'
              OR SQLLicenseType = 'AHUB'
            THEN 1 ELSE 0 
        END) AS SqlCount,

        SUM(CASE 
            WHEN HybridBenefit = 'Azure Hybrid Benefit (Tag)'
            THEN 1 ELSE 0 
        END) AS TagCount,

        SUM(CASE 
            WHEN ChangeType IS NOT NULL 
              OR AHUB_Status IS NOT NULL 
              OR ChangedBy IS NOT NULL 
              OR Time IS NOT NULL 
            THEN 1 ELSE 0 
        END) AS ChangeCount,

        MAX(ScanDate) AS LastScanDate
    FROM dbo.ReporteBeneficioHibrido;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_HB_GetTopCustomers
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 10
        Customer,
        COUNT(*) AS TotalResources
    FROM dbo.ReporteBeneficioHibrido
    GROUP BY Customer
    ORDER BY COUNT(*) DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_HB_GetDistribution
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 'Windows' AS BenefitType,
           COUNT(*) AS Total
    FROM dbo.ReporteBeneficioHibrido
    WHERE HybridBenefit = 'Azure Hybrid Benefit'
      AND LicenseType IN ('Windows_Server','Windows_Client')

    UNION ALL

    SELECT 'SQL' AS BenefitType,
           COUNT(*) AS Total
    FROM dbo.ReporteBeneficioHibrido
    WHERE HybridBenefit = 'Azure Hybrid Benefit (SQL)'
       OR SQLLicenseType = 'AHUB'

    UNION ALL

    SELECT 'Tags' AS BenefitType,
           COUNT(*) AS Total
    FROM dbo.ReporteBeneficioHibrido
    WHERE HybridBenefit = 'Azure Hybrid Benefit (Tag)';
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_HB_GetResources
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 500
        Id,
        Customer,
        TenantId,
        SubscriptionId,
        Subscription,
        ResourceGroup,
        ResourceName,
        HybridBenefit,
        LicenseType,
        SQLLicenseType,
        TagHB,
        TagHBSQL,
        AHUB_Status,
        ChangeType,
        ChangedBy,
        Time,
        ScanDate
    FROM dbo.ReporteBeneficioHibrido
    ORDER BY ScanDate DESC, Customer, ResourceName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_HB_GetChanges
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 100
        Id,
        Customer,
        Subscription,
        ResourceGroup,
        ResourceName,
        AHUB_Status,
        ChangeType,
        ChangedBy,
        Time,
        ScanDate
    FROM dbo.ReporteBeneficioHibrido
    WHERE ChangeType IS NOT NULL
       OR AHUB_Status IS NOT NULL
       OR ChangedBy IS NOT NULL
       OR Time IS NOT NULL
    ORDER BY ISNULL(Time, ScanDate) DESC;
END;
GO
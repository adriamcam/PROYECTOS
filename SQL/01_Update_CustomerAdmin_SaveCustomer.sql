/* =====================================================================
   CustomerAdmin - SaveCustomer con soporte para editar TenantId
   Ejecutar en base REPORTES antes de desplegar los archivos C#.
   ===================================================================== */

CREATE OR ALTER PROCEDURE dbo.ITQS_SOC_sp_CustomerAdmin_SaveCustomer
    @OriginalTenantId UNIQUEIDENTIFIER = NULL,
    @TenantId UNIQUEIDENTIFIER,
    @CustomerName NVARCHAR(250),
    @CustomerNamePortal NVARCHAR(250) = NULL,
    @ClientId NVARCHAR(100),
    @SecretName NVARCHAR(250),
    @IsActive BIT,
    @Source NVARCHAR(100) = NULL,
    @Notes NVARCHAR(MAX) = NULL,
    @UpdatedBy NVARCHAR(250) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LookupTenantId UNIQUEIDENTIFIER = ISNULL(@OriginalTenantId, @TenantId);

    IF EXISTS (SELECT 1 FROM dbo.ITQS_Customers WHERE TenantId = @LookupTenantId)
    BEGIN
        UPDATE dbo.ITQS_Customers
        SET
            TenantId = @TenantId,
            CustomerName = @CustomerName,
            CustomerNamePortal = ISNULL(NULLIF(@CustomerNamePortal, ''), @CustomerName),
            ClientId = @ClientId,
            SecretName = @SecretName,
            IsActive = @IsActive,
            Source = ISNULL(NULLIF(@Source, ''), 'SupportCloud'),
            Notes = @Notes,
            UpdatedAt = SYSUTCDATETIME()
        WHERE TenantId = @LookupTenantId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.ITQS_Customers
        (
            TenantId,
            CustomerName,
            CustomerNamePortal,
            ClientId,
            SecretName,
            IsActive,
            Source,
            Notes,
            CreatedAt,
            UpdatedAt
        )
        VALUES
        (
            @TenantId,
            @CustomerName,
            ISNULL(NULLIF(@CustomerNamePortal, ''), @CustomerName),
            @ClientId,
            @SecretName,
            @IsActive,
            ISNULL(NULLIF(@Source, ''), 'SupportCloud'),
            @Notes,
            SYSUTCDATETIME(),
            SYSUTCDATETIME()
        );
    END

    SELECT TOP (1)
        TenantId,
        CustomerName,
        CustomerNamePortal,
        ClientId,
        SecretName,
        IsActive,
        Source,
        Notes,
        CreatedAt,
        UpdatedAt
    FROM dbo.ITQS_Customers
    WHERE TenantId = @TenantId;
END
GO

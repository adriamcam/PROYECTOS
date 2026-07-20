/* ============================================================
   ITQS SUPPORT OPERATIONS CENTER
   ACTUALIZACIÓN DE COMPLEJIDAD - AZURE ADVISOR

   Tabla:
       dbo.AdvisorRecommendationCatalog

   Comportamiento:
       - Conserva los valores de Complexity existentes.
       - Clasifica únicamente registros con Complexity NULL o vacío.
       - Utiliza horas, minutos y ventana de mantenimiento.
       - Ejecuta todo dentro de una transacción.
   ============================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    /* ========================================================
       1. RESULTADO ANTES DEL CAMBIO
       ======================================================== */

    SELECT
        COALESCE(NULLIF(LTRIM(RTRIM(Complexity)), N''), N'Sin complejidad') 
            AS Complexity,
        COUNT_BIG(*) AS Total
    FROM dbo.AdvisorRecommendationCatalog
    GROUP BY
        COALESCE(NULLIF(LTRIM(RTRIM(Complexity)), N''), N'Sin complejidad')
    ORDER BY Total DESC;


    /* ========================================================
       2. RESPALDO DE LOS REGISTROS QUE SERÁN MODIFICADOS
       ======================================================== */

    IF OBJECT_ID(N'dbo.AdvisorRecommendationCatalogComplexityBackup', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AdvisorRecommendationCatalogComplexityBackup
        (
            BackupId                       BIGINT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_AdvisorRecommendationCatalogComplexityBackup
                PRIMARY KEY,

            CatalogId                      INT NOT NULL,
            PreviousComplexity             NVARCHAR(100) NULL,
            CalculatedComplexity           NVARCHAR(100) NOT NULL,

            ImplementationMinutes          INT NULL,
            ImplementationHours            DECIMAL(18,2) NULL,
            RequiresMaintenanceWindow      BIT NULL,

            BackupDateUtc                  DATETIME2(7) NOT NULL
                CONSTRAINT DF_AdvisorRecommendationCatalogComplexityBackup_BackupDateUtc
                DEFAULT SYSUTCDATETIME()
        );
    END;


    INSERT INTO dbo.AdvisorRecommendationCatalogComplexityBackup
    (
        CatalogId,
        PreviousComplexity,
        CalculatedComplexity,
        ImplementationMinutes,
        ImplementationHours,
        RequiresMaintenanceWindow
    )
    SELECT
        C.Id,
        C.Complexity,

        CASE
            WHEN ISNULL(C.ImplementationHours, 0) >= 4
              OR ISNULL(C.ImplementationMinutes, 0) >= 240
              OR ISNULL(C.RequiresMaintenanceWindow, 0) = 1
                THEN N'Alta'

            WHEN ISNULL(C.ImplementationHours, 0) >= 1
              OR ISNULL(C.ImplementationMinutes, 0) >= 60
                THEN N'Media'

            WHEN ISNULL(C.ImplementationHours, 0) > 0
              OR ISNULL(C.ImplementationMinutes, 0) > 0
                THEN N'Baja'

            ELSE N'Sin clasificar'
        END,

        C.ImplementationMinutes,
        C.ImplementationHours,
        C.RequiresMaintenanceWindow

    FROM dbo.AdvisorRecommendationCatalog AS C
    WHERE C.Complexity IS NULL
       OR LTRIM(RTRIM(C.Complexity)) = N'';


    DECLARE @BackupsCreated BIGINT = @@ROWCOUNT;


    /* ========================================================
       3. ACTUALIZAR EL CATÁLOGO
       ======================================================== */

    UPDATE C
       SET C.Complexity =
           CASE
               WHEN ISNULL(C.ImplementationHours, 0) >= 4
                 OR ISNULL(C.ImplementationMinutes, 0) >= 240
                 OR ISNULL(C.RequiresMaintenanceWindow, 0) = 1
                   THEN N'Alta'

               WHEN ISNULL(C.ImplementationHours, 0) >= 1
                 OR ISNULL(C.ImplementationMinutes, 0) >= 60
                   THEN N'Media'

               WHEN ISNULL(C.ImplementationHours, 0) > 0
                 OR ISNULL(C.ImplementationMinutes, 0) > 0
                   THEN N'Baja'

               ELSE N'Sin clasificar'
           END,

           C.UpdatedAt = SYSUTCDATETIME()

    FROM dbo.AdvisorRecommendationCatalog AS C

    WHERE C.Complexity IS NULL
       OR LTRIM(RTRIM(C.Complexity)) = N'';


    DECLARE @RowsUpdated BIGINT = @@ROWCOUNT;


    /* ========================================================
       4. VALIDACIONES
       ======================================================== */

    DECLARE @RemainingWithoutComplexity BIGINT;

    SELECT
        @RemainingWithoutComplexity = COUNT_BIG(*)
    FROM dbo.AdvisorRecommendationCatalog
    WHERE Complexity IS NULL
       OR LTRIM(RTRIM(Complexity)) = N'';


    IF @RemainingWithoutComplexity > 0
    BEGIN
        THROW 50001,
              'La actualización terminó con registros sin Complexity.',
              1;
    END;


    COMMIT TRANSACTION;


    /* ========================================================
       5. RESUMEN DE EJECUCIÓN
       ======================================================== */

    SELECT
        @BackupsCreated AS RegistrosRespaldados,
        @RowsUpdated AS RegistrosActualizados,
        @RemainingWithoutComplexity AS RegistrosSinComplejidad;


    SELECT
        Complexity,
        COUNT_BIG(*) AS TotalCatalogo
    FROM dbo.AdvisorRecommendationCatalog
    GROUP BY Complexity
    ORDER BY
        CASE Complexity
            WHEN N'Crítica' THEN 1
            WHEN N'Alta' THEN 2
            WHEN N'Media' THEN 3
            WHEN N'Baja' THEN 4
            WHEN N'Sin clasificar' THEN 5
            ELSE 6
        END;


    /* ========================================================
       6. VALIDAR IMPACTO EN LA VISTA
       ======================================================== */

    SELECT
        Complexity,
        COUNT_BIG(*) AS Recomendaciones
    FROM dbo.vw_AdvisorRecommendationsValued
    GROUP BY Complexity
    ORDER BY
        CASE Complexity
            WHEN N'Crítica' THEN 1
            WHEN N'Alta' THEN 2
            WHEN N'Media' THEN 3
            WHEN N'Baja' THEN 4
            WHEN N'Sin clasificar' THEN 5
            ELSE 6
        END;

END TRY
BEGIN CATCH

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    SELECT
        ERROR_NUMBER() AS ErrorNumber,
        ERROR_LINE() AS ErrorLine,
        ERROR_MESSAGE() AS ErrorMessage;

    THROW;

END CATCH;

$ErrorActionPreference = "Stop"

# ==========================================================
# CONFIGURACIÓN
# ==========================================================

$ConnectionString = "Server=tcp:TU_SERVIDOR.database.windows.net,1433;Initial Catalog=REPORTES;User ID=TU_USUARIO;Password=TU_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

$ViewName = "dbo.vw_AdvisorRecommendationsValued"
$BackupFolder = ".\DatabaseBackups"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

if (-not (Test-Path $BackupFolder)) {
    New-Item -ItemType Directory -Path $BackupFolder | Out-Null
}

$BackupFile = Join-Path `
    $BackupFolder `
    "vw_AdvisorRecommendationsValued-$Timestamp.sql"

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " ACTUALIZACIÓN DE COMPLEJIDAD - AZURE ADVISOR" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

# ==========================================================
# CARGAR CLIENTE SQL
# ==========================================================

Add-Type -AssemblyName System.Data

$Connection = New-Object System.Data.SqlClient.SqlConnection
$Connection.ConnectionString = $ConnectionString
$Connection.Open()

try {

    # ======================================================
    # 1. LEER DEFINICIÓN ACTUAL
    # ======================================================

    $ReadCommand = $Connection.CreateCommand()
    $ReadCommand.CommandText = @"
SELECT OBJECT_DEFINITION(
    OBJECT_ID(N'$ViewName')
);
"@

    $CurrentDefinition = $ReadCommand.ExecuteScalar()

    if ([string]::IsNullOrWhiteSpace($CurrentDefinition)) {
        throw "No se pudo leer la definición de $ViewName."
    }

    Set-Content `
        -Path $BackupFile `
        -Value $CurrentDefinition `
        -Encoding UTF8

    Write-Host "Respaldo creado:" -ForegroundColor Green
    Write-Host $BackupFile -ForegroundColor DarkGray
    Write-Host ""

    # ======================================================
    # 2. DEFINICIÓN NUEVA DE LA VISTA
    # ======================================================

    $NewViewDefinition = @"
ALTER VIEW dbo.vw_AdvisorRecommendationsValued
AS
SELECT
    AR.Id,

    AR.CustomerName,
    AR.TenantId,
    AR.SubscriptionId,
    AR.SubscriptionName,

    AR.ResourceGroup,
    AR.AffectedResource,

    AR.Category AS AzureCategory,
    AR.AdvisorType,
    AR.ImpactedField,
    AR.ImpactedValue,
    AR.Impact,

    AR.Currency,
    AR.AnnualSavingsAmount,

    AR.Recommendation AS RecommendationAzure,
    AR.Remediation AS RemediationAzure,

    C.Id AS CatalogId,

    C.RecommendationSpanish,
    C.DescriptionSpanish,
    C.CostClassification,
    C.RequiresMaintenanceWindow,
    C.ImplementationMinutes,
    C.ImplementationHours,

    CASE
        WHEN NULLIF(LTRIM(RTRIM(C.Complexity)), N'') IS NOT NULL
            THEN LTRIM(RTRIM(C.Complexity))

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
    END AS Complexity,

    C.Notes,

    CASE
        WHEN C.Id IS NOT NULL THEN 1
        ELSE 0
    END AS HasInternalValuation,

    CASE
        WHEN C.RequiresMaintenanceWindow = 1 THEN N'Sí'
        WHEN C.RequiresMaintenanceWindow = 0 THEN N'No'
        ELSE N'No valorado'
    END AS MaintenanceWindowText,

    AR.InsertedAt AS AdvisorUpdatedAt,
    C.UpdatedAt AS CatalogUpdatedAt

FROM dbo.AdvisorRecommendations AS AR

LEFT JOIN dbo.AdvisorRecommendationCatalog AS C
    ON C.RecommendationKeyHash = AR.RecommendationKeyHash
   AND LOWER(LTRIM(RTRIM(C.Recommendation)))
       =
       LOWER(LTRIM(RTRIM(AR.Recommendation)))
   AND C.IsActive = 1;
"@

    # ======================================================
    # 3. EJECUTAR ALTER VIEW
    # ======================================================

    $AlterCommand = $Connection.CreateCommand()
    $AlterCommand.CommandTimeout = 120
    $AlterCommand.CommandText = $NewViewDefinition
    $AlterCommand.ExecuteNonQuery() | Out-Null

    Write-Host "Vista actualizada correctamente." -ForegroundColor Green
    Write-Host ""

    # ======================================================
    # 4. VALIDAR DISTRIBUCIÓN
    # ======================================================

    $ValidationCommand = $Connection.CreateCommand()
    $ValidationCommand.CommandTimeout = 120
    $ValidationCommand.CommandText = @"
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
        ELSE 5
    END;
"@

    $Adapter = New-Object System.Data.SqlClient.SqlDataAdapter
    $Adapter.SelectCommand = $ValidationCommand

    $Table = New-Object System.Data.DataTable
    [void]$Adapter.Fill($Table)

    Write-Host "Distribución resultante:" -ForegroundColor Cyan
    Write-Host ""

    $Table |
        Format-Table `
            Complexity,
            Recomendaciones `
            -AutoSize

    # ======================================================
    # 5. VALIDAR NULLS
    # ======================================================

    $NullCommand = $Connection.CreateCommand()
    $NullCommand.CommandText = @"
SELECT
    COUNT_BIG(*) AS ComplejidadNula
FROM dbo.vw_AdvisorRecommendationsValued
WHERE Complexity IS NULL
   OR LTRIM(RTRIM(Complexity)) = N'';
"@

    $NullCount = [long]$NullCommand.ExecuteScalar()

    if ($NullCount -gt 0) {
        throw "Todavía existen $NullCount recomendaciones sin complejidad."
    }

    Write-Host ""
    Write-Host "Validación correcta: no quedan valores NULL." -ForegroundColor Green
}
finally {
    if ($Connection.State -eq "Open") {
        $Connection.Close()
    }

    $Connection.Dispose()
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " COMPLEJIDAD ACTUALIZADA CORRECTAMENTE" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Ahora recarga Advisor con Ctrl + F5." -ForegroundColor Cyan

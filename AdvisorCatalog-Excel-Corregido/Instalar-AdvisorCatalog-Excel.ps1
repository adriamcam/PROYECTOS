$ErrorActionPreference = "Stop"

$root = "C:\Git\PROYECTOS"
$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

$targets = @{
    "AdvisorCatalog.razor.txt" = "$root\Components\Clients\ClientsWorkspace\Tabs\AdvisorCatalog.razor"
    "AdvisorCatalog.razor.css.txt" = "$root\Components\Clients\ClientsWorkspace\Tabs\AdvisorCatalog.razor.css"
    "AdvisorCatalogService.cs.txt" = "$root\Components\Clients\ClientsWorkspace\Services\AdvisorCatalogService.cs"
    "IAdvisorCatalogService.cs.txt" = "$root\Components\Clients\ClientsWorkspace\Services\IAdvisorCatalogService.cs"
    "AdvisorCatalogItem.cs.txt" = "$root\Components\Clients\ClientsWorkspace\Models\AdvisorCatalogItem.cs"
    "advisorCatalogDownload.js.txt" = "$root\wwwroot\js\advisorCatalogDownload.js"
}

Write-Host "Creando respaldos..." -ForegroundColor Cyan
foreach ($entry in $targets.GetEnumerator()) {
    $origin = Join-Path $source $entry.Key
    if (!(Test-Path $origin)) { throw "No existe el archivo del paquete: $origin" }
    $targetDir = Split-Path -Parent $entry.Value
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    if (Test-Path $entry.Value) { Copy-Item $entry.Value "$($entry.Value).bak-$timestamp" -Force }
    Copy-Item $origin $entry.Value -Force
}

# Eliminar copias accidentales en la raíz que .NET compilaría como duplicados.
@(
    "$root\AdvisorCatalog.razor",
    "$root\AdvisorCatalogService.cs",
    "$root\IAdvisorCatalogService.cs",
    "$root\AdvisorCatalogItem.cs"
) | ForEach-Object { if (Test-Path $_) { Remove-Item $_ -Force } }

$appRazor = "$root\Components\App.razor"
if (!(Test-Path $appRazor)) { throw "No se encontró $appRazor" }
$appContent = Get-Content $appRazor -Raw
$scriptTag = '<script src="js/advisorCatalogDownload.js"></script>'
if ($appContent -notmatch 'advisorCatalogDownload\.js') {
    $appContent = $appContent -replace '</body>', "    $scriptTag`r`n</body>"
    Set-Content $appRazor $appContent -Encoding UTF8
}

Set-Location $root
if (-not (Select-String -Path "$root\ITQS.SupportOperationsCenter.csproj" -Pattern 'PackageReference Include="ClosedXML"' -Quiet)) {
    dotnet add package ClosedXML --version 0.105.0
    if ($LASTEXITCODE -ne 0) { throw "No fue posible instalar ClosedXML." }
}

Remove-Item "$root\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$root\obj" -Recurse -Force -ErrorAction SilentlyContinue

dotnet restore
if ($LASTEXITCODE -ne 0) { throw "dotnet restore falló." }

dotnet build
if ($LASTEXITCODE -ne 0) { throw "La compilación falló. Revise los errores mostrados." }

Write-Host ""
Write-Host "Implementación completada correctamente." -ForegroundColor Green
Write-Host "Ejecute: dotnet run" -ForegroundColor Cyan

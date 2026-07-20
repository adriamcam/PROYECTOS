$ErrorActionPreference = "Stop"

$File = ".\Components\Clients\ClientsWorkspace\Tabs\ClientAdvisor.razor"
$Backup = "$File.$(Get-Date -Format 'yyyyMMdd-HHmmss').bak"

if (-not (Test-Path $File)) {
    throw "No existe el archivo: $File"
}

Copy-Item $File $Backup -Force

$Content = Get-Content $File -Raw

if ($Content.Contains('class="advisor-data-row"')) {
    Write-Host "La fila ya es interactiva." -ForegroundColor Yellow
}
else {
    $Pattern = '(?s)(@foreach\s*\(var item in PagedRecommendations\)\s*\{\s*)<tr>'

    $Regex = [regex]::new($Pattern)

    $Match = $Regex.Match($Content)

    if (-not $Match.Success) {
        throw "No se encontró la fila dentro de PagedRecommendations."
    }

    $Replacement =
        $Match.Groups[1].Value +
        '<tr class="advisor-data-row"' + "`r`n" +
        '                                title="Abrir detalle de la recomendación"' + "`r`n" +
        '                                @onclick=''() => OpenRecommendation(item)''>'

    $Content = $Content.Remove(
        $Match.Index,
        $Match.Length
    ).Insert(
        $Match.Index,
        $Replacement
    )

    Set-Content `
        -Path $File `
        -Value $Content `
        -Encoding UTF8

    Write-Host "Fila interactiva agregada." -ForegroundColor Green
}

$Required = @(
    'class="advisor-data-row"',
    'OpenRecommendation(item)',
    'advisor-detail-backdrop',
    'SelectedRecommendation'
)

foreach ($Item in $Required) {
    if (-not (Select-String -Path $File -SimpleMatch $Item -Quiet)) {
        throw "Falta la implementación requerida: $Item"
    }

    Write-Host "OK: $Item" -ForegroundColor DarkGreen
}

Write-Host "Respaldo: $Backup" -ForegroundColor DarkGray

Remove-Item ".\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item ".\obj" -Recurse -Force -ErrorAction SilentlyContinue

dotnet build

if ($LASTEXITCODE -ne 0) {
    throw "La compilación terminó con errores."
}

Write-Host ""
Write-Host "Compilación correcta." -ForegroundColor Green
Write-Host "Ejecuta ahora: dotnet run" -ForegroundColor Cyan

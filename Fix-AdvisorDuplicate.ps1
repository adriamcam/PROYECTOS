$ErrorActionPreference = "Stop"

$File = ".\Components\Clients\ClientsWorkspace\Tabs\ClientAdvisor.razor"
$Backup = "$File.$(Get-Date -Format 'yyyyMMdd-HHmmss').bak"

if (-not (Test-Path $File)) {
    throw "No existe el archivo: $File"
}

Copy-Item $File $Backup -Force

$Content = Get-Content $File -Raw
$Signature = "private string GetSummaryCardClass(string filter)"

$Indexes = New-Object System.Collections.Generic.List[int]
$Position = 0

while ($true) {
    $Index = $Content.IndexOf(
        $Signature,
        $Position,
        [StringComparison]::Ordinal
    )

    if ($Index -lt 0) {
        break
    }

    $Indexes.Add($Index)
    $Position = $Index + $Signature.Length
}

Write-Host "Métodos encontrados: $($Indexes.Count)" -ForegroundColor Cyan

if ($Indexes.Count -lt 1) {
    throw "No se encontró GetSummaryCardClass."
}

while ($Indexes.Count -gt 1) {
    $MethodStart = $Indexes[$Indexes.Count - 1]

    $OpeningBrace = $Content.IndexOf(
        "{",
        $MethodStart,
        [StringComparison]::Ordinal
    )

    if ($OpeningBrace -lt 0) {
        throw "No se encontró la llave inicial del método duplicado."
    }

    $Depth = 0
    $MethodEnd = -1

    for ($i = $OpeningBrace; $i -lt $Content.Length; $i++) {
        if ($Content[$i] -eq "{") {
            $Depth++
        }
        elseif ($Content[$i] -eq "}") {
            $Depth--

            if ($Depth -eq 0) {
                $MethodEnd = $i + 1
                break
            }
        }
    }

    if ($MethodEnd -lt 0) {
        throw "No se encontró el cierre del método duplicado."
    }

    while (
        $MethodEnd -lt $Content.Length -and
        (
            $Content[$MethodEnd] -eq "`r" -or
            $Content[$MethodEnd] -eq "`n"
        )
    ) {
        $MethodEnd++
    }

    $Content = $Content.Remove(
        $MethodStart,
        $MethodEnd - $MethodStart
    )

    $Indexes.Clear()
    $Position = 0

    while ($true) {
        $Index = $Content.IndexOf(
            $Signature,
            $Position,
            [StringComparison]::Ordinal
        )

        if ($Index -lt 0) {
            break
        }

        $Indexes.Add($Index)
        $Position = $Index + $Signature.Length
    }
}

Set-Content $File $Content -Encoding UTF8

$Count = (
    Select-String `
        -Path $File `
        -Pattern "private string GetSummaryCardClass" `
        -AllMatches
).Matches.Count

Write-Host "GetSummaryCardClass restantes: $Count" -ForegroundColor Green
Write-Host "Respaldo: $Backup" -ForegroundColor DarkGray

if ($Count -ne 1) {
    throw "No quedó exactamente una definición."
}

Remove-Item ".\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item ".\obj" -Recurse -Force -ErrorAction SilentlyContinue

dotnet build

if ($LASTEXITCODE -ne 0) {
    throw "La compilación terminó con errores."
}

Write-Host ""
Write-Host "Compilación correcta." -ForegroundColor Green
Write-Host "Ejecuta: dotnet run" -ForegroundColor Cyan

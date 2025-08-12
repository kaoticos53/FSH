param(
    [string]$SolutionPath = "FSH.sln",
    [string]$Configuration = "Release",
    [string]$CoverageDir = "coverage-report",
    [int]$Threshold = 90,
    [switch]$FailOnThreshold
)

$ErrorActionPreference = 'Stop'

Write-Host "[cobertura] Ejecutando pruebas con cobertura..." -ForegroundColor Cyan
Write-Host "[cobertura] Solución: $SolutionPath, Config: $Configuration, Umbral: $Threshold%" -ForegroundColor DarkCyan

# 1) Ejecutar tests y recolectar cobertura con el data collector XPlat (no requiere paquetes)
& dotnet test $SolutionPath -c $Configuration --collect:"XPlat Code Coverage" -l "trx;LogFileName=TestResults.trx"
if ($LASTEXITCODE -ne 0) {
    Write-Error "[cobertura] 'dotnet test' terminó con errores. Revisa el TRX generado."
    exit $LASTEXITCODE
}

# 2) Localizar todos los ficheros Cobertura generados
$covFiles = Get-ChildItem -Path . -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
if (-not $covFiles -or $covFiles.Count -eq 0) {
    Write-Error "[cobertura] No se encontraron archivos 'coverage.cobertura.xml'. Asegúrate de usar el collector XPlat o añade coverlet a los proyectos de test."
    exit 1
}

# 3) Preparar directorio de reportes
if (-not (Test-Path $CoverageDir)) {
    New-Item -ItemType Directory -Force -Path $CoverageDir | Out-Null
}

# 4) Generar reportes con ReportGenerator si está disponible
function Invoke-ReportGenerator([string]$reports, [string]$targetDir) {
    Write-Host "[cobertura] Generando informes con ReportGenerator..." -ForegroundColor Cyan
    if (Get-Command reportgenerator -ErrorAction SilentlyContinue) {
        & reportgenerator "-reports:$reports" "-targetdir:$targetDir" '-reporttypes:"Html;HtmlSummary;JsonSummary;Cobertura"'
        return $LASTEXITCODE
    }

    if (Test-Path ".\.config\dotnet-tools.json") {
        Write-Host "[cobertura] Intentando 'dotnet tool restore' y 'dotnet tool run reportgenerator'..." -ForegroundColor DarkYellow
        & dotnet tool restore
        if ($LASTEXITCODE -eq 0) {
            & dotnet tool run reportgenerator "-reports:$reports" "-targetdir:$targetDir" '-reporttypes:"Html;HtmlSummary;JsonSummary;Cobertura"'
            return $LASTEXITCODE
        }
    }

    Write-Warning "[cobertura] ReportGenerator no está instalado. Instálalo con 'dotnet tool install -g dotnet-reportgenerator-globaltool' o añade '.config/dotnet-tools.json' y ejecuta 'dotnet tool restore'."
    return 1
}

$reportsArg = ($covFiles -join ';')
$usedReportGenerator = $true
$rgExit = Invoke-ReportGenerator -reports $reportsArg -targetDir $CoverageDir
if ($rgExit -ne 0) {
    $usedReportGenerator = $false
    Write-Warning "[cobertura] No se pudo generar el informe con ReportGenerator. Se continuará con el cálculo de umbral usando los archivos Cobertura sin agregación visual."
}

# 5) Calcular cobertura total (preferencia: Cobertura.xml generado por ReportGenerator)
$percent = $null
$coberturaMerged = Join-Path $CoverageDir 'Cobertura.xml'
if ($usedReportGenerator -and (Test-Path $coberturaMerged)) {
    try {
        [xml]$cov = Get-Content $coberturaMerged
        $rate = [double]$cov.coverage.'line-rate'
        $percent = [math]::Round($rate * 100, 2)
        Write-Host "[cobertura] Cobertura total (ReportGenerator/Cobertura.xml): $percent%" -ForegroundColor Green
    } catch {
        Write-Warning "[cobertura] No se pudo leer 'Cobertura.xml'. Se intenta cálculo alternativo."
    }
}

if ($null -eq $percent) {
    # Fallback: sumar líneas cubiertas y válidas de todos los coverage.cobertura.xml
    $totalCovered = 0
    $totalValid = 0
    foreach ($f in $covFiles) {
        try {
            [xml]$c = Get-Content $f
            $covered = [int]$c.coverage.'lines-covered'
            $valid = [int]$c.coverage.'lines-valid'
            $totalCovered += $covered
            $totalValid += $valid
        } catch {
            Write-Warning "[cobertura] No se pudo procesar: $f"
        }
    }
    if ($totalValid -gt 0) {
        $percent = [math]::Round(($totalCovered / $totalValid) * 100, 2)
        Write-Host "[cobertura] Cobertura total (agregada por fallback): $percent%" -ForegroundColor Green
    } else {
        Write-Error "[cobertura] No fue posible calcular la cobertura total."
        exit 1
    }
}

# 6) Gating de cobertura
if ($FailOnThreshold) {
    if ($percent -lt $Threshold) {
        Write-Error "[cobertura] Umbral no alcanzado: $percent% < $Threshold%"
        exit 1
    } else {
        Write-Host "[cobertura] Umbral alcanzado: $percent% ≥ $Threshold%" -ForegroundColor Green
    }
} else {
    Write-Host "[cobertura] Umbral (opcional) no aplicado. Cobertura actual: $percent%" -ForegroundColor DarkGreen
}

Write-Host "[cobertura] Reportes en: $CoverageDir" -ForegroundColor Cyan
exit 0

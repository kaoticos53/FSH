# Script para ejecutar la aplicación FSH
# Uso: .\run.ps1

# Configuración
$projectPath = "src/aspire/Host/Host.csproj"
$appUrl = "https://localhost:7100/"

# Mostrar mensaje de inicio
Write-Host "Iniciando la aplicación FSH..." -ForegroundColor Green
Write-Host "Proyecto: $projectPath" -ForegroundColor Cyan
Write-Host "URL: $appUrl" -ForegroundColor Cyan

# Verificar si el proyecto existe
if (-not (Test-Path -Path $projectPath)) {
    Write-Host "Error: No se encontró el proyecto en $projectPath" -ForegroundColor Red
    exit 1
}

# Función para abrir la URL en el navegador
function Open-Browser {
    param([string]$url)
    try {
        Start-Process $url
        Write-Host "Abriendo la aplicación en el navegador..." -ForegroundColor Green
    } catch {
        Write-Host "No se pudo abrir el navegador automáticamente. Por favor, abre manualmente: $url" -ForegroundColor Yellow
    }
}

# Iniciar la aplicación en segundo plano
$job = Start-ThreadJob -ScriptBlock {
    param($path)
    dotnet run --project $path
} -ArgumentList $projectPath

# Esperar a que la aplicación esté lista (darle un poco de tiempo)
Start-Sleep -Seconds 5

# Abrir el navegador
Open-Browser -url $appUrl

try {
    # Mantener el script en ejecución mientras la aplicación esté corriendo
    while ($job.State -eq 'Running') {
        Start-Sleep -Seconds 1
    }
    
    # Si llegamos aquí, el trabajo ha terminado
    Write-Host "\nLa aplicación se ha cerrado. Presiona cualquier tecla para continuar..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
} catch {
    Write-Host "\nOcurrió un error al ejecutar la aplicación:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "\nPresiona cualquier tecla para salir..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
} finally {
    # Asegurarse de que el trabajo se detenga correctamente
    if ($job.State -eq 'Running') {
        Stop-Job $job -ErrorAction SilentlyContinue
    }
    Remove-Job $job -Force -ErrorAction SilentlyContinue
}

# Setup Script para Inventory API
# Ejecutar desde la raíz del proyecto: .\scripts\setup.ps1

param(
    [string]$Environment = "Development",
    [string]$SqlServer = "localhost",
    [string]$Database = "InventoryDb",
    [string]$SqlUser = "sa",
    [string]$SqlPassword = "YourPassword123!"
)

Write-Host "========================================" -ForegroundColor Green
Write-Host "Inventory API - Setup Script" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# 1. Restore dependencies
Write-Host "1️⃣  Restaurando dependencias..." -ForegroundColor Cyan
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error al restaurar dependencias" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Dependencias restauradas" -ForegroundColor Green
Write-Host ""

# 2. Build project
Write-Host "2️⃣  Compilando proyecto..." -ForegroundColor Cyan
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error al compilar" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Proyecto compilado" -ForegroundColor Green
Write-Host ""

# 3. Create database
Write-Host "3️⃣  Creando base de datos..." -ForegroundColor Cyan
Write-Host "   Server: $SqlServer" -ForegroundColor Yellow
Write-Host "   Database: $Database" -ForegroundColor Yellow

# Verificar si sqlcmd está disponible
$sqlcmdPath = "sqlcmd"
if (-not (Get-Command $sqlcmdPath -ErrorAction SilentlyContinue)) {
    Write-Host "⚠️  sqlcmd no encontrado. Instalar SQL Server Management Studio o usar Docker." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Para usar Docker con SQL Server:" -ForegroundColor Cyan
    Write-Host 'docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest' -ForegroundColor Gray
    Write-Host ""
}
else {
    # Ejecutar script SQL
    $sqlScript = "sql/create_db.sql"
    if (Test-Path $sqlScript) {
        & $sqlcmdPath -S $SqlServer -U $SqlUser -P $SqlPassword -i $sqlScript
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Base de datos creada" -ForegroundColor Green
        }
        else {
            Write-Host "❌ Error al crear base de datos" -ForegroundColor Red
        }
    }
    else {
        Write-Host "⚠️  Script SQL no encontrado: $sqlScript" -ForegroundColor Yellow
    }
}
Write-Host ""

# 4. Configure appsettings
Write-Host "4️⃣  Configurando appsettings..." -ForegroundColor Cyan
$appSettingsFile = "src/Inventory.Api/appsettings.$Environment.json"
if (Test-Path $appSettingsFile) {
    Write-Host "✅ Archivo de configuración encontrado: $appSettingsFile" -ForegroundColor Green
    Write-Host ""
    Write-Host "⚠️  Editar manualmente si es necesario:" -ForegroundColor Yellow
    Write-Host "   - ConnectionString de SQL Server" -ForegroundColor Gray
    Write-Host "   - Jwt Secret (mín. 32 caracteres)" -ForegroundColor Gray
}
else {
    Write-Host "⚠️  Archivo de configuración no encontrado: $appSettingsFile" -ForegroundColor Yellow
}
Write-Host ""

# 5. Summary
Write-Host "========================================" -ForegroundColor Green
Write-Host "✅ Setup completado" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Próximos pasos:" -ForegroundColor Cyan
Write-Host "1. Ejecutar: dotnet watch run --project src/Inventory.Api" -ForegroundColor Gray
Write-Host "2. Abrir: http://localhost:5000/swagger" -ForegroundColor Gray
Write-Host ""
Write-Host "Endpoints de prueba:" -ForegroundColor Cyan
Write-Host "• POST   /api/auth/register - Registrar usuario" -ForegroundColor Gray
Write-Host "• POST   /api/auth/login    - Login y obtener token" -ForegroundColor Gray
Write-Host "• GET    /api/products      - Listar productos" -ForegroundColor Gray
Write-Host "• GET    /health/live       - Health check" -ForegroundColor Gray
Write-Host ""

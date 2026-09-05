# Script to add EF migration and update database (run from repo root)
param(
    [string]$MigrationName = "Initial"
)

Write-Host "Ensure dotnet-ef is installed: dotnet tool install --global dotnet-ef"
Write-Host "Adding migration '$MigrationName' (project: Inventory.Infrastructure, startup: Inventory.Api)"

dotnet ef migrations add $MigrationName --project src\Inventory.Infrastructure --startup-project src\Inventory.Api --output-dir Migrations

Write-Host "Applying migrations to database"

dotnet ef database update --project src\Inventory.Infrastructure --startup-project src\Inventory.Api

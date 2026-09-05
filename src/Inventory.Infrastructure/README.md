# Infrastructure layer

Implementaciones concretas: EF Core DbContext, repositorios Dapper, migraciones, adaptadores externos.

Migrations (local):
1. From repo root, ensure dotnet-ef is installed: dotnet tool install --global dotnet-ef
2. Add a connection string to src/Inventory.Api/appsettings.Development.json or user secrets.
3. Create migration:
   dotnet ef migrations add Initial --project src/Inventory.Infrastructure --startup-project src/Inventory.Api --output-dir Migrations
4. Apply migration:
   dotnet ef database update --project src/Inventory.Infrastructure --startup-project src/Inventory.Api

Seeding: SeedData.EnsureSeedDataAsync is called at startup when DbContext is registered.

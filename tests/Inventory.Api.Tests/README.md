# Tests

## Unitarios (sin Docker, sin BD)

```bash
dotnet test --filter "Category!=Integration"
```

Servicios (`SaleService`/`PurchaseService`, incluyendo el reintento por
concurrencia con rollback simulado), validadores de `AuthController`, y un
ejemplo de test de controller. Usan fakes en memoria, no una base de datos real.

## Integración (requiere Docker)

```bash
dotnet test --filter "Category=Integration"
```

`Integration/` levanta un SQL Server descartable con Testcontainers y prueba
el fix de concurrencia de stock contra una base de datos real — el mismo
`AppDbContext`, `RowVersion` e índice único filtrado que usa la app en
producción, no fakes. Tarda más (baja la imagen de SQL Server la primera vez)
y no corre si Docker no está disponible; en ese caso falla rápido (~10s) con
un mensaje claro de "Docker unavailable", no se cuelga.

Sin filtro, `dotnet test` corre ambos grupos.

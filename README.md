# Inventory API

API REST de inventario construida con .NET 9, EF Core, Dapper, SQL Server y Clean Architecture.

> **Estado**: la base de datos todavía no se ha creado en ningún entorno; todas las
> contraseñas/secrets que aparecen en `appsettings.*.json`, `docker-compose.yml` y
> `ci_cd/kubernetes/secrets.yaml` son valores de ejemplo para desarrollo local,
> **no credenciales reales**. Reemplázalos antes de desplegar en cualquier entorno compartido.

## Requisitos

- .NET 9.0 SDK
- SQL Server 2019+
- Docker (opcional)

## Instalación

### Opción 1: Desarrollo Local

```bash
git clone <repo>
cd inventory-api

dotnet restore
dotnet run --project src/Inventory.Api
```

API en: `http://localhost:5000`

Editar `src/Inventory.Api/appsettings.Development.json` con tu connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=InventoryDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;"
  },
  "Jwt": {
    "Secret": "tu-clave-secreta-minimo-32-caracteres",
    "Issuer": "inventory-api",
    "Audience": "inventory-api-users",
    "ExpirationMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  }
}
```

Crear base de datos (esquema de ejemplo sin usar EF migrations):

```bash
sqlcmd -S localhost -U sa -P YourPassword123! -i sql/create_db.sql
```

O deja que EF Core cree el esquema automáticamente a partir del modelo la primera vez
que arranca la API (`SeedData.EnsureSeedDataAsync` llama a `EnsureCreatedAsync`).

### Opción 2: Con Docker (Recomendado)

```bash
cd ci_cd/docker
docker-compose up -d
cd ../..
dotnet run --project src/Inventory.Api
```

SQL Server estará en `localhost:1433`. El entorno no instala ni expone una interfaz web para la base de datos.

## Credenciales por Defecto (seed de desarrollo)

| Usuario | Email | Contraseña | Rol |
|---------|-------|-----------|-----|
| admin | admin@example.com | Admin123!@# | Admin |
| user | user@example.com | User123!@# | User |

## Respuesta estándar

Todos los endpoints devuelven el mismo sobre (`ApiResponse<T>`):

```json
{
  "success": true,
  "data": { "...": "..." },
  "message": "Operación completada exitosamente",
  "errors": [],
  "timestamp": "2026-08-04T12:00:00Z"
}
```

Los listados (`GET` sin `{id}`) devuelven, además, `data` como un objeto paginado:

```json
{
  "success": true,
  "data": {
    "items": [ ],
    "page": 1,
    "pageSize": 10,
    "total": 42,
    "totalPages": 5,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

Acepta `?page=1&pageSize=10` (por defecto 10, máximo 100).

## Endpoints Principales

### Auth (sin token)
- `POST /api/auth/register` - Registrar (valida username, password fuerte y email/username únicos)
- `POST /api/auth/login` - Login

### Products (con token)
- `GET /api/products?page=&pageSize=` - Listar (paginado)
- `GET /api/products/{id}`
- `GET /api/products/low-stock` - Productos que alcanzaron su mínimo configurado (Admin)
- `POST /api/products` - Crear (Admin)
- `PUT /api/products/{id}` - Actualizar (Admin)
- `DELETE /api/products/{id}` - Eliminar (Admin, bloqueado si el producto tiene compras/ventas)

### Categories
- `GET /api/categories?page=&pageSize=`
- `GET /api/categories/{id}`
- `POST /api/categories` (Admin)
- `PUT /api/categories/{id}` (Admin)
- `DELETE /api/categories/{id}` (Admin)

### Purchases (Admin) & Sales
- `POST /api/purchases` - Registrar compra (incrementa stock, atómico)
- `GET /api/purchases?page=&pageSize=`
- `GET /api/purchases/{id}`
- `POST /api/sales` - Registrar venta con múltiples productos (captura automáticamente cada precio unitario vigente, calcula el total y descuenta stock de forma atómica)
- `GET /api/sales?page=&pageSize=` (Admin)
- `GET /api/sales/{id}`
- `GET /api/sales/reports/top-products?top=10` (Admin) - Reporte agregado vía Dapper
- `POST /api/inventory-operations/adjustments` (Admin) - Ajuste por conteo, merma o daño; requiere motivo y nunca permite saldo negativo
- `POST /api/inventory-operations/returns` (Admin) - Devolución de una venta; requiere motivo y no permite devolver más de lo vendido

Las compras y ventas se registran dentro de una transacción con reintento
automático ante conflictos de concurrencia (dos requests tocando el stock del
mismo producto al mismo tiempo); si el conflicto persiste tras varios
reintentos, responden `409 Conflict`.

Las ventas no aceptan un precio ni un total del cliente. El servidor captura el
precio unitario vigente al momento de registrar la venta y el total de cada
consulta se deriva de ese precio y la cantidad. Por eso, cambiar después el
precio de catálogo no modifica el historial ni los reportes ya registrados.
Cada entrada y salida nueva queda vinculada al usuario que la registró y deja un
movimiento de inventario inmutable con el saldo posterior. El stock ya no se
modifica al editar un producto: se incrementa mediante compras y disminuye mediante
ventas.
Cada producto configura su propio stock mínimo; el tablero y catálogo destacan los
productos que requieren reabastecimiento.

## Flujos de negocio demostrables

1. **Punto de venta:** busca un producto por SKU, agrega varias líneas a la venta
   y registra el documento. Los precios y totales se calculan en el servidor.
2. **Reabastecimiento:** registra una compra para incrementar el inventario y
   conservar el costo documentado.
3. **Control físico:** un administrador registra un ajuste positivo o negativo
   con un motivo (merma, daño o conteo); no se permite que el saldo sea negativo.
4. **Devolución:** un administrador asocia la devolución con una venta y el
   sistema impide exceder la cantidad vendida.
5. **Reabastecimiento preventivo:** configura el stock mínimo por producto y
   consulta en el tablero los artículos que ya requieren reposición.

Cada flujo crea un movimiento inmutable de inventario que conserva responsable,
motivo, variación y saldo resultante.

Si ya existe una base creada con la versión anterior, ejecuta una sola vez, en este
orden, `sql/migrate_sales_total_price_to_unit_price.sql` y
`sql/migrate_sales_to_documents.sql` antes de desplegar esta versión. Las ventas
históricas se convierten en documentos de una línea y se marcan con el usuario técnico
`system_migration`; no se generan movimientos de inventario ficticios para el pasado.

### Users
- `GET /api/users/me` - Perfil del usuario autenticado
- `GET /api/users?page=&pageSize=` (Admin)

### Health
- `GET /health/live` - ¿API activa?
- `GET /health/ready` - ¿BD disponible?

### Docs
- `GET /swagger` - API interactiva

## Ejemplos cURL

### Login
```bash
curl -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Admin123!@#"
  }'
```

Copiar el `data.token` de la respuesta.

### Crear Producto
```bash
curl -X POST "http://localhost:5000/api/products" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Laptop",
    "price": 999.99,
    "stock": 50,
    "minimumStock": 5
  }'
```

### Crear Venta
```bash
curl -X POST "http://localhost:5000/api/sales" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "lines": [
      { "productId": "PRODUCT_ID", "quantity": 5 },
      { "productId": "SECOND_PRODUCT_ID", "quantity": 2 }
    ]
  }'
```

## Estructura del Proyecto

```
inventory-api/
├── ci_cd/
│   ├── docker/
│   │   └── docker-compose.yml   # SQL Server para desarrollo local
│   └── kubernetes/
│       ├── deployment.yaml
│       ├── service.yaml
│       └── secrets.yaml
├── src/
│   ├── Inventory.Api/            # Controllers, DTOs, Middleware, Dockerfile
│   ├── Inventory.Application/    # Interfaces, servicios de negocio (Sale/Purchase), excepciones
│   ├── Inventory.Domain/         # Entidades
│   └── Inventory.Infrastructure/ # EF Core, repositorios, Dapper, seed
├── tests/
│   └── Inventory.Api.Tests/      # Unit tests (servicios, validadores, controllers)
├── sql/
│   └── create_db.sql             # Schema alternativo sin EF migrations
├── .dockerignore
├── .github/
│   └── workflows/
│       └── ci.yml                # Build + test + (en main) build & push de imagen Docker
└── README.md
```

## Tests

```bash
# Unitarios: rápidos, no necesitan Docker ni BD (usan fakes en memoria)
dotnet test --filter "Category!=Integration"

# Integración: levantan un SQL Server real y descartable con Testcontainers.
# Requieren Docker corriendo. Prueban el fix de concurrencia de stock contra
# una BD de verdad, no fakes.
dotnet test --filter "Category=Integration"
```

Ver [tests/Inventory.Api.Tests/README.md](./tests/Inventory.Api.Tests/README.md)
para más detalle. La CI corre los unitarios en cada push/PR y los de
integración solo al pushear a `main`/`master` (ver `.github/workflows/ci.yml`).

## Deployment

Ver [DOCKER.md](./DOCKER.md) para instrucciones de Docker y Kubernetes.

## Licencia

MIT

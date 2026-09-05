# Docker & Kubernetes

## Docker Compose (Desarrollo Local)

```bash
cd ci_cd/docker
docker-compose up -d
cd ../..
```

Levanta:
- **SQL Server** en `localhost:1433`

No se inicia una interfaz web para la base de datos; el único servicio del
entorno local es SQL Server.

Luego ejecutar:

```bash
dotnet run --project src/Inventory.Api
```

API en `http://localhost:5000`

## Docker Image

### Build

```bash
docker build -f src/Inventory.Api/Dockerfile -t inventory-api:latest .
```

### Run

```bash
docker run -p 5000:80 \
  -e "ConnectionStrings__DefaultConnection=Server=sqlserver;Database=InventoryDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;" \
  -e "Jwt__Secret=tu-clave-min-32-caracteres" \
  -e "ASPNETCORE_ENVIRONMENT=Production" \
  inventory-api:latest
```

API en `http://localhost:5000`

## Kubernetes

### Apply manifests

```bash
kubectl apply -f ci_cd/kubernetes/secrets.yaml
kubectl apply -f ci_cd/kubernetes/deployment.yaml
kubectl apply -f ci_cd/kubernetes/service.yaml
```

### Port Forward

```bash
kubectl port-forward svc/inventory-api 5000:80
```

API en `http://localhost:5000`

### See Status

```bash
kubectl get pods
kubectl logs deployment/inventory-api
```

## GitHub Actions

CI pipeline configurado en `.github/workflows/ci.yml`

Triggers:
- Push a `main`/`master` → Build + Test + build & push de la imagen a GHCR
- Pull requests → Build + Test (sin publicar imagen)

No hay un job de "deploy" automático: no hay todavía un destino de despliegue
real configurado. Aplica los manifiestos de `ci_cd/kubernetes/` manualmente
con `kubectl` cuando tengas un clúster al que apuntar.

## Variables de Entorno

- `ConnectionStrings__DefaultConnection` - Connection string SQL Server
- `Jwt__Secret` - Clave secreta (min 32 caracteres)
- `Jwt__Issuer` - Emisor JWT
- `Jwt__Audience` - Audiencia JWT
- `Jwt__ExpirationHours` - Horas de expiración
- `ASPNETCORE_ENVIRONMENT` - Development/Production

## Production Checklist

- [ ] Cambiar JWT__Secret a valor seguro
- [ ] Cambiar credenciales SQL Server
- [ ] Habilitar HTTPS
- [ ] Configurar Rate Limiting
- [ ] Configurar CORS específicamente
- [ ] Habilitar logging en production
- [ ] Backup automático de BD

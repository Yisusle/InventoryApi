# Multi-stage Dockerfile para Inventory.Api
# - Compila con SDK en stage de build
# - Publica artefacto optimizado
# - Usa imagen runtime más ligera
# - Ejecuta como usuario non-root

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar todos los .csproj (incluyendo los referenciados) y restaurar primero
# para aprovechar el cache de capas de Docker.
COPY ["src/Inventory.Api/Inventory.Api.csproj", "src/Inventory.Api/"]
COPY ["src/Inventory.Domain/Inventory.Domain.csproj", "src/Inventory.Domain/"]
COPY ["src/Inventory.Application/Inventory.Application.csproj", "src/Inventory.Application/"]
COPY ["src/Inventory.Infrastructure/Inventory.Infrastructure.csproj", "src/Inventory.Infrastructure/"]
RUN dotnet restore "src/Inventory.Api/Inventory.Api.csproj"

# Copiar todo y publicar (release, sin símbolos innecesarios)
COPY . .
WORKDIR "/src/src/Inventory.Api"
RUN dotnet publish -c Release -o /app --no-restore /p:PublishSingleFile=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
LABEL org.opencontainers.image.source="https://example.com/your-repo" \
      org.opencontainers.image.licenses="MIT"
WORKDIR /app
ENV ASPNETCORE_URLS="http://+:80"
ENV DOTNET_RUNNING_IN_CONTAINER=true

# curl is not present in the aspnet base image; needed for HEALTHCHECK below.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

# Crear usuario no-root y ajustar permisos
RUN addgroup --system appgroup || true \
 && adduser --system --ingroup appgroup appuser || true \
 && chown -R appuser:appgroup /app
USER appuser

EXPOSE 80

# Healthcheck: call liveness endpoint. Under Kubernetes this is superseded by
# the liveness/readiness probes in ci_cd/kubernetes/deployment.yaml; this one
# matters for plain `docker run`/docker-compose usage.
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 CMD curl -f --silent --show-error http://localhost/health/live || exit 1

ENTRYPOINT ["dotnet", "Inventory.Api.dll"]

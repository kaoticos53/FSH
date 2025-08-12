# Despliegue en Proxmox con Docker

Este documento describe cómo publicar la aplicación (API + Blazor + PostgreSQL) en un servidor Proxmox utilizando Docker y Docker Compose.

## Requisitos

- VM Debian/Ubuntu en Proxmox (recomendado). También puede usarse LXC con nesting habilitado (más ajustes necesarios)
- Docker Engine y Docker Compose Plugin instalados
- Puertos expuestos en el host: 7000 (API), 7100 (Blazor), 5533 (PostgreSQL, opcional)

## Estructura creada

- `deploy/docker/docker-compose.yml`: orquesta PostgreSQL, API y Blazor
- `deploy/docker/.env.sample`: variables de entorno de ejemplo
- `src/Dockerfile.Api`: imagen de la API
- `src/Dockerfile.Blazor`: ahora acepta `--build-arg API_BASE_URL` y genera `wwwroot/appsettings.json` en build

## Variables y secretos

1. Copia `.env.sample` a `.env` y ajusta valores:
   - `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`
   - `API_HOST_PORT`, `BLAZOR_HOST_PORT`, `POSTGRES_HOST_PORT`
   - `API_BASE_URL` (ej. `http://IP:7000/` o `https://api.tu-dominio/`)
   - `BLAZOR_PUBLIC_URL` (ej. `http://IP:7100` o `https://app.tu-dominio`)
   - `JWT_KEY` (clave fuerte)
   - `CORS_ALLOWED_ORIGIN_0` (igual a `BLAZOR_PUBLIC_URL` o tu dominio)

## Construcción y arranque

Dentro de `deploy/docker/`:

```bash
# Construir imágenes
docker compose build

# Arrancar en segundo plano
docker compose up -d

# Ver estado
docker compose ps

# Logs (ejemplo API)
docker compose logs -f webapi
```

## Migración y seed de base de datos

- La API ejecuta automáticamente migraciones y seed en el arranque por cada tenant configurado.
- Se crea el tenant raíz por defecto (`root`) y se aplican migraciones/seed de módulos (Identity, Catalog, Todo).
- Usuario admin por defecto (tenant root):
  - Email: `admin@root.com`
  - Password: `123Pa$$word!`

Notas técnicas:

- Multitenancy: `UseMultitenancy()` inicializa el tenant store y ejecuta `IDbInitializer.MigrateAsync/SeedAsync` por tenant (`src/api/framework/Infrastructure/Tenant/Extensions.cs`).
- Origin de archivos/imágenes: `OriginOptions__OriginUrl` debe apuntar a la URL de la API (sirve `/assets/...`).

### Crear un nuevo tenant (opcional)

Endpoint: `POST /api/tenants`

Ejemplo de payload:

```json
{
  "id": "tenant01",
  "name": "Tenant 01",
  "connectionString": "",
  "adminEmail": "admin@tenant01.com",
  "issuer": null
}
```

- Deja `connectionString` vacío para usar la conexión por defecto del sistema.
- Tras crear el tenant, la API migra y semilla automáticamente las tablas de ese tenant.

## Acceso y verificación

- Blazor: `http://IP_SERVIDOR:7100/`
- API: `http://IP_SERVIDOR:7000/` (agrega `/swagger` si está habilitado)
- Base de datos (opcional desde host): `localhost:5533`

Comprobaciones útiles:

```bash
# Health (Listo/Vivo)
curl -i http://IP_SERVIDOR:7000/health
curl -i http://IP_SERVIDOR:7000/alive

# Obtener token (admin root por defecto)
curl -s -X POST http://IP_SERVIDOR:7000/api/token \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@root.com","password":"123Pa$$word!"}'
```

## Consideraciones de producción

- TLS y dominios: añade Traefik o Caddy como reverse proxy en el mismo Compose.
- CORS/Origin: ya parametrizados mediante variables (`OriginOptions__OriginUrl`, `CorsOptions__AllowedOrigins__0`).
- Persistencia: volumen `pgdata` creado para PostgreSQL.
- Healthchecks: PostgreSQL con healthcheck. Puedes añadir healthcheck para la API si expones `/health`.
- Escalabilidad: para K8s, migra a manifiestos equivalentes.

## Actualizaciones

```bash
# Traer cambios
git pull

# Rebuild si cambian fuentes
docker compose build webapi blazor

# Reinicio rápido
docker compose up -d
```

## Solución de problemas

- Errores de conexión DB: verifica `DatabaseOptions__ConnectionString` (el host debe ser `postgres`, puerto 5432 interno)
- CORS bloqueado: revisa `CORS_ALLOWED_ORIGIN_0` y `BLAZOR_PUBLIC_URL`
- JWT inválido: coloca una `JWT_KEY` fuerte y consistente en cada despliegue

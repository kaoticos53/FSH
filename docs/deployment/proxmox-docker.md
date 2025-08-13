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
- Origen público: `OriginOptions__OriginUrl` debe apuntar a la URL pública del frontend (Blazor), usada por la API para generar enlaces correctos (assets y callbacks).

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

## Notas Proxmox (VM vs LXC)

- VM (recomendado): menos fricción con Docker y systemd.
- LXC: habilitar nesting y cgroups/overlayfs.
  - En Proxmox: Opciones del contenedor → Características → Marcar "Nesting".
  - Asegurar: `features: keyctl=1,nesting=1` en la config del CT.
  - Si hay problemas con overlay: usar `fuse-overlayfs` o `vfs` como storage driver para Docker.

## Reverse proxy y TLS (Traefik)

Ejemplo básico de Traefik en el mismo Compose con Let's Encrypt:

```yaml
services:
  traefik:
    image: traefik:v3.1
    container_name: fsh_traefik
    command:
      - "--providers.docker=true"
      - "--entrypoints.web.address=:80"
      - "--entrypoints.websecure.address=:443"
      - "--certificatesresolvers.le.acme.tlschallenge=true"
      - "--certificatesresolvers.le.acme.email=${LETSENCRYPT_EMAIL}"
      - "--certificatesresolvers.le.acme.storage=/letsencrypt/acme.json"
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - "/var/run/docker.sock:/var/run/docker.sock:ro"
      - "letsencrypt:/letsencrypt"
    restart: unless-stopped

  webapi:
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.api.rule=Host(`${API_HOST}`)"
      - "traefik.http.routers.api.entrypoints=websecure"
      - "traefik.http.routers.api.tls.certresolver=le"
      - "traefik.http.services.api.loadbalancer.server.port=8080"

  blazor:
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.blazor.rule=Host(`${BLAZOR_HOST}`)"
      - "traefik.http.routers.blazor.entrypoints=websecure"
      - "traefik.http.routers.blazor.tls.certresolver=le"
      - "traefik.http.services.blazor.loadbalancer.server.port=80"

volumes:
  letsencrypt:
```

Variables típicas en `.env`:

```dotenv
API_HOST=api.tu-dominio.com
BLAZOR_HOST=app.tu-dominio.com
LETSENCRYPT_EMAIL=tu-email@dominio.com
```

Notas:
- Con Traefik no es necesario publicar puertos 7000/7100 hacia el host; expone 80/443 y enruta por hostnames.
- Mantén `API_BASE_URL` y `BLAZOR_PUBLIC_URL` con `https://` y los dominios finales para compilar/servir correctamente.

## Healthchecks (API)

Añade un healthcheck a la API para reinicios automáticos si algo falla:

```yaml
webapi:
  healthcheck:
    test: ["CMD", "wget", "-qO-", "http://localhost:8080/health"]
    interval: 15s
    timeout: 5s
    retries: 5
```

Comprobaciones manuales:

```bash
curl -i http://IP_SERVIDOR:7000/health
curl -i http://IP_SERVIDOR:7000/alive
```

## Backups y restore

- PostgreSQL (dump lógico):

```bash
# Backup
docker exec -t fsh_postgres pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" > backup_$(date +%F).sql

# Restore (con servicio detenido o nueva DB)
docker exec -i fsh_postgres psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" < backup.sql
```

- Volúmenes (persistencia):
  - DB: `pgdata` (incluye WAL y datos). Para copias consistentes, preferible dump lógico o parada temporal del contenedor.
  - App: si añades volúmenes para logs o uploads, inclúyelos en la política de backup.

- Programación: usar `cron` en la VM para dumps diarios/semanales y rotación.

## Observabilidad y logs

- Logs: la API emite logs (Serilog). Usa `docker compose logs` o monta un volumen para exportarlos a un colector (Loki/ELK).
- Métricas: expón `/metrics` si Prometheus está habilitado y documentado; de lo contrario, planifica integración (Future Work).
- Trazas: planificar OpenTelemetry para traces distribuidos en el roadmap.

## Operativa post-deploy

- Smoke tests tras cada despliegue:

```bash
curl -Sf http://IP_SERVIDOR:7000/health
curl -Sf http://IP_SERVIDOR:7000/alive
curl -Sf http://IP_SERVIDOR:7100/ || true  # si no usas proxy
```

- Actualizaciones:

```bash
git pull
# Recompilar si cambian fuentes
docker compose build webapi blazor
# Reaplicar
docker compose up -d
```

- Rollback:
  - Etiqueta imágenes con versiones (tags) y conserva la anterior.
  - Si falla un despliegue, vuelve al tag previo y `docker compose up -d`.

## Seguridad

- No publiques Postgres hacia Internet salvo necesidad; si es necesario, restringe por firewall y redes.
- Usa `JWT_KEY` fuerte y rota periódicamente.
- Revisa CORS: `CORS_ALLOWED_ORIGIN_0` debe coincidir con el origen público del frontend.
- Mantén `.env` fuera de Git (ya ignorado) y usa secretos/variables seguras en CI/CD.

## Solución de problemas (ampliado)

- DB: si `pg_isready` falla, revisa credenciales y volumen `pgdata`.
- API: si no arranca, valida `DatabaseOptions__ConnectionString` y que `postgres` está `healthy`.
- Blazor: si no carga, verifica `API_BASE_URL` en build y CORS en la API.

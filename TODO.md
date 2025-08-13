# TODO

## Estado actual

- Aplicación .NET 9.0.5
- Frontend Blazor Client en `src/apps/blazor/client/` escuchando en `https://localhost:7100`.
- Página `Dashboard` creada en `src/apps/blazor/client/Pages/Dashboard.razor` (datos reales básicos: conteos).
- Script `run.ps1` abre automáticamente `https://localhost:7100/`.

## Tareas

- [x] Crear página Dashboard con tarjetas de métricas placeholder (MudBlazor).
- [x] Actualizar `run.ps1` para abrir el navegador automáticamente.
- [x] Automatizar login en Blazor Client y enviar formulario.
- [x] Verificar acceso a `/dashboard` en `https://localhost:7100` tras login.
- [ ] Implementar métricas reales en Dashboard:
  - [x] Exponer/usar métodos en `IApiClient` para consumir endpoints de búsqueda/listado (existentes).
  - [x] Integrar llamadas en `Dashboard.razor` y mostrar resultados (conteos de usuarios, roles, productos, marcas).
  - [x] Manejo de errores y estados de carga en UI: loaders y toasts integrados; skeletons pendientes.
- [ ] Autorización:
  - [x] Confirmar que el acceso a Dashboard requiere permisos adecuados (`Permissions.Dashboard.View`).
  - [ ] Asegurar visibilidad del enlace en `NavMenu` según permisos.
- [ ] UX/UI:
  - [ ] Añadir widgets adicionales (gráficas, tendencias, últimos eventos).
  - [x] Añadir loaders mientras se cargan métricas.
  - [ ] Añadir skeletons.
- [ ] Pruebas:
  - [ ] Añadir tests de integración para endpoints de métricas.
  - [ ] Añadir tests de cliente (si aplica) o validaciones básicas.
- [ ] Documentación:
  - [x] Actualizar `CHANGELOG.md` con los cambios.
  - [x] Mantener `ROADMAP.md` y este `TODO.md` al día.
  - [x] Actualizar `docs/deployment/proxmox-docker.md` con migraciones/seed automáticos, ejemplo `POST /api/tenants` y `POST /api/token`.

## Despliegue (Proxmox + Docker Compose)

- [x] Añadir `Dockerfile.Api` y `Dockerfile.Blazor` (build ARG `API_BASE_URL` y generación de `appsettings.json`).
- [x] Crear `deploy/docker/docker-compose.yml` y `deploy/docker/.env.sample`.
- [x] Alinear CORS en compose: usar `CORS_ALLOWED_ORIGIN_0`.
- [ ] Validar despliegue end-to-end en servidor Proxmox:
  - Nota: validado localmente con Docker Compose; replicar en Proxmox.
  - [x] Copiar `.env.sample` a `.env` y configurar variables.
  - [x] `docker compose build` (API, Blazor) y `docker compose up -d`.
  - [x] Verificar `/health` y `/alive` en API.
  - [ ] Obtener token con `POST /api/token` (admin root) y probar endpoints protegidos.
  - [ ] Crear tenant con `POST /api/tenants` y comprobar migraciones/seed por tenant.
  - [x] Acceder a Blazor y confirmar conectividad contra la API.
  - [ ] Opcional: configurar reverse proxy (Traefik/Caddy) con TLS.

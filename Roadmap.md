# Roadmap

## Hito 1: Estabilización y Calidad del Código

- [x] Corregir todos los errores y advertencias de compilación en la solución.
- [x] Ejecutar todos los tests unitarios y asegurar que pasan correctamente.
- [x] Resolver `NullReferenceException` en `IdentityDbContext` tests de unidad.
- [x] Revisar y mejorar la cobertura de tests para los componentes críticos.
- [x] Refactorizar tests de `UpdatePermissions` para usar `IdentityDbContext` real con EF InMemory y `DbSet` real para `AuditTrails`.
- [x] Sustituir mocks de `IMultiTenantContextAccessor<FshTenantInfo>` por stub concreto o mocks duales (genérico y no genérico) según sea necesario.
- [x] Uniformar el patrón de creación de `RoleService` en `UpdatePermissionsTests.cs`, `UpdatePermissionsStandaloneTests.cs` y `UpdatePermissionsTestsFixed.cs`.
- [x] Verificar que todos los tests de `Identity/Roles` pasan y que las aserciones validan la persistencia real en `DbContext`.
- [x] Consolidar utilidades multi-tenant en helper compartido `tests/unit/FSH.Framework.Core.Tests/Shared/TestMultiTenantAccessor.cs`.
- [x] Añadir test para filtrar permisos Root en tenant no root (`UpdatePermissions_ShouldNotAddRootPermissions_ForNonRootTenant`).
- [x] Aumentar cobertura de tests de `RoleService.UpdatePermissionsAsync` (permisos Root en Root, duplicados, `CreatedBy`).
- [x] Documentar el patrón de uso de `IdentityDbContext` InMemory en los tests (`docs/testing/IdentityDbContextInMemory.md`).
- [x] Añadir documentación XML a las APIs públicas en los proyectos `Core` e `Infrastructure`.

## Próximos pasos

- [x] Aumentar cobertura de tests de `RoleService.UpdatePermissionsAsync` (casos admin, tenants no root, errores de `RemoveClaimAsync`).
- [x] Consolidar utilidades de pruebas multi-tenant en un helper compartido.
- [x] Documentar el patrón de uso de `IdentityDbContext` InMemory en los tests (`docs/testing/IdentityDbContextInMemory.md`).

### UI: Blazor Dashboard

- [x] Crear página Dashboard en `src/apps/blazor/client/Pages/Dashboard.razor` con ruta `/dashboard`.
- [x] Ejecutar Blazor Client en `https://localhost:7100` (perfiles en `Properties/launchSettings.json`).
- [x] Automatizar apertura con `run.ps1` a `https://localhost:7100/`.
- [x] Automatizar login en Blazor Client y envío de formulario.
- [x] Verificar acceso a `/dashboard` tras login (HTTPS) y ausencia de botón "Sign In".
- [x] Conectar métricas reales vía `IApiClient` y endpoints backend (usuarios, roles, productos, marcas) – conteos básicos integrados.
- [x] Autorización específica aplicada (`Permissions.Dashboard.View`) además del `[Authorize]` global.

- [ ] UX/UI:
  - [x] Loaders durante carga de métricas (MudProgressCircular).
  - [x] Manejo de errores y notificaciones (MudAlert, ISnackbar).
  - [ ] Skeletons.
  - [ ] Gráficas y tendencias.

### Despliegue (Docker Compose)

- [x] Crear `.env` desde `.env.sample` y configurar variables.
- [x] `docker compose build` (API, Blazor) y `docker compose up -d`.
- [x] Verificar API: `GET /alive` 200, `GET /health` 200, `GET /metrics` 200.
- [x] Verificar Blazor: respuesta 200 en raíz y acceso al Dashboard.
- [x] Corregido healthcheck de Postgres: `pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}`; contenedor en estado `healthy`.
- [x] Documentado reverse proxy con Traefik y creado override opcional `deploy/docker/docker-compose.traefik.yml`; añadido `healthcheck` a `webapi` y variables opcionales en `.env.sample`.
- [ ] Obtener token con `POST /api/token` (admin root) y probar endpoints protegidos.
- [ ] Crear tenant con `POST /api/tenants` y comprobar migraciones/seed por tenant.
- [ ] Replicar en servidor Proxmox y documentar ajustes.

### Estado actual (2025-08-10)

- Tests unitarios: 123/123 pasados en `FSH.Framework.Core.Tests` (NET 9, `Microsoft.EntityFrameworkCore.InMemory` 9.0.2).
- Resultados TRX: `tests/unit/FSH.Framework.Core.Tests/TestResults/TestResults.trx`.
- Tests de integración: 12/12 pasados en `FSH.Catalog.Infrastructure.Tests`.
- Resultados TRX (Infra): `tests/integration/FSH.Catalog.Infrastructure.Tests/TestResults/TestResults.trx`.
- Cobertura añadida: test de propagación de `DbUpdateException` en `RoleService.UpdatePermissionsAsync` cuando falla `SaveChangesAsync`.
- Infra de pruebas: helper `BuildRoleEndpointApp` en `TestFixture` para `TestServer` y pruebas de endpoint `UpdateRolePermissions`.
- Manejo de errores en endpoint: añadido test 404 NotFound cuando el rol no existe; `BuildRoleEndpointApp` registra `CustomExceptionHandler` y `ProblemDetails` para mapear la `NotFoundException` a 404.
- Soporte añadido: `CancellationToken` en `IRoleService.UpdatePermissionsAsync` y en el endpoint correspondiente.
- Mejora aplicada: desduplicación y normalización (Trim + case-insensitive) de permisos en `UpdatePermissionsAsync`.
- Validación: `UpdatePermissionsValidator` ahora valida cada elemento (no nulo ni solo whitespace) y cuenta con tests dedicados.
- Endpoint: `UpdateRolePermissionsEndpoint` aplica `IValidator<UpdatePermissionsCommand>` y devuelve 400 (`ValidationProblem`) cuando la entrada es inválida.

- Correcciones tests Tenant: en `CreateTenantValidatorTests` se eliminaron setups innecesarios de `IConnectionStringValidator.TryValidate` cuando `ConnectionString` es nula o vacía (la regla hace short-circuit); esto resolvió fallos de verificación Moq.
- Ejecución de comandos: adoptado patrón sentinel `[[CASCADE_DONE]]` para detectar fin de comandos en PowerShell de forma fiable.
- Cobertura actual (último `coverage.cobertura.xml`): global ≈ 21.19%, `FSH.Framework.Core` ≈ 53.91%.
- Informe HTML de cobertura regenerado: `coverage-report/index.html`.

## Hito 2: Cobertura ≥90%

- [ ] Medir cobertura global con Coverlet y generar reporte HTML con ReportGenerator.
  - Comando sugerido: `dotnet test FSH.sln -c Release -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura` y `reportgenerator`.
  - Umbral en CI: `-p:Threshold=90 -p:ThresholdType=line`.

- [ ] FSH.Framework.Core (prioridad alta)
  - [x] `FshCore` (get_Name) – test simple de propiedad.
  - [ ] Tenant Features (Create/Disable/Get/GetById/UpgradeSubscription):
    - [x] Handlers: interacciones con `ITenantService`, caminos felices y errores.
    - [x] Validators: entradas válidas/ inválidas y mensajes esperados (parcial: Activate/Disable/UpgradeSubscription).
  - [x] DTOs TenantDetail (`Core.Tenant.Dtos` y `Core.Tenancy.Dtos`): cobertura de valores por defecto y setters/getters.
  - [x] `CustomExceptionHandler`: mapeo de `ValidationException` (400), `NotFoundException` (404), `ForbiddenException` (403), desconocidas (500).

  - [x] Consolidar duplicados de tests de `CreateTenantHandler` detectados en:
    - `tests/unit/FSH.Framework.Core.Tests/Tenant/CreateTenantHandlerTests.cs`
    - `tests/unit/FSH.Framework.Core.Tests/Tenant/Features/Handlers/CreateTenantHandlerTests.cs`
    - Consolidado: eliminado `tests/unit/FSH.Framework.Core.Tests/Tenant/CreateTenantHandlerTests.cs`; se mantiene `Tenant/Features/Handlers/CreateTenantHandlerTests.cs` como fuente de verdad.

- [ ] Endpoints de Roles (Framework API)
  - [ ] `UpdateRolePermissionsEndpoint`:
    - [x] 401 Unauthorized (NoAuthHandler)
    - [x] Validación 400 (shape)
    - [x] Idempotencia
    - [x] 403 Forbidden (falta de permisos)
    - [x] 404 NotFound (rol inexistente)
    - [x] 500 Internal Server Error (excepción no controlada -> middleware)
  - [ ] Otros endpoints de Roles (si aplican): 200/201/204 y errores 404/409/422.

- [ ] Infrastructure (Repos / UnitOfWork / DbContext)
  - [x] Catalog.Infrastructure: CRUD repositorio con SQLite InMemory (ProductRepository).
  - [x] Catalog.Infrastructure: CRUD repositorio con SQLite InMemory (BrandRepository).
  - [x] Catalog.Infrastructure: Relación Product-Brand (Include y actualización de BrandId).
  - [x] Catalog.Infrastructure: Operaciones mixtas en una única SaveChanges (Add/Update/Delete).
  - [x] Catalog.Infrastructure: Aislamiento multi-tenant y verificación de filtros globales (IgnoreQueryFilters).
  - [x] Catalog.Infrastructure: `UnitOfWork` (transacciones) commit/rollback validados con SQLite InMemory.
  - [x] Catalog.Infrastructure: Concurrencia de actualización tras eliminación lanza `DbUpdateConcurrencyException`.
  - [x] Catalog.Infrastructure: Soft delete de `Brand` con `Product` dependientes verificado (test con `Detach` para evitar `ClientSetNull`).
  - [ ] `IdentityDbContext`: configuración, claves y auditoría mínima.

- [ ] Módulos (Catalog, Todo)
  - [ ] Application/Domain: Commands/Queries (caminos felices, validación, reglas, Domain Events).
  - [ ] Infraestructura del módulo (si existe): repos/servicios.

- [ ] Server y Shared
  - [x] Server: health endpoints, CORS, versionado, OpenAPI registrado.
  - [ ] Shared: helpers/utilidades puras.

- [ ] Tooling / CI / Docs
  - [x] Crear script `scripts/test-coverage.ps1`.
  - [x] Añadir `dotnet-reportgenerator-globaltool` como herramienta local (`.config/dotnet-tools.json`) y usarlo para generar reportes.
  - [x] Gating de cobertura en CI a 90%.
  - [x] Ignorar artefactos de cobertura en Git y limpiar índice (`coverage-report/`, `deploy/docker/.env`).
  - [ ] Documentación: `docs/testing/RepositoryAndUnitOfWork.md`, `docs/testing/DomainEvents.md`.

## Hito 3: Arquitectura, Observabilidad y Seguridad

- __Observabilidad (OpenTelemetry + Serilog)__
  - [ ] Integrar OpenTelemetry (tracing/metrics/logs) en `src/api/server/Program.cs` y `src/api/framework/Infrastructure/Logging/Serilog/`.
  - [ ] Propagar `TraceId`/`TenantId` en logs y respuestas (correlación) usando `StaticLogger` y middlewares.
  - [ ] Instrumentar EF Core (ActivitySource/diagnostics) y `HttpClient`.
  - [ ] Añadir HealthChecks UI y cubrir `Infrastructure/HealthChecks/HealthCheckEndpoint` y `HealthCheckMiddleware` con tests.

- __Seguridad y autorización__
  - [ ] Rotación de claves JWT y validación end-to-end del flujo de refresh tokens.
  - [ ] Tests de endpoint para `Users` y `Tokens` (200/400/401/403/404/422/500) en `Infrastructure.Identity.Users.Endpoints.*` y `Infrastructure.Identity.Tokens.Endpoints.*`.
  - [ ] Cobertura completa de `RequiredPermissionAuthorizationHandler` y políticas por endpoint (Catalog/Todo).
  - [ ] Planificación de 2FA y recuperación de cuenta (documentar alcance y dependencias).

- __Versionado y OpenAPI__
  - [ ] Versionado de API v1 en `src/api/server/Extensions.cs` y `Infrastructure/OpenApi/*` (agrupación de endpoints, example providers).
  - [ ] Documentar permisos por endpoint y añadir ejemplos en `Server.http`.

- __Rate limiting y cabeceras de seguridad__
  - [ ] Configurar `RateLimit` por tenant y por usuario con pruebas de límites y cabeceras `Retry-After`.
  - [ ] Completar `SecurityHeaders` (CSP, HSTS, X-Content-Type-Options) y tests de middleware/policies.

## Hito 4: Funcionalidades de negocio (Módulo Catalog)

- __Entidades y relaciones__
  - [ ] `Category` y relación N:N `ProductCategory`; endpoints y validadores en `Catalog.Application`/`Catalog.Infrastructure.Endpoints`.
  - [ ] `ProductImage` con almacenamiento en `Infrastructure/Storage` y endpoints de subida (validación tamaño/tipo, antivirus opcional).
  - [ ] `Inventory` y `StockMovement` con `ProductStockChangedEvent` (Domain Event) y handlers.
  - [ ] `Price` multi-moneda (`Currency`) con reglas de consistencia.

- __Búsqueda y filtros__
  - [ ] Especificaciones avanzadas (filtros/ordenación/paginación) y tests sobre `CatalogRepository`.
  - [ ] Búsqueda facetada o full-text (si aplica) y caching de resultados con invalidación por eventos.

- __Eventos y outbox__
  - [ ] Publicar eventos en creación/actualización (MediatR Domain Events).
  - [ ] Implementar patrón Outbox en `Infrastructure/Persistence` y dispatcher en `Infrastructure/Jobs`.
  - [ ] Pruebas de idempotencia y entrega al menos una vez.

- __Soft delete avanzado__
  - [ ] Endpoint de "restore" para `Brand`/`Product` y tests que verifiquen `HasQueryFilter` y auditoría.
  - [ ] Hard delete administrativo con registro en `AuditTrail`.

## Hito 5: Módulo Todo y Experiencia de usuario (API)

- __Dominio y features__
  - [ ] `TodoList` y `TodoItem` con `Assignee`, `DueDate`, `Reminder`, `Comment`.
  - [ ] Commands/Queries y validadores siguiendo Vertical Slice.
  - [ ] Endpoints en `TodoModule` y tests de endpoint (200/400/401/403/404/422/500).

- __Notificaciones y tiempo real__
  - [ ] Notificaciones con SignalR (cambios en tareas/listas) con autorización por hub.

- __Productividad__
  - [ ] Filtros, ordenación y paginación en endpoints.

## Hito 6: Rendimiento, Escalabilidad y Datos

- __EF Core y base de datos__
  - [ ] `AsNoTracking`/consultas compiladas en lecturas; batching de cambios; uso de `SaveChanges` optimizado.
  - [ ] Índices y constraints críticos; migraciones automatizadas y seeding multi-tenant en `DbInitializer`.

- __Conexiones y resiliencia__
  - [ ] Resiliencia (reintentos/timeouts) y connection pooling.

- __Jobs y procesos diferidos__
  - [ ] Configurar `Infrastructure/Jobs` (Hangfire) con colas por tenant; tests de activator y filtros.

- __Caching distribuido__
  - [ ] `DistributedCacheService` con backplane (Redis), expiraciones y políticas; invalidación por eventos de dominio.

## Backlog técnico (Cobertura y calidad por área)

- [x] `Auth.CurrentUserMiddleware`
- [ ] `Auth.Jwt.Extensions`
- [ ] `Caching.DistributedCacheService` y `Caching.Extensions`
- [ ] `Common.Extensions.EnumExtensions` y `RegexExtensions`
- [ ] `Cors.*`
- [ ] `HealthChecks.HealthCheckEndpoint` y `HealthCheckMiddleware`
- [ ] `Identity.Audit.*` (Service, EventHandler, Endpoints)
- [ ] `Identity.Users.Endpoints.*` y `Identity.Tokens.Endpoints.*`
- [ ] `Persistence.*` (`FshDbContext`, `Interceptors.AuditInterceptor`, `ModelBuilderExtensions`)
- [ ] `OpenApi.*`
- [ ] `Mail.SmtpMailService`
- [ ] `Logging.Serilog.*`
- [ ] `RateLimit.*` y `SecurityHeaders.*`

## Tooling / CI adicionales

- [x] Workflows en `.github/workflows` con gating de cobertura ≥90% y publicación de reportes HTML como artefacto.
- [ ] `dotnet format` y analizadores (FxCop/StyleCop) activados en `.csproj`.
- [ ] CodeQL/Dependabot y auditoría de vulnerabilidades NuGet.
- [ ] Scripts: `scripts/test-coverage.ps1`, `scripts/dev-setup.ps1`.
- [ ] Pipeline de empaquetado `FSH.StarterKit.nuspec` en releases.

## Documentación

- [ ] `docs/testing/RepositoryAndUnitOfWork.md`, `docs/testing/DomainEvents.md`.
- [ ] `docs/security/PermissionsMatrix.md`.
- [ ] `docs/observability/OpenTelemetry.md`.
- [ ] `docs/multi-tenancy/TenantResolution.md`.
- [ ] `docs/catalog/DomainModel.md`.
- [ ] `docs/api/Versioning.md`.

- [ ] Server y Shared
  - [ ] Server: health endpoints, CORS, versionado, OpenAPI registrado.
  - [ ] Shared: helpers/utilidades puras.

- [ ] Tooling / CI / Docs
  - [ ] Añadir `dotnet-reportgenerator-globaltool` y script `scripts/test-coverage.ps1`.
  - [ ] Gating de cobertura en CI a 90%.
  - [ ] Documentación: `docs/testing/RepositoryAndUnitOfWork.md`, `docs/testing/DomainEvents.md`.

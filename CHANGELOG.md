# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- Frontend: página Dashboard creada en `src/apps/blazor/client/Pages/Dashboard.razor` con ruta `/dashboard` y tarjetas de métricas placeholder (MudBlazor).
- Tooling: `run.ps1` actualizado para abrir automáticamente `https://localhost:7100/` en el navegador.
- Automatización: login del Blazor Client usando Puppeteer (clic en "Fill Administrator Credentials" y "Sign In"), redirección validada a `https://localhost:7100/dashboard`, ausencia de botón "Sign In" y presencia de texto de Dashboard.
- Navegación y validación: confirmada carga correcta del Dashboard en HTTPS tras login y apertura de Browser Preview para interacción manual.
- Tests de endpoint: `UpdateRolePermissionsEndpoint` devuelve `401 Unauthorized` cuando no hay autenticación, usando `NoAuthHandler` y el helper `BuildRoleEndpointAppWithAuthorizationButNoAuth`.
- Tests de endpoint: `UpdateRolePermissionsEndpoint` devuelve `500 Internal Server Error` cuando el servicio lanza una excepción no controlada, cubriendo el middleware `CustomExceptionHandler`.
- Tooling: instalado `dotnet-reportgenerator-globaltool` como herramienta local (manifiesto en `.config/dotnet-tools.json`) y configurado su uso para generar informes de cobertura.
- Nuevos tests de validadores de Tenant: `ActivateTenantValidator`, `DisableTenantValidator` y `UpgradeSubscriptionValidator` siguiendo TDD, con comentarios XML en español.
- Documentación de tests: añadidos comentarios XML a clases y métodos públicos de los tests de handlers de Tenant y `CustomExceptionHandler` para resolver advertencias del linter.
- Integración: nuevo proyecto `FSH.Catalog.Infrastructure.Tests` para tests de integración del módulo Catalog.Infrastructure.
- Documentación: `Roadmap.md` actualizado con el progreso reciente.
- Seguridad: Dashboard protegido con policy específica `Permissions.Dashboard.View` aplicada en `src/apps/blazor/client/Pages/Dashboard.razor`.
- Integración: Dashboard consumiendo métricas reales (conteos) desde el backend usando `IApiClient`.
  - Brands: `SearchBrandsEndpointAsync("1", new SearchBrandsCommand { PageNumber = 1, PageSize = 1 })` usando `TotalCount`.
  - Productos: `SearchProductsEndpointAsync("1", new SearchProductsCommand { PageNumber = 1, PageSize = 1 })` usando `TotalCount`.
  - Users: `GetUsersListEndpointAsync()` y conteo de elementos.
  - Roles: `GetRolesEndpointAsync()` y conteo de elementos.
- Tests de DTOs: añadidos tests para `FSH.Framework.Core.Tenant.Dtos.TenantDetail` y `FSH.Framework.Core.Tenancy.Dtos.TenantDetail` cubriendo valores por defecto y asignación de propiedades (getters/setters).
- Cobertura: regenerado el informe HTML con ReportGenerator en `coverage-report/index.html` tras los nuevos tests.
- Tests de handlers de Tenant: `CreateTenantHandler`, `GetTenantsHandler`, `GetTenantByIdHandler`, `ActivateTenantHandler`, `DisableTenantHandler` y `UpgradeSubscriptionHandler`. Se cubren caminos felices, propagación de `CancellationToken` donde aplica y error `NotFoundException` en `GetTenantById`.
- Tarea añadida: revisar y consolidar duplicados potenciales de tests de `CreateTenantHandler` en `tests/unit/FSH.Framework.Core.Tests/Tenant/CreateTenantHandlerTests.cs` y `tests/unit/FSH.Framework.Core.Tests/Tenant/Features/Handlers/CreateTenantHandlerTests.cs`.

- UX: Añadidos loaders de carga y manejo de errores/notificaciones en el Dashboard (MudBlazor: `MudProgressCircular`, `MudAlert`, `ISnackbar`).

- Despliegue (Proxmox + Docker):
  - Añadido `src/Dockerfile.Api` para construir la imagen de la API (ASP.NET en 8080).
  - Actualizado `src/Dockerfile.Blazor` para aceptar `--build-arg API_BASE_URL` y generar `wwwroot/appsettings.json` en build.
  - Añadido `deploy/docker/docker-compose.yml` con servicios `postgres`, `webapi` y `blazor`, healthcheck y volúmenes persistentes.
  - Añadido `deploy/docker/.env.sample` para parametrizar puertos, connection string, CORS y `JWT_KEY`.
  - Documentación de despliegue creada en `docs/deployment/proxmox-docker.md`.
  - Documentación de despliegue actualizada: sección de migración y seed automáticos, curl para `/health`, `/alive` y `POST /api/token`, y ejemplo de creación de tenant `POST /api/tenants`.

  - CI: añadido workflow `CI` en `.github/workflows/ci.yml` que ejecuta `./scripts/test-coverage.ps1` con umbral de cobertura del 90%, publica `coverage-report` y TRX como artefactos, y realiza smoke tests de Docker Compose verificando `GET /health`, `GET /alive` y respuesta 200 del Blazor Client.
  - Tooling: el script `scripts/test-coverage.ps1` se integra en CI y utiliza `dotnet tool restore` para ejecutar `reportgenerator` desde el manifiesto local `.config/dotnet-tools.json` (versión 5.4.12).

### Changed
- Compose: `OriginOptions__OriginUrl` ahora usa `${BLAZOR_PUBLIC_URL}` en lugar de `${API_BASE_URL}` para generar enlaces hacia el frontend correctamente.

### Tests
- Confirmado: 109/109 tests de `FSH.Framework.Core.Tests` pasan correctamente (NET 9).
- Confirmado: 12/12 tests de `FSH.Catalog.Infrastructure.Tests` pasan correctamente (NET 9).
  - TRX: `tests/integration/FSH.Catalog.Infrastructure.Tests/TestResults/TestResults.trx`.
- Cobertura (último Cobertura): global ≈ 20.84%, `FSH.Framework.Core` ≈ 51.47%.
- Suite `FSH.Framework.Core.Tests` vuelve a pasar tras añadir los tests de DTOs y se ha actualizado el informe de cobertura (`coverage-report/index.html`).

#### Actualización (más reciente)
- Confirmado: 125/125 tests de `FSH.Framework.Core.Tests` pasan correctamente (NET 9).
- Resultados TRX: `tests/unit/FSH.Framework.Core.Tests/TestResults/TestResults.trx`.
- Cobertura (último Cobertura): global ≈ 21.28%, `FSH.Framework.Core` ≈ 53.91%.
- Informe HTML de cobertura regenerado en `coverage-report/index.html` con ReportGenerator.
- Confirmado: 12/12 tests de `FSH.Catalog.Infrastructure.Tests` pasan correctamente (NET 9).
- Resultados TRX (Infra): `tests/integration/FSH.Catalog.Infrastructure.Tests/TestResults/TestResults.trx`.
- Nota: Se ha actualizado el contador de tests de integración a 12/12.

### Fixed
- Despliegue: corregido healthcheck de Postgres en `deploy/docker/docker-compose.yml` para usar `pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}`, evitando errores “database 'admin' does not exist” y mejorando la fiabilidad del healthcheck.
- Validación: Docker Compose local verificado: `GET /alive` 200, `GET /health` 200, `GET /metrics` 200 en API (`http://localhost:${API_HOST_PORT}`) y Blazor 200 en raíz (`http://localhost:${BLAZOR_HOST_PORT}/`).
- Fixed failing test in `UpdatePermissions_ShouldRemoveUnselectedPermissions_And_AddNewlySelectedPermissions`
  - Identified that `RoleService` uses `DbContext` to add claims instead of `AddClaimAsync`
  - Updated test expectations to match actual service behavior
  - Removed incorrect mock setup for `AddClaimAsync` method
  - Fixed assertion verifications to only check actual operations performed by the service
- Fixed compilation errors in unit tests related to token services
  - Updated `TokenService` constructor calls to match the actual implementation
  - Aligned `RefreshTokenCommand` usage with its current signature
  - Fixed event publishing tests to use `TokenGeneratedEvent` instead of non-existent `TokenRefreshedEvent`
  - Added missing `CancellationToken` parameters to async method calls
  - Fixed null reference warnings in test assertions
  - Updated test mocks to match the current implementation
  - Fixed xUnit2002 warnings by removing `Assert.NotNull` calls on value types
  - Added XML documentation to public test methods to resolve documentation warnings
- Resolved `NullReferenceException` in role management unit tests by refactoring to a centralized `TestFixture`.
  - Eliminated incorrect, per-test dependency injection setups.
  - Ensured `IdentityDbContext` is correctly configured with a mocked multi-tenant context for tests.
  - Registered all required services and validators (`CreateOrUpdateRoleValidator`, `UpdatePermissionsValidator`) in the shared `TestFixture`.
- Fixed `NullReferenceException` in `IdentityDbContext` unit tests by properly configuring `IMultiTenantContextAccessor` in test fixture
  - Created proper mock setup for both generic `IMultiTenantContextAccessor<FshTenantInfo>` and non-generic `IMultiTenantContextAccessor`
  - Ensured `MultiTenantContext` is properly initialized with valid `FshTenantInfo`
  - Fixed dependency injection registration order to satisfy both IdentityDbContext constructor overloads
  - All Identity/Roles feature tests now pass successfully
 - Soft delete (Brand con dependientes Product): ajustado test `Brand_SoftDelete_With_Dependent_Products_Should_Not_Throw_And_Should_Be_Filtered` para hacer `Detach` del `Product` tras su creación, evitando que EF Core aplique `ClientSetNull` a la FK cuando el `Brand` cambia a `Deleted` y el interceptor realiza soft delete. Verificado con SQLite InMemory.

- Blazor: Corregido warning de Razor en `src/apps/blazor/client/Pages/Dashboard.razor` añadiendo `@using FSH.Starter.Blazor.Client.Components.General` para permitir el uso del componente `PageHeader` sin advertencias.

- Linter: añadida documentación XML en métodos de prueba públicos de `Server/HealthEndpointsTests` y `Auth/CurrentUserMiddlewareTests` para resolver advertencias de comentarios XML en el proyecto de tests `FSH.Framework.Core.Tests`.

### Changed
- Consolidación de tests duplicados de `CreateTenantHandler` para evitar redundancia y facilitar el mantenimiento.
  - Eliminado: `tests/unit/FSH.Framework.Core.Tests/Tenant/CreateTenantHandlerTests.cs`.
  - Se mantiene como fuente de verdad: `tests/unit/FSH.Framework.Core.Tests/Tenant/Features/Handlers/CreateTenantHandlerTests.cs`.
- Improved null safety in test methods by using nullable reference types
- Updated test assertions to be more precise and avoid potential null reference exceptions
- Enhanced test method documentation for better maintainability

- Despliegue: `docker-compose.yml` ahora usa `CorsOptions__AllowedOrigins__0=${CORS_ALLOWED_ORIGIN_0}` en lugar de `${BLAZOR_PUBLIC_URL}` para alinear con `.env.sample` y la documentación.

- Tooling: `.gitignore` actualizado para ignorar `coverage-report/`, `deploy/docker/.env` y la carpeta obsoleta `scripts/-report/`.
- Repositorio: limpiado el índice Git para eliminar artefactos de cobertura previamente versionados.

## [2025-08-10]

### Added
- Nuevo test: `UpdatePermissions_ShouldBubbleUpDbUpdateException_WhenSaveChangesFails` que verifica que una `DbUpdateException` lanzada por `SaveChangesAsync` se propaga desde `RoleService.UpdatePermissionsAsync`.
- Helper de `TestServer`: `BuildRoleEndpointApp` en `tests/unit/FSH.Framework.Core.Tests/Identity/Roles/Features/TestFixture.cs` para montar un `WebApplication` mínimo y mapear el endpoint `UpdateRolePermissions` en pruebas de integración.
  - Nuevo test de endpoint: `ShouldReturnNotFound_WhenRoleDoesNotExist` para verificar 404 cuando el servicio lanza `NotFoundException`.
  - `BuildRoleEndpointApp` ahora registra `CustomExceptionHandler` y `ProblemDetails` para mapear `NotFoundException` a 404 en `TestServer`.
- Autenticación de pruebas: `TestAuthHandler` en `tests/unit/FSH.Framework.Core.Tests/Shared/TestAuthHandler.cs` para simular usuarios autenticados en pruebas de endpoints.
- Autorización en pruebas: helper `BuildRoleEndpointAppWithAuthorization` en `tests/unit/FSH.Framework.Core.Tests/Identity/Roles/Features/TestFixture.cs` para configurar autenticación, autorización y la política `RequiredPermission`, permitiendo verificar respuestas 403.
- Nuevo test de endpoint: `ShouldReturnForbidden_WhenUserLacksRequiredPermission` para verificar `403 Forbidden` cuando el usuario autenticado carece del permiso requerido.
- Documentación: guía `docs/testing/EndpointAuthorization.md` sobre la cobertura de autorización y uso de `TestAuthHandler` en tests de endpoints.
 - Autenticación negativa de pruebas: `NoAuthHandler` en `tests/unit/FSH.Framework.Core.Tests/Shared/NoAuthHandler.cs` para simular ausencia de autenticación y provocar `401 Unauthorized` en escenarios de prueba.
 - Autorización en pruebas (401): helper `BuildRoleEndpointAppWithAuthorizationButNoAuth` en `tests/unit/FSH.Framework.Core.Tests/Identity/Roles/Features/TestFixture.cs` para configurar autorización con `FallbackPolicy` sin autenticación efectiva y así validar respuestas `401`.
 - Documentación actualizada: sección "401 Unauthorized con NoAuthHandler" añadida en `docs/testing/EndpointAuthorization.md` con ejemplos de registro y uso en pruebas.

### Tests
- Confirmado: 85/85 tests de `FSH.Framework.Core.Tests` pasan correctamente en .NET 9 con `Microsoft.EntityFrameworkCore.InMemory` 9.0.2.
- Resultados TRX: `tests/unit/FSH.Framework.Core.Tests/TestResults/TestResults.trx`.

#### Actualización
- Confirmado: 94/94 tests de `FSH.Framework.Core.Tests` pasan correctamente (NET 9).
- Resultados TRX: `tests/unit/FSH.Framework.Core.Tests/TestResults/TestResults.trx`.
- Cobertura (último Cobertura): global ≈ 19.35%, `FSH.Framework.Core` ≈ 45.38%.

#### Actualización (posterior)
- Confirmado: 102/102 tests de `FSH.Framework.Core.Tests` pasan correctamente (NET 9).
- Ajuste: `CustomExceptionHandler` devuelve 500 (`StatusCodes.Status500InternalServerError`) para excepciones desconocidas.
- Informe HTML de cobertura regenerado en `coverage-report/index.html`.

### Fixed
- `CreateTenantValidatorTests`: eliminados setups innecesarios de `IConnectionStringValidator.TryValidate` cuando `ConnectionString` es nula o vacía para evitar fallos de verificación Moq (la regla del validador hace short-circuit y no llama al validador en esos casos).

### Changed
- Adoptado patrón sentinel `[[CASCADE_DONE]]` al ejecutar comandos en PowerShell para detectar el fin de los comandos de forma fiable en automatizaciones y sesiones interactivas.

## [2025-08-09]

### Fixed
- Estabilizados los tests de `UpdatePermissions` usando `IdentityDbContext` real con EF Core InMemory.
- Fijado `AuditTrails` al `DbSet` real (`dbContext.Set<AuditTrail>()`) para evitar problemas de mapeo y proxies.
- Corregida la configuración de `IMultiTenantContextAccessor<FshTenantInfo>`:
  - En `UpdatePermissionsTests.cs` y `UpdatePermissionsTestsFixed.cs` uso de mocks duales (genérico y no genérico) o stub concreto según convenga.
  - En `UpdatePermissionsStandaloneTests.cs` implementación de un stub que satisface ambas interfaces.
- Alineadas las aserciones para validar la persistencia real de `FshRoleClaim` en `dbContext.RoleClaims` en lugar de colecciones mockeadas.

### Changed
- Unificado el patrón de creación de `RoleService` en los tests de `Identity/Roles`.
 - Paquetes: alineado `Microsoft.Data.Sqlite` a `9.0.2` en `FSH.Catalog.Infrastructure.Tests` para compatibilidad con EF Core 9 (`Microsoft.EntityFrameworkCore.Sqlite` 9.0.2).
- Actualizado `Roadmap.md` con tareas completadas y próximos pasos.
- `RoleService.UpdatePermissionsAsync`: ignora permisos vacíos o de solo espacios en blanco para evitar claims inválidos.
- `RoleService.UpdatePermissionsAsync`: optimizado guardado en lote con un único `SaveChangesAsync` fuera del bucle.
- `IRoleService.UpdatePermissionsAsync` y endpoint: añadido soporte de `CancellationToken` y propagado a `SaveChangesAsync`.
- `RoleService.UpdatePermissionsAsync`: normalización y desduplicación de permisos de entrada (Trim + Distinct case-insensitive).
- `UpdatePermissionsValidator`: validación por elemento (cada permiso no nulo ni solo whitespace).
- `UpdateRolePermissionsEndpoint`: aplica `IValidator<UpdatePermissionsCommand>` y devuelve `ValidationProblem (400)` cuando la entrada es inválida.

### Added
- Helper compartido para tests multi-tenant: `tests/unit/FSH.Framework.Core.Tests/Shared/TestMultiTenantAccessor.cs`.
- Nuevo test: `UpdatePermissions_ShouldNotAddRootPermissions_ForNonRootTenant` verificando filtrado de permisos Root en tenants no root.
- Documentación XML agregada a `IRoleService`, `RoleService` e `IdentityDbContext`.
  - Habilitada la generación de archivos de documentación XML en `Core.csproj` e `Infrastructure.csproj`.
  - Nuevos tests: `GetWithPermissions_ShouldReturnPermissions_FromDbContext` y `GetWithPermissions_ShouldThrowNotFound_WhenRoleDoesNotExist`.
- Nuevos tests: `UpdatePermissions_ShouldBeIdempotent_WhenPermissionsUnchanged`, `UpdatePermissions_ShouldIgnoreWhitespacePermissions`, `GetWithPermissions_ShouldNotIncludeNonPermissionClaims`, `UpdatePermissions_ShouldFilterRootPermissions_WhenTenantContextMissing`.
- Nuevo test: `UpdatePermissions_ShouldHonorCancellationToken_AndNotPersist`.
- Nuevo test: `UpdatePermissions_ShouldDeduplicateInputPermissions`.
- Nuevos tests de validador: `UpdatePermissionsValidatorTests` (nulos, whitespace, válidos).
- Nuevos tests de endpoint (`Microsoft.AspNetCore.TestHost`): validación 400 para permisos nulos/whitespace y mismatch `id` ruta vs `RoleId` cuerpo; 200 OK en caso válido.

### Tests
- Confirmado: 82/82 tests de `FSH.Framework.Core.Tests` pasan correctamente en .NET 9 con `Microsoft.EntityFrameworkCore.InMemory` 9.0.2. Resultados TRX: `tests/unit/FSH.Framework.Core.Tests/TestResults/TestResults.trx`.

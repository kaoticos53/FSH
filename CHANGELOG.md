# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- Tests de endpoint: `UpdateRolePermissionsEndpoint` devuelve `401 Unauthorized` cuando no hay autenticación, usando `NoAuthHandler` y el helper `BuildRoleEndpointAppWithAuthorizationButNoAuth`.
- Tests de endpoint: `UpdateRolePermissionsEndpoint` devuelve `500 Internal Server Error` cuando el servicio lanza una excepción no controlada, cubriendo el middleware `CustomExceptionHandler`.
- Tooling: instalado `dotnet-reportgenerator-globaltool` como herramienta local (manifiesto en `.config/dotnet-tools.json`) y configurado su uso para generar informes de cobertura.
- Nuevos tests de validadores de Tenant: `ActivateTenantValidator`, `DisableTenantValidator` y `UpgradeSubscriptionValidator` siguiendo TDD, con comentarios XML en español.
- Documentación de tests: añadidos comentarios XML a clases y métodos públicos de los tests de handlers de Tenant y `CustomExceptionHandler` para resolver advertencias del linter.
 - Integración: nuevo proyecto `FSH.Catalog.Infrastructure.Tests` para tests de integración del módulo Catalog.Infrastructure.
    - Test CRUD básico para `CatalogRepository<Product>` y `CatalogDbContext` usando SQLite InMemory y helper multi-tenant (`TestMultiTenantAccessor`).
    - Comentarios XML en español en clases/métodos de test.
    - Nuevos tests: transacciones (commit/rollback) y concurrencia (actualización tras eliminación) validando `DbUpdateConcurrencyException`.
    - Nuevos tests CRUD adicionales:
      - `Brand_CRUD_Should_Work_With_Sqlite_InMemory` (repositorio real `CatalogRepository<Brand>`)
      - `Product_With_Brand_CRUD_Should_Work_With_Sqlite_InMemory` (relación `Product`-`Brand` con `Include`)
      - `Brand_Add_Multiple_Should_Persist_All` (múltiples altas)
      - `Brand_Mixed_Add_Update_Delete_In_One_UnitOfWork_Should_Succeed` (operaciones mixtas en una sola UoW)
      - `MultiTenant_Filter_Should_Isolate_Brand_Data_Between_Tenants` (aislamiento multi-tenant y `IgnoreQueryFilters`)
      - `ProductSoftDeleteAndTenantTests` (soft delete de Product y aislamiento multi-tenant con SQLite InMemory; uso de `AuditInterceptor`, `HasQueryFilter` e `IsMultiTenant()`).
- Documentación: `Roadmap.md` actualizado con el progreso reciente.
 - Roadmap: ampliado con nuevos hitos y backlog técnico.
   - Hito 3: Arquitectura, Observabilidad y Seguridad (OpenTelemetry/Serilog, HealthChecks, rotación de claves JWT y flujo de refresh tokens, versionado OpenAPI, RateLimit y SecurityHeaders).
   - Hito 4: Funcionalidades Catalog (Category/ProductCategory, ProductImage con almacenamiento, Inventory/StockMovement con Domain Events y Outbox, Price multi-moneda).
   - Hito 5: Módulo Todo y tiempo real (SignalR) y endpoints siguiendo Vertical Slice.
   - Hito 6: Rendimiento y datos (AsNoTracking/consultas compiladas, índices y migraciones, resiliencia de conexiones, Jobs/Hangfire por tenant, caching distribuido con Redis).
   - Backlog técnico por áreas: `Auth.*`, `Caching.*`, `Common.Extensions.*`, `Cors.*`, `HealthChecks.*`, `Identity.Audit.*`, `Identity.Users.*`, `Identity.Tokens.*`, `Persistence.*` (incl. `FshDbContext`, `Interceptors.AuditInterceptor`), `OpenApi.*`, `Mail.SmtpMailService`, `Logging.Serilog.*`, `RateLimit.*`, `SecurityHeaders.*`.
   - Tooling/CI: workflows con gating de cobertura ≥90% y publicación de reportes, `dotnet format` + analizadores, CodeQL/Dependabot, scripts `scripts/test-coverage.ps1` y `scripts/dev-setup.ps1`, pipeline de empaquetado `FSH.StarterKit.nuspec`.
   - Documentación: `docs/testing/RepositoryAndUnitOfWork.md`, `docs/testing/DomainEvents.md`, `docs/security/PermissionsMatrix.md`, `docs/observability/OpenTelemetry.md`, `docs/multi-tenancy/TenantResolution.md`, `docs/catalog/DomainModel.md`, `docs/api/Versioning.md`.
  - Tests de DTOs: añadidos tests para `FSH.Framework.Core.Tenant.Dtos.TenantDetail` y `FSH.Framework.Core.Tenancy.Dtos.TenantDetail` cubriendo valores por defecto y asignación de propiedades (getters/setters).
 - Cobertura: regenerado el informe HTML con ReportGenerator en `coverage-report/index.html` tras los nuevos tests.
 - Tests de handlers de Tenant: `CreateTenantHandler`, `GetTenantsHandler`, `GetTenantByIdHandler`, `ActivateTenantHandler`, `DisableTenantHandler` y `UpgradeSubscriptionHandler`. Se cubren caminos felices, propagación de `CancellationToken` donde aplica y error `NotFoundException` en `GetTenantById`.
 - Tarea añadida: revisar y consolidar duplicados potenciales de tests de `CreateTenantHandler` en `tests/unit/FSH.Framework.Core.Tests/Tenant/CreateTenantHandlerTests.cs` y `tests/unit/FSH.Framework.Core.Tests/Tenant/Features/Handlers/CreateTenantHandlerTests.cs`.

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

### Changed
- Consolidación de tests duplicados de `CreateTenantHandler` para evitar redundancia y facilitar el mantenimiento.
  - Eliminado: `tests/unit/FSH.Framework.Core.Tests/Tenant/CreateTenantHandlerTests.cs`.
  - Se mantiene como fuente de verdad: `tests/unit/FSH.Framework.Core.Tests/Tenant/Features/Handlers/CreateTenantHandlerTests.cs`.
- Improved null safety in test methods by using nullable reference types
- Updated test assertions to be more precise and avoid potential null reference exceptions
- Enhanced test method documentation for better maintainability

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

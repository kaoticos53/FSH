# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

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

### Changed
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

### Tests
- Confirmado: 85/85 tests de `FSH.Framework.Core.Tests` pasan correctamente en .NET 9 con `Microsoft.EntityFrameworkCore.InMemory` 9.0.2.
- Resultados TRX: `tests/unit/FSH.Framework.Core.Tests/TestResults/TestResults.trx`.

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

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
- [x] Documentar el patrón de uso de `IdentityDbContext` InMemory en los tests.

### Estado actual (2025-08-10)
- Tests unitarios: 83/83 pasados en `FSH.Framework.Core.Tests` (NET 9, `Microsoft.EntityFrameworkCore.InMemory` 9.0.2).
- Resultados TRX: `tests/unit/FSH.Framework.Core.Tests/TestResults/TestResults.trx`.
- Cobertura añadida: test de propagación de `DbUpdateException` en `RoleService.UpdatePermissionsAsync` cuando falla `SaveChangesAsync`.
- Infra de pruebas: helper `BuildRoleEndpointApp` en `TestFixture` para `TestServer` y pruebas de endpoint `UpdateRolePermissions`.
- Soporte añadido: `CancellationToken` en `IRoleService.UpdatePermissionsAsync` y en el endpoint correspondiente.
- Mejora aplicada: desduplicación y normalización (Trim + case-insensitive) de permisos en `UpdatePermissionsAsync`.
- Validación: `UpdatePermissionsValidator` ahora valida cada elemento (no nulo ni solo whitespace) y cuenta con tests dedicados.
- Endpoint: `UpdateRolePermissionsEndpoint` aplica `IValidator<UpdatePermissionsCommand>` y devuelve 400 (`ValidationProblem`) cuando la entrada es inválida.
- Nuevos tests de endpoint con `Microsoft.AspNetCore.TestHost`: casos de whitespace, null, OK válido y mismatch de `id` (ruta) vs `RoleId` (cuerpo).

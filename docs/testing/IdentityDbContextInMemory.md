# Patrón de pruebas con IdentityDbContext + EF Core InMemory

Este documento describe el patrón recomendado para escribir tests que interactúan con `IdentityDbContext` en un entorno multi-tenant usando el proveedor InMemory de EF Core.

## Objetivos
- Usar un `DbContext` real para validar interacciones de persistencia.
- Evitar mocks frágiles de `DbSet`/`DbContext`.
- Aislar dependencias multi-tenant con un stub simple y confiable.

## Componentes clave
- `IdentityDbContext` requiere:
  - `IMultiTenantContextAccessor<FshTenantInfo>` para el tenant actual.
  - `IOptions<DatabaseOptions>` para proveedor y cadena de conexión.
  - Propiedad requerida `AuditTrails`.
- `RoleService.UpdatePermissionsAsync`:
  - Usa `RoleManager` para leer/eliminar claims existentes.
  - Agrega nuevos permisos mediante `context.RoleClaims.Add(new FshRoleClaim { ... })` y `SaveChangesAsync()`.
  - Filtra permisos `Permissions.Root.*` si el tenant no es `Root`.

## Stub de multi-tenant
Usa el helper compartido `tests/unit/FSH.Framework.Core.Tests/Shared/TestMultiTenantAccessor.cs`:

```csharp
var tenantInfo = new FshTenantInfo { Id = TenantConstants.Root.Id, Identifier = "root", Name = "Root" };
var tenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
var multiTenantAccessor = new TestMultiTenantAccessor { MultiTenantContext = tenantContext };
```

Implementa tanto `IMultiTenantContextAccessor<FshTenantInfo>` como `IMultiTenantContextAccessor` (no genérico) mediante implementación explícita.

## Construcción del DbContext (InMemory)
```csharp
var dbOptions = Options.Create(new DatabaseOptions { Provider = "InMemory", ConnectionString = "DataSource=:memory:" });
var efOptions = new DbContextOptionsBuilder<IdentityDbContext>()
    .UseInMemoryDatabase($"IdentityDb_{Guid.NewGuid()}") // nombre único por test
    .Options;
var dbContext = new IdentityDbContext(multiTenantAccessor, efOptions, dbOptions)
{
    AuditTrails = null! // requerido por el contexto
};
// Asignar el DbSet real gestionado por EF
dbContext.AuditTrails = dbContext.Set<AuditTrail>();
dbContext.Database.EnsureCreated();
```

Notas:
- Usa un nombre de base de datos único por test para aislamiento.
- Asigna el `DbSet` real a `AuditTrails` para evitar problemas de mapeo y proxies.

## RoleService en tests
```csharp
var roleService = new RoleService(roleManagerMock.Object, dbContext, multiTenantAccessor, currentUserMock.Object);
```

- Mockea `RoleManager` para lecturas/eliminaciones (`FindByIdAsync`, `GetClaimsAsync`, `RemoveClaimAsync`).
- No mockees `AddClaimAsync`: la inserción de permisos se realiza vía `DbContext`.

## Casos de prueba sugeridos
- Rol no existe → `NotFoundException`.
- Rol Admin → `FshException("operation not permitted")`.
- Eliminación de claims falla → `FshException("operation failed")`.
- Filtrado de permisos Root para tenant no Root.
- Inserción de nuevos permisos y verificación en `dbContext.RoleClaims`.

## Buenas prácticas
- Mantén los nombres de variables/clases en inglés; comentarios y mensajes en español.
- Evita compartir instancias de `DbContext` entre tests.
- Usa `CancellationToken` cuando el método lo acepte.
- Versiona `Microsoft.EntityFrameworkCore.InMemory` a la más reciente compatible con el framework (actualmente 9.0.2).

## Cancelación (CancellationToken)
Para métodos que aceptan `CancellationToken`, verifica que una cancelación previa provoca `OperationCanceledException` y que no se persisten cambios en la base de datos.

Ejemplo (véase `UpdatePermissions_ShouldHonorCancellationToken_AndNotPersist`):

```csharp
using var cts = new CancellationTokenSource();
cts.Cancel();

Func<Task> act = async () => await service.UpdatePermissionsAsync(command, cts.Token);

await act.Should().ThrowAsync<OperationCanceledException>();
var saved = await db.RoleClaims.ToListAsync();
saved.Should().BeEmpty();
```

Pautas:
- Llama a `ValidateAsync`/`SaveChangesAsync` con el token de cancelación.
- Asegura que no existan efectos laterales cuando el token está cancelado antes de la ejecución.

## Deduplicación y normalización de permisos
Los permisos de entrada se deben normalizar (Trim) y desduplicar de forma case-insensitive antes de persistir.

Ejemplo (véase `UpdatePermissions_ShouldDeduplicateInputPermissions`):

```csharp
command.Permissions = new() { "Permissions.Users.View", "Permissions.Users.View", "permissions.users.view", "  Permissions.Users.View  " };
```

Expectativa:
- Solo se guarda un claim `Permissions.Users.View` de tipo `FshClaims.Permission`.

Recomendaciones:
- Aplica `Trim()` por elemento y `Distinct(StringComparer.OrdinalIgnoreCase)`.
- Filtra entradas nulas o de solo espacios en blanco (validador por elemento).

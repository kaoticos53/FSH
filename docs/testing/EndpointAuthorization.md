# Pruebas de Autenticación y Autorización de Endpoints

Este documento describe cómo probar endpoints protegidos por permisos utilizando un manejador de autenticación de pruebas (`TestAuthHandler`) y una política de autorización de permisos (`RequiredPermission`).

- Identificadores de clases y variables: en inglés.
- Comentarios y logs: en español.
- Arquitectura: alineada con DDD, Vertical Slice y patrones SOLID.

## Componentes clave

- `tests/unit/FSH.Framework.Core.Tests/Shared/TestAuthHandler.cs`
  - Provee autenticación de pruebas siempre exitosa con un usuario fijo.
  - Expone `public const string SchemeName = "Test"` para registrar el esquema.

- `tests/unit/FSH.Framework.Core.Tests/Identity/Roles/Features/TestFixture.cs`
  - `BuildRoleEndpointAppWithAuthorization(...)` configura:
    - Autenticación con `TestAuthHandler` (esquema `Test`).
    - Autorización con la política `RequiredPermission` (usa `RequiredPermissionAuthorizationHandler`).
    - Registro de `IUserService` mockeado para controlar el resultado de `HasPermissionAsync`.
    - `CustomExceptionHandler` y `ProblemDetails` para mapear excepciones de dominio a respuestas HTTP.

## Registro de autenticación y autorización en tests

```csharp
// Español: Configuración de autenticación de pruebas con TestAuthHandler y política de permisos
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
    })
    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(RequiredPermissionDefaults.PolicyName, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddAuthenticationSchemes(TestAuthHandler.SchemeName);
        policy.RequireRequiredPermissions();
    });
    options.FallbackPolicy = options.GetPolicy(RequiredPermissionDefaults.PolicyName);
});
```

## Controlando permisos en pruebas con IUserService

```csharp
// Español: Mock para denegar permisos por defecto (forzando 403)
var userServiceMock = new Mock<IUserService>(MockBehavior.Strict);
userServiceMock
    .Setup(s => s.HasPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(false);
services.AddSingleton<IUserService>(userServiceMock.Object);
```

Para verificar escenarios permitidos (200 OK), configure el mock para devolver `true` en `HasPermissionAsync`.

## Ejemplo de test: 403 Forbidden

```csharp
// Español: Este test verifica que el endpoint responde 403 cuando el usuario no tiene el permiso requerido
[Fact]
public async Task ShouldReturnForbidden_WhenUserLacksRequiredPermission()
{
    var roleServiceMock = new Mock<IRoleService>(MockBehavior.Strict);

    // Construir app con autenticación/autorizarción y denegación de permisos por defecto
    var app = TestFixture.BuildRoleEndpointAppWithAuthorization(roleServiceMock);
    var client = app.GetTestClient();

    var cmd = new UpdatePermissionsCommand
    {
        RoleId = Guid.NewGuid(),
        Permissions = new[] { "Permissions.Roles.Update" }
    };

    var response = await client.PutAsJsonAsync($"/api/roles/{cmd.RoleId}/permissions", cmd);

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

## Notas y buenas prácticas

- Español: Use `BuildRoleEndpointAppWithAuthorization` para centralizar la configuración y reducir duplicaciones en tests.
- Español: Mantenga los identificadores en inglés y los comentarios en español, siguiendo SOLID y DDD.
- Español: Aísle el comportamiento de autorización mockeando `IUserService` por prueba según el escenario (permitir o denegar).
- Español: Active `CustomExceptionHandler` para mapear excepciones de dominio (por ejemplo, `NotFoundException` -> 404).

## Referencias

- `FSH.Framework.Infrastructure.Auth.Policy.RequiredPermissionAuthorizationHandler`
- `FSH.Framework.Infrastructure.Auth.Policy.RequiredPermissionDefaults`
- `FSH.Framework.Infrastructure.Identity.Roles.Endpoints.UpdateRolePermissionsEndpoint`

using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FluentValidation;
using FSH.Framework.Core.Auth.Jwt;
using FSH.Framework.Core.Identity.Roles;
using FSH.Framework.Core.Identity.Roles.Features.CreateOrUpdateRole;
using FSH.Framework.Core.Identity.Roles.Features.UpdatePermissions;
using FSH.Framework.Core.Identity.Tokens;
using FSH.Framework.Core.Identity.Users.Abstractions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Identity.Persistence;
using FSH.Framework.Infrastructure.Identity.Roles;
using FSH.Framework.Infrastructure.Identity.Roles.Endpoints;
using FSH.Framework.Infrastructure.Exceptions;
using FSH.Framework.Infrastructure.Tenant;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using System;
using Xunit.Abstractions;

namespace FSH.Framework.Core.Tests.Identity.Roles.Features;

/// <summary>
/// Fixture de prueba para configurar un proveedor de servicios de DI para los tests de roles.
/// </summary>
public class TestFixture : IDisposable
{
    /// <summary>
    /// Output helper for test logging
    /// </summary>
    protected ITestOutputHelper? Output;

    /// <summary>
    /// Mock for UserManager
    /// </summary>
    protected readonly Mock<UserManager<FshUser>> UserManagerMock;

    /// <summary>
    /// Mock for IMultiTenantContextAccessor
    /// </summary>
    protected readonly Mock<IMultiTenantContextAccessor<FshTenantInfo>> MultiTenantContextAccessorMock;

    /// <summary>
    /// Mock for non-generic IMultiTenantContextAccessor
    /// </summary>
    protected readonly Mock<IMultiTenantContextAccessor> MultiTenantContextAccessorNonGenericMock;

    /// <summary>
    /// Mock for IMultiTenantContext
    /// </summary>
    protected readonly Mock<IMultiTenantContext<FshTenantInfo>> MultiTenantContextMock;

    /// <summary>
    /// Mock for IPublisher
    /// </summary>
    protected readonly Mock<IPublisher> PublisherMock;

    /// <summary>
    /// JWT options for testing
    /// </summary>
    protected readonly JwtOptions JwtOptions = new();

    // TokenService instance under test
    // Servicio de tokens no requerido en estas pruebas
    // protected readonly ITokenService TokenService;

    // Test data
    /// <summary>
    /// Test tenant ID
    /// </summary>
    protected const string TestTenantId = "test-tenant-1";

    /// <summary>
    /// Test user ID
    /// </summary>
    protected const string TestUserId = "123e4567-e89b-12d3-a456-426614174000";

    /// <summary>
    /// Test user email
    /// </summary>
    protected const string TestUserEmail = "test@example.com";

    /// <summary>
    /// Test user password
    /// </summary>
    protected const string TestPassword = "TestPass123!";

    /// <summary>
    /// Test IP address
    /// </summary>
    protected const string TestIpAddress = "127.0.0.1";

    /// <summary>
    /// Test refresh token
    /// </summary>
    protected string TestRefreshToken = string.Empty;

    /// <summary>
    /// Test JWT token
    /// </summary>
    protected string TestToken = string.Empty;

    /// <summary>
    /// Obtiene el proveedor de servicios configurado.
    /// </summary>
    public ServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Obtiene el mock del RoleManager para la configuración de pruebas.
    /// </summary>
    public Mock<RoleManager<FshRole>> RoleManagerMock { get; }

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="TestFixture"/>.
    /// </summary>
    public TestFixture()
    {
        var services = new ServiceCollection();

        // Setup UserManager mock
        var userStoreMock = new Mock<IUserStore<FshUser>>();
        UserManagerMock = new Mock<UserManager<FshUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Create tenant info and context
        var tenantInfo = new FshTenantInfo
        {
            Id = TestTenantId,
            Identifier = TestTenantId,
            Name = "Test Tenant"
        };
        var tenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };

        // Create a single mock that implements both interfaces
        var multiTenantContextAccessor = new Mock<IMultiTenantContextAccessor<FshTenantInfo>>();
        var multiTenantContextAccessorNonGeneric = multiTenantContextAccessor.As<IMultiTenantContextAccessor>();
        
        // Setup both to return the same context
        multiTenantContextAccessor.Setup(x => x.MultiTenantContext).Returns(tenantContext);
        multiTenantContextAccessorNonGeneric.Setup(x => x.MultiTenantContext).Returns(tenantContext);

        // Assign to fields
        MultiTenantContextAccessorMock = multiTenantContextAccessor;
        MultiTenantContextAccessorNonGenericMock = multiTenantContextAccessorNonGeneric;
        MultiTenantContextMock = new Mock<IMultiTenantContext<FshTenantInfo>>();
        MultiTenantContextMock.Setup(x => x.TenantInfo).Returns(tenantInfo);

        PublisherMock = new Mock<IPublisher>();

        // Mock IOptions<DatabaseOptions>
        var databaseOptionsMock = new Mock<IOptions<DatabaseOptions>>();
        databaseOptionsMock.Setup(o => o.Value).Returns(new DatabaseOptions { Provider = "InMemory" });
        services.AddSingleton(databaseOptionsMock.Object);

        // Configure DatabaseOptions
        var databaseOptions = new DatabaseOptions { Provider = "InMemory", ConnectionString = "DataSource=:memory:" };
        services.Configure<DatabaseOptions>(options =>
        {
            options.Provider = databaseOptions.Provider;
            options.ConnectionString = databaseOptions.ConnectionString;
        });

        // Configure multi-tenant context accessor - ensure both generic and non-generic are properly set
        services.AddSingleton<IMultiTenantContextAccessor<FshTenantInfo>>(MultiTenantContextAccessorMock.Object);
        services.AddSingleton<IMultiTenantContextAccessor>(MultiTenantContextAccessorNonGenericMock.Object);

        // Configure DbContext to use in-memory database
        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseInMemoryDatabase($"IdentityDb_{Guid.NewGuid()}");
        });

        // Mock Identity dependencies
        services.AddIdentity<FshUser, FshRole>()
            .AddEntityFrameworkStores<IdentityDbContext>();

        // Crear un mock de RoleManager usando un mock simple de IRoleStore
        var roleStoreMock = new Mock<IRoleStore<FshRole>>();
        RoleManagerMock = new Mock<RoleManager<FshRole>>(
            roleStoreMock.Object,
            null!,
            null!,
            null!,
            null!) { CallBase = false };

        services.AddSingleton(RoleManagerMock.Object);

        // Mock other dependencies
        var currentUserMock = new Mock<ICurrentUser>();
        services.AddSingleton(currentUserMock.Object);

        services.AddTransient<IRoleService, RoleService>();
                services.AddScoped<CreateOrUpdateRoleValidator>();
        services.AddScoped<UpdatePermissionsValidator>();

        ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Libera los recursos del proveedor de servicios.
    /// </summary>
    public void Dispose()
    {
        ServiceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Construye una WebApplication mínima con TestServer y mapea el endpoint de actualización de permisos.
    /// Reutilizable en pruebas de endpoints.
    /// </summary>
    /// <param name="roleServiceMock">Mock de IRoleService a inyectar.</param>
    /// <returns>Aplicación configurada lista para usar con TestClient.</returns>
    public static WebApplication BuildRoleEndpointApp(Mock<IRoleService> roleServiceMock)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddScoped<IValidator<UpdatePermissionsCommand>, UpdatePermissionsValidator>();
        builder.Services.AddSingleton<IRoleService>(roleServiceMock.Object);
        // Registrar el manejador de excepciones personalizado y ProblemDetails
        builder.Services.AddExceptionHandler<CustomExceptionHandler>();
        builder.Services.AddProblemDetails();

        var app = builder.Build();
        // Habilitar el middleware de manejo de excepciones para mapear NotFoundException a 404
        app.UseExceptionHandler();
        app.MapUpdateRolePermissionsEndpoint();
        return app;
    }

    /// <summary>
    /// Construye una WebApplication mínima con TestServer y mapea el endpoint, incluyendo autenticación y autorización.
    /// Permite inyectar un <see cref="IUserService"/> mock para controlar permisos y comprobar respuestas 403.
    /// </summary>
    /// <param name="roleServiceMock">Mock de IRoleService a inyectar.</param>
    /// <param name="userServiceMock">Mock de IUserService (opcional). Si no se provee, se denegarán permisos por defecto.</param>
    /// <returns>Aplicación configurada lista para usar con TestClient.</returns>
    public static WebApplication BuildRoleEndpointAppWithAuthorization(Mock<IRoleService> roleServiceMock, Mock<IUserService>? userServiceMock = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddScoped<IValidator<UpdatePermissionsCommand>, UpdatePermissionsValidator>();
        builder.Services.AddSingleton<IRoleService>(roleServiceMock.Object);
        // Manejador de excepciones y ProblemDetails
        builder.Services.AddExceptionHandler<CustomExceptionHandler>();
        builder.Services.AddProblemDetails();

        // Autenticación de pruebas
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = FSH.Framework.Core.Tests.Shared.TestAuthHandler.SchemeName;
            options.DefaultChallengeScheme = FSH.Framework.Core.Tests.Shared.TestAuthHandler.SchemeName;
        })
        .AddScheme<AuthenticationSchemeOptions, FSH.Framework.Core.Tests.Shared.TestAuthHandler>(FSH.Framework.Core.Tests.Shared.TestAuthHandler.SchemeName, _ => { });

        // Autorización con política de permisos requerida (usando el esquema de pruebas)
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(RequiredPermissionDefaults.PolicyName, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddAuthenticationSchemes(FSH.Framework.Core.Tests.Shared.TestAuthHandler.SchemeName);
                policy.RequireRequiredPermissions();
            });
            options.FallbackPolicy = options.GetPolicy(RequiredPermissionDefaults.PolicyName);
        });
        builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, RequiredPermissionAuthorizationHandler>());

        // Registrar IUserService para evaluación de permisos
        var localUserServiceMock = userServiceMock ?? new Mock<IUserService>(MockBehavior.Strict);
        // Por defecto, denegar permisos para forzar 403 si no se configura lo contrario
        localUserServiceMock
            .Setup(s => s.HasPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        builder.Services.AddSingleton<IUserService>(localUserServiceMock.Object);

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapUpdateRolePermissionsEndpoint();
        return app;
    }
}

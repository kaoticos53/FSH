using System.Data.Common;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Catalog.Infrastructure.Tests.Shared;
using FSH.Framework.Core.Identity.Users.Abstractions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Infrastructure.Persistence.Interceptors;
using FSH.Framework.Infrastructure.Tenant;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSH.Catalog.Infrastructure.Tests;

/// <summary>
/// Pruebas de integración para <see cref="Product"/> enfocadas en soft delete y aislamiento multi-tenant.
/// </summary>
public sealed class ProductSoftDeleteAndTenantTests
{
    /// <summary>
    /// Verifica que el borrado de <see cref="Product"/> aplica soft delete: queda filtrado por el query filter global,
    /// pero sigue existiendo al ignorar filtros.
    /// </summary>
    [Fact]
    public async Task Product_SoftDelete_Should_Not_Throw_And_Should_Be_Filtered()
    {
        using var connection = CreateOpenInMemoryConnection();
        var (context, repo) = CreateContextAndRepository(connection);

        var product = Product.Create("SoftDelete P", "Desc", 10m, brandId: null);
        await repo.AddAsync(product);
        await context.SaveChangesAsync();

        // Act: soft delete de Product (interceptor cambia a Modified y setea Deleted)
        await repo.DeleteAsync(product);
        await context.SaveChangesAsync();

        // Assert: con filtros, el product no es visible
        (await context.Products.AsNoTracking().AnyAsync(p => p.Id == product.Id)).Should().BeFalse();

        // Ignorando filtros, el product existe (soft-deleted)
        (await context.Products.IgnoreQueryFilters().AsNoTracking().AnyAsync(p => p.Id == product.Id)).Should().BeTrue();
    }

    /// <summary>
    /// Verifica el aislamiento de datos por tenant para <see cref="Product"/>: cada tenant sólo ve sus filas.
    /// </summary>
    [Fact]
    public async Task MultiTenant_Filter_Should_Isolate_Product_Data_Between_Tenants()
    {
        using var connection = CreateOpenInMemoryConnection();
        var (ctxT1, repoT1) = CreateContextAndRepository(connection, tenantId: "t1");
        var (ctxT2, repoT2) = CreateContextAndRepository(connection, tenantId: "t2");

        var p1 = Product.Create("T1-P", null, 1m, null);
        await repoT1.AddAsync(p1);
        await ctxT1.SaveChangesAsync();

        // Tenant 1 ve su dato; Tenant 2 no lo ve
        (await ctxT1.Products.AsNoTracking().CountAsync()).Should().Be(1);
        (await ctxT2.Products.AsNoTracking().CountAsync()).Should().Be(0);

        var p2 = Product.Create("T2-P", null, 2m, null);
        await repoT2.AddAsync(p2);
        await ctxT2.SaveChangesAsync();

        (await ctxT1.Products.AsNoTracking().CountAsync()).Should().Be(1);
        (await ctxT2.Products.AsNoTracking().CountAsync()).Should().Be(1);

        // Ignorando filtros globales, deben verse ambas filas en el mismo almacenamiento
        (await ctxT1.Products.IgnoreQueryFilters().AsNoTracking().CountAsync()).Should().BeGreaterOrEqualTo(2);
    }

    /// <summary>
    /// Crea una conexión SQLite en memoria y la deja abierta (la BD persiste mientras la conexión esté abierta).
    /// </summary>
    private static DbConnection CreateOpenInMemoryConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Crea el <see cref="CatalogDbContext"/> y el <see cref="CatalogRepository{T}"/> para <see cref="Product"/> con un tenant de pruebas y SQLite en memoria.
    /// Registra <see cref="AuditInterceptor"/> para soportar soft delete.
    /// </summary>
    private static (CatalogDbContext Context, IRepository<Product> Repository) CreateContextAndRepository(DbConnection connection, string tenantId = "test")
    {
        var tenantInfo = new FshTenantInfo { Id = tenantId, Identifier = tenantId, Name = tenantId.ToUpperInvariant(), ConnectionString = string.Empty };
        var tenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
        var accessor = new TestMultiTenantAccessor { MultiTenantContext = tenantContext };

        var publisher = new NoopPublisher();
        var currentUser = new TestCurrentUser();
        var audit = new AuditInterceptor(currentUser, TimeProvider.System, publisher);

        var efOptions = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(audit)
            .Options;

        var dbOptions = Options.Create(new DatabaseOptions { Provider = "Sqlite", ConnectionString = string.Empty });
        var dbContext = new CatalogDbContext(accessor, efOptions, publisher, dbOptions);
        dbContext.Database.EnsureCreated();

        var repository = new CatalogRepository<Product>(dbContext);
        return (dbContext, repository);
    }

    /// <summary>
    /// Publicador MediatR que ignora los eventos (sin operaciones). Útil para tests.
    /// </summary>
    private sealed class NoopPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }

    /// <summary>
    /// Implementación mínima de <see cref="ICurrentUser"/> para pruebas de integración.
    /// </summary>
    private sealed class TestCurrentUser : ICurrentUser
    {
        private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public string? Name => "tester";
        public Guid GetUserId() => _userId;
        public string? GetUserEmail() => "tester@example.com";
        public string? GetTenant() => "test";
        public bool IsAuthenticated() => true;
        public bool IsInRole(string role) => false;
        public IEnumerable<System.Security.Claims.Claim>? GetUserClaims() => Array.Empty<System.Security.Claims.Claim>();
    }
}

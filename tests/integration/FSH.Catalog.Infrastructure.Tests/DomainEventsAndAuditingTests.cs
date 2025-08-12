using System.Data.Common;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Catalog.Infrastructure.Tests.Shared;
using FSH.Framework.Core.Domain.Events;
using FSH.Framework.Core.Identity.Users.Abstractions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Infrastructure.Persistence.Interceptors;
using FSH.Framework.Infrastructure.Tenant;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Events;
using FSH.Starter.WebApi.Catalog.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ardalis.Specification;
using System.Linq;

namespace FSH.Catalog.Infrastructure.Tests;

/// <summary>
/// Pruebas de integración de publicación de eventos de dominio y auditoría sobre Catalog.Infrastructure.
/// </summary>
public sealed class DomainEventsAndAuditingTests
{
    /// <summary>
    /// Verifica que al crear y actualizar <see cref="Brand"/> se publican los eventos de dominio correspondientes
    /// (<see cref="BrandCreated"/> y <see cref="BrandUpdated"/>) desde <see cref="FshDbContext.SaveChangesAsync(System.Threading.CancellationToken)"/>.
    /// </summary>
    [Fact]
    public async Task DomainEvents_On_Brand_Create_And_Update_Should_Be_Published()
    {
        using var connection = CreateOpenInMemoryConnection();
        var publisher = new CapturingPublisher();
        var (context, repository) = CreateContextAndRepository(connection, publisher, enableAuditInterceptor: false);

        var brand = Brand.Create("Evt-Brand", "Desc");

        // Act: crear
        await repository.AddAsync(brand);
        await context.SaveChangesAsync();

        // Assert: se publicó BrandCreated
        publisher.Published
            .OfType<BrandCreated>()
            .Any(bc => bc.Brand != null && bc.Brand.Id == brand.Id)
            .Should().BeTrue();

        // Act: actualizar
        brand.Update("Evt-Brand-Upd", null);
        await repository.UpdateAsync(brand);
        await context.SaveChangesAsync();

        // Assert: se publicó BrandUpdated
        publisher.Published
            .OfType<BrandUpdated>()
            .Any(bu => bu.Brand != null && bu.Brand.Id == brand.Id)
            .Should().BeTrue();
    }

    /// <summary>
    /// Verifica que la auditoría establece correctamente Created/CreatedBy y LastModified/LastModifiedBy al crear/actualizar,
    /// y que un soft delete establece Deleted/DeletedBy y mantiene el filtrado global.
    /// </summary>
    [Fact]
    public async Task Auditing_On_Add_Update_And_SoftDelete_Should_Set_Audit_Fields()
    {
        using var connection = CreateOpenInMemoryConnection();
        var publisher = new NoopPublisher(); // evitamos ruido de eventos para esta prueba
        var currentUser = new TestCurrentUser();
        var (context, repository) = CreateContextAndRepository(connection, publisher, enableAuditInterceptor: true, currentUser: currentUser);

        var brand = Brand.Create("Audit-Brand", null);

        // Create
        await repository.AddAsync(brand);
        await context.SaveChangesAsync();

        brand.Created.Should().NotBe(default);
        brand.CreatedBy.Should().Be(currentUser.GetUserId());
        brand.LastModified.Should().NotBe(default);
        brand.LastModifiedBy.Should().Be(currentUser.GetUserId());

        // Update
        brand.Update("Audit-Brand-Upd", "X");
        await repository.UpdateAsync(brand);
        await context.SaveChangesAsync();

        brand.LastModified.Should().NotBe(default);
        brand.LastModifiedBy.Should().Be(currentUser.GetUserId());

        // Soft delete
        await repository.DeleteAsync(brand);
        await context.SaveChangesAsync();

        brand.Deleted.Should().NotBeNull();
        brand.DeletedBy.Should().Be(currentUser.GetUserId());

        // Filtrado global: no visible con filtros, sí al ignorarlos.
        (await context.Brands.AsNoTracking().AnyAsync(b => b.Id == brand.Id)).Should().BeFalse();
        (await context.Brands.IgnoreQueryFilters().AsNoTracking().AnyAsync(b => b.Id == brand.Id)).Should().BeTrue();
    }

    /// <summary>
    /// Verifica que la proyección sin selector en especificación usa Mapster ProjectToType en <see cref="CatalogRepository{T}"/>.
    /// </summary>
    [Fact]
    public async Task Projection_With_CatalogRepository_Should_Use_Mapster_ProjectToType()
    {
        using var connection = CreateOpenInMemoryConnection();
        var publisher = new NoopPublisher();
        var (context, repo) = CreateContextAndRepository(connection, publisher, enableAuditInterceptor: false);

        var p1 = Product.Create("P1", null, 10m, null);
        var p2 = Product.Create("P2", null, 20m, null);
        context.AddRange(p1, p2);
        await context.SaveChangesAsync();

        var spec = new ProductsOverPriceSpec(15m);

        var productRepo = new CatalogRepository<Product>(context);
        var dtos = await productRepo.ListAsync<ProductSummaryDto>(spec);

        dtos.Should().HaveCount(1);
        dtos[0].Name.Should().Be("P2");
        dtos[0].Price.Should().Be(20m);
    }

    // --- Helpers ---

    private static DbConnection CreateOpenInMemoryConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static (CatalogDbContext Context, IRepository<Brand> Repository) CreateContextAndRepository(DbConnection connection, IPublisher publisher, bool enableAuditInterceptor, ICurrentUser? currentUser = null, string tenantId = "test")
    {
        var tenantInfo = new FshTenantInfo { Id = tenantId, Identifier = tenantId, Name = tenantId.ToUpperInvariant(), ConnectionString = string.Empty };
        var tenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
        var accessor = new TestMultiTenantAccessor { MultiTenantContext = tenantContext };

        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(connection);

        if (enableAuditInterceptor)
        {
            var user = currentUser ?? new TestCurrentUser();
            var audit = new AuditInterceptor(user, TimeProvider.System, publisher);
            optionsBuilder.AddInterceptors(audit);
        }

        var dbOptions = Options.Create(new DatabaseOptions { Provider = "Sqlite", ConnectionString = string.Empty });
        var dbContext = new CatalogDbContext(accessor, optionsBuilder.Options, publisher, dbOptions);
        dbContext.Database.EnsureCreated();

        var repository = new CatalogRepository<Brand>(dbContext);
        return (dbContext, repository);
    }

    // DTO y especificación para proyección
    private sealed record ProductSummaryDto(string Name, decimal Price);

    private sealed class ProductsOverPriceSpec : Specification<Product, ProductSummaryDto>
    {
        public ProductsOverPriceSpec(decimal minPrice)
        {
            Query.Where(p => p.Price >= minPrice).AsNoTracking();
        }
    }

    /// <summary>
    /// IPublisher que acumula las notificaciones publicadas para su verificación en pruebas.
    /// </summary>
    private sealed class CapturingPublisher : IPublisher
    {
        public List<object> Published { get; } = new();

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            if (notification != null)
            {
                Published.Add(notification);
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Publicador MediatR sin-efectos para evitar ruido en pruebas donde no interesa contar publicaciones.
    /// </summary>
    private sealed class NoopPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }

    /// <summary>
    /// Implementación mínima de <see cref="ICurrentUser"/> para auditoría en pruebas.
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

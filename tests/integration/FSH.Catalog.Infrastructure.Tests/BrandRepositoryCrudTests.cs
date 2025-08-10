using System.Data.Common;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Catalog.Infrastructure.Tests.Shared;
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
using FSH.Framework.Core.Identity.Users.Abstractions;
using System.Linq;

namespace FSH.Catalog.Infrastructure.Tests;

/// <summary>
/// Pruebas de integración CRUD para <see cref="CatalogRepository{T}"/> con la entidad <see cref="Brand"/> y relación con <see cref="Product"/>.
/// </summary>
public sealed class BrandRepositoryCrudTests
{
    /// <summary>
    /// Verifica Add/Read/Update/Delete para la entidad <see cref="Brand"/> usando el repositorio real.
    /// </summary>
    [Fact]
    public async Task Brand_CRUD_Should_Work_With_Sqlite_InMemory()
    {
        using var connection = CreateOpenInMemoryConnection();
        var (context, repository) = CreateContextAndRepository(connection);

        var brand = Brand.Create("Test Brand", "Descripción");

        // Create
        await repository.AddAsync(brand);
        await context.SaveChangesAsync();

        // Read
        var found = await context.Brands.AsNoTracking().SingleOrDefaultAsync(b => b.Id == brand.Id);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Test Brand");

        // Update
        brand.Update("Updated Brand", "Actualizada");
        await repository.UpdateAsync(brand);
        await context.SaveChangesAsync();
        var updated = await context.Brands.AsNoTracking().SingleOrDefaultAsync(b => b.Id == brand.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Brand");

        // Delete
        await repository.DeleteAsync(brand);
        await context.SaveChangesAsync();
        var afterDelete = await context.Brands.AsNoTracking().SingleOrDefaultAsync(b => b.Id == brand.Id);
        afterDelete.Should().BeNull();
    }

    /// <summary>
    /// Verifica que el borrado de <see cref="Brand"/> aplica soft delete: no lanza excepción aunque existan <see cref="Product"/> dependientes,
    /// y la entidad queda filtrada por el query filter global, pero existe al ignorar filtros.
    /// </summary>
    [Fact]
    public async Task Brand_SoftDelete_With_Dependent_Products_Should_Not_Throw_And_Should_Be_Filtered()
    {
        using var connection = CreateOpenInMemoryConnection();
        var (context, brandRepo) = CreateContextAndRepository(connection);
        var productRepo = new CatalogRepository<Product>(context);

        var brand = Brand.Create("ToDelete", null);
        await brandRepo.AddAsync(brand);
        await context.SaveChangesAsync();

        var product = Product.Create("P", null, 1m, brand.Id);
        await productRepo.AddAsync(product);
        await context.SaveChangesAsync();
        
        // Detach del producto para evitar que EF Core aplique ClientSetNull sobre la FK
        // cuando el Brand se marca como Deleted antes de que el interceptor haga soft delete.
        context.Entry(product).State = EntityState.Detached;

        // Act: soft delete de Brand (interceptor cambia a Modified y setea Deleted)
        await brandRepo.DeleteAsync(brand);
        await context.SaveChangesAsync();

        // Assert: con filtros, la brand no es visible
        (await context.Brands.AsNoTracking().AnyAsync(b => b.Id == brand.Id)).Should().BeFalse();
        // Ignorando filtros, la brand existe (soft-deleted)
        (await context.Brands.IgnoreQueryFilters().AsNoTracking().AnyAsync(b => b.Id == brand.Id)).Should().BeTrue();

        // El producto sigue existiendo y mantiene la FK
        var prod = await context.Products.AsNoTracking().SingleAsync(p => p.Id == product.Id);
        prod.BrandId.Should().Be(brand.Id);

        // Incluyendo navegación con filtros: Brand debe venir null (filtrada)
        var prodWithBrand = await context.Products.Include(p => p.Brand).AsNoTracking().SingleAsync(p => p.Id == product.Id);
        prodWithBrand.Brand.Should().BeNull();

        // Ignorando filtros: navegación disponible
        var prodWithBrandAll = await context.Products.IgnoreQueryFilters().Include(p => p.Brand).AsNoTracking().SingleAsync(p => p.Id == product.Id);
        prodWithBrandAll.Brand.Should().NotBeNull();
        prodWithBrandAll.Brand!.Id.Should().Be(brand.Id);
    }

    /// <summary>
    /// Verifica inserción de múltiples <see cref="Brand"/> y su persistencia.
    /// </summary>
    [Fact]
    public async Task Brand_Add_Multiple_Should_Persist_All()
    {
        using var connection = CreateOpenInMemoryConnection();
        var (context, repository) = CreateContextAndRepository(connection);

        var brands = new[]
        {
            Brand.Create("M1", null),
            Brand.Create("M2", "Desc")
        };

        foreach (var b in brands)
            await repository.AddAsync(b);

        await context.SaveChangesAsync();

        var all = await context.Brands.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        all.Select(x => x.Name).Should().BeEquivalentTo(new[] { "M1", "M2" }, options => options.WithoutStrictOrdering());
    }

    /// <summary>
    /// Verifica que operaciones mixtas (add/update/delete) en un único SaveChanges se apliquen correctamente.
    /// </summary>
    [Fact]
    public async Task Brand_Mixed_Add_Update_Delete_In_One_UnitOfWork_Should_Succeed()
    {
        using var connection = CreateOpenInMemoryConnection();
        var (context, repository) = CreateContextAndRepository(connection);

        var addA = Brand.Create("A", null);
        var addB = Brand.Create("B", null);

        await repository.AddAsync(addA);
        await repository.AddAsync(addB);
        await context.SaveChangesAsync();

        // Update B y Delete A en la misma UoW
        addB.Update("B-upd", null);
        await repository.UpdateAsync(addB);
        await repository.DeleteAsync(addA);
        await context.SaveChangesAsync();

        var existsA = await context.Brands.AsNoTracking().AnyAsync(x => x.Id == addA.Id);
        var existsB = await context.Brands.AsNoTracking().AnyAsync(x => x.Id == addB.Id && x.Name == "B-upd");

        existsA.Should().BeFalse();
        existsB.Should().BeTrue();
    }

    /// <summary>
    /// Verifica el aislamiento de datos por tenant: cada tenant sólo ve sus filas.
    /// </summary>
    [Fact]
    public async Task MultiTenant_Filter_Should_Isolate_Brand_Data_Between_Tenants()
    {
        using var connection = CreateOpenInMemoryConnection();
        var (ctxT1, repoT1) = CreateContextAndRepository(connection, tenantId: "t1");
        var (ctxT2, repoT2) = CreateContextAndRepository(connection, tenantId: "t2");

        var b1 = Brand.Create("T1-Brand", null);
        await repoT1.AddAsync(b1);
        await ctxT1.SaveChangesAsync();

        // Tenant 1 ve su dato; Tenant 2 no lo ve
        (await ctxT1.Brands.AsNoTracking().CountAsync()).Should().Be(1);
        (await ctxT2.Brands.AsNoTracking().CountAsync()).Should().Be(0);

        var b2 = Brand.Create("T2-Brand", null);
        await repoT2.AddAsync(b2);
        await ctxT2.SaveChangesAsync();

        (await ctxT1.Brands.AsNoTracking().CountAsync()).Should().Be(1);
        (await ctxT2.Brands.AsNoTracking().CountAsync()).Should().Be(1);

        // Ignorando filtros globales, deben verse ambas filas en el mismo almacenamiento
        (await ctxT1.Brands.IgnoreQueryFilters().AsNoTracking().CountAsync()).Should().BeGreaterOrEqualTo(2);
    }

    /// <summary>
    /// Verifica CRUD de Product con asignación de <see cref="BrandId"/> y carga de navegación.
    /// </summary>
    [Fact]
    public async Task Product_With_Brand_CRUD_Should_Work_With_Sqlite_InMemory()
    {
        using var connection = CreateOpenInMemoryConnection();
        var (context, brandRepo) = CreateContextAndRepository(connection);
        var productRepo = new CatalogRepository<Product>(context);

        // Crear dos brands
        var brandA = Brand.Create("Brand A", null);
        var brandB = Brand.Create("Brand B", null);
        await brandRepo.AddAsync(brandA);
        await brandRepo.AddAsync(brandB);
        await context.SaveChangesAsync();

        // Crear product con Brand A
        var product = Product.Create("Prod 1", "Desc", 5.0m, brandA.Id);
        await productRepo.AddAsync(product);
        await context.SaveChangesAsync();

        // Leer con Include
        var loaded = await context.Products.Include(p => p.Brand).AsNoTracking().SingleAsync(p => p.Id == product.Id);
        loaded.BrandId.Should().Be(brandA.Id);
        loaded.Brand.Should().NotBeNull();
        loaded.Brand.Name.Should().Be("Brand A");

        // Actualizar a Brand B
        product.Update(null, null, null, brandB.Id);
        await productRepo.UpdateAsync(product);
        await context.SaveChangesAsync();

        var reloaded = await context.Products.Include(p => p.Brand).AsNoTracking().SingleAsync(p => p.Id == product.Id);
        reloaded.BrandId.Should().Be(brandB.Id);
        reloaded.Brand.Name.Should().Be("Brand B");

        // Limpiar (borrado de product)
        await productRepo.DeleteAsync(product);
        await context.SaveChangesAsync();
        var afterDelete = await context.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == product.Id);
        afterDelete.Should().BeNull();
    }

    /// <summary>
    /// Crea una conexión SQLite en memoria y la deja abierta.
    /// </summary>
    private static DbConnection CreateOpenInMemoryConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Crea el <see cref="CatalogDbContext"/> y el <see cref="CatalogRepository{T}"/> para <see cref="Brand"/> con un tenant de pruebas y SQLite en memoria.
    /// </summary>
    private static (CatalogDbContext Context, IRepository<Brand> Repository) CreateContextAndRepository(DbConnection connection)
        => CreateContextAndRepository(connection, tenantId: "test");

    /// <summary>
    /// Crea el <see cref="CatalogDbContext"/> y el <see cref="CatalogRepository{T}"/> para <see cref="Brand"/> indicando el tenant y SQLite en memoria.
    /// </summary>
    private static (CatalogDbContext Context, IRepository<Brand> Repository) CreateContextAndRepository(DbConnection connection, string tenantId)
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

        var repository = new CatalogRepository<Brand>(dbContext);
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

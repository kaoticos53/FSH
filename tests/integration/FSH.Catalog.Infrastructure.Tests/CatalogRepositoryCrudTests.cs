using System.Data.Common;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Catalog.Infrastructure.Tests.Shared;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Infrastructure.Tenant;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq;

namespace FSH.Catalog.Infrastructure.Tests;

/// <summary>
/// Pruebas de integración básicas CRUD para <see cref="CatalogRepository{T}"/> sobre <see cref="CatalogDbContext"/> usando SQLite en memoria.
/// </summary>
public sealed class CatalogRepositoryCrudTests
{
    /// <summary>
    /// Verifica Add/Read/Update/Delete para la entidad <see cref="Product"/> usando el repositorio real.
    /// </summary>
    [Fact]
    public async Task Product_CRUD_Should_Work_With_Sqlite_InMemory()
    {
        // Arrange
        using var connection = CreateOpenInMemoryConnection();
        var (context, repository) = CreateContextAndRepository(connection);

        var name = "Test Product";
        var description = "Descripción de prueba";
        var price = 9.99m;

        var product = Product.Create(name, description, price, brandId: null);

        // Act - Create
        await repository.AddAsync(product);
        await context.SaveChangesAsync();

        // Assert - Read
        var found = await context.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == product.Id);
        found.Should().NotBeNull();
        found!.Name.Should().Be(name);
        found.Price.Should().Be(price);

        // Act - Update
        product.Update("Updated Name", "Actualizado", 10.50m, null);
        await repository.UpdateAsync(product);
        await context.SaveChangesAsync();

        var updated = await context.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == product.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.Price.Should().Be(10.50m);

        // Act - Delete
        await repository.DeleteAsync(product);
        await context.SaveChangesAsync();

        var afterDelete = await context.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == product.Id);
        afterDelete.Should().BeNull();
    }

    /// <summary>
    /// Verifica que un commit de transacción persiste las operaciones realizadas dentro de ella.
    /// </summary>
    [Fact]
    public async Task Transaction_Commit_Should_Persist_Changes()
    {
        // Arrange
        using var connection = CreateOpenInMemoryConnection();
        var (context, repository) = CreateContextAndRepository(connection);

        await using var tx = await context.Database.BeginTransactionAsync();

        var p1 = Product.Create("P1", "Desc", 1.0m, null);
        var p2 = Product.Create("P2", "Desc", 2.0m, null);

        // Act
        await repository.AddAsync(p1);
        await repository.AddAsync(p2);
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        // Assert en un nuevo contexto (sin tracking)
        var (freshContext, _) = CreateContextAndRepository(connection);
        var all = await freshContext.Products.AsNoTracking().ToListAsync();
        all.Select(x => x.Name).Should().Contain(new[] { "P1", "P2" });
    }

    /// <summary>
    /// Verifica que un rollback de transacción descarta las operaciones realizadas dentro de ella.
    /// </summary>
    [Fact]
    public async Task Transaction_Rollback_Should_Discard_Changes()
    {
        // Arrange
        using var connection = CreateOpenInMemoryConnection();
        var (context, repository) = CreateContextAndRepository(connection);

        await using var tx = await context.Database.BeginTransactionAsync();

        var p = Product.Create("Rollback P", "Desc", 1.5m, null);

        // Act
        await repository.AddAsync(p);
        await context.SaveChangesAsync();
        await tx.RollbackAsync();

        // Assert en un nuevo contexto (sin tracking)
        var (freshContext, _) = CreateContextAndRepository(connection);
        var found = await freshContext.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == p.Id);
        found.Should().BeNull();
    }

    /// <summary>
    /// Verifica que una actualización concurrente lanza DbUpdateConcurrencyException cuando la fila fue eliminada por otro contexto.
    /// </summary>
    [Fact]
    public async Task Concurrency_Update_When_Row_Deleted_Should_Throw()
    {
        // Arrange
        using var connection = CreateOpenInMemoryConnection();
        var (contextA, repositoryA) = CreateContextAndRepository(connection);
        var (contextB, _) = CreateContextAndRepository(connection);

        var product = Product.Create("Concurrent P", "Desc", 3.0m, null);
        await repositoryA.AddAsync(product);
        await contextA.SaveChangesAsync();

        // Cargar en A para modificar después
        var loadedA = await contextA.Products.SingleAsync(x => x.Id == product.Id);

        // B elimina la fila y confirma
        var loadedB = await contextB.Products.SingleAsync(x => x.Id == product.Id);
        contextB.Remove(loadedB);
        await contextB.SaveChangesAsync();

        // Act: A intenta modificar y guardar la misma fila eliminada
        loadedA.Update("Updated After Delete", null, 3.5m, null);

        Func<Task> act = async () =>
        {
            await contextA.SaveChangesAsync();
        };

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    /// <summary>
    /// Crea una conexión SQLite en memoria y la deja abierta (requerido para que la BD persista en memoria durante el test).
    /// </summary>
    private static DbConnection CreateOpenInMemoryConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Crea el <see cref="CatalogDbContext"/> y el <see cref="CatalogRepository{T}"/> con un tenant de pruebas y SQLite en memoria.
    /// </summary>
    private static (CatalogDbContext Context, IRepository<Product> Repository) CreateContextAndRepository(DbConnection connection)
    {
        // Tenant de pruebas sin cadena de conexión (evita que OnConfiguring sobreescriba el provider de EF configurado aquí).
        var tenantInfo = new FshTenantInfo { Id = "test", Identifier = "test", Name = "Test", ConnectionString = string.Empty };
        var tenantContext = new MultiTenantContext<FshTenantInfo> { TenantInfo = tenantInfo };
        var accessor = new TestMultiTenantAccessor { MultiTenantContext = tenantContext };

        var efOptions = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(connection)
            .Options;

        // Publisher no-op para evitar efectos colaterales de eventos de dominio.
        var publisher = new NoopPublisher();

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
}

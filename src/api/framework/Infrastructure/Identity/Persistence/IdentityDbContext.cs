using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using FSH.Framework.Core.Audit;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Identity.RoleClaims;
using FSH.Framework.Infrastructure.Identity.Roles;
using FSH.Framework.Infrastructure.Identity.Users;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Infrastructure.Tenant;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Infrastructure.Identity.Persistence;
/// <summary>
/// Contexto de identidad multi-tenant para la capa de infraestructura.
/// Gestiona usuarios, roles y claims asociados, soportando configuración por tenant.
/// </summary>
public class IdentityDbContext : MultiTenantIdentityDbContext<FshUser,
    FshRole,
    string,
    IdentityUserClaim<string>,
    IdentityUserRole<string>,
    IdentityUserLogin<string>,
    FshRoleClaim,
    IdentityUserToken<string>>
{
    private readonly DatabaseOptions _settings;
    private new FshTenantInfo? TenantInfo { get; set; }
    /// <summary>
    /// Crea una nueva instancia del contexto usando el <see cref="IMultiTenantContextAccessor{T}"/> y las opciones de base de datos.
    /// </summary>
    /// <param name="multiTenantContextAccessor">Acceso al contexto multi-tenant actual.</param>
    /// <param name="settings">Opciones de configuración de base de datos.</param>
    public IdentityDbContext(IMultiTenantContextAccessor<FshTenantInfo> multiTenantContextAccessor, IOptions<DatabaseOptions> settings) : base(multiTenantContextAccessor)
    {
        _settings = settings.Value;
        TenantInfo = multiTenantContextAccessor.MultiTenantContext.TenantInfo!;
        if (TenantInfo != null) 
        {
            TenantInfo.ConnectionString = _settings.ConnectionString;
        }
    }
    /// <summary>
    /// Crea una nueva instancia del contexto con <see cref="DbContextOptions{TContext}"/> explícitas (útil para pruebas) y configuración multi-tenant.
    /// </summary>
    /// <param name="multiTenantContextAccessor">Acceso al contexto multi-tenant actual.</param>
    /// <param name="options">Opciones del DbContext (proveedor, conexión, etc.).</param>
    /// <param name="settings">Opciones de configuración de base de datos.</param>
    public IdentityDbContext(IMultiTenantContextAccessor<FshTenantInfo> multiTenantContextAccessor, DbContextOptions<IdentityDbContext> options, IOptions<DatabaseOptions> settings) : base(multiTenantContextAccessor, options)
    {
        _settings = settings.Value;
        TenantInfo = multiTenantContextAccessor.MultiTenantContext.TenantInfo!;
    }

    /// <summary>
    /// Conjunto de entidades de auditoría.
    /// </summary>
    public required DbSet<AuditTrail> AuditTrails { get; set; }

    /// <summary>
    /// Configura el mapeo del modelo de identidad y aplica configuraciones por ensamblado.
    /// </summary>
    /// <param name="builder">Constructor del modelo.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }

    /// <summary>
    /// Configura el proveedor de base de datos según la cadena de conexión del tenant actual.
    /// </summary>
    /// <param name="optionsBuilder">Constructor de opciones del contexto.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!string.IsNullOrWhiteSpace(TenantInfo?.ConnectionString))
        {
            optionsBuilder.ConfigureDatabase(_settings.Provider, TenantInfo.ConnectionString);
        }
    }
}

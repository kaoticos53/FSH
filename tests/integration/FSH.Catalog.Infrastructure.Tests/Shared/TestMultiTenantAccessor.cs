using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Infrastructure.Tenant;

namespace FSH.Catalog.Infrastructure.Tests.Shared;

/// <summary>
/// Stub de IMultiTenantContextAccessor para pruebas de integración.
/// Implementa las interfaces genérica y no genérica y expone una única propiedad.
/// </summary>
public sealed class TestMultiTenantAccessor : IMultiTenantContextAccessor<FshTenantInfo>, IMultiTenantContextAccessor
{
    /// <summary>
    /// Contexto multi-tenant de pruebas. Debe establecerse antes de usar el stub.
    /// </summary>
    public IMultiTenantContext<FshTenantInfo> MultiTenantContext { get; set; } = default!;

    IMultiTenantContext IMultiTenantContextAccessor.MultiTenantContext => MultiTenantContext;
}

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using FSH.Framework.Core.Tenant.Dtos;
using FSH.Framework.Core.Tenant.Features.GetTenantById;
using FSH.Framework.Core.Tenant.Features.GetTenants;
using MediatR;

namespace FSH.Framework.Core.Tests.Tenant.Features;

/// <summary>
/// Pruebas de unidad para consultas GetTenantById y GetTenants.
/// </summary>
public class GetTenantQueriesTests
{
    /// <summary>
    /// Debe mantener el TenantId proporcionado al construir la query.
    /// </summary>
    [Fact]
    public void GetTenantByIdQuery_ShouldKeep_TenantId()
    {
        var q = new GetTenantByIdQuery("tenant-007");
        q.TenantId.Should().Be("tenant-007");
    }

    /// <summary>
    /// Debe poder instanciarse y ser del tipo esperado de petición genérica.
    /// </summary>
    [Fact]
    public void GetTenantsQuery_ShouldImplement_IRequestOfListTenantDetail()
    {
        var q = new GetTenantsQuery();
        q.Should().NotBeNull();
        // Verificación del contrato IRequest<List<TenantDetail>>
        var requestInterface = typeof(GetTenantsQuery)
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

        requestInterface.Should().NotBeNull();
        requestInterface!.GetGenericArguments()[0].Should().Be(typeof(List<TenantDetail>));
    }
}

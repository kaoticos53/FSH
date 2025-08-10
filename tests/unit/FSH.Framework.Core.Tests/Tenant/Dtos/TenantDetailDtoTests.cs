using FluentAssertions;
using Xunit;
using FSH.Framework.Core.Tenant.Dtos;

namespace FSH.Framework.Core.Tests.Tenant.Dtos;

/// <summary>
/// Pruebas de unidad para <see cref="FSH.Framework.Core.Tenant.Dtos.TenantDetail"/>.
/// Valida valores por defecto y asignación de propiedades (getters/setters).
/// </summary>
public class Tenant_TenantDetailDtoTests
{
    /// <summary>
    /// Debe inicializar valores por defecto coherentes y permitir asignación de propiedades.
    /// </summary>
    [Fact]
    public void Defaults_And_Setters_ShouldWork_AsExpected()
    {
        // Preparación: instancia con valores por defecto
        var dto = new TenantDetail();

        // Verificación de valores por defecto
        dto.Id.Should().BeNull(); // default! -> null en runtime hasta asignación
        dto.Name.Should().BeNull(); // default! -> null
        dto.ConnectionString.Should().BeNull(); // propiedad anulable en este DTO
        dto.AdminEmail.Should().BeNull(); // default! -> null
        dto.IsActive.Should().BeFalse();
        dto.ValidUpto.Should().Be(default);
        dto.Issuer.Should().BeNull();

        // Asignación de propiedades (getters/setters)
        dto.Id = "tenant-001";
        dto.Name = "Acme";
        dto.ConnectionString = "Server=(local);Database=acme;Trusted_Connection=True;";
        dto.AdminEmail = "admin@acme.com";
        dto.IsActive = true;
        dto.ValidUpto = new System.DateTime(2030, 12, 31);
        dto.Issuer = "https://issuer.example";

        // Verificación post-asignación
        dto.Id.Should().Be("tenant-001");
        dto.Name.Should().Be("Acme");
        dto.ConnectionString.Should().NotBeNullOrWhiteSpace();
        dto.AdminEmail.Should().Be("admin@acme.com");
        dto.IsActive.Should().BeTrue();
        dto.ValidUpto.Year.Should().Be(2030);
        dto.Issuer.Should().Be("https://issuer.example");
    }
}

using FluentAssertions;
using Xunit;
using FSH.Framework.Core.Tenancy.Dtos;

namespace FSH.Framework.Core.Tests.Tenancy.Dtos;

/// <summary>
/// Pruebas de unidad para <see cref="FSH.Framework.Core.Tenancy.Dtos.TenantDetail"/>.
/// Valida valores por defecto y asignación de propiedades (getters/setters).
/// </summary>
public class Tenancy_TenantDetailDtoTests
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
        dto.Id.Should().BeEmpty();
        dto.Name.Should().BeEmpty();
        dto.ConnectionString.Should().BeEmpty(); // no anulable, inicializa string.Empty
        dto.IsActive.Should().BeFalse();
        dto.SubscriptionExpiryDate.Should().BeNull();

        // Asignación de propiedades (getters/setters)
        dto.Id = "tenant-XYZ";
        dto.Name = "Contoso";
        dto.ConnectionString = "Server=(local);Database=contoso;Trusted_Connection=True;";
        dto.IsActive = true;
        dto.SubscriptionExpiryDate = new System.DateTime(2031, 1, 1);

        // Verificación post-asignación
        dto.Id.Should().Be("tenant-XYZ");
        dto.Name.Should().Be("Contoso");
        dto.ConnectionString.Should().NotBeNullOrWhiteSpace();
        dto.IsActive.Should().BeTrue();
        dto.SubscriptionExpiryDate!.Value.Year.Should().Be(2031);
    }
}

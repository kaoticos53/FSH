using FluentAssertions;
using Xunit;
using FSH.Framework.Core;

namespace FSH.Framework.Core.Tests.Core;

/// <summary>
/// Pruebas básicas para <see cref="FshCore"/> para cubrir el getter de Name.
/// </summary>
public class FshCoreTests
{
    /// <summary>
    /// Verifica que <see cref="FshCore.Name"/> devuelve el valor por defecto esperado.
    /// </summary>
    [Fact]
    public void Name_ShouldReturnDefaultValue()
    {
        // Verificación directa del valor por defecto.
        FshCore.Name.Should().Be("FshCore", "el nombre por defecto debe coincidir");
    }
}

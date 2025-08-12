using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;
using FluentAssertions;
using FSH.Framework.Infrastructure.Common.Extensions;
using Xunit;

namespace FSH.Framework.Core.Tests.Infrastructure.Common.Extensions;

/// <summary>
/// Pruebas unitarias para <see cref="EnumExtensions"/> cubriendo descripción y lista de descripciones.
/// </summary>
public class EnumExtensionsTests
{
    private enum Sample
    {
        [Description("Descripción Personalizada")]
        WithDescription = 0,
        TestValue1A = 1
    }

    [Flags]
    private enum FlagsSample
    {
        Alpha = 1,
        BetaGamma = 2
    }

    /// <summary>
    /// Verifica que cuando existe <see cref="DescriptionAttribute"/>, se devuelve dicho texto.
    /// </summary>
    [Fact]
    public void GetDescription_Should_Return_Attribute_Value_When_Present()
    {
        // Act
        var desc = Sample.WithDescription.GetDescription();

        // Assert
        desc.Should().Be("Descripción Personalizada", "debe respetar el DescriptionAttribute si existe");
    }

    /// <summary>
    /// Verifica que para nombres PascalCase con dígitos, la descripción inserta espacios entre letras y números.
    /// </summary>
    [Fact]
    public void GetDescription_Should_Split_PascalCase_And_Digits()
    {
        // Act
        var desc = Sample.TestValue1A.GetDescription();

        // Assert
        desc.Should().Be("Test Value 1 A", "debe separar correctamente letras mayúsculas y dígitos");
    }

    /// <summary>
    /// Verifica que <see cref="EnumExtensions.GetDescriptionList(Enum)"/> divide por comas en flags combinados.
    /// Nota: el método no recorta espacios, por lo que se preserva el espacio inicial del segundo elemento.
    /// </summary>
    [Fact]
    public void GetDescriptionList_Should_Split_Flag_Values_By_Comma()
    {
        // Arrange
        var combined = FlagsSample.Alpha | FlagsSample.BetaGamma; // ToString(): "Alpha, BetaGamma"

        // Act
        ReadOnlyCollection<string> list = combined.GetDescriptionList();

        // Assert
        list.Should().HaveCount(2);
        list[0].Should().Be("Alpha");
        list[1].Should().Be(" Beta Gamma", "se preserva el espacio inicial tras la coma según implementación actual");
    }
}

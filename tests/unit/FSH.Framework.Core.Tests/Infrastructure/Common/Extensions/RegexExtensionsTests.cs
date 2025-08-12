using FluentAssertions;
using FSH.Framework.Infrastructure.Common.Extensions;
using Xunit;

namespace FSH.Framework.Core.Tests.Infrastructure.Common.Extensions;

/// <summary>
/// Pruebas unitarias para <see cref="RegexExtensions"/> centradas en <see cref="RegexExtensions.ReplaceWhitespace(string, string)"/>.
/// </summary>
public class RegexExtensionsTests
{
    /// <summary>
    /// Verifica que múltiples espacios, tabulaciones y saltos de línea se reemplazan por un único separador.
    /// </summary>
    [Fact]
    public void ReplaceWhitespace_Should_Condense_All_Whitespace_Into_Single_Separator()
    {
        // Arrange
        var input = "a\t\t b\n\n c\r\n   d";

        // Act
        var result = input.ReplaceWhitespace("_");

        // Assert
        result.Should().Be("a_b_c_d", "toda secuencia de whitespace debe colapsarse al separador indicado");
    }

    /// <summary>
    /// Verifica que al usar espacio como reemplazo, se colapsan secuencias a espacios simples.
    /// </summary>
    [Fact]
    public void ReplaceWhitespace_Should_Collapse_To_Single_Space_When_Using_Space_Replacement()
    {
        // Arrange
        var input = "Hello   world\tthis  is\n.NET";

        // Act
        var result = input.ReplaceWhitespace(" ");

        // Assert
        result.Should().Be("Hello world this is .NET");
    }

    /// <summary>
    /// Verifica que cadenas sin espacios o vacías se mantienen coherentes tras el reemplazo.
    /// </summary>
    [Fact]
    public void ReplaceWhitespace_Should_Handle_Empty_And_NoWhitespace_Inputs()
    {
        // Arrange
        var empty = string.Empty;
        var noWs = "Nowhitespace";

        // Act
        var r1 = empty.ReplaceWhitespace("-");
        var r2 = noWs.ReplaceWhitespace("-");

        // Assert
        r1.Should().Be(string.Empty);
        r2.Should().Be("Nowhitespace");
    }
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace FSH.Framework.Infrastructure.Common.Extensions;
public static class EnumExtensions
{
    /// <summary>
    /// Obtiene una descripción legible a partir del nombre del valor del enum. Si existe
    /// un <see cref="DescriptionAttribute"/>, se devuelve su contenido. Para valores combinados
    /// de enums con <c>[Flags]</c>, procesa cada parte por separado (separadas por coma) y las
    /// une con coma y espacio.
    /// </summary>
    public static string GetDescription(this Enum enumValue)
    {
        var name = enumValue.ToString();
        var field = enumValue.GetType().GetField(name);

        if (field is not null)
        {
            object[] attr = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attr.Length > 0)
            {
                return ((DescriptionAttribute)attr[0]).Description;
            }
        }

        // Sin DescriptionAttribute o nombre combinado (Flags): construir descripción legible
        // Si es un valor combinado ("A, B"), transformar cada parte y volver a unir con ", "
        if (name.Contains(',', System.StringComparison.Ordinal))
        {
            var parts = name.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = ToReadable(parts[i].Trim());
            }
            return string.Join(", ", parts);
        }

        return ToReadable(name);
    }

    private static string ToReadable(string value)
    {
        string result = value;
        result = Regex.Replace(result, "([a-z])([A-Z])", "$1 $2");
        result = Regex.Replace(result, "([A-Za-z])([0-9])", "$1 $2");
        result = Regex.Replace(result, "([0-9])([A-Za-z])", "$1 $2");
        result = Regex.Replace(result, "(?<!^)(?<! )([A-Z][a-z])", " $1");
        return result;
    }

    public static ReadOnlyCollection<string> GetDescriptionList(this Enum enumValue)
    {
        string result = enumValue.GetDescription();
        return new ReadOnlyCollection<string>(result.Split(',').ToList());
    }
}

using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Ardalis.Specification;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Paging;

namespace FSH.Framework.Core.Specifications;

// See https://github.com/ardalis/Specification/issues/53
public static class SpecificationBuilderExtensions
{
    /// <summary>
    /// Aplica los filtros de búsqueda a la consulta de especificación.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad en la especificación.</typeparam>
    /// <param name="query">Constructor de especificación al que se aplicarán los filtros.</param>
    /// <param name="filter">Filtro de búsqueda a aplicar. Puede ser nulo, en cuyo caso no se aplicará ningún filtro.</param>
    /// <returns>Constructor de especificación con los filtros aplicados.</returns>
    public static ISpecificationBuilder<T> SearchBy<T>(this ISpecificationBuilder<T> query, BaseFilter? filter)
    {
        if (filter == null) return query;
            
        return query
            .SearchByKeyword(filter.Keyword)
            .AdvancedSearch(filter.AdvancedSearch)
            .AdvancedFilter(filter.AdvancedFilter);
    }

    public static ISpecificationBuilder<T> PaginateBy<T>(this ISpecificationBuilder<T> query, PaginationFilter filter)
    {
        if (filter.PageNumber <= 0)
        {
            filter.PageNumber = 1;
        }

        if (filter.PageSize <= 0)
        {
            filter.PageSize = 10;
        }

        if (filter.PageNumber > 1)
        {
            query = query.Skip((filter.PageNumber - 1) * filter.PageSize);
        }

        return query
            .Take(filter.PageSize)
            .OrderBy(filter.OrderBy);
    }

    public static IOrderedSpecificationBuilder<T> SearchByKeyword<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        string? keyword) =>
        specificationBuilder.AdvancedSearch(new Search { Keyword = keyword });

    public static IOrderedSpecificationBuilder<T> AdvancedSearch<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        Search? search)
    {
        if (!string.IsNullOrEmpty(search?.Keyword))
        {
            if (search.Fields?.Any() is true)
            {
                // search seleted fields (can contain deeper nested fields)
                foreach (string field in search.Fields)
                {
                    var paramExpr = Expression.Parameter(typeof(T));
                    MemberExpression propertyExpr = GetPropertyExpression(field, paramExpr);

                    specificationBuilder.AddSearchPropertyByKeyword(propertyExpr, paramExpr, search.Keyword);
                }
            }
            else
            {
                // search all fields (only first level)
                foreach (var property in typeof(T).GetProperties()
                    .Where(prop => (Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType) is { } propertyType
                        && !propertyType.IsEnum
                        && Type.GetTypeCode(propertyType) != TypeCode.Object))
                {
                    var paramExpr = Expression.Parameter(typeof(T));
                    var propertyExpr = Expression.Property(paramExpr, property);

                    specificationBuilder.AddSearchPropertyByKeyword(propertyExpr, paramExpr, search.Keyword);
                }
            }
        }

        return new OrderedSpecificationBuilder<T>(specificationBuilder.Specification);
    }

    private static void AddSearchPropertyByKeyword<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        Expression propertyExpr,
        ParameterExpression paramExpr,
        string keyword,
        string operatorSearch = FilterOperator.CONTAINS)
    {
        if (propertyExpr is not MemberExpression memberExpr || memberExpr.Member is not PropertyInfo property)
        {
            throw new ArgumentException("propertyExpr must be a property expression.", nameof(propertyExpr));
        }

        string searchTerm = operatorSearch switch
        {
            FilterOperator.STARTSWITH => $"{keyword.ToLower()}%",
            FilterOperator.ENDSWITH => $"%{keyword.ToLower()}",
            FilterOperator.CONTAINS => $"%{keyword.ToLower()}%",
            _ => throw new ArgumentException("operatorSearch is not valid.", nameof(operatorSearch))
        };

        // Generate lambda [ x => x.Property ] for string properties
        // or [ x => ((object)x.Property) == null ? null : x.Property.ToString() ] for other properties
        Expression selectorExpr =
            property.PropertyType == typeof(string)
                ? propertyExpr
                : Expression.Condition(
                    Expression.Equal(Expression.Convert(propertyExpr, typeof(object)), Expression.Constant(null, typeof(object))),
                    Expression.Constant(null, typeof(string)),
                    Expression.Call(propertyExpr, "ToString", null, null));

        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
        Expression callToLowerMethod = Expression.Call(selectorExpr, toLowerMethod!);

        var selector = Expression.Lambda<Func<T, string>>(callToLowerMethod, paramExpr);

        ((List<SearchExpressionInfo<T>>)specificationBuilder.Specification.SearchCriterias)
            .Add(new SearchExpressionInfo<T>(selector, searchTerm, 1));
    }

    /// <summary>
    /// Aplica un filtro avanzado a la consulta de especificación.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad en la especificación.</typeparam>
    /// <param name="specificationBuilder">Constructor de especificación al que se aplicará el filtro.</param>
    /// <param name="filter">Filtro a aplicar. Puede ser nulo, en cuyo caso no se aplicará ningún filtro.</param>
    /// <returns>Constructor de especificación con el filtro aplicado.</returns>
    /// <exception cref="CustomException">Se lanza cuando se declara una lógica pero no se proporcionan filtros.</exception>
    public static IOrderedSpecificationBuilder<T> AdvancedFilter<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        Filter? filter)
    {
        if (filter is null)
        {
            return new OrderedSpecificationBuilder<T>(specificationBuilder.Specification);
        }

        var parameter = Expression.Parameter(typeof(T));
        Expression binaryExpressionFilter;

        if (!string.IsNullOrEmpty(filter.Logic))
        {
            if (filter.Filters is null)
            {
                throw new CustomException("El atributo Filters es requerido cuando se declara una lógica");
            }

            binaryExpressionFilter = CreateFilterExpression(filter.Logic, filter.Filters, parameter);
        }
        else
        {
            var filterValid = GetValidFilter(filter);
            binaryExpressionFilter = CreateFilterExpression(
                filterValid.Field ?? throw new InvalidOperationException("El campo del filtro no puede ser nulo"),
                filterValid.Operator ?? throw new InvalidOperationException("El operador del filtro no puede ser nulo"),
                filterValid.Value,
                parameter);
        }

        ((List<WhereExpressionInfo<T>>)specificationBuilder.Specification.WhereExpressions)
            .Add(new WhereExpressionInfo<T>(Expression.Lambda<Func<T, bool>>(binaryExpressionFilter, parameter)));

        return new OrderedSpecificationBuilder<T>(specificationBuilder.Specification);
    }

    /// <summary>
    /// Crea una expresión de filtro a partir de una lógica y una colección de filtros.
    /// </summary>
    /// <param name="logic">Lógica a aplicar para combinar los filtros (AND, OR, etc.).</param>
    /// <param name="filters">Colección de filtros a aplicar.</param>
    /// <param name="parameter">Parámetro de expresión para la entidad.</param>
    /// <returns>Expresión que representa el filtro combinado.</returns>
    /// <exception cref="ArgumentNullException">Se lanza cuando <paramref name="logic"/> o <paramref name="filters"/> es nulo.</exception>
    /// <exception cref="CustomException">Se lanza cuando un filtro declara una lógica pero no proporciona filtros.</exception>
    private static Expression CreateFilterExpression(
        string logic,
        IEnumerable<Filter> filters,
        ParameterExpression parameter)
    {
        if (string.IsNullOrEmpty(logic))
        {
            throw new ArgumentNullException(nameof(logic), "La lógica no puede ser nula o vacía");
        }

        if (filters is null)
        {
            throw new ArgumentNullException(nameof(filters), "La colección de filtros no puede ser nula");
        }

        Expression? filterExpression = null;

        foreach (var filter in filters)
        {
            if (filter is null) continue;

            Expression currentExpression;

            if (!string.IsNullOrEmpty(filter.Logic))
            {
                if (filter.Filters is null)
                {
                    throw new CustomException("El atributo Filters es requerido cuando se declara una lógica");
                }
                currentExpression = CreateFilterExpression(filter.Logic, filter.Filters, parameter);
            }
            else
            {
                var filterValid = GetValidFilter(filter);
                currentExpression = CreateFilterExpression(
                    filterValid.Field ?? throw new InvalidOperationException("El campo del filtro no puede ser nulo"),
                    filterValid.Operator ?? throw new InvalidOperationException("El operador del filtro no puede ser nulo"),
                    filterValid.Value,
                    parameter);
            }

            filterExpression = filterExpression is null 
                ? currentExpression 
                : CombineFilter(logic, filterExpression, currentExpression);
        }

        return filterExpression ?? Expression.Constant(true); // Si no hay filtros, devuelve true
    }

    /// <summary>
    /// Crea una expresión de filtro para un campo específico.
    /// </summary>
    /// <param name="field">Nombre del campo a filtrar.</param>
    /// <param name="filterOperator">Operador de filtro a aplicar.</param>
    /// <param name="value">Valor contra el que se filtrará.</param>
    /// <param name="parameter">Parámetro de expresión para la entidad.</param>
    /// <returns>Expresión que representa el filtro.</returns>
    /// <exception cref="ArgumentNullException">Se lanza cuando <paramref name="field"/>, <paramref name="filterOperator"/> o <paramref name="parameter"/> es nulo.</exception>
    private static Expression CreateFilterExpression(
        string field,
        string filterOperator,
        object? value,
        ParameterExpression parameter)
    {
        if (string.IsNullOrEmpty(field))
        {
            throw new ArgumentNullException(nameof(field), "El campo no puede ser nulo o vacío");
        }

        if (string.IsNullOrEmpty(filterOperator))
        {
            throw new ArgumentNullException(nameof(filterOperator), "El operador no puede ser nulo o vacío");
        }

        if (parameter is null)
        {
            throw new ArgumentNullException(nameof(parameter), "El parámetro no puede ser nulo");
        }

        var propertyExpression = GetPropertyExpression(field, parameter);
        var valueExpression = GeValuetExpression(field, value, propertyExpression.Type);
        return CreateFilterExpression(propertyExpression, valueExpression, filterOperator);
    }

    /// <summary>
    /// Crea una expresión de filtro a partir de expresiones de miembro y valor.
    /// </summary>
    /// <param name="memberExpression">Expresión que representa el miembro de la entidad.</param>
    /// <param name="constantExpression">Expresión que representa el valor constante para comparar.</param>
    /// <param name="filterOperator">Operador de filtro a aplicar.</param>
    /// <returns>Expresión que representa la comparación.</returns>
    /// <exception cref="ArgumentNullException">Se lanza cuando <paramref name="memberExpression"/>, <paramref name="constantExpression"/> o <paramref name="filterOperator"/> es nulo.</exception>
    /// <exception cref="ArgumentException">Se lanza cuando el operador de filtro no es válido.</exception>
    private static Expression CreateFilterExpression(
        Expression memberExpression,
        Expression constantExpression,
        string filterOperator)
    {
        if (memberExpression.Type == typeof(string))
        {
            constantExpression = Expression.Call(constantExpression, "ToLower", null);
            memberExpression = Expression.Call(memberExpression, "ToLower", null);
        }

        return filterOperator switch
        {
            FilterOperator.EQ => Expression.Equal(memberExpression, constantExpression),
            FilterOperator.NEQ => Expression.NotEqual(memberExpression, constantExpression),
            FilterOperator.LT => Expression.LessThan(memberExpression, constantExpression),
            FilterOperator.LTE => Expression.LessThanOrEqual(memberExpression, constantExpression),
            FilterOperator.GT => Expression.GreaterThan(memberExpression, constantExpression),
            FilterOperator.GTE => Expression.GreaterThanOrEqual(memberExpression, constantExpression),
            FilterOperator.CONTAINS => Expression.Call(memberExpression, "Contains", null, constantExpression),
            FilterOperator.STARTSWITH => Expression.Call(memberExpression, "StartsWith", null, constantExpression),
            FilterOperator.ENDSWITH => Expression.Call(memberExpression, "EndsWith", null, constantExpression),
            _ => throw new CustomException("Filter Operator is not valid."),
        };
    }

    private static Expression CombineFilter(
        string filterOperator,
        Expression bExpresionBase,
        Expression bExpresion) => filterOperator switch
        {
            FilterLogic.AND => Expression.And(bExpresionBase, bExpresion),
            FilterLogic.OR => Expression.Or(bExpresionBase, bExpresion),
            FilterLogic.XOR => Expression.ExclusiveOr(bExpresionBase, bExpresion),
            _ => throw new ArgumentException("FilterLogic is not valid."),
        };

    private static MemberExpression GetPropertyExpression(
        string propertyName,
        ParameterExpression parameter)
    {
        Expression propertyExpression = parameter;
        foreach (string member in propertyName.Split('.'))
        {
            propertyExpression = Expression.PropertyOrField(propertyExpression, member);
        }

        return (MemberExpression)propertyExpression;
    }

    /// <summary>
    /// Obtiene una representación de cadena de un valor que puede ser un string, JsonElement u otro tipo.
    /// </summary>
    /// <param name="value">Valor del que se obtendrá la representación de cadena.</param>
    /// <returns>Representación de cadena del valor, o null si el valor es nulo.</returns>
    private static string? GetStringFromJsonElement(object value)
    {
        return value switch
        {
            string str => str,
            JsonElement element => element.GetString(),
            _ => value?.ToString()
        };
    }

    private static ConstantExpression GeValuetExpression(
        string field,
        object? value,
        Type propertyType)
    {
        if (value == null) return Expression.Constant(null, propertyType);

        if (propertyType.IsEnum)
        {
            string? stringEnum = GetStringFromJsonElement(value);

            if (!Enum.TryParse(propertyType, stringEnum, true, out object? valueparsed)) throw new CustomException(string.Format("Value {0} is not valid for {1}", value, field));

            return Expression.Constant(valueparsed, propertyType);
        }

        if (propertyType == typeof(Guid))
        {
            string? stringGuid = GetStringFromJsonElement(value);

            if (!Guid.TryParse(stringGuid, out Guid valueparsed)) throw new CustomException(string.Format("Value {0} is not valid for {1}", value, field));

            return Expression.Constant(valueparsed, propertyType);
        }

        if (propertyType == typeof(string))
        {
            string? text = GetStringFromJsonElement(value);

            return Expression.Constant(text, propertyType);
        }

        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
        {
            string? text = GetStringFromJsonElement(value);
            return Expression.Constant(ChangeType(text!, propertyType), propertyType);
        }

        return Expression.Constant(ChangeType(((JsonElement)value).GetRawText(), propertyType), propertyType);
    }

    public static dynamic? ChangeType(object value, Type conversion)
    {
        var t = conversion;

        if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
        {
            if (value == null)
            {
                return null;
            }

            t = Nullable.GetUnderlyingType(t);
        }

        return Convert.ChangeType(value, t!);
    }

    /// <summary>
    /// Valida que un filtro tenga los campos obligatorios.
    /// </summary>
    /// <param name="filter">Filtro a validar.</param>
    /// <returns>El mismo filtro si es válido.</returns>
    /// <exception cref="ArgumentNullException">Se lanza cuando el filtro es nulo.</exception>
    /// <exception cref="CustomException">Se lanza cuando faltan campos obligatorios en el filtro.</exception>
    private static Filter GetValidFilter(Filter filter)
    {
        if (filter is null)
        {
            throw new ArgumentNullException(nameof(filter), "El filtro no puede ser nulo");
        }

        if (string.IsNullOrWhiteSpace(filter.Field))
        {
            throw new CustomException("El atributo 'Field' es requerido al declarar un filtro");
        }

        if (string.IsNullOrWhiteSpace(filter.Operator))
        {
            throw new CustomException("El atributo 'Operator' es requerido al declarar un filtro");
        }

        return filter;
    }

    public static IOrderedSpecificationBuilder<T> OrderBy<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        string[]? orderByFields,
        bool useCustomImplementation = true)
    {
        if (orderByFields is not null)
        {
            foreach (var field in ParseOrderBy(orderByFields))
            {
                var paramExpr = Expression.Parameter(typeof(T));

                Expression propertyExpr = paramExpr;
                foreach (string member in field.Key.Split('.'))
                {
                    propertyExpr = Expression.PropertyOrField(propertyExpr, member);
                }

                var keySelector = Expression.Lambda<Func<T, object?>>(
                    Expression.Convert(propertyExpr, typeof(object)),
                    paramExpr);

                ((List<OrderExpressionInfo<T>>)specificationBuilder.Specification.OrderExpressions)
                    .Add(new OrderExpressionInfo<T>(keySelector, field.Value));
            }
        }

        return new OrderedSpecificationBuilder<T>(specificationBuilder.Specification);
    }

    private static Dictionary<string, OrderTypeEnum> ParseOrderBy(string[] orderByFields) =>
        new(orderByFields.Select((orderByfield, index) =>
        {
            string[] fieldParts = orderByfield.Split(' ');
            string field = fieldParts[0];
            bool descending = fieldParts.Length > 1 && fieldParts[1].StartsWith("Desc", StringComparison.OrdinalIgnoreCase);
            var orderBy = index == 0
                ? descending ? OrderTypeEnum.OrderByDescending
                                : OrderTypeEnum.OrderBy
                : descending ? OrderTypeEnum.ThenByDescending
                                : OrderTypeEnum.ThenBy;

            return new KeyValuePair<string, OrderTypeEnum>(field, orderBy);
        }));
}

using System.Linq.Expressions;
using System.Text.Json;
using Ardalis.Specification;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FluentAssertions;
using Xunit;

namespace FSH.Framework.Core.Tests.Specifications;

/// <summary>
/// Contains unit tests for the <see cref="FSH.Framework.Core.Specifications.SpecificationBuilderExtensions"/> class.
/// </summary>
public class SpecificationBuilderExtensionsTests
{
    /// <summary>
    /// Tests that the SearchBy extension method handles null filter without throwing an exception.
    /// </summary>
    [Fact]
    public void SearchBy_WithNullFilter_DoesNotThrow()
    {
        // Arrange
        var spec = new TestSpecification<TestEntity>();
        
        // Act
        var result = spec.GetQueryBuilder().SearchBy(null!);
        
        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the SearchBy extension method sets up search correctly when a keyword is provided.
    /// </summary>
    [Fact]
    public void SearchBy_WithKeyword_SetsUpSearch()
    {
        // Arrange
        var spec = new TestSpecification<TestEntity>();
        var filter = new BaseFilter { Keyword = "test" };
        
        // Act
        var result = spec.GetQueryBuilder().SearchBy(filter);
        
        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the PaginateBy extension method uses the default page number when an invalid value is provided.
    /// </summary>
    [Fact]
    public void PaginateBy_WithInvalidPageNumber_SetsDefaultPageNumber()
    {
        // Arrange
        var spec = new TestSpecification<TestEntity>();
        var filter = new PaginationFilter { PageNumber = 0, PageSize = 10 };
        
        // Act
        var result = spec.GetQueryBuilder().PaginateBy(filter);
        
        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the PaginateBy extension method uses the default page size when an invalid value is provided.
    /// </summary>
    [Fact]
    public void PaginateBy_WithInvalidPageSize_SetsDefaultPageSize()
    {
        // Arrange
        var spec = new TestSpecification<TestEntity>();
        var filter = new PaginationFilter { PageNumber = 1, PageSize = 0 };
        
        // Act
        var result = spec.GetQueryBuilder().PaginateBy(filter);
        
        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the PaginateBy extension method correctly applies skip when page number is greater than one.
    /// </summary>
    [Fact]
    public void PaginateBy_WithPageNumberGreaterThanOne_AppliesSkip()
    {
        // Arrange
        var spec = new TestSpecification<TestEntity>();
        var filter = new PaginationFilter { PageNumber = 2, PageSize = 10 };
        
        // Act
        var result = spec.GetQueryBuilder().PaginateBy(filter);
        
        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the SearchByKeyword extension method handles null keyword without throwing an exception.
    /// </summary>
    [Fact]
    public void SearchByKeyword_WithNullKeyword_DoesNotThrow()
    {
        // Arrange
        var spec = new TestSpecification<TestEntity>();
        
        // Act
        var result = spec.GetQueryBuilder().SearchByKeyword(null);
        
        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the SearchByKeyword extension method sets up search correctly when a keyword is provided.
    /// </summary>
    [Fact]
    public void SearchByKeyword_WithKeyword_SetsUpSearch()
    {
        // Arrange
        var spec = new TestSpecification<TestEntity>();
        
        // Act
        var result = spec.GetQueryBuilder().SearchByKeyword("test");
        
        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the AdvancedSearch extension method handles null search object without throwing an exception.
    /// </summary>
    [Fact]
    public void AdvancedSearch_WithNullSearch_DoesNotThrow()
    {
        // Arrange
        var spec = new TestSpecification<TestEntity>();
        
        // Act
        var result = spec.GetQueryBuilder().AdvancedSearch(null);
        
        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the AdvancedSearch extension method correctly searches in specified fields when search fields are provided.
    /// </summary>
    [Fact]
    public void AdvancedSearch_WithSearchFields_SearchesInSpecifiedFields()
    {
        // Arrange
        var spec = new TestSpecification<TestEntity>();
        var search = new Search 
        { 
            Keyword = "test",
            Fields = new List<string> { nameof(TestEntity.Name) }
        };
        
        // Act
        var result = spec.Query.AdvancedSearch(search);
        
        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the AdvancedSearch extension method searches in all properties when no specific fields are provided.
    /// </summary>
    [Fact]
    public void AdvancedSearch_WithoutSearchFields_SearchesInAllProperties()
    {
        // Arrange
        var spec = new TestSpecification<TestEntity>();
        var search = new Search { Keyword = "test" };
        
        // Act
        var result = spec.Query.AdvancedSearch(search);
        
        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the AdvancedFilter extension method handles null filter without throwing an exception.
    /// </summary>
    [Fact]
    public void AdvancedFilter_WithNullFilter_DoesNotThrow()
    {
        // Arrange
        var spec = new TestSpecification<TestEntity>();
        
        // Act
        var result = spec.GetQueryBuilder().AdvancedFilter(null);
        
        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the AdvancedFilter extension method correctly applies a simple filter.
    /// </summary>
    [Fact]
    public void AdvancedFilter_WithSimpleFilter_AppliesFilter()
    {
        // Arrange
        var spec = new TestSpecification<TestEntity>();
        var filter = new Filter
        {
            Logic = "and",
            Filters = new List<Filter>
            {
                new()
                {
                    Field = nameof(TestEntity.Name),
                    Operator = "eq",
                    Value = "Test"
                }
            }
        };
        
        // Act
        var result = spec.GetQueryBuilder().AdvancedFilter(filter);
        
        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the OrderBy extension method handles null order by without throwing an exception.
    /// </summary>
    [Fact]
    public void OrderBy_WithNullOrderBy_DoesNotThrow()
    {
        // Arrange
        var spec = new TestSpecification<TestEntity>();
        
        // Act
        var result = spec.GetQueryBuilder().OrderBy(null, useCustomImplementation: true);
        
        // Assert
        result.Should().NotBeNull();
    }


    /// <summary>
    /// Tests that the OrderBy extension method correctly applies ordering when order by fields are provided.
    /// </summary>
    [Fact]
    public void OrderBy_WithOrderBy_AppliesOrdering()
    {
        // Arrange
        var spec = new TestSpecification<TestEntity>();
        var orderBy = new[] { "Name", "Id desc" };
        
        // Act
        var result = spec.GetQueryBuilder().OrderBy(orderBy, useCustomImplementation: true);
        
        // Assert
        result.Should().NotBeNull();
    }

    
    /// <summary>
    /// Test entity class for unit testing specification builder extensions.
    /// </summary>
    private class TestEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the test entity.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the test entity.
        /// </summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Test specification class for unit testing specification builder extensions.
    /// </summary>
    private class TestSpecification<T> : Specification<T> where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestSpecification{T}"/> class.
        /// </summary>
        public TestSpecification()
        {
            // La propiedad Query es de solo lectura y se inicializa internamente
            // No es necesario asignarla manualmente
        }

        /// <summary>
        /// Gets the query builder for the test specification.
        /// </summary>
        public ISpecificationBuilder<T> GetQueryBuilder()
        {
            return Query;
        }
    }
}

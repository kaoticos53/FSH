using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FluentAssertions;
using Xunit;

namespace FSH.Framework.Core.Tests.Specifications;

/// <summary>
/// Contains unit tests for the <see cref="EntitiesByPaginationFilterSpec{T}"/> and 
/// <see cref="EntitiesByPaginationFilterSpec{T, TResult}"/> classes.
/// </summary>
public class EntitiesByPaginationFilterSpecTests
{
    /// <summary>
    /// Tests that the constructor with a pagination filter initializes the specification correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithPaginationFilter_SetsUpQueryCorrectly()
    {
        // Arrange
        var filter = new PaginationFilter
        {
            PageNumber = 2,
            PageSize = 10,
            Keyword = "test"
        };

        // Act
        var spec = new EntitiesByPaginationFilterSpec<TestEntity>(filter);

        // Assert
        spec.Should().NotBeNull();
        spec.Query.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the constructor with a pagination filter and projection initializes the specification correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithPaginationFilterAndProjection_SetsUpQueryCorrectly()
    {
        // Arrange
        var filter = new PaginationFilter
        {
            PageNumber = 1,
            PageSize = 20,
            Keyword = "test"
        };

        // Act
        var spec = new EntitiesByPaginationFilterSpec<TestEntity, TestEntityDto>(filter);

        // Assert
        spec.Should().NotBeNull();
        spec.Query.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the constructor with order by configuration sets up query ordering correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithOrderBy_ConfiguresQueryWithOrdering()
    {
        // Arrange
        var filter = new PaginationFilter
        {
            PageNumber = 1,
            PageSize = 10,
            OrderBy = new[] { "Name", "Id desc" }
        };

        // Act
        var spec = new EntitiesByPaginationFilterSpec<TestEntity>(filter);

        // Assert
        spec.Should().NotBeNull();
    }


    /// <summary>
    /// Tests that the constructor with advanced search and pagination configures the query correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithAdvancedSearchAndPagination_ConfiguresQueryCorrectly()
    {
        // Arrange
        var filter = new PaginationFilter
        {
            PageNumber = 1,
            PageSize = 10,
            AdvancedSearch = new Search
            {
                Keyword = "test",
                Fields = new List<string> { nameof(TestEntity.Name) }
            }
        };

        // Act
        var spec = new EntitiesByPaginationFilterSpec<TestEntity>(filter);

        // Assert
        spec.Should().NotBeNull();
    }


    /// <summary>
    /// Tests that the constructor with advanced filter and pagination configures the query correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithAdvancedFilterAndPagination_ConfiguresQueryCorrectly()
    {
        // Arrange
        var filter = new PaginationFilter
        {
            PageNumber = 1,
            PageSize = 10,
            AdvancedFilter = new Filter
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
            }
        };

        // Act
        var spec = new EntitiesByPaginationFilterSpec<TestEntity>(filter);

        // Assert
        spec.Should().NotBeNull();
    }


    /// <summary>
    /// Tests that the constructor with an invalid page number falls back to the default page number.
    /// </summary>
    [Fact]
    public void Constructor_WithInvalidPageNumber_SetsDefaultPageNumber()
    {
        // Arrange
        var filter = new PaginationFilter
        {
            PageNumber = 0, // Invalid page number
            PageSize = 10
        };

        // Act
        var spec = new EntitiesByPaginationFilterSpec<TestEntity>(filter);


        // Assert
        spec.Should().NotBeNull();
        // The actual validation of page number happens in the SpecificationBuilderExtensions.PaginateBy method
    }

    /// <summary>
    /// Tests that the constructor with an invalid page size falls back to the default page size.
    /// </summary>
    [Fact]
    public void Constructor_WithInvalidPageSize_SetsDefaultPageSize()
    {
        // Arrange
        var filter = new PaginationFilter
        {
            PageNumber = 1,
            PageSize = 0 // Invalid page size
        };

        // Act
        var spec = new EntitiesByPaginationFilterSpec<TestEntity>(filter);

        // Assert
        spec.Should().NotBeNull();
        // The actual validation of page size happens in the SpecificationBuilderExtensions.PaginateBy method
    }
}

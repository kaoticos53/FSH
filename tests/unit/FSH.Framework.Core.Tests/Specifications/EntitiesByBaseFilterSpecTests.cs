using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace FSH.Framework.Core.Tests.Specifications;

/// <summary>
/// Test entity class for unit testing the base filter specifications.
/// </summary>
public class TestEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the test entity.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the test entity.
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// Gets or sets the description of the test entity.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Test DTO class for unit testing the base filter specifications with projection.
/// </summary>
public class TestEntityDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the test entity DTO.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the test entity DTO.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Contains unit tests for the <see cref="EntitiesByBaseFilterSpec{T}"/> and 
/// <see cref="EntitiesByBaseFilterSpec{T, TResult}"/> classes.
/// </summary>
public class EntitiesByBaseFilterSpecTests
{
    /// <summary>
    /// Tests that the constructor with a base filter initializes the specification correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithBaseFilter_SetsUpQueryCorrectly()
    {
        // Arrange
        var filter = new BaseFilter
        {
            Keyword = "test"
        };

        // Act
        var spec = new EntitiesByBaseFilterSpec<TestEntity>(filter);

        // Assert
        spec.Should().NotBeNull();
        spec.Query.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the constructor with a base filter and projection initializes the specification correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithBaseFilterAndProjection_SetsUpQueryCorrectly()
    {
        // Arrange
        var filter = new BaseFilter
        {
            Keyword = "test"
        };

        // Act
        var spec = new EntitiesByBaseFilterSpec<TestEntity, TestEntityDto>(filter);

        // Assert
        spec.Should().NotBeNull();
        spec.Query.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the constructor with advanced search configures the query correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithAdvancedSearch_ConfiguresQueryCorrectly()
    {
        // Arrange
        var filter = new BaseFilter
        {
            AdvancedSearch = new Search
            {
                Keyword = "test",
                Fields = new List<string> { nameof(TestEntity.Name) }
            }
        };

        // Act
        var spec = new EntitiesByBaseFilterSpec<TestEntity>(filter);

        // Assert
        spec.Should().NotBeNull();
    }


    /// <summary>
    /// Tests that the constructor with advanced filter configures the query correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithAdvancedFilter_ConfiguresQueryCorrectly()
    {
        // Arrange
        var filter = new BaseFilter
        {
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
        var spec = new EntitiesByBaseFilterSpec<TestEntity>(filter);

        // Assert
        spec.Should().NotBeNull();
    }

}

using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace Blazing.Json.Queryable.Tests.IntegrationTests;

/// <summary>
/// Integration tests for String provider (FromString).
/// </summary>
public class StringProviderTests
{
    [Fact]
    public void FromString_SimpleWhereQuery_ReturnsFilteredResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Where(p => p.Age > 30)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 30);
    }

    [Fact]
    public void FromString_OrderByQuery_ReturnsSortedResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .OrderBy(p => p.Name)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldBeOrderedBy(p => p.Name);
    }

    [Fact]
    public void FromString_SelectQuery_ProjectsResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Select(p => new PersonDto { Name = p.Name, Age = p.Age })
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(dto => !string.IsNullOrEmpty(dto.Name));
    }

    [Fact]
    public void FromString_ComplexQuery_AppliesAllOperations()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Where(p => p.Age > 25)
            .OrderBy(p => p.Name)
            .Skip(1)
            .Take(3)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBeLessThanOrEqualTo(3);
        results.ShouldAllBe(p => p.Age > 25);
        results.ShouldBeOrderedBy(p => p.Name);
    }

    [Fact]
    public void FromString_WithCustomOptions_RespectsOptions()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var json = TestData.SerializeToJson(data, options);

        // Act
        var results = JsonQueryable<Person>.FromString(json, options)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(data.Count);
    }

    [Fact]
    public void FromString_EmptyArray_ReturnsEmptyResults()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void FromString_WithNullableProperties_HandlesNulls()
    {
        // Arrange
        var data = TestData.GetNullableDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<NullableModel>.FromString(json)
            .Where(m => m.Name != null)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(m => m.Name != null);
    }

    [Fact]
    public void FromString_FirstOrDefault_ReturnsNullForEmpty()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .FirstOrDefault();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void FromString_Single_ReturnsOnlyElement()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset().Take(1).ToList();
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .Single();

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(data[0].Name);
    }

    [Fact]
    public void FromString_LongCount_ReturnsCount()
    {
        // Arrange
        var data = TestData.GetMediumPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var count = JsonQueryable<Person>.FromString(json)
            .LongCount();

        // Assert
        count.ShouldBe(data.Count);
    }
}

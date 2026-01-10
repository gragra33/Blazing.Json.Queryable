using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace Blazing.Json.Queryable.Tests.IntegrationTests;

/// <summary>
/// Integration tests for UTF-8 provider (FromUtf8).
/// </summary>
public class Utf8ProviderTests
{
    [Fact]
    public void FromUtf8_SimpleWhereQuery_ReturnsFilteredResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Age > 30)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 30);
    }

    [Fact]
    public void FromUtf8_OrderByQuery_ReturnsSortedResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .OrderBy(p => p.Name)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldBeOrderedBy(p => p.Name);
    }

    [Fact]
    public void FromUtf8_SelectQuery_ProjectsResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Select(p => new PersonDto { Name = p.Name, Age = p.Age })
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(dto => !string.IsNullOrEmpty(dto.Name));
    }

    [Fact]
    public void FromUtf8_ComplexQuery_AppliesAllOperations()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
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
    public void FromUtf8_WithCustomOptions_RespectsOptions()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var utf8Json = TestData.SerializeToUtf8(data, options);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json, options)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(data.Count);
    }

    [Fact]
    public void FromUtf8_EmptyArray_ReturnsEmptyResults()
    {
        // Arrange
        var utf8Json = TestData.GetEmptyJsonArrayUtf8();

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void FromUtf8_OrderByDescending_ReturnsSortedDescending()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .OrderByDescending(p => p.Age)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldBeOrderedBy(p => p.Age, descending: true);
    }

    [Fact]
    public void FromUtf8_ThenBy_AppliesSecondarySorting()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .OrderBy(p => p.IsActive)
            .ThenBy(p => p.Name)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        // Results should be sorted by IsActive first, then by Name within each group
    }

    [Fact]
    public void FromUtf8_First_ReturnsFirstElement()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var result = JsonQueryable<Person>.FromUtf8(utf8Json)
            .OrderBy(p => p.Age)
            .First();

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public void FromUtf8_Count_ReturnsCorrectCount()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var count = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Age > 25)
            .Count();

        // Assert
        count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void FromUtf8_Any_ReturnsTrue_WhenResultsExist()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var exists = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Any(p => p.Age > 25);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public void FromUtf8_MultipleWhereConditions_CombinesFilters()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Age > 25)
            .Where(p => p.IsActive)
            .ToList();

        // Assert
        results.ShouldAllBe(p => p.Age > 25 && p.IsActive);
    }
}

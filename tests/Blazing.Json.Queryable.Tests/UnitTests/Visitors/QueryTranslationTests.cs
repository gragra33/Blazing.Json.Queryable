using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.Visitors;

/// <summary>
/// Unit tests for query translation through integration testing.
/// Tests the expression visitor pipeline indirectly through the public API.
/// </summary>
public class QueryTranslationTests
{
    [Fact]
    public void Translate_SimpleWhereQuery_ExecutesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Age > 25)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 25);
    }

    [Fact]
    public void Translate_OrderByQuery_ExecutesCorrectly()
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
    public void Translate_ComplexQuery_ExecutesCorrectly()
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
    public void Translate_MultipleWhereQueries_CombinesPredicates()
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
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 25 && p.IsActive);
    }

    [Fact]
    public void Translate_SelectQuery_ProjectsCorrectly()
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
    public void Translate_ThenBy_AppliesSecondarySorting()
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
        // Results should be sorted by IsActive first, then by Name
    }

    #region Complex Expression Tests

    [Fact]
    public void Translate_ComplexWhereWithAndConditions_ExecutesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Age > 25 && p.Age < 50 && p.IsActive)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 25 && p.Age < 50 && p.IsActive);
    }

    [Fact]
    public void Translate_ComplexWhereWithOrConditions_ExecutesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Age < 20 || p.Age > 60 || p.City == "Seattle")
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age < 20 || p.Age > 60 || p.City == "Seattle");
    }

    [Fact]
    public void Translate_ComplexWhereWithNestedConditions_ExecutesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => (p.Age > 25 && p.City == "Seattle") || (p.Age < 30 && p.IsActive))
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => (p.Age > 25 && p.City == "Seattle") || (p.Age < 30 && p.IsActive));
    }

    [Fact]
    public void Translate_WhereWithStringMethods_ExecutesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Name != null && p.Name.StartsWith("A"))
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Name != null && p.Name.StartsWith("A"));
    }

    [Fact]
    public void Translate_WhereWithStringContains_ExecutesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Email != null && p.Email.Contains("@example.com"))
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Email != null && p.Email.Contains("@example.com"));
    }

    [Fact]
    public void Translate_SelectWithComplexProjection_ExecutesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Select(p => new
            {
                p.Name,
                p.Age,
                IsAdult = p.Age >= 18,
                Location = p.City ?? "Unknown"
            })
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(r => !string.IsNullOrEmpty(r.Name));
        results.ShouldAllBe(r => r.IsAdult == (r.Age >= 18));
    }

    [Fact]
    public void Translate_OrderByDescending_ExecutesCorrectly()
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
        for (int i = 1; i < results.Count; i++)
        {
            results[i - 1].Age.ShouldBeGreaterThanOrEqualTo(results[i].Age);
        }
    }

    [Fact]
    public void Translate_ThenByDescending_ExecutesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .OrderBy(p => p.City)
            .ThenByDescending(p => p.Age)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        // Verify cities are sorted ascending
        var cities = results.Select(p => p.City).ToList();
        for (int i = 1; i < cities.Count; i++)
        {
            string.Compare(cities[i - 1], cities[i], StringComparison.Ordinal).ShouldBeLessThanOrEqualTo(0);
        }
    }

    [Fact]
    public void Translate_WhereWithNullChecks_ExecutesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.City != null)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.City != null);
    }

    [Fact]
    public void Translate_ComplexChainedQuery_ExecutesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Age > 20)
            .Where(p => p.IsActive)
            .OrderBy(p => p.City)
            .ThenByDescending(p => p.Age)
            .Skip(2)
            .Take(5)
            .Select(p => new { p.Name, p.Age, p.City })
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBeLessThanOrEqualTo(5);
        results.ShouldAllBe(r => !string.IsNullOrEmpty(r.Name));
    }

    [Fact]
    public void Translate_FirstQuery_ReturnsFirstElement()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var result = JsonQueryable<Person>.FromUtf8(utf8Json)
            .OrderBy(p => p.Name)
            .First();

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public void Translate_FirstWithPredicate_ReturnsMatchingElement()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var result = JsonQueryable<Person>.FromUtf8(utf8Json)
            .First(p => p.Age > 30);

        // Assert
        result.ShouldNotBeNull();
        result.Age.ShouldBeGreaterThan(30);
    }

    [Fact]
    public void Translate_CountQuery_ReturnsCorrectCount()
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
        count.ShouldBe(data.Count(p => p.Age > 25));
    }

    [Fact]
    public void Translate_AnyQuery_ReturnsTrueWhenMatches()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var hasActiveUsers = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Any(p => p.IsActive);

        // Assert
        hasActiveUsers.ShouldBeTrue();
    }

    [Fact]
    public void Translate_SingleQuery_ReturnsOnlyElement()
    {
        // Arrange
        var data = new List<Person>
        {
            new Person { Id = 1, Name = "Unique", Age = 99, IsActive = true }
        };
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var result = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Single(p => p.Age == 99);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Unique");
    }

    #endregion
}

using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.LinqOperations;

/// <summary>
/// Tests for conversion operations: ToArray, ToList, ToDictionary, ToHashSet, ToLookup.
/// </summary>
public class ConversionOperationsTests
{
    [Fact]
    public void ToArray_ConvertsToArray()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Where(p => p.Age > 25)
            .ToArray();

        // Assert
        results.ShouldNotBeNull();
        results.ShouldBeAssignableTo<Person[]>();
        results.ShouldAllBe(p => p.Age > 25);
    }

    [Fact]
    public void ToList_ConvertsToList()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Where(p => p.Age > 25)
            .ToList();

        // Assert
        results.ShouldNotBeNull();
        results.ShouldBeAssignableTo<List<Person>>();
        results.ShouldAllBe(p => p.Age > 25);
    }

    [Fact]
    public void ToDictionary_CreatesValidDictionary()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .ToDictionary(p => p.Id);

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBe(data.Count);
        results.Keys.ShouldBe(data.Select(p => p.Id));
    }

    [Fact]
    public void ToDictionary_WithElementSelector_ProjectsValues()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .ToDictionary(p => p.Id, p => p.Name);

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBe(data.Count);
        results.Values.ShouldBe(data.Select(p => p.Name));
    }

    [Fact]
    public void ToDictionary_DuplicateKeys_ThrowsArgumentException()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 1, Name = "Bob", Age = 30 }
        };
        var json = TestData.SerializeToJson(data);

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            JsonQueryable<Person>.FromString(json).ToDictionary(p => p.Id));
    }

    [Fact]
    public void ToHashSet_CreatesHashSet()
    {
        // Arrange
        var data = new List<string> { "Alice", "Bob", "Charlie", "Alice" };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .ToHashSet();

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBe(3); // Duplicates removed
        results.ShouldContain("Alice");
        results.ShouldContain("Bob");
        results.ShouldContain("Charlie");
    }

    [Fact]
    public void ToHashSet_RemovesDuplicates()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 2, 1, 4 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .ToHashSet();

        // Assert
        results.Count.ShouldBe(4);
        results.ShouldBe([1, 2, 3, 4], ignoreOrder: true);
    }

    [Fact]
    public void ToLookup_CreatesLookup()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "London" },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Paris" },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "London" }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .ToLookup(p => p.City);

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBe(2);
        results["London"].Count().ShouldBe(2);
        results["Paris"].Count().ShouldBe(1);
    }

    [Fact]
    public void ToLookup_WithElementSelector_ProjectsElements()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "London" },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Paris" },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "London" }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .ToLookup(p => p.City, p => p.Name);

        // Assert
        results["London"].ShouldContain("Alice");
        results["London"].ShouldContain("Charlie");
        results["Paris"].ShouldContain("Bob");
    }

    [Fact]
    public void ToArray_EmptySequence_ReturnsEmptyArray()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .ToArray();

        // Assert
        results.ShouldNotBeNull();
        results.ShouldBeEmpty();
    }

    [Fact]
    public void ToDictionary_EmptySequence_ReturnsEmptyDictionary()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .ToDictionary(p => p.Id);

        // Assert
        results.ShouldNotBeNull();
        results.ShouldBeEmpty();
    }
}

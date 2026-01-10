using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.LinqOperations;

/// <summary>
/// Tests for set operations: Distinct, DistinctBy, Union, UnionBy, Intersect, IntersectBy, Except, ExceptBy.
/// </summary>
public class SetOperationsTests
{
    [Fact]
    public void Distinct_RemovesDuplicates()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 },
            new() { Id = 3, Name = "Alice", Age = 25 }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Select(p => p.Name)
            .Distinct()
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldContain("Alice");
        results.ShouldContain("Bob");
    }

    [Fact]
    public void DistinctBy_RemovesDuplicatesByKey()
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
            .DistinctBy(p => p.City)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.Select(p => p.City).Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public void Union_CombinesSequencesWithoutDuplicates()
    {
        // Arrange
        var data1 = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 }
        };
        var data2 = new List<Person>
        {
            new() { Id = 2, Name = "Bob", Age = 30 },
            new() { Id = 3, Name = "Charlie", Age = 35 }
        };
        var json = TestData.SerializeToJson(data1);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Select(p => p.Name)
            .Union(data2.Select(p => p.Name))
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldContain("Alice");
        results.ShouldContain("Bob");
        results.ShouldContain("Charlie");
    }

    [Fact]
    public void UnionBy_CombinesByKey()
    {
        // Arrange
        var data1 = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 }
        };
        var data2 = new List<Person>
        {
            new() { Id = 2, Name = "Robert", Age = 31 },
            new() { Id = 3, Name = "Charlie", Age = 35 }
        };
        var json = TestData.SerializeToJson(data1);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .UnionBy(data2, p => p.Id)
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.Select(p => p.Id).ShouldBe([1, 2, 3]);
        results.First(p => p.Id == 2).Name.ShouldBe("Bob"); // First occurrence wins
    }

    [Fact]
    public void Intersect_FindsCommonElements()
    {
        // Arrange
        var data1 = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 },
            new() { Id = 3, Name = "Charlie", Age = 35 }
        };
        var data2 = new[] { "Bob", "Charlie", "David" };
        var json = TestData.SerializeToJson(data1);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Select(p => p.Name)
            .Intersect(data2)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldContain("Bob");
        results.ShouldContain("Charlie");
        results.ShouldNotContain("Alice");
    }

    [Fact]
    public void IntersectBy_FindsCommonByKey()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 },
            new() { Id = 3, Name = "Charlie", Age = 35 }
        };
        var keys = new[] { 2, 3, 4 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .IntersectBy(keys, p => p.Id)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.Select(p => p.Id).ShouldBe([2, 3]);
    }

    [Fact]
    public void Except_RemovesCommonElements()
    {
        // Arrange
        var data1 = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 },
            new() { Id = 3, Name = "Charlie", Age = 35 }
        };
        var data2 = new[] { "Bob", "David" };
        var json = TestData.SerializeToJson(data1);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Select(p => p.Name)
            .Except(data2)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldContain("Alice");
        results.ShouldContain("Charlie");
        results.ShouldNotContain("Bob");
    }

    [Fact]
    public void ExceptBy_RemovesByKey()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 },
            new() { Id = 3, Name = "Charlie", Age = 35 }
        };
        var keys = new[] { 2, 4 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .ExceptBy(keys, p => p.Id)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.Select(p => p.Id).ShouldBe([1, 3]);
    }

    [Fact]
    public void Distinct_EmptySequence_ReturnsEmpty()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Distinct()
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void Union_FirstSequenceEmpty_ReturnsSecond()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();
        var data2 = new[] { "Alice", "Bob" };

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Select(p => p.Name)
            .Union(data2)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldBe(data2);
    }
}

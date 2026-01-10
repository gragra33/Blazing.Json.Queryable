using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.LinqOperations;

/// <summary>
/// Tests for sorting operations: Reverse.
/// </summary>
public class SortingOperationsTests
{
    [Fact]
    public void Reverse_ReversesSequenceOrder()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 4, 5 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .Reverse()
            .ToList();

        // Assert
        results.Count.ShouldBe(5);
        results.ShouldBe([5, 4, 3, 2, 1]);
    }

    [Fact]
    public void Reverse_WithOrderBy_ReversesAfterSorting()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 3, Name = "Charlie", Age = 35 },
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .OrderBy(p => p.Age)
            .Reverse()
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].Age.ShouldBe(35);
        results[1].Age.ShouldBe(30);
        results[2].Age.ShouldBe(25);
    }

    [Fact]
    public void Reverse_EmptySequence_ReturnsEmpty()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .Reverse()
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void Reverse_SingleElement_ReturnsSameElement()
    {
        // Arrange
        var data = new List<string> { "Alice" };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .Reverse()
            .ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldBe("Alice");
    }

    [Fact]
    public void Reverse_WithFilter_FiltersFirstThenReverses()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 4, 5, 6 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .Where(n => n % 2 == 0)
            .Reverse()
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldBe([6, 4, 2]);
    }
}

using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.LinqOperations;

/// <summary>
/// Tests for sequence operations: Append, Prepend, Concat, Zip.
/// </summary>
public class SequenceOperationsTests
{
    [Fact]
    public void Append_AddsElementToEnd()
    {
        // Arrange
        var data = new List<string> { "Alice", "Bob" };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .Append("Charlie")
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldBe(["Alice", "Bob", "Charlie"]);
    }

    [Fact]
    public void Prepend_AddsElementToStart()
    {
        // Arrange
        var data = new List<string> { "Bob", "Charlie" };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .Prepend("Alice")
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldBe(["Alice", "Bob", "Charlie"]);
    }

    [Fact]
    public void Append_ToEmptySequence_ReturnsSingleElement()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .Append("Alice")
            .ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldBe("Alice");
    }

    [Fact]
    public void Concat_CombinesSequences()
    {
        // Arrange
        var data1 = new List<string> { "Alice", "Bob" };
        var data2 = new List<string> { "Charlie", "David" };
        var json = TestData.SerializeToJson(data1);

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .Concat(data2)
            .ToList();

        // Assert
        results.Count.ShouldBe(4);
        results.ShouldBe(["Alice", "Bob", "Charlie", "David"]);
    }

    [Fact]
    public void Concat_WithEmptySequence_ReturnsOriginal()
    {
        // Arrange
        var data1 = new List<string> { "Alice", "Bob" };
        var data2 = new List<string>();
        var json = TestData.SerializeToJson(data1);

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .Concat(data2)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldBe(["Alice", "Bob"]);
    }

    [Fact]
    public void Zip_PairsElements()
    {
        // Arrange
        var data1 = new List<string> { "Alice", "Bob", "Charlie" };
        var data2 = new List<int> { 25, 30, 35 };
        var json = TestData.SerializeToJson(data1);

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .Zip(data2, (name, age) => new { Name = name, Age = age })
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].Name.ShouldBe("Alice");
        results[0].Age.ShouldBe(25);
        results[2].Name.ShouldBe("Charlie");
        results[2].Age.ShouldBe(35);
    }

    [Fact]
    public void Zip_TupleOverload_CreatesTuples()
    {
        // Arrange
        var data1 = new List<string> { "Alice", "Bob" };
        var data2 = new List<int> { 25, 30 };
        var json = TestData.SerializeToJson(data1);

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .Zip(data2)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results[0].First.ShouldBe("Alice");
        results[0].Second.ShouldBe(25);
        results[1].First.ShouldBe("Bob");
        results[1].Second.ShouldBe(30);
    }

    [Fact]
    public void Zip_DifferentLengths_StopsAtShorter()
    {
        // Arrange
        var data1 = new List<string> { "Alice", "Bob", "Charlie" };
        var data2 = new List<int> { 25, 30 };
        var json = TestData.SerializeToJson(data1);

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .Zip(data2)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
    }

    [Fact]
    public void Zip_ThreeSequences_CreatesTriplets()
    {
        // Arrange
        var data1 = new List<string> { "Alice", "Bob" };
        var data2 = new List<int> { 25, 30 };
        var data3 = new List<string> { "London", "Paris" };
        var json = TestData.SerializeToJson(data1);

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .Zip(data2, data3)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results[0].First.ShouldBe("Alice");
        results[0].Second.ShouldBe(25);
        results[0].Third.ShouldBe("London");
    }

    [Fact]
    public void Zip_EmptySequence_ReturnsEmpty()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();
        var data2 = new List<int> { 25, 30 };

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .Zip(data2)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }
}

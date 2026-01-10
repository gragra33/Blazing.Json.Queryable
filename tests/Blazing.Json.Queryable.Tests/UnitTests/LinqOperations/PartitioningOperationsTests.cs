using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.LinqOperations;

/// <summary>
/// Tests for partitioning operations: TakeLast, SkipLast, TakeWhile, SkipWhile, Chunk.
/// </summary>
public class PartitioningOperationsTests
{
    [Fact]
    public void TakeLast_ReturnsLastElements()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 4, 5 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .TakeLast(2)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldBe([4, 5]);
    }

    [Fact]
    public void TakeLast_CountGreaterThanSequence_ReturnsAll()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .TakeLast(10)
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void SkipLast_SkipsLastElements()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 4, 5 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .SkipLast(2)
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void SkipLast_CountGreaterThanSequence_ReturnsEmpty()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .SkipLast(10)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void TakeWhile_TakesUntilConditionFails()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 4, 5 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .TakeWhile(n => n < 4)
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void TakeWhile_WithIndex_UsesIndexInPredicate()
    {
        // Arrange
        var data = new List<string> { "Alice", "Bob", "Charlie", "David" };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .TakeWhile((name, index) => index < 2)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldBe(["Alice", "Bob"]);
    }

    [Fact]
    public void TakeWhile_ConditionNeverFails_TakesAll()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .TakeWhile(n => n < 10)
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
    }

    [Fact]
    public void SkipWhile_SkipsUntilConditionFails()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 4, 5 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .SkipWhile(n => n < 3)
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldBe([3, 4, 5]);
    }

    [Fact]
    public void SkipWhile_WithIndex_UsesIndexInPredicate()
    {
        // Arrange
        var data = new List<string> { "Alice", "Bob", "Charlie", "David" };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .SkipWhile((name, index) => index < 2)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldBe(["Charlie", "David"]);
    }

    [Fact]
    public void SkipWhile_ConditionAlwaysTrue_ReturnsEmpty()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .SkipWhile(n => n < 10)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void Chunk_SplitsIntoChunks()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .Chunk(3)
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].ShouldBe([1, 2, 3]);
        results[1].ShouldBe([4, 5, 6]);
        results[2].ShouldBe([7]);
    }

    [Fact]
    public void Chunk_ExactDivision_AllChunksSameSize()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 4, 5, 6 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .Chunk(2)
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldAllBe(chunk => chunk.Length == 2);
    }

    [Fact]
    public void Chunk_EmptySequence_ReturnsEmpty()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .Chunk(3)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }
}

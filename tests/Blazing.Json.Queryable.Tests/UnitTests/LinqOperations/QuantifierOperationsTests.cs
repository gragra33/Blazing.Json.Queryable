using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.LinqOperations;

/// <summary>
/// Tests for quantifier operations: All, Contains, SequenceEqual.
/// </summary>
public class QuantifierOperationsTests
{
    [Fact]
    public void All_AllMatch_ReturnsTrue()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 },
            new() { Id = 3, Name = "Charlie", Age = 35 }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .All(p => p.Age >= 18);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void All_OneMismatch_ReturnsFalse()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 15 },
            new() { Id = 3, Name = "Charlie", Age = 35 }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .All(p => p.Age >= 18);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void All_EmptySequence_ReturnsTrue()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .All(p => p.Age >= 18);

        // Assert
        result.ShouldBeTrue(); // Empty sequence satisfies all conditions
    }

    [Fact]
    public void Contains_ItemExists_ReturnsTrue()
    {
        // Arrange
        var data = new List<string> { "Alice", "Bob", "Charlie" };
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .Contains("Bob");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Contains_ItemMissing_ReturnsFalse()
    {
        // Arrange
        var data = new List<string> { "Alice", "Bob", "Charlie" };
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .Contains("David");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Contains_EmptySequence_ReturnsFalse()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .Contains("Alice");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void SequenceEqual_IdenticalSequences_ReturnsTrue()
    {
        // Arrange
        var data1 = new List<string> { "Alice", "Bob", "Charlie" };
        var data2 = new List<string> { "Alice", "Bob", "Charlie" };
        var json = TestData.SerializeToJson(data1);

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .SequenceEqual(data2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void SequenceEqual_DifferentSequences_ReturnsFalse()
    {
        // Arrange
        var data1 = new List<string> { "Alice", "Bob", "Charlie" };
        var data2 = new List<string> { "Alice", "Bob", "David" };
        var json = TestData.SerializeToJson(data1);

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .SequenceEqual(data2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void SequenceEqual_DifferentLengths_ReturnsFalse()
    {
        // Arrange
        var data1 = new List<string> { "Alice", "Bob", "Charlie" };
        var data2 = new List<string> { "Alice", "Bob" };
        var json = TestData.SerializeToJson(data1);

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .SequenceEqual(data2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void SequenceEqual_BothEmpty_ReturnsTrue()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();
        var data2 = new List<string>();

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .SequenceEqual(data2);

        // Assert
        result.ShouldBeTrue();
    }
}

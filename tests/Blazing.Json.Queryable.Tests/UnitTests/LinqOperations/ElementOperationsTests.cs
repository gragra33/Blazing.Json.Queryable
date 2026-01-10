using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.LinqOperations;

/// <summary>
/// Tests for element access operations: ElementAt, ElementAtOrDefault, Last, LastOrDefault.
/// </summary>
public class ElementOperationsTests
{
    [Fact]
    public void ElementAt_ValidIndex_ReturnsElement()
    {
        // Arrange
        var data = new List<string> { "Alice", "Bob", "Charlie" };
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .ElementAt(1);

        // Assert
        result.ShouldBe("Bob");
    }

    [Fact]
    public void ElementAt_InvalidIndex_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var data = new List<string> { "Alice", "Bob" };
        var json = TestData.SerializeToJson(data);

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            JsonQueryable<string>.FromString(json).ElementAt(5));
    }

    [Fact]
    public void ElementAt_FromEndIndex_ReturnsCorrectElement()
    {
        // Arrange
        var data = new List<string> { "Alice", "Bob", "Charlie" };
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .ElementAt(^1); // Last element

        // Assert
        result.ShouldBe("Charlie");
    }

    [Fact]
    public void ElementAtOrDefault_ValidIndex_ReturnsElement()
    {
        // Arrange
        var data = new List<string> { "Alice", "Bob", "Charlie" };
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .ElementAtOrDefault(1);

        // Assert
        result.ShouldBe("Bob");
    }

    [Fact]
    public void ElementAtOrDefault_InvalidIndex_ReturnsNull()
    {
        // Arrange
        var data = new List<string> { "Alice", "Bob" };
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .ElementAtOrDefault(5);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ElementAtOrDefault_FromEndIndex_ReturnsCorrectElement()
    {
        // Arrange
        var data = new List<string> { "Alice", "Bob", "Charlie" };
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .ElementAtOrDefault(^2); // Second to last

        // Assert
        result.ShouldBe("Bob");
    }

    [Fact]
    public void Last_ReturnsLastElement()
    {
        // Arrange
        var data = new List<string> { "Alice", "Bob", "Charlie" };
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .Last();

        // Assert
        result.ShouldBe("Charlie");
    }

    [Fact]
    public void Last_WithPredicate_ReturnsLastMatch()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 },
            new() { Id = 3, Name = "Charlie", Age = 25 }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .Last(p => p.Age == 25);

        // Assert
        result.Name.ShouldBe("Charlie");
    }

    [Fact]
    public void Last_EmptySequence_ThrowsInvalidOperation()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            JsonQueryable<string>.FromString(json).Last());
    }

    [Fact]
    public void Last_NoMatch_ThrowsInvalidOperation()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25 }
        };
        var json = TestData.SerializeToJson(data);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            JsonQueryable<Person>.FromString(json).Last(p => p.Age > 50));
    }

    [Fact]
    public void LastOrDefault_ReturnsLastElement()
    {
        // Arrange
        var data = new List<string> { "Alice", "Bob", "Charlie" };
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .LastOrDefault();

        // Assert
        result.ShouldBe("Charlie");
    }

    [Fact]
    public void LastOrDefault_WithPredicate_ReturnsLastMatch()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 },
            new() { Id = 3, Name = "Charlie", Age = 25 }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .LastOrDefault(p => p.Age == 25);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Charlie");
    }

    [Fact]
    public void LastOrDefault_EmptySequence_ReturnsNull()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var result = JsonQueryable<string>.FromString(json)
            .LastOrDefault();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void LastOrDefault_NoMatch_ReturnsNull()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25 }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .LastOrDefault(p => p.Age > 50);

        // Assert
        result.ShouldBeNull();
    }
}

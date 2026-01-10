using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.LinqOperations;

/// <summary>
/// Tests for aggregation LINQ operations: Sum, Average, Min, Max, MinBy, MaxBy, Aggregate.
/// </summary>
public class AggregationOperationsTests
{
    [Fact]
    public void Sum_IntProperty_ReturnsCorrectSum()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .Sum(p => p.Age);

        // Assert
        var expected = data.Sum(p => p.Age);
        result.ShouldBe(expected);
    }

    [Fact]
    public void Sum_DecimalProperty_ReturnsCorrectSum()
    {
        // Arrange
        var data = TestData.GetSmallProductDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Product>.FromString(json)
            .Sum(p => p.Price);

        // Assert
        var expected = data.Sum(p => p.Price);
        result.ShouldBe(expected);
    }

    [Fact]
    public void Sum_WithFilter_SumsFilteredElements()
    {
        // Arrange
        var data = TestData.GetSmallProductDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Product>.FromString(json)
            .Where(p => p.Stock > 10)
            .Sum(p => p.Price);

        // Assert
        var expected = data.Where(p => p.Stock > 10).Sum(p => p.Price);
        result.ShouldBe(expected);
    }

    [Fact]
    public void Average_IntProperty_ReturnsCorrectAverage()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .Average(p => p.Age);

        // Assert
        var expected = data.Average(p => p.Age);
        result.ShouldBe(expected);
    }

    [Fact]
    public void Average_DecimalProperty_ReturnsCorrectAverage()
    {
        // Arrange
        var data = TestData.GetSmallProductDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Product>.FromString(json)
            .Average(p => p.Price);

        // Assert
        var expected = data.Average(p => p.Price);
        result.ShouldBe(expected);
    }

    [Fact]
    public void Min_IntProperty_ReturnsMinimum()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .Min(p => p.Age);

        // Assert
        var expected = data.Min(p => p.Age);
        result.ShouldBe(expected);
    }

    [Fact]
    public void Max_IntProperty_ReturnsMaximum()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .Max(p => p.Age);

        // Assert
        var expected = data.Max(p => p.Age);
        result.ShouldBe(expected);
    }

    [Fact]
    public void MinBy_ReturnsElementWithMinKey()
    {
        // Arrange
        var data = TestData.GetSmallProductDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Product>.FromString(json)
            .MinBy(p => p.Price);

        // Assert
        var expected = data.MinBy(p => p.Price);
        result.ShouldNotBeNull();
        result.Price.ShouldBe(expected!.Price);
    }

    [Fact]
    public void MaxBy_ReturnsElementWithMaxKey()
    {
        // Arrange
        var data = TestData.GetSmallProductDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Product>.FromString(json)
            .MaxBy(p => p.Price);

        // Assert
        var expected = data.MaxBy(p => p.Price);
        result.ShouldNotBeNull();
        result.Price.ShouldBe(expected!.Price);
    }

    [Fact]
    public void Aggregate_WithSeed_AccumulatesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .Aggregate(0, (acc, p) => acc + p.Age);

        // Assert
        var expected = data.Aggregate(0, (acc, p) => acc + p.Age);
        result.ShouldBe(expected);
    }

    [Fact]
    public void Aggregate_WithSeedAndResultSelector_TransformsCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .Aggregate(
                0,
                (acc, p) => acc + p.Age,
                total => total / 10.0);

        // Assert
        var expected = data.Aggregate(
            0,
            (acc, p) => acc + p.Age,
            total => total / 10.0);
        result.ShouldBe(expected);
    }

    [Fact]
    public void Sum_EmptySequence_ReturnsZero()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var result = JsonQueryable<Person>.FromString(json)
            .Sum(p => p.Age);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public void Average_EmptySequence_ThrowsInvalidOperation()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            JsonQueryable<Person>.FromString(json).Average(p => p.Age));
    }

    [Fact]
    public void MinBy_EmptySequence_ReturnsNull()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var result = JsonQueryable<Product>.FromString(json)
            .MinBy(p => p.Price);

        // Assert
        result.ShouldBeNull();
    }
}

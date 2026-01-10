using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.LinqOperations;

/// <summary>
/// Tests for utility operations: DefaultIfEmpty.
/// </summary>
public class UtilityOperationsTests
{
    [Fact]
    public void DefaultIfEmpty_EmptySequence_ReturnsDefault()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .DefaultIfEmpty()
            .ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldBeNull();
    }

    [Fact]
    public void DefaultIfEmpty_WithValue_EmptySequence_ReturnsSpecifiedDefault()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();
        var defaultPerson = new Person { Id = 0, Name = "Default", Age = 0 };

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .DefaultIfEmpty(defaultPerson)
            .ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldBe(defaultPerson);
        results[0].Name.ShouldBe("Default");
    }

    [Fact]
    public void DefaultIfEmpty_NonEmptySequence_ReturnsOriginalSequence()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .DefaultIfEmpty()
            .ToList();

        // Assert
        results.Count.ShouldBe(data.Count);
        results.ShouldAllBe(p => p != null);
    }

    [Fact]
    public void DefaultIfEmpty_WithValue_NonEmptySequence_ReturnsOriginalSequence()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        var defaultPerson = new Person { Id = 0, Name = "Default", Age = 0 };

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .DefaultIfEmpty(defaultPerson)
            .ToList();

        // Assert
        results.Count.ShouldBe(data.Count);
        results.ShouldNotContain(defaultPerson);
    }

    [Fact]
    public void DefaultIfEmpty_AfterFilter_EmptyResult_ReturnsDefault()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Where(p => p.Age > 1000) // No matches
            .DefaultIfEmpty()
            .ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldBeNull();
    }

    [Fact]
    public void DefaultIfEmpty_ValueTypes_ReturnsZero()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .DefaultIfEmpty()
            .ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldBe(0);
    }

    [Fact]
    public void DefaultIfEmpty_WithSpecificValue_ValueTypes_ReturnsSpecifiedValue()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .DefaultIfEmpty(42)
            .ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldBe(42);
    }
}

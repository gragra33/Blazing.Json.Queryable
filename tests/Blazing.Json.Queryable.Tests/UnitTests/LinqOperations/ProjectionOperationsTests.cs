using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.LinqOperations;

/// <summary>
/// Tests for projection operations: SelectMany, Cast, OfType.
/// </summary>
public class ProjectionOperationsTests
{
    [Fact]
    public void SelectMany_FlattensNestedCollections()
    {
        // Arrange
        var data = new List<Order>
        {
            new() { Id = 1, Items = [
                new() { ProductId = 1, ProductName = "Widget", Quantity = 2 },
                new() { ProductId = 2, ProductName = "Gadget", Quantity = 1 }
            ]},
            new() { Id = 2, Items = [
                new() { ProductId = 3, ProductName = "Tool", Quantity = 3 }
            ]}
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Order>.FromString(json)
            .SelectMany(o => o.Items)
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.Select(i => i.ProductName).ShouldBe(["Widget", "Gadget", "Tool"]);
    }

    [Fact]
    public void SelectMany_WithResultSelector_ProjectsCorrectly()
    {
        // Arrange
        var data = new List<Order>
        {
            new() { Id = 1, CustomerName = "Alice", Items = [
                new() { ProductId = 1, ProductName = "Widget", Quantity = 2, UnitPrice = 10.0m }
            ]},
            new() { Id = 2, CustomerName = "Bob", Items = [
                new() { ProductId = 2, ProductName = "Gadget", Quantity = 1, UnitPrice = 20.0m }
            ]}
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Order>.FromString(json)
            .SelectMany(
                o => o.Items,
                (order, item) => new { order.CustomerName, item.ProductName, item.Total })
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results[0].CustomerName.ShouldBe("Alice");
        results[0].ProductName.ShouldBe("Widget");
        results[0].Total.ShouldBe(20.0m);
    }

    [Fact]
    public void SelectMany_EmptyNestedCollections_ReturnsEmpty()
    {
        // Arrange
        var data = new List<Order>
        {
            new() { Id = 1, Items = [] },
            new() { Id = 2, Items = [] }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Order>.FromString(json)
            .SelectMany(o => o.Items)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void SelectMany_WithFilter_FlattensAndFilters()
    {
        // Arrange
        var data = new List<Order>
        {
            new() { Id = 1, Items = [
                new() { ProductId = 1, ProductName = "Widget", Quantity = 2 },
                new() { ProductId = 2, ProductName = "Gadget", Quantity = 5 }
            ]},
            new() { Id = 2, Items = [
                new() { ProductId = 3, ProductName = "Tool", Quantity = 1 }
            ]}
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Order>.FromString(json)
            .SelectMany(o => o.Items)
            .Where(i => i.Quantity > 1)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldAllBe(i => i.Quantity > 1);
    }

    [Fact]
    public void OfType_FiltersByType()
    {
        // Arrange - Using different cities as type discriminator
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "Person" },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Person" },
            new() { Id = 3, Name = "Corp", Age = 0, City = "Company" }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Where(p => p.City == "Person")
            .OfType<Person>()
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldAllBe(p => p.City == "Person");
    }

    [Fact]
    public void Cast_ConvertsElements()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Select(p => (object)p)
            .Cast<object>()
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(data.Count);
    }
}

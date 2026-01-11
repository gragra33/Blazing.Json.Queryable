using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.IntegrationTests;

/// <summary>
/// Integration tests for combining JSONPath filtering with LINQ GroupBy, GroupJoin, and Join operations.
/// These tests ensure that RFC 9535 JSONPath filters work correctly with complex LINQ operations.
/// </summary>
public class JsonPathWithGroupingAndJoinsTests
{
    #region JSONPath + GroupBy Tests

    [Fact]
    public void JsonPath_GroupBy_SimpleGrouping()
    {
        // Arrange
        var ordersJson = """
        [
            {"orderId": 1, "customer": "Alice", "amount": 150.00, "status": "shipped", "items": 3},
            {"orderId": 2, "customer": "Bob", "amount": 89.99, "status": "pending", "items": 2},
            {"orderId": 3, "customer": "Charlie", "amount": 250.00, "status": "shipped", "items": 5},
            {"orderId": 4, "customer": "Diana", "amount": 120.00, "status": "shipped", "items": 2},
            {"orderId": 5, "customer": "Alice", "amount": 75.50, "status": "pending", "items": 1},
            {"orderId": 6, "customer": "Frank", "amount": 200.00, "status": "shipped", "items": 4}
        ]
        """;

        // Act - JSONPath filter for shipped orders, then GroupBy customer
        var result = JsonQueryable<Order>
            .FromString(ordersJson, "$[?@.status == 'shipped']")
            .GroupBy(o => o.Customer)
            .Select(g => new
            {
                Customer = g.Key,
                Count = g.Count()
            })
            .ToList();

        // Assert
        result.Count.ShouldBe(4); // Alice, Charlie, Diana, Frank
        result.First(r => r.Customer == "Alice").Count.ShouldBe(1);
        result.First(r => r.Customer == "Charlie").Count.ShouldBe(1);
    }

    [Fact]
    public void JsonPath_GroupBy_WithSelectAggregation()
    {
        // Arrange
        var ordersJson = """
        [
            {"orderId": 1, "customer": "Alice", "amount": 150.00, "status": "shipped"},
            {"orderId": 2, "customer": "Bob", "amount": 89.99, "status": "pending"},
            {"orderId": 3, "customer": "Alice", "amount": 100.00, "status": "shipped"},
            {"orderId": 4, "customer": "Charlie", "amount": 250.00, "status": "shipped"}
        ]
        """;

        // Act - JSONPath filter then GroupBy with aggregation
        var result = JsonQueryable<Order>
            .FromString(ordersJson, "$[?@.status == 'shipped']")
            .GroupBy(o => o.Customer)
            .Select(g => new
            {
                Customer = g.Key,
                OrderCount = g.Count(),
                TotalAmount = g.Sum(o => o.Amount)
            })
            .ToList();

        // Assert
        result.Count.ShouldBe(2); // Alice, Charlie
        var aliceGroup = result.First(g => g.Customer == "Alice");
        aliceGroup.OrderCount.ShouldBe(2);
        aliceGroup.TotalAmount.ShouldBe(250.00m);
    }

    [Fact]
    public void JsonPath_GroupBy_WithOrderByDescending()
    {
        // Arrange
        var ordersJson = """
        [
            {"orderId": 1, "customer": "Alice", "amount": 150.00, "status": "shipped"},
            {"orderId": 2, "customer": "Charlie", "amount": 250.00, "status": "shipped"},
            {"orderId": 3, "customer": "Diana", "amount": 120.00, "status": "shipped"},
            {"orderId": 4, "customer": "Frank", "amount": 200.00, "status": "shipped"}
        ]
        """;

        // Act - The exact scenario from the bug report
        var result = JsonQueryable<Order>
            .FromString(ordersJson, "$[?@.status == 'shipped']")
            .GroupBy(o => o.Customer)
            .Select(g => new
            {
                Customer = g.Key,
                OrderCount = g.Count(),
                TotalAmount = g.Sum(o => o.Amount)
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        // Assert
        result.Count.ShouldBe(4);
        result[0].Customer.ShouldBe("Charlie"); // Highest total: $250
        result[1].Customer.ShouldBe("Frank");    // Second: $200
        result[2].Customer.ShouldBe("Alice");    // Third: $150
        result[3].Customer.ShouldBe("Diana");    // Fourth: $120
    }

    [Fact]
    public void JsonPath_GroupBy_WithComplexFilter()
    {
        // Arrange
        var ordersJson = """
        [
            {"orderId": 1, "customer": "Alice", "amount": 150.00, "status": "shipped", "priority": "high"},
            {"orderId": 2, "customer": "Bob", "amount": 89.99, "status": "shipped", "priority": "low"},
            {"orderId": 3, "customer": "Alice", "amount": 200.00, "status": "shipped", "priority": "high"},
            {"orderId": 4, "customer": "Charlie", "amount": 250.00, "status": "pending", "priority": "high"}
        ]
        """;

        // Act - Complex JSONPath filter with multiple conditions
        var result = JsonQueryable<OrderWithPriority>
            .FromString(ordersJson, "$[?@.status == 'shipped' && @.priority == 'high']")
            .GroupBy(o => o.Customer)
            .Select(g => new
            {
                Customer = g.Key,
                Count = g.Count(),
                AvgAmount = g.Average(o => o.Amount)
            })
            .ToList();

        // Assert
        result.Count.ShouldBe(1); // Only Alice has shipped high-priority orders
        result[0].Customer.ShouldBe("Alice");
        result[0].Count.ShouldBe(2);
        result[0].AvgAmount.ShouldBe(175.00m); // (150 + 200) / 2
    }

    [Fact]
    public void JsonPath_GroupBy_WithElementSelector()
    {
        // Arrange
        var ordersJson = """
        [
            {"orderId": 1, "customer": "Alice", "amount": 150.00, "status": "shipped"},
            {"orderId": 2, "customer": "Alice", "amount": 100.00, "status": "shipped"},
            {"orderId": 3, "customer": "Bob", "amount": 250.00, "status": "shipped"}
        ]
        """;

        // Act - GroupBy with element selector
        var result = JsonQueryable<Order>.FromString(ordersJson, "$[?@.status == 'shipped']")
            .GroupBy(o => o.Customer, o => o.Amount)
            .Select(g => new
            {
                Customer = g.Key,
                TotalAmount = g.Sum()
            })
            .ToList();

        // Assert
        result.Count.ShouldBe(2);
        result.First(r => r.Customer == "Alice").TotalAmount.ShouldBe(250.00m);
        result.First(r => r.Customer == "Bob").TotalAmount.ShouldBe(250.00m);
    }

    #endregion

    #region JSONPath + Join Tests

    [Fact]
    public void JsonPath_Join_SimpleInnerJoin()
    {
        // Arrange
        var ordersJson = """
        [
            {"orderId": 1, "customerId": 1, "amount": 150.00, "status": "shipped"},
            {"orderId": 2, "customerId": 2, "amount": 89.99, "status": "pending"},
            {"orderId": 3, "customerId": 1, "amount": 200.00, "status": "shipped"}
        ]
        """;

        var customers = new List<Customer>
        {
            new() { CustomerId = 1, Name = "Alice", City = "Seattle" },
            new() { CustomerId = 2, Name = "Bob", City = "Portland" }
        };

        // Act - JSONPath filter shipped orders, then join with customers
        var result = JsonQueryable<OrderWithCustomerId>
            .FromString(ordersJson, "$[?@.status == 'shipped']")
            .Join(
                customers,
                o => o.CustomerId,
                c => c.CustomerId,
                (o, c) => new
                {
                    OrderId = o.OrderId,
                    CustomerName = c.Name,
                    Amount = o.Amount,
                    City = c.City
                })
            .ToList();

        // Assert
        result.Count.ShouldBe(2); // Two shipped orders (orderId 1 and 3, both for Alice)
        result.ShouldAllBe(r => r.CustomerName == "Alice");
        result.Sum(r => r.Amount).ShouldBe(350.00m);
    }

    [Fact]
    public void JsonPath_Join_WithOrderBy()
    {
        // Arrange
        var ordersJson = """
        [
            {"orderId": 1, "customerId": 1, "amount": 150.00, "status": "shipped"},
            {"orderId": 2, "customerId": 2, "amount": 200.00, "status": "shipped"},
            {"orderId": 3, "customerId": 1, "amount": 100.00, "status": "shipped"}
        ]
        """;

        var customers = new List<Customer>
        {
            new() { CustomerId = 1, Name = "Alice", City = "Seattle" },
            new() { CustomerId = 2, Name = "Bob", City = "Portland" }
        };

        // Act - Join then order by amount descending
        var result = JsonQueryable<OrderWithCustomerId>
            .FromString(ordersJson, "$[?@.status == 'shipped']")
            .Join(
                customers,
                o => o.CustomerId,
                c => c.CustomerId,
                (o, c) => new
                {
                    OrderId = o.OrderId,
                    CustomerName = c.Name,
                    Amount = o.Amount
                })
            .OrderByDescending(x => x.Amount)
            .ToList();

        // Assert
        result.Count.ShouldBe(3);
        result[0].Amount.ShouldBe(200.00m); // Bob's order
        result[1].Amount.ShouldBe(150.00m); // Alice's first order
        result[2].Amount.ShouldBe(100.00m); // Alice's second order
    }

    #endregion

    #region JSONPath + GroupJoin Tests

    [Fact]
    public void JsonPath_GroupJoin_SimpleLeftOuterJoin()
    {
        // Arrange
        var customersJson = """
        [
            {"customerId": 1, "name": "Alice", "city": "Seattle", "status": "active"},
            {"customerId": 2, "name": "Bob", "city": "Portland", "status": "active"},
            {"customerId": 3, "name": "Charlie", "city": "Boston", "status": "inactive"}
        ]
        """;

        var orders = new List<OrderWithCustomerId>
        {
            new() { OrderId = 1, CustomerId = 1, Amount = 150.00m, Status = "shipped" },
            new() { OrderId = 2, CustomerId = 1, Amount = 200.00m, Status = "shipped" },
            new() { OrderId = 3, CustomerId = 2, Amount = 100.00m, Status = "shipped" }
        };

        // Act - JSONPath filter for active customers, then GroupJoin with orders
        var result = JsonQueryable<Customer>.FromString(customersJson, "$[?@.status == 'active']")
            .GroupJoin(
                orders,
                c => c.CustomerId,
                o => o.CustomerId,
                (c, orderGroup) => new
                {
                    CustomerName = c.Name,
                    OrderCount = orderGroup.Count(),
                    TotalAmount = orderGroup.Sum(o => o.Amount)
                })
            .ToList();

        // Assert
        result.Count.ShouldBe(2); // Only active customers (Alice and Bob)
        var aliceResult = result.First(r => r.CustomerName == "Alice");
        aliceResult.OrderCount.ShouldBe(2);
        aliceResult.TotalAmount.ShouldBe(350.00m);

        var bobResult = result.First(r => r.CustomerName == "Bob");
        bobResult.OrderCount.ShouldBe(1);
        bobResult.TotalAmount.ShouldBe(100.00m);
    }

    [Fact]
    public void JsonPath_GroupJoin_WithOrderByAndSelect()
    {
        // Arrange
        var customersJson = """
        [
            {"customerId": 1, "name": "Alice", "city": "Seattle", "tier": "gold"},
            {"customerId": 2, "name": "Bob", "city": "Portland", "tier": "silver"},
            {"customerId": 3, "name": "Charlie", "city": "Boston", "tier": "gold"}
        ]
        """;

        var orders = new List<OrderWithCustomerId>
        {
            new() { OrderId = 1, CustomerId = 1, Amount = 150.00m },
            new() { OrderId = 2, CustomerId = 1, Amount = 200.00m },
            new() { OrderId = 3, CustomerId = 3, Amount = 300.00m }
        };

        // Act - Filter gold tier customers, GroupJoin, then order by total
        var result = JsonQueryable<CustomerWithTier>.FromString(customersJson, "$[?@.tier == 'gold']")
            .GroupJoin(
                orders,
                c => c.CustomerId,
                o => o.CustomerId,
                (c, orderGroup) => new
                {
                    CustomerName = c.Name,
                    City = c.City,
                    OrderCount = orderGroup.Count(),
                    TotalAmount = orderGroup.Sum(o => o.Amount)
                })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        // Assert
        result.Count.ShouldBe(2); // Alice and Charlie (gold tier)
        result[0].CustomerName.ShouldBe("Alice"); // Total: $350
        result[0].TotalAmount.ShouldBe(350.00m);
        result[1].CustomerName.ShouldBe("Charlie"); // Total: $300
        result[1].TotalAmount.ShouldBe(300.00m);
    }

    [Fact]
    public void JsonPath_GroupJoin_WithNoMatchingOrders()
    {
        // Arrange
        var customersJson = """
        [
            {"customerId": 1, "name": "Alice", "status": "active"},
            {"customerId": 2, "name": "Bob", "status": "active"}
        ]
        """;

        var orders = new List<OrderWithCustomerId>(); // Empty orders list

        // Act - Active customers with no orders
        var result = JsonQueryable<Customer>.FromString(customersJson, "$[?@.status == 'active']")
            .GroupJoin(
                orders,
                c => c.CustomerId,
                o => o.CustomerId,
                (c, orderGroup) => new
                {
                    CustomerName = c.Name,
                    OrderCount = orderGroup.Count(),
                    HasOrders = orderGroup.Any()
                })
            .ToList();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(r => r.OrderCount == 0);
        result.ShouldAllBe(r => !r.HasOrders);
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void JsonPath_GroupBy_Then_Join_ComplexPipeline()
    {
        // Arrange
        var ordersJson = """
        [
            {"orderId": 1, "customerId": 1, "amount": 150.00, "status": "shipped", "region": "west"},
            {"orderId": 2, "customerId": 1, "amount": 200.00, "status": "shipped", "region": "west"},
            {"orderId": 3, "customerId": 2, "amount": 100.00, "status": "shipped", "region": "east"},
            {"orderId": 4, "customerId": 3, "amount": 250.00, "status": "pending", "region": "west"}
        ]
        """;

        var customers = new List<Customer>
        {
            new() { CustomerId = 1, Name = "Alice", City = "Seattle" },
            new() { CustomerId = 2, Name = "Bob", City = "Boston" }
        };

        // Act - JSONPath filter, GroupBy, then Join - complex multi-step pipeline
        var groupedOrders = JsonQueryable<OrderWithRegion>.FromString(
            ordersJson,
            "$[?@.status == 'shipped' && @.region == 'west']")
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                OrderCount = g.Count(),
                TotalAmount = g.Sum(o => o.Amount)
            })
            .ToList(); // Materialize grouped results

        var result = groupedOrders
            .Join(
                customers,
                g => g.CustomerId,
                c => c.CustomerId,
                (g, c) => new
                {
                    CustomerName = c.Name,
                    City = c.City,
                    OrderCount = g.OrderCount,
                    TotalAmount = g.TotalAmount
                })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        // Assert
        result.Count.ShouldBe(1); // Only Alice has shipped west region orders
        result[0].CustomerName.ShouldBe("Alice");
        result[0].OrderCount.ShouldBe(2);
        result[0].TotalAmount.ShouldBe(350.00m);
    }

    [Fact]
    public void JsonPath_GroupJoin_WithOrderByDescending_ExecutionStepsPath()
    {
        // Arrange
        var customersJson = """
        [
            {"customerId": 1, "name": "Alice", "city": "Seattle"},
            {"customerId": 2, "name": "Bob", "city": "Portland"},
            {"customerId": 3, "name": "Charlie", "city": "Boston"},
            {"customerId": 4, "name": "Diana", "city": "Miami"}
        ]
        """;

        var orders = new List<OrderWithCustomerId>
        {
            new() { OrderId = 1, CustomerId = 1, Amount = 150.00m },
            new() { OrderId = 2, CustomerId = 1, Amount = 200.00m },
            new() { OrderId = 3, CustomerId = 2, Amount = 100.00m },
            new() { OrderId = 4, CustomerId = 3, Amount = 300.00m },
            new() { OrderId = 5, CustomerId = 3, Amount = 250.00m }
        };

        // Act - GroupJoin followed by OrderByDescending (tests ExecutionSteps path)
        var result = JsonQueryable<Customer>.FromString(customersJson)
            .GroupJoin(
                orders,
                c => c.CustomerId,
                o => o.CustomerId,
                (c, orderGroup) => new
                {
                    CustomerName = c.Name,
                    City = c.City,
                    OrderCount = orderGroup.Count(),
                    TotalAmount = orderGroup.Sum(o => o.Amount)
                })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        // Assert - Verify correct ordering by TotalAmount descending
        result.Count.ShouldBe(4);
        result[0].CustomerName.ShouldBe("Charlie"); // Total: $550 (300 + 250)
        result[0].TotalAmount.ShouldBe(550.00m);
        result[1].CustomerName.ShouldBe("Alice");   // Total: $350 (150 + 200)
        result[1].TotalAmount.ShouldBe(350.00m);
        result[2].CustomerName.ShouldBe("Bob");     // Total: $100
        result[2].TotalAmount.ShouldBe(100.00m);
        result[3].CustomerName.ShouldBe("Diana");   // Total: $0 (no orders)
        result[3].TotalAmount.ShouldBe(0.00m);
    }

    [Fact]
    public void JsonPath_GroupJoin_WithOrderBy_ThenBy()
    {
        // Arrange
        var customersJson = """
        [
            {"customerId": 1, "name": "Alice", "city": "Seattle"},
            {"customerId": 2, "name": "Bob", "city": "Portland"},
            {"customerId": 3, "name": "Charlie", "city": "Boston"},
            {"customerId": 4, "name": "Diana", "city": "Seattle"}
        ]
        """;

        var orders = new List<OrderWithCustomerId>
        {
            new() { OrderId = 1, CustomerId = 1, Amount = 100.00m },
            new() { OrderId = 2, CustomerId = 2, Amount = 100.00m },
            new() { OrderId = 3, CustomerId = 3, Amount = 200.00m }
        };

        // Act - GroupJoin with OrderBy and ThenBy (tests multi-level sorting in ExecutionSteps)
        var result = JsonQueryable<Customer>.FromString(customersJson)
            .GroupJoin(
                orders,
                c => c.CustomerId,
                o => o.CustomerId,
                (c, orderGroup) => new
                {
                    CustomerName = c.Name,
                    City = c.City,
                    TotalAmount = orderGroup.Sum(o => o.Amount)
                })
            .OrderByDescending(x => x.TotalAmount)
            .ThenBy(x => x.CustomerName)
            .ToList();

        // Assert - Verify multi-level sorting
        result.Count.ShouldBe(4);
        result[0].CustomerName.ShouldBe("Charlie"); // Total: $200
        result[1].CustomerName.ShouldBe("Alice");   // Total: $100, name "Alice" < "Bob"
        result[2].CustomerName.ShouldBe("Bob");     // Total: $100, name "Bob" > "Alice"
        result[3].CustomerName.ShouldBe("Diana");   // Total: $0
    }

    [Fact]
    public void JsonPath_GroupJoin_WithJSONPathFilter_AndOrderBy()
    {
        // Arrange
        var customersJson = """
        [
            {"customerId": 1, "name": "Alice", "city": "Seattle", "tier": "premium"},
            {"customerId": 2, "name": "Bob", "city": "Portland", "tier": "standard"},
            {"customerId": 3, "name": "Charlie", "city": "Boston", "tier": "premium"},
            {"customerId": 4, "name": "Diana", "city": "Miami", "tier": "standard"}
        ]
        """;

        var orders = new List<OrderWithCustomerId>
        {
            new() { OrderId = 1, CustomerId = 1, Amount = 150.00m },
            new() { OrderId = 2, CustomerId = 1, Amount = 200.00m },
            new() { OrderId = 3, CustomerId = 3, Amount = 300.00m }
        };

        // Act - JSONPath filter + GroupJoin + OrderBy (full integration test)
        var result = JsonQueryable<CustomerWithTier>.FromString(
            customersJson, 
            "$[?@.tier == 'premium']")
            .GroupJoin(
                orders,
                c => c.CustomerId,
                o => o.CustomerId,
                (c, orderGroup) => new
                {
                    CustomerName = c.Name,
                    City = c.City,
                    Tier = c.Tier,
                    OrderCount = orderGroup.Count(),
                    TotalAmount = orderGroup.Sum(o => o.Amount)
                })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        // Assert
        result.Count.ShouldBe(2); // Only premium tier customers
        result.ShouldAllBe(r => r.Tier == "premium");
        result[0].CustomerName.ShouldBe("Alice");   // Total: $350
        result[0].TotalAmount.ShouldBe(350.00m);
        result[1].CustomerName.ShouldBe("Charlie"); // Total: $300
        result[1].TotalAmount.ShouldBe(300.00m);
    }

    #endregion
}

#region Test Model Classes

public record Order(int OrderId, string Customer, decimal Amount, string Status);
public record OrderWithPriority(int OrderId, string Customer, decimal Amount, string Status, string Priority);
public record OrderWithRegion(int OrderId, int CustomerId, decimal Amount, string Status, string Region);

public class OrderWithCustomerId
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class Customer
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Status { get; set; }
}

public class CustomerWithTier
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
}

#endregion

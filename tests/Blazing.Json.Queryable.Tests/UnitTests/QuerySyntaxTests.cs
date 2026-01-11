using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests;

/// <summary>
/// Tests for LINQ query syntax (query expression syntax).
/// Verifies that query syntax is correctly translated to expression trees and produces identical results to method syntax.
/// Query syntax provides an alternative, SQL-like way to write LINQ queries.
/// </summary>
public class QuerySyntaxTests
{
    #region Basic Query Syntax Tests

    [Fact]
    public void QuerySyntax_BasicWhereAndSelect_WorksCorrectly()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 30, City = "London" },
            new() { Id = 2, Name = "Bob", Age = 25, City = "Paris" },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "London" }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Using query syntax
        var results = (from p in JsonQueryable<Person>.FromString(json)
                       where p.Age > 25
                       select new { p.Name, p.Age })
                      .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results[0].Name.ShouldBe("Alice");
        results[0].Age.ShouldBe(30);
        results[1].Name.ShouldBe("Charlie");
        results[1].Age.ShouldBe(35);
    }

    [Fact]
    public void QuerySyntax_MultipleWhereConditions_WorksCorrectly()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 30, City = "London", IsActive = true },
            new() { Id = 2, Name = "Bob", Age = 25, City = "Paris", IsActive = true },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "London", IsActive = false },
            new() { Id = 4, Name = "David", Age = 28, City = "London", IsActive = true }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Using query syntax with multiple where clauses
        var results = (from p in JsonQueryable<Person>.FromString(json)
                       where p.City == "London"
                       where p.IsActive
                       where p.Age >= 28
                       select p)
                      .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldContain(p => p.Name == "Alice");
        results.ShouldContain(p => p.Name == "David");
    }

    [Fact]
    public void QuerySyntax_SelectOnly_ReturnsAllElements()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 4, 5 };
        var json = TestData.SerializeToJson(data);

        // Act - Query syntax with just select
        var results = (from n in JsonQueryable<int>.FromString(json)
                       select n)
                      .ToList();

        // Assert
        results.Count.ShouldBe(5);
        results.ShouldBe(data);
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public void QuerySyntax_OrderBy_SortsCorrectly()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 3, Name = "Charlie", Age = 35 },
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Using query syntax
        var results = (from p in JsonQueryable<Person>.FromString(json)
                       orderby p.Age
                       select p)
                      .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].Age.ShouldBe(25);
        results[1].Age.ShouldBe(30);
        results[2].Age.ShouldBe(35);
    }

    [Fact]
    public void QuerySyntax_OrderByDescending_SortsCorrectly()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 3, Name = "Charlie", Age = 35 },
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Using query syntax with descending
        var results = (from p in JsonQueryable<Person>.FromString(json)
                       orderby p.Age descending
                       select p)
                      .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].Age.ShouldBe(35);
        results[1].Age.ShouldBe(30);
        results[2].Age.ShouldBe(25);
    }

    [Fact]
    public void QuerySyntax_OrderByThenBy_TwoLevels_SortsCorrectly()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Charlie", Age = 30, City = "London" },
            new() { Id = 2, Name = "Alice", Age = 30, City = "Paris" },
            new() { Id = 3, Name = "Bob", Age = 25, City = "London" },
            new() { Id = 4, Name = "David", Age = 30, City = "London" }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Using query syntax with multiple orderby
        var results = (from p in JsonQueryable<Person>.FromString(json)
                       orderby p.Age, p.Name
                       select p)
                      .ToList();

        // Assert
        results.Count.ShouldBe(4);
        // First by Age (25, then 30s)
        results[0].Age.ShouldBe(25);
        results[0].Name.ShouldBe("Bob");
        // Then by Name within Age=30
        results[1].Age.ShouldBe(30);
        results[1].Name.ShouldBe("Alice");
        results[2].Age.ShouldBe(30);
        results[2].Name.ShouldBe("Charlie");
        results[3].Age.ShouldBe(30);
        results[3].Name.ShouldBe("David");
    }

    [Fact]
    public void QuerySyntax_OrderByMixedDirections_SortsCorrectly()
    {
        // Arrange
        var data = new List<Product>
        {
            new() { Id = 1, Name = "Widget", Category = "Tools", Rating = 4.5, Price = 25.00m },
            new() { Id = 2, Name = "Gadget", Category = "Tools", Rating = 4.5, Price = 15.00m },
            new() { Id = 3, Name = "Device", Category = "Electronics", Rating = 4.8, Price = 50.00m },
            new() { Id = 4, Name = "Doohickey", Category = "Tools", Rating = 4.2, Price = 30.00m }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Query syntax with ascending and descending
        var results = (from p in JsonQueryable<Product>.FromString(json)
                       orderby p.Category, p.Rating descending, p.Price
                       select p)
                      .ToList();

        // Assert
        results.Count.ShouldBe(4);
        // Electronics first
        results[0].Category.ShouldBe("Electronics");
        // Tools sorted by Rating descending, then Price ascending
        results[1].Category.ShouldBe("Tools");
        results[1].Rating.ShouldBe(4.5);
        results[1].Price.ShouldBe(15.00m); // Lower price first
        results[2].Rating.ShouldBe(4.5);
        results[2].Price.ShouldBe(25.00m);
        results[3].Rating.ShouldBe(4.2);
    }

    #endregion

    #region Grouping Tests

    [Fact]
    public void QuerySyntax_GroupBy_WorksCorrectly()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 30, City = "London" },
            new() { Id = 2, Name = "Bob", Age = 25, City = "Paris" },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "London" },
            new() { Id = 4, Name = "David", Age = 28, City = "Paris" }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Using query syntax with group by
        var results = (from p in JsonQueryable<Person>.FromString(json)
                       group p by p.City into g
                       select new { City = g.Key, Count = g.Count() })
                      .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldContain(x => x.City == "London" && x.Count == 2);
        results.ShouldContain(x => x.City == "Paris" && x.Count == 2);
    }

    [Fact]
    public void QuerySyntax_GroupByWithAggregations_WorksCorrectly()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 30, City = "London", Department = "Engineering", Score = 75000 },
            new() { Id = 2, Name = "Bob", Age = 25, City = "Paris", Department = "Sales", Score = 65000 },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "London", Department = "Engineering", Score = 85000 },
            new() { Id = 4, Name = "David", Age = 28, City = "Paris", Department = "Sales", Score = 70000 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Query syntax with complex grouping
        var results = (from p in JsonQueryable<Person>.FromString(json)
                       group p by p.City into cityGroup
                       select new
                       {
                           City = cityGroup.Key,
                           Count = cityGroup.Count(),
                           AvgAge = cityGroup.Average(p => p.Age),
                           TotalScore = cityGroup.Sum(p => p.Score),
                           MaxScore = cityGroup.Max(p => p.Score)
                       })
                      .ToList();

        // Assert
        results.Count.ShouldBe(2);
        
        var london = results.First(r => r.City == "London");
        london.Count.ShouldBe(2);
        london.AvgAge.ShouldBe(32.5);
        london.TotalScore.ShouldBe(160000);
        london.MaxScore.ShouldBe(85000);
        
        var paris = results.First(r => r.City == "Paris");
        paris.Count.ShouldBe(2);
        paris.AvgAge.ShouldBe(26.5);
        paris.TotalScore.ShouldBe(135000);
    }

    [Fact]
    public void QuerySyntax_GroupByWithOrderBy_WorksCorrectly()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 30, City = "London" },
            new() { Id = 2, Name = "Bob", Age = 25, City = "Paris" },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "Berlin" }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Query syntax with group by and ordering
        var results = (from p in JsonQueryable<Person>.FromString(json)
                       group p by p.City into g
                       orderby g.Key
                       select new { City = g.Key, Count = g.Count() })
                      .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].City.ShouldBe("Berlin");
        results[1].City.ShouldBe("London");
        results[2].City.ShouldBe("Paris");
    }

    #endregion

    #region Comparison with Method Syntax

    [Fact]
    public void QuerySyntax_ProducesSameResultAsMethodSyntax_WhereSelect()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 30 },
            new() { Id = 2, Name = "Bob", Age = 25 },
            new() { Id = 3, Name = "Charlie", Age = 35 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Method syntax
        var methodResults = JsonQueryable<Person>.FromString(json)
            .Where(p => p.Age > 25)
            .OrderBy(p => p.Name)
            .Select(p => p.Name)
            .ToList();

        // Act - Query syntax
        var queryResults = (from p in JsonQueryable<Person>.FromString(json)
                            where p.Age > 25
                            orderby p.Name
                            select p.Name)
                           .ToList();

        // Assert - Both should produce identical results
        queryResults.ShouldBe(methodResults);
        queryResults.Count.ShouldBe(2);
        queryResults[0].ShouldBe("Alice");
        queryResults[1].ShouldBe("Charlie");
    }

    [Fact]
    public void QuerySyntax_ProducesSameResultAsMethodSyntax_GroupBy()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", City = "London" },
            new() { Id = 2, Name = "Bob", City = "Paris" },
            new() { Id = 3, Name = "Charlie", City = "London" }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Method syntax
        var methodResults = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City)
            .Select(g => new { City = g.Key, Count = g.Count() })
            .OrderBy(x => x.City)
            .ToList();

        // Act - Query syntax
        var queryResults = (from p in JsonQueryable<Person>.FromString(json)
                            group p by p.City into g
                            orderby g.Key
                            select new { City = g.Key, Count = g.Count() })
                           .ToList();

        // Assert
        queryResults.Count.ShouldBe(methodResults.Count);
        for (int i = 0; i < queryResults.Count; i++)
        {
            queryResults[i].City.ShouldBe(methodResults[i].City);
            queryResults[i].Count.ShouldBe(methodResults[i].Count);
        }
    }

    #endregion

    #region JSONPath Integration Tests

    [Fact]
    public void QuerySyntax_WithJSONPath_WorksCorrectly()
    {
        // Arrange
        var data = new List<Product>
        {
            new() { Id = 1, Name = "Widget", Price = 15.00m, Stock = 100, Rating = 4.5 },
            new() { Id = 2, Name = "Gadget", Price = 25.00m, Stock = 200, Rating = 4.8 },
            new() { Id = 3, Name = "Device", Price = 150.00m, Stock = 0, Rating = 4.2 },
            new() { Id = 4, Name = "Doohickey", Price = 85.00m, Stock = 50, Rating = 4.6 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Query syntax with JSONPath pre-filter
        var results = (from p in JsonQueryable<Product>
                           .FromString(json, "$[?@.Price < 100 && @.Stock > 0]")
                       orderby p.Price
                       select new { p.Name, p.Price })
                      .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].Name.ShouldBe("Widget");
        results[0].Price.ShouldBe(15.00m);
        results[1].Name.ShouldBe("Gadget");
        results[1].Price.ShouldBe(25.00m);
        results[2].Name.ShouldBe("Doohickey");
        results[2].Price.ShouldBe(85.00m);
    }

    [Fact]
    public void QuerySyntax_WithJSONPathAndGrouping_WorksCorrectly()
    {
        // Arrange
        var data = new List<Product>
        {
            new() { Id = 1, Name = "Widget", Category = "Tools", Price = 15.00m, Stock = 100, Rating = 4.5 },
            new() { Id = 2, Name = "Gadget", Category = "Tools", Price = 25.00m, Stock = 200, Rating = 4.8 },
            new() { Id = 3, Name = "Device", Category = "Electronics", Price = 50.00m, Stock = 150, Rating = 4.2 },
            new() { Id = 4, Name = "Doohickey", Category = "Tools", Price = 150.00m, Stock = 0, Rating = 4.6 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - JSONPath pre-filter + Query syntax grouping
        var results = (from p in JsonQueryable<Product>
                           .FromString(json, "$[?@.Stock > 0]")
                       group p by p.Category into g
                       select new
                       {
                           Category = g.Key,
                           Count = g.Count(),
                           AvgPrice = g.Average(p => p.Price)
                       })
                      .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldContain(x => x.Category == "Tools" && x.Count == 2);
        results.ShouldContain(x => x.Category == "Electronics" && x.Count == 1);
    }

    #endregion

    #region Complex Query Tests

    [Fact]
    public void QuerySyntax_ComplexQueryWithMultipleOperations_WorksCorrectly()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 30, City = "London", Department = "Engineering", IsActive = true },
            new() { Id = 2, Name = "Bob", Age = 25, City = "Paris", Department = "Sales", IsActive = true },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "London", Department = "Engineering", IsActive = false },
            new() { Id = 4, Name = "David", Age = 28, City = "London", Department = "Sales", IsActive = true },
            new() { Id = 5, Name = "Eve", Age = 32, City = "Paris", Department = "Engineering", IsActive = true }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Complex query: filter, group, filter groups, project
        var results = (from p in JsonQueryable<Person>.FromString(json)
                       where p.IsActive
                       group p by p.Department into deptGroup
                       where deptGroup.Count() > 1
                       orderby deptGroup.Key
                       select new
                       {
                           Department = deptGroup.Key,
                           EmployeeCount = deptGroup.Count(),
                           AvgAge = deptGroup.Average(p => p.Age),
                           Cities = deptGroup.Select(p => p.City).Distinct().OrderBy(c => c).ToList()
                       })
                      .ToList();

        // Assert
        results.Count.ShouldBe(2);
        
        var engineering = results[0];
        engineering.Department.ShouldBe("Engineering");
        engineering.EmployeeCount.ShouldBe(2);
        engineering.AvgAge.ShouldBe(31.0); // (30 + 32) / 2
        engineering.Cities.ShouldContain("London");
        engineering.Cities.ShouldContain("Paris");
        
        var sales = results[1];
        sales.Department.ShouldBe("Sales");
        sales.EmployeeCount.ShouldBe(2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void QuerySyntax_EmptyResult_ReturnsEmpty()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 30 },
            new() { Id = 2, Name = "Bob", Age = 25 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Query with no matches
        var results = (from p in JsonQueryable<Person>.FromString(json)
                       where p.Age > 50
                       select p)
                      .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void QuerySyntax_EmptySource_ReturnsEmpty()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var results = (from p in JsonQueryable<Person>.FromString(json)
                       where p.Age > 25
                       select p)
                      .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    #endregion
}

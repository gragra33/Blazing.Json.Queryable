using Blazing.Json.Queryable.Providers;
using System.Text.Json;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates complex GroupBy operations with aggregations, multiple keys, and nested grouping.
/// </summary>
public static class ComplexGroupingSamples
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static void RunAll()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Complex GroupBy Operations");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        GroupByWithAggregation();
        GroupByMultipleKeys();
        GroupByWithProjection();
        GroupByWithCount();
        GroupByWithElementSelector();
        NestedGrouping();
        GroupByDistinct();
        GroupByThenFilter();
    }

    private static void GroupByWithAggregation()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("1. GroupBy with Aggregation (Average salary by department)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        [
            {"name": "Alice", "department": "Engineering", "salary": 95000},
            {"name": "Bob", "department": "Engineering", "salary": 105000},
            {"name": "Charlie", "department": "Sales", "salary": 75000},
            {"name": "Diana", "department": "Sales", "salary": 85000},
            {"name": "Eve", "department": "HR", "salary": 65000}
        ]
        """;

        var results = JsonQueryable<Employee>.FromString(json, _options)
            .GroupBy(e => e.Department)
            .Select(g => new 
            { 
                Department = g.Key, 
                AverageSalary = g.Average(e => e.Salary),
                EmployeeCount = g.Count()
            })
            .ToList();

        foreach (var result in results)
        {
            Console.WriteLine($"  {result.Department}: Avg Salary = ${result.AverageSalary:N0}, Count = {result.EmployeeCount}");
        }
        Console.WriteLine();
    }

    private static void GroupByMultipleKeys()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("2. GroupBy Multiple Keys (Department + Experience Level)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        [
            {"name": "Alice", "department": "Engineering", "level": "Senior", "salary": 95000},
            {"name": "Bob", "department": "Engineering", "level": "Senior", "salary": 105000},
            {"name": "Charlie", "department": "Engineering", "level": "Junior", "salary": 65000},
            {"name": "Diana", "department": "Sales", "level": "Senior", "salary": 85000},
            {"name": "Eve", "department": "Sales", "level": "Junior", "salary": 55000}
        ]
        """;

        var results = JsonQueryable<EmployeeLevel>.FromString(json, _options)
            .GroupBy(e => new { e.Department, e.Level })
            .Select(g => new
            {
                g.Key.Department,
                g.Key.Level,
                Count = g.Count(),
                TotalSalary = g.Sum(e => e.Salary)
            })
            .OrderBy(r => r.Department)
            .ThenBy(r => r.Level)
            .ToList();

        foreach (var result in results)
        {
            Console.WriteLine($"  {result.Department} - {result.Level}: {result.Count} employees, Total: ${result.TotalSalary:N0}");
        }
        Console.WriteLine();
    }

    private static void GroupByWithProjection()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("3. GroupBy with Custom Projection");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        [
            {"id": 1, "productName": "Laptop", "category": "Electronics", "price": 1200, "quantity": 5},
            {"id": 2, "productName": "Mouse", "category": "Electronics", "price": 25, "quantity": 50},
            {"id": 3, "productName": "Desk", "category": "Furniture", "price": 350, "quantity": 10},
            {"id": 4, "productName": "Chair", "category": "Furniture", "price": 200, "quantity": 15},
            {"id": 5, "productName": "Monitor", "category": "Electronics", "price": 400, "quantity": 8}
        ]
        """;

        var results = JsonQueryable<Product>.FromString(json, _options)
            .GroupBy(p => p.Category)
            .Select(g => new CategorySummary
            {
                Category = g.Key,
                ProductCount = g.Count(),
                TotalValue = g.Sum(p => p.Price * p.Quantity),
                AveragePrice = g.Average(p => p.Price),
                Products = g.Select(p => p.ProductName).ToList()
            })
            .ToList();

        foreach (var summary in results)
        {
            Console.WriteLine($"  Category: {summary.Category}");
            Console.WriteLine($"    Products: {summary.ProductCount}");
            Console.WriteLine($"    Total Inventory Value: ${summary.TotalValue:N0}");
            Console.WriteLine($"    Average Price: ${summary.AveragePrice:N0}");
            Console.WriteLine($"    Items: {string.Join(", ", summary.Products)}");
            Console.WriteLine();
        }
    }

    private static void GroupByWithCount()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("4. GroupBy with Count (Orders by status)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        [
            {"orderId": 1, "customer": "Alice", "status": "Completed", "total": 150},
            {"orderId": 2, "customer": "Bob", "status": "Pending", "total": 200},
            {"orderId": 3, "customer": "Charlie", "status": "Completed", "total": 75},
            {"orderId": 4, "customer": "Diana", "status": "Cancelled", "total": 50},
            {"orderId": 5, "customer": "Eve", "status": "Completed", "total": 300},
            {"orderId": 6, "customer": "Frank", "status": "Pending", "total": 125}
        ]
        """;

        var results = JsonQueryable<Order>.FromString(json, _options)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), TotalRevenue = g.Sum(o => o.Total) })
            .OrderByDescending(r => r.Count)
            .ToList();

        foreach (var result in results)
        {
            Console.WriteLine($"  {result.Status}: {result.Count} orders, Revenue: ${result.TotalRevenue:N0}");
        }
        Console.WriteLine();
    }

    private static void GroupByWithElementSelector()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("5. GroupBy with Counting");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        [
            {"name": "Alice", "city": "Seattle", "age": 30, "score": 95},
            {"name": "Bob", "city": "Seattle", "age": 35, "score": 88},
            {"name": "Charlie", "city": "Portland", "age": 28, "score": 92},
            {"name": "Diana", "city": "Portland", "age": 32, "score": 85}
        ]
        """;

        var results = JsonQueryable<Person>.FromString(json, _options)
            .GroupBy(p => p.City)
            .Select(g => new
            {
                City = g.Key,
                PeopleCount = g.Count(),
                AverageScore = g.Average(p => p.Score),
                AverageAge = g.Average(p => p.Age)
            })
            .ToList();

        foreach (var result in results)
        {
            Console.WriteLine($"  {result.City}: {result.PeopleCount} people, Avg Score={result.AverageScore:F1}, Avg Age={result.AverageAge:F1}");
        }
        Console.WriteLine();
    }

    private static void NestedGrouping()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("6. Nested GroupBy (Department -> Level -> Employees)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        [
            {"name": "Alice", "department": "Engineering", "level": "Senior"},
            {"name": "Bob", "department": "Engineering", "level": "Senior"},
            {"name": "Charlie", "department": "Engineering", "level": "Junior"},
            {"name": "Diana", "department": "Sales", "level": "Senior"},
            {"name": "Eve", "department": "Sales", "level": "Junior"},
            {"name": "Frank", "department": "Sales", "level": "Junior"}
        ]
        """;

        var departmentGroups = JsonQueryable<EmployeeLevel>.FromString(json, _options)
            .GroupBy(e => e.Department)
            .ToList();

        foreach (var dept in departmentGroups)
        {
            Console.WriteLine($"  {dept.Key}:");
            
            var levelGroups = dept.GroupBy(e => e.Level).ToList();
            foreach (var level in levelGroups)
            {
                var names = level.Select(e => e.Name).ToList();
                Console.WriteLine($"    {level.Key}: {string.Join(", ", names)}");
            }
        }
        Console.WriteLine();
    }

    private static void GroupByDistinct()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("7. GroupBy + Distinct (Unique categories per customer)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        [
            {"customer": "Alice", "category": "Electronics"},
            {"customer": "Alice", "category": "Electronics"},
            {"customer": "Alice", "category": "Books"},
            {"customer": "Bob", "category": "Electronics"},
            {"customer": "Bob", "category": "Furniture"},
            {"customer": "Charlie", "category": "Books"}
        ]
        """;

        var results = JsonQueryable<Purchase>.FromString(json, _options)
            .GroupBy(p => p.Customer)
            .Select(g => new
            {
                Customer = g.Key,
                UniqueCategories = g.Select(p => p.Category).Distinct().ToList(),
                CategoryCount = g.Select(p => p.Category).Distinct().Count()
            })
            .ToList();

        foreach (var result in results)
        {
            Console.WriteLine($"  {result.Customer}: {result.CategoryCount} unique categories - {string.Join(", ", result.UniqueCategories)}");
        }
        Console.WriteLine();
    }

    private static void GroupByThenFilter()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("8. GroupBy Then Filter (Departments with >2 employees)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        [
            {"name": "Alice", "department": "Engineering"},
            {"name": "Bob", "department": "Engineering"},
            {"name": "Charlie", "department": "Engineering"},
            {"name": "Diana", "department": "Sales"},
            {"name": "Eve", "department": "HR"}
        ]
        """;

        var results = JsonQueryable<Employee>.FromString(json, _options)
            .GroupBy(e => e.Department)
            .Where(g => g.Count() > 2)
            .Select(g => new { Department = g.Key, Count = g.Count() })
            .ToList();

        Console.WriteLine("  Departments with more than 2 employees:");
        foreach (var result in results)
        {
            Console.WriteLine($"    {result.Department}: {result.Count} employees");
        }
        Console.WriteLine();
    }

    #region Model Classes

    private class Employee
    {
        public string Name { get; set; } = "";
        public string Department { get; set; } = "";
        public decimal Salary { get; set; }
    }

    private class EmployeeLevel
    {
        public string Name { get; set; } = "";
        public string Department { get; set; } = "";
        public string Level { get; set; } = "";
        public decimal Salary { get; set; }
    }

    private class Product
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    private class CategorySummary
    {
        public string Category { get; set; } = "";
        public int ProductCount { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AveragePrice { get; set; }
        public List<string> Products { get; set; } = new();
    }

    private class Order
    {
        public int OrderId { get; set; }
        public string Customer { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal Total { get; set; }
    }

    private class Person
    {
        public string Name { get; set; } = "";
        public string City { get; set; } = "";
        public int Age { get; set; }
        public int Score { get; set; }
    }

    private class Purchase
    {
        public string Customer { get; set; } = "";
        public string Category { get; set; } = "";
    }

    #endregion
}

using Blazing.Json.Queryable.Providers;
using System.Text.Json;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates advanced LINQ operations: Chunk, Join, GroupJoin, and complex GroupBy scenarios.
/// These examples showcase real-world use cases for data partitioning, combining datasets, and hierarchical grouping.
/// </summary>
public static class AdvancedLinqOperationsSamples
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static void RunAll()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Advanced LINQ Operations - Comprehensive Examples");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        ChunkOperation_BatchProcessing();
        ChunkOperation_Pagination();
        ChunkOperation_ParallelProcessing();
        
        JoinOperation_InnerJoin();
        JoinOperation_WithProjection();
        JoinOperation_MultipleKeys();
        
        GroupJoinOperation_LeftJoin();
        GroupJoinOperation_Hierarchical();
        GroupJoinOperation_WithAggregation();
        
        GroupByAdvanced_ElementSelector();
        GroupByAdvanced_NestedGrouping();
        GroupByAdvanced_Regrouping();
        GroupByAdvanced_ComplexPipeline();
    }

    #region Chunk Operations

    private static void ChunkOperation_BatchProcessing()
    {
        Console.WriteLine("1. Chunk - Batch Processing (Process records in batches of 3)");
        Console.WriteLine("───────────────────────────────────────────────────────────────");

        var json = """
        [
            {"id": 1, "name": "Alice", "status": "Active"},
            {"id": 2, "name": "Bob", "status": "Active"},
            {"id": 3, "name": "Charlie", "status": "Pending"},
            {"id": 4, "name": "Diana", "status": "Active"},
            {"id": 5, "name": "Eve", "status": "Inactive"},
            {"id": 6, "name": "Frank", "status": "Active"},
            {"id": 7, "name": "Grace", "status": "Pending"}
        ]
        """;

        try
        {
            // Note: Chunk returns IQueryable<T[]> which has known limitations
            // Workaround: Materialize first, then chunk
            var records = JsonQueryable<Record>.FromString(json, _options)
                .ToList();

            var batches = records.Chunk(3);
            
            int batchNumber = 1;
            foreach (var batch in batches)
            {
                Console.WriteLine($"  Batch {batchNumber} ({batch.Length} records):");
                foreach (var record in batch)
                {
                    Console.WriteLine($"    - {record.Name} (ID: {record.Id}, Status: {record.Status})");
                }
                batchNumber++;
            }
        }
        catch (Exception)
        {
            Console.WriteLine($"  Note: Chunk has known limitations. Workaround demonstrated.");
            Console.WriteLine($"  Alternative: Use Skip/Take for pagination.");
        }

        Console.WriteLine();
    }

    private static void ChunkOperation_Pagination()
    {
        Console.WriteLine("2. Chunk Alternative - Pagination with Skip/Take");
        Console.WriteLine("───────────────────────────────────────────────────────────────");

        var json = """
        [
            {"id": 1, "name": "Product A", "price": 29.99},
            {"id": 2, "name": "Product B", "price": 49.99},
            {"id": 3, "name": "Product C", "price": 19.99},
            {"id": 4, "name": "Product D", "price": 39.99},
            {"id": 5, "name": "Product E", "price": 59.99},
            {"id": 6, "name": "Product F", "price": 24.99},
            {"id": 7, "name": "Product G", "price": 44.99},
            {"id": 8, "name": "Product H", "price": 34.99}
        ]
        """;

        int pageSize = 3;
        int totalItems = JsonQueryable<Product>.FromString(json, _options).Count();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        Console.WriteLine($"  Total Items: {totalItems}, Page Size: {pageSize}, Total Pages: {totalPages}");
        Console.WriteLine();

        for (int page = 0; page < totalPages; page++)
        {
            var pageItems = JsonQueryable<Product>.FromString(json, _options)
                .OrderBy(p => p.Id)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            Console.WriteLine($"  Page {page + 1}:");
            foreach (var item in pageItems)
            {
                Console.WriteLine($"    - {item.Name}: ${item.Price:F2}");
            }
        }

        Console.WriteLine();
    }

    private static void ChunkOperation_ParallelProcessing()
    {
        Console.WriteLine("3. Chunk - Parallel Batch Processing Simulation");
        Console.WriteLine("───────────────────────────────────────────────────────────────");

        var json = """
        [
            {"id": 1, "value": 100}, {"id": 2, "value": 200},
            {"id": 3, "value": 150}, {"id": 4, "value": 300},
            {"id": 5, "value": 250}, {"id": 6, "value": 180},
            {"id": 7, "value": 220}, {"id": 8, "value": 190},
            {"id": 9, "value": 280}
        ]
        """;

        var data = JsonQueryable<DataPoint>.FromString(json, _options).ToList();
        var batches = data.Chunk(3).ToList();

        Console.WriteLine($"  Processing {data.Count} items in {batches.Count} batches...");
        Console.WriteLine();

        for (int i = 0; i < batches.Count; i++)
        {
            var batchSum = batches[i].Sum(d => d.Value);
            var batchAvg = batches[i].Average(d => d.Value);
            
            Console.WriteLine($"  Batch {i + 1}:");
            Console.WriteLine($"    Items: {batches[i].Length}");
            Console.WriteLine($"    Sum: {batchSum}");
            Console.WriteLine($"    Average: {batchAvg:F2}");
        }

        Console.WriteLine();
    }

    #endregion

    #region Join Operations

    private static void JoinOperation_InnerJoin()
    {
        Console.WriteLine("4. Join - Inner Join (Employees with Departments)");
        Console.WriteLine("───────────────────────────────────────────────────────────────");

        var employeesJson = """
        [
            {"id": 1, "name": "Alice", "departmentId": 101, "salary": 75000},
            {"id": 2, "name": "Bob", "departmentId": 102, "salary": 85000},
            {"id": 3, "name": "Charlie", "departmentId": 101, "salary": 70000},
            {"id": 4, "name": "Diana", "departmentId": 103, "salary": 90000},
            {"id": 5, "name": "Eve", "departmentId": 999, "salary": 65000}
        ]
        """;

        var departmentsJson = """
        [
            {"id": 101, "name": "Engineering", "location": "Building A"},
            {"id": 102, "name": "Sales", "location": "Building B"},
            {"id": 103, "name": "HR", "location": "Building C"}
        ]
        """;

        var employees = JsonQueryable<Employee>.FromString(employeesJson, _options).ToList();
        var departments = JsonQueryable<Department>.FromString(departmentsJson, _options).ToList();

        var results = employees.AsQueryable()
            .Join(
                departments,
                emp => emp.DepartmentId,
                dept => dept.Id,
                (emp, dept) => new
                {
                    EmployeeName = emp.Name,
                    Department = dept.Name,
                    Location = dept.Location,
                    Salary = emp.Salary
                })
            .ToList();

        Console.WriteLine($"  Matched {results.Count} employees with departments:");
        foreach (var result in results)
        {
            Console.WriteLine($"    {result.EmployeeName} - {result.Department} ({result.Location}) - ${result.Salary:N0}");
        }

        Console.WriteLine();
    }

    private static void JoinOperation_WithProjection()
    {
        Console.WriteLine("5. Join - With Filtering and Projection");
        Console.WriteLine("───────────────────────────────────────────────────────────────");

        var ordersJson = """
        [
            {"orderId": 1, "customerId": 1, "total": 150.00, "status": "Shipped"},
            {"orderId": 2, "customerId": 2, "total": 200.00, "status": "Pending"},
            {"orderId": 3, "customerId": 1, "total": 75.00, "status": "Delivered"},
            {"orderId": 4, "customerId": 3, "total": 300.00, "status": "Shipped"}
        ]
        """;

        var customersJson = """
        [
            {"customerId": 1, "name": "ACME Corp", "tier": "Gold"},
            {"customerId": 2, "name": "TechStart Inc", "tier": "Silver"},
            {"customerId": 3, "name": "Global Industries", "tier": "Platinum"}
        ]
        """;

        var orders = JsonQueryable<Order>.FromString(ordersJson, _options).ToList();
        var customers = JsonQueryable<Customer>.FromString(customersJson, _options).ToList();

        var results = orders.AsQueryable()
            .Where(o => o.Status == "Shipped")
            .Join(
                customers,
                order => order.CustomerId,
                customer => customer.CustomerId,
                (order, customer) => new
                {
                    OrderId = order.OrderId,
                    Customer = customer.Name,
                    Tier = customer.Tier,
                    Amount = order.Total
                })
            .OrderByDescending(x => x.Amount)
            .ToList();

        Console.WriteLine($"  Shipped orders (high to low):");
        foreach (var result in results)
        {
            Console.WriteLine($"    Order #{result.OrderId}: {result.Customer} ({result.Tier}) - ${result.Amount:F2}");
        }

        Console.WriteLine();
    }

    private static void JoinOperation_MultipleKeys()
    {
        Console.WriteLine("6. Join - Composite Key Join");
        Console.WriteLine("───────────────────────────────────────────────────────────────");

        var salesJson = """
        [
            {"region": "West", "quarter": "Q1", "amount": 100000},
            {"region": "East", "quarter": "Q1", "amount": 120000},
            {"region": "West", "quarter": "Q2", "amount": 110000},
            {"region": "East", "quarter": "Q2", "amount": 130000}
        ]
        """;

        var targetsJson = """
        [
            {"region": "West", "quarter": "Q1", "target": 95000},
            {"region": "East", "quarter": "Q1", "target": 115000},
            {"region": "West", "quarter": "Q2", "target": 105000},
            {"region": "East", "quarter": "Q2", "target": 125000}
        ]
        """;

        var sales = JsonQueryable<SalesData>.FromString(salesJson, _options).ToList();
        var targets = JsonQueryable<SalesTarget>.FromString(targetsJson, _options).ToList();

        var results = sales.AsQueryable()
            .Join(
                targets,
                s => new { s.Region, s.Quarter },
                t => new { t.Region, t.Quarter },
                (s, t) => new
                {
                    Region = s.Region,
                    Quarter = s.Quarter,
                    Actual = s.Amount,
                    Target = t.Target,
                    PercentOfTarget = (s.Amount / (double)t.Target * 100)
                })
            .ToList();

        Console.WriteLine($"  Sales Performance by Region & Quarter:");
        foreach (var result in results)
        {
            var status = result.PercentOfTarget >= 100 ? "* ACHIEVED" : "[X] BELOW";
            Console.WriteLine($"    {result.Region} {result.Quarter}: ${result.Actual:N0} / ${result.Target:N0} ({result.PercentOfTarget:F1}%) {status}");
        }

        Console.WriteLine();
    }

    #endregion

    #region GroupJoin Operations

    private static void GroupJoinOperation_LeftJoin()
    {
        Console.WriteLine("7. GroupJoin - Left Join (All Departments with Employee Counts)");
        Console.WriteLine("───────────────────────────────────────────────────────────────");

        var departmentsJson = """
        [
            {"id": 101, "name": "Engineering"},
            {"id": 102, "name": "Sales"},
            {"id": 103, "name": "HR"},
            {"id": 104, "name": "Marketing"}
        ]
        """;

        var employeesJson = """
        [
            {"id": 1, "name": "Alice", "departmentId": 101},
            {"id": 2, "name": "Bob", "departmentId": 102},
            {"id": 3, "name": "Charlie", "departmentId": 101},
            {"id": 4, "name": "Diana", "departmentId": 103},
            {"id": 5, "name": "Eve", "departmentId": 101}
        ]
        """;

        var departments = JsonQueryable<Department>.FromString(departmentsJson, _options).ToList();
        var employees = JsonQueryable<Employee>.FromString(employeesJson, _options).ToList();

        var results = departments.AsQueryable()
            .GroupJoin(
                employees,
                dept => dept.Id,
                emp => emp.DepartmentId,
                (dept, emps) => new
                {
                    Department = dept.Name,
                    EmployeeCount = emps.Count(),
                    Employees = emps.Select(e => e.Name).ToList()
                })
            .ToList();

        Console.WriteLine($"  Department Employee Summary:");
        foreach (var result in results)
        {
            Console.WriteLine($"    {result.Department}: {result.EmployeeCount} employee(s)");
            if (result.EmployeeCount > 0)
            {
                Console.WriteLine($"      -> {string.Join(", ", result.Employees)}");
            }
            else
            {
                Console.WriteLine($"      -> (No employees)");
            }
        }

        Console.WriteLine();
    }

    private static void GroupJoinOperation_Hierarchical()
    {
        Console.WriteLine("8. GroupJoin - Hierarchical Data (Categories with Products)");
        Console.WriteLine("───────────────────────────────────────────────────────────────");

        var categoriesJson = """
        [
            {"id": 1, "name": "Electronics"},
            {"id": 2, "name": "Clothing"},
            {"id": 3, "name": "Books"}
        ]
        """;

        var productsJson = """
        [
            {"id": 101, "name": "Laptop", "categoryId": 1, "price": 999.99},
            {"id": 102, "name": "Mouse", "categoryId": 1, "price": 29.99},
            {"id": 103, "name": "T-Shirt", "categoryId": 2, "price": 19.99},
            {"id": 104, "name": "Jeans", "categoryId": 2, "price": 59.99},
            {"id": 105, "name": "Keyboard", "categoryId": 1, "price": 79.99}
        ]
        """;

        var categories = JsonQueryable<Category>.FromString(categoriesJson, _options).ToList();
        var products = JsonQueryable<ProductItem>.FromString(productsJson, _options).ToList();

        var results = categories.AsQueryable()
            .GroupJoin(
                products,
                cat => cat.Id,
                prod => prod.CategoryId,
                (cat, prods) => new
                {
                    Category = cat.Name,
                    ProductCount = prods.Count(),
                    TotalValue = prods.Sum(p => p.Price),
                    Products = prods.Select(p => new { p.Name, p.Price }).ToList()
                })
            .OrderByDescending(x => x.TotalValue)
            .ToList();

        Console.WriteLine($"  Product Catalog by Category:");
        foreach (var result in results)
        {
            Console.WriteLine($"    {result.Category}:");
            Console.WriteLine($"      Products: {result.ProductCount}");
            Console.WriteLine($"      Total Value: ${result.TotalValue:F2}");
            foreach (var product in result.Products)
            {
                Console.WriteLine($"        - {product.Name}: ${product.Price:F2}");
            }
        }

        Console.WriteLine();
    }

    private static void GroupJoinOperation_WithAggregation()
    {
        Console.WriteLine("9. GroupJoin - With Aggregation (Customer Order Summary)");
        Console.WriteLine("───────────────────────────────────────────────────────────────");

        var customersJson = """
        [
            {"customerId": 1, "name": "ACME Corp", "tier": "Gold"},
            {"customerId": 2, "name": "TechStart Inc", "tier": "Silver"},
            {"customerId": 3, "name": "Global Industries", "tier": "Platinum"},
            {"customerId": 4, "name": "Small Biz LLC", "tier": "Bronze"}
        ]
        """;

        var ordersJson = """
        [
            {"orderId": 1, "customerId": 1, "total": 1500.00, "date": "2024-01-15"},
            {"orderId": 2, "customerId": 2, "total": 2000.00, "date": "2024-01-16"},
            {"orderId": 3, "customerId": 1, "total": 750.00, "date": "2024-01-17"},
            {"orderId": 4, "customerId": 3, "total": 3000.00, "date": "2024-01-18"},
            {"orderId": 5, "customerId": 1, "total": 1200.00, "date": "2024-01-19"}
        ]
        """;

        var customers = JsonQueryable<Customer>.FromString(customersJson, _options).ToList();
        var orders = JsonQueryable<Order>.FromString(ordersJson, _options).ToList();

        var results = customers.AsQueryable()
            .GroupJoin(
                orders,
                cust => cust.CustomerId,
                order => order.CustomerId,
                (cust, orders) => new
                {
                    Customer = cust.Name,
                    Tier = cust.Tier,
                    OrderCount = orders.Count(),
                    TotalSpent = orders.Sum(o => o.Total),
                    AverageOrder = orders.Any() ? orders.Average(o => o.Total) : 0
                })
            .OrderByDescending(x => x.TotalSpent)
            .ToList();

        Console.WriteLine($"  Customer Order Analytics:");
        foreach (var result in results)
        {
            Console.WriteLine($"    {result.Customer} ({result.Tier}):");
            Console.WriteLine($"      Orders: {result.OrderCount}");
            Console.WriteLine($"      Total Spent: ${result.TotalSpent:F2}");
            Console.WriteLine($"      Average Order: ${result.AverageOrder:F2}");
        }

        Console.WriteLine();
    }

    #endregion

    #region Advanced GroupBy Operations

    private static void GroupByAdvanced_ElementSelector()
    {
        Console.WriteLine("10. GroupBy - Element Selector with Aggregations");
        Console.WriteLine("───────────────────────────────────────────────────────────────");

        var json = """
        [
            {"city": "Seattle", "name": "Alice", "score": 95, "age": 28},
            {"city": "Seattle", "name": "Bob", "score": 88, "age": 32},
            {"city": "Portland", "name": "Charlie", "score": 92, "age": 25},
            {"city": "Portland", "name": "Diana", "score": 85, "age": 30},
            {"city": "Seattle", "name": "Eve", "score": 90, "age": 27}
        ]
        """;

        var results = JsonQueryable<PersonScore>.FromString(json, _options)
            .GroupBy(p => p.City, p => p.Score)
            .Select(g => new
            {
                City = g.Key,
                AverageScore = g.Average(),
                HighScore = g.Max(),
                LowScore = g.Min(),
                Count = g.Count()
            })
            .OrderByDescending(x => x.AverageScore)
            .ToList();

        Console.WriteLine($"  City Score Statistics:");
        foreach (var result in results)
        {
            Console.WriteLine($"    {result.City}:");
            Console.WriteLine($"      Average: {result.AverageScore:F2}");
            Console.WriteLine($"      Range: {result.LowScore} - {result.HighScore}");
            Console.WriteLine($"      Participants: {result.Count}");
        }

        Console.WriteLine();
    }

    private static void GroupByAdvanced_NestedGrouping()
    {
        Console.WriteLine("11. GroupBy - Nested Grouping (City -> Department)");
        Console.WriteLine("───────────────────────────────────────────────────────────────");

        var json = """
        [
            {"name": "Alice", "city": "Seattle", "department": "Engineering", "salary": 95000},
            {"name": "Bob", "city": "Seattle", "department": "Sales", "salary": 85000},
            {"name": "Charlie", "city": "Seattle", "department": "Engineering", "salary": 90000},
            {"name": "Diana", "city": "Portland", "department": "Engineering", "salary": 92000},
            {"name": "Eve", "city": "Portland", "department": "Sales", "salary": 88000}
        ]
        """;

        var results = JsonQueryable<EmployeeFull>.FromString(json, _options)
            .GroupBy(e => e.City)
            .Select(cityGroup => new
            {
                City = cityGroup.Key,
                Departments = cityGroup
                    .GroupBy(e => e.Department)
                    .Select(deptGroup => new
                    {
                        Department = deptGroup.Key,
                        Count = deptGroup.Count(),
                        AverageSalary = deptGroup.Average(e => e.Salary)
                    })
                    .ToList()
            })
            .ToList();

        Console.WriteLine($"  Employee Distribution by City and Department:");
        foreach (var city in results)
        {
            Console.WriteLine($"    {city.City}:");
            foreach (var dept in city.Departments)
            {
                Console.WriteLine($"      {dept.Department}: {dept.Count} employee(s), Avg Salary: ${dept.AverageSalary:N0}");
            }
        }

        Console.WriteLine();
    }

    private static void GroupByAdvanced_Regrouping()
    {
        Console.WriteLine("12. GroupBy - Regrouping (Group -> Select -> Group Again)");
        Console.WriteLine("───────────────────────────────────────────────────────────────");

        var json = """
        [
            {"name": "Alice", "city": "Seattle", "score": 95},
            {"name": "Bob", "city": "Seattle", "score": 88},
            {"name": "Charlie", "city": "Portland", "score": 92},
            {"name": "Diana", "city": "Portland", "score": 85},
            {"name": "Eve", "city": "Boston", "score": 78}
        ]
        """;

        var results = JsonQueryable<PersonScore>.FromString(json, _options)
            .GroupBy(p => p.City, p => p.Score)
            .Select(g => new
            {
                City = g.Key,
                AverageScore = g.Average(),
                MaxScore = g.Max()
            })
            .GroupBy(x => x.AverageScore >= 90 ? "High Performance" : "Standard")
            .Select(g => new
            {
                Category = g.Key,
                CityCount = g.Count(),
                Cities = g.Select(x => x.City).ToList(),
                OverallAverage = g.Average(x => x.AverageScore)
            })
            .ToList();

        Console.WriteLine($"  Performance Categories:");
        foreach (var result in results)
        {
            Console.WriteLine($"    {result.Category}:");
            Console.WriteLine($"      Cities: {string.Join(", ", result.Cities)} ({result.CityCount} total)");
            Console.WriteLine($"      Overall Average: {result.OverallAverage:F2}");
        }

        Console.WriteLine();
    }

    private static void GroupByAdvanced_ComplexPipeline()
    {
        Console.WriteLine("13. GroupBy - Complex Pipeline with Multiple Operations");
        Console.WriteLine("───────────────────────────────────────────────────────────────");

        var json = """
        [
            {"name": "Alice", "city": "Seattle", "department": "Engineering", "score": 95, "age": 28},
            {"name": "Bob", "city": "Seattle", "department": "Engineering", "score": 88, "age": 32},
            {"name": "Charlie", "city": "Portland", "department": "Sales", "score": 92, "age": 25},
            {"name": "Diana", "city": "Portland", "department": "Sales", "score": 85, "age": 30},
            {"name": "Eve", "city": "Seattle", "department": "Sales", "score": 90, "age": 27},
            {"name": "Frank", "city": "Boston", "department": "Engineering", "score": 78, "age": 35}
        ]
        """;

        var results = JsonQueryable<EmployeeFull>.FromString(json, _options)
            // First grouping by City with element selector
            .GroupBy(e => e.City, e => new { e.Department, e.Score, e.Age })
            .Select(cityGroup => new
            {
                City = cityGroup.Key,
                DepartmentStats = cityGroup
                    // Second grouping by Department
                    .GroupBy(x => x.Department, x => x.Score)
                    .Select(deptGroup => new
                    {
                        Department = deptGroup.Key,
                        AverageScore = deptGroup.Average(),
                        Count = deptGroup.Count(),
                        TopScore = deptGroup.Max()
                    })
                    .OrderByDescending(d => d.AverageScore)
                    .ToList(),
                TotalEmployees = cityGroup.Count(),
                CityAverageAge = cityGroup.Average(x => x.Age)
            })
            .OrderByDescending(x => x.TotalEmployees)
            .ToList();

        Console.WriteLine($"  Comprehensive City & Department Analysis:");
        foreach (var city in results)
        {
            Console.WriteLine($"    {city.City} ({city.TotalEmployees} employees, Avg Age: {city.CityAverageAge:F1}):");
            foreach (var dept in city.DepartmentStats)
            {
                Console.WriteLine($"      {dept.Department}:");
                Console.WriteLine($"        Count: {dept.Count}, Avg Score: {dept.AverageScore:F2}, Top: {dept.TopScore}");
            }
        }

        Console.WriteLine();
    }

    #endregion

    #region Model Classes

    private class Record
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    private class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    private class DataPoint
    {
        public int Id { get; set; }
        public int Value { get; set; }
    }

    private class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public decimal Salary { get; set; }
    }

    private class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
    }

    private class Order
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Date { get; set; }
    }

    private class Customer
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
    }

    private class SalesData
    {
        public string Region { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    private class SalesTarget
    {
        public string Region { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public int Target { get; set; }
    }

    private class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class ProductItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
    }

    private class PersonScore
    {
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int Score { get; set; }
        public int Age { get; set; }
    }

    private class EmployeeFull
    {
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public int Score { get; set; }
        public int Age { get; set; }
    }

    #endregion
}

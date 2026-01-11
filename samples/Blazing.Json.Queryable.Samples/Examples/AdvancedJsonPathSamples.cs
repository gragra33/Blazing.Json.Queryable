using Blazing.Json.Queryable.Providers;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates advanced JSONPath RFC 9535 features with Blazing.Json.Queryable.
/// Shows how to use filters, functions, slicing, and complex queries.
/// </summary>
public static class AdvancedJsonPathSamples
{
    public static void RunAll()
    {
        Console.WriteLine("=== Advanced JSONPath (RFC 9535) Examples ===\n");
        
        FilterExpressions();
        BuiltInFunctions();
        ArraySlicing();
        NestedPaths();
        CombiningJsonPathWithLinq();
        RealWorldScenarios();
        
        Console.WriteLine("\n=== Advanced JSONPath Examples Complete ===\n");
    }

    /// <summary>
    /// Demonstrates RFC 9535 filter expressions.
    /// [PURE JSONPATH] - Uses only JSONPath filters for data selection.
    /// </summary>
    private static void FilterExpressions()
    {
        Console.WriteLine("1. RFC 9535 Filter Expressions [PURE JSONPATH]");
        
        var booksJson = """
        [
            {"title": "The Great Gatsby", "author": "F. Scott Fitzgerald", "price": 12.99, "year": 1925, "inStock": true},
            {"title": "To Kill a Mockingbird", "author": "Harper Lee", "price": 14.99, "year": 1960, "inStock": true},
            {"title": "1984", "author": "George Orwell", "price": 13.99, "year": 1949, "inStock": false},
            {"title": "Pride and Prejudice", "author": "Jane Austen", "price": 11.99, "year": 1813, "inStock": true},
            {"title": "The Catcher in the Rye", "author": "J.D. Salinger", "price": 10.99, "year": 1951, "inStock": false}
        ]
        """;

        // Simple comparison filter
        Console.WriteLine("   a) Books under $13:");
        var affordable = JsonQueryable<Book>.FromString(booksJson, "$[?@.price < 13]")
            .ToList();
        
        foreach (var book in affordable)
        {
            Console.WriteLine($"      - {book.Title}: ${book.Price}");
        }

        // Logical AND filter
        Console.WriteLine("\n   b) Books under $13 AND in stock:");
        var affordableInStock = JsonQueryable<Book>.FromString(booksJson, "$[?@.price < 13 && @.inStock == true]")
            .ToList();
        
        foreach (var book in affordableInStock)
        {
            Console.WriteLine($"      - {book.Title}: ${book.Price} (In Stock)");
        }

        // Complex filter with multiple conditions
        Console.WriteLine("\n   c) Books published after 1900, under $15, in stock:");
        var modern = JsonQueryable<Book>.FromString(booksJson, "$[?@.year > 1900 && @.price < 15 && @.inStock == true]")
            .ToList();
        
        foreach (var book in modern)
        {
            Console.WriteLine($"      - {book.Title} ({book.Year}): ${book.Price}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates RFC 9535 built-in functions.
    /// [PURE JSONPATH] - Uses only JSONPath functions and filters.
    /// </summary>
    private static void BuiltInFunctions()
    {
        Console.WriteLine("2. RFC 9535 Built-in Functions [PURE JSONPATH]");
        
        var productsJson = """
        [
            {"id": 1, "name": "Laptop Pro", "category": "Electronics", "price": 1299.99, "code": "ELEC-001"},
            {"id": 2, "name": "Wireless Mouse", "category": "Electronics", "price": 29.99, "code": "ELEC-002"},
            {"id": 3, "name": "USB-C Cable", "category": "Electronics", "price": 12.99, "code": "ELEC-003"},
            {"id": 4, "name": "Office Desk", "category": "Furniture", "price": 399.99, "code": "FURN-001"},
            {"id": 5, "name": "Ergonomic Chair", "category": "Furniture", "price": 299.99, "code": "FURN-002"}
        ]
        """;

        // length() function - filter by string length
        Console.WriteLine("   a) Products with long names (length > 12):");
        var longNames = JsonQueryable<JsonPathProduct>.FromString(productsJson, "$[?length(@.name) > 12]")
            .ToList();
        
        foreach (var product in longNames)
        {
            Console.WriteLine($"      - {product.Name} ({product.Name.Length} chars)");
        }

        // match() function - regex pattern matching (I-Regexp RFC 9485)
        Console.WriteLine("\n   b) Electronics products (code starts with 'ELEC'):");
        var electronics = JsonQueryable<JsonPathProduct>.FromString(productsJson, "$[?match(@.code, '^ELEC-.*')]")
            .ToList();
        
        foreach (var product in electronics)
        {
            Console.WriteLine($"      - {product.Name}: {product.Code}");
        }

        // search() function - substring search
        Console.WriteLine("\n   c) Products with 'Chair' in name:");
        var chairs = JsonQueryable<JsonPathProduct>.FromString(productsJson, "$[?search(@.name, 'Chair')]")
            .ToList();
        
        foreach (var product in chairs)
        {
            Console.WriteLine($"      - {product.Name}: ${product.Price}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates RFC 9535 array slicing with step.
    /// [PURE JSONPATH] - Uses only JSONPath array slicing syntax.
    /// </summary>
    private static void ArraySlicing()
    {
        Console.WriteLine("3. RFC 9535 Array Slicing [PURE JSONPATH]");
        
        var numbersJson = """
        [
            {"id": 1, "value": 10},
            {"id": 2, "value": 20},
            {"id": 3, "value": 30},
            {"id": 4, "value": 40},
            {"id": 5, "value": 50},
            {"id": 6, "value": 60},
            {"id": 7, "value": 70},
            {"id": 8, "value": 80},
            {"id": 9, "value": 90},
            {"id": 10, "value": 100}
        ]
        """;

        // Basic slice [start:end]
        Console.WriteLine("   a) Elements 2-5 [2:5]:");
        var slice1 = JsonQueryable<NumberItem>.FromString(numbersJson, "$[2:5]")
            .ToList();
        
        foreach (var item in slice1)
        {
            Console.WriteLine($"      - ID {item.Id}: {item.Value}");
        }

        // Slice with step [start:end:step]
        Console.WriteLine("\n   b) Every other element [0:10:2]:");
        var slice2 = JsonQueryable<NumberItem>.FromString(numbersJson, "$[0:10:2]")
            .ToList();
        
        foreach (var item in slice2)
        {
            Console.WriteLine($"      - ID {item.Id}: {item.Value}");
        }

        // Negative indices (from end)
        Console.WriteLine("\n   c) Last 3 elements [-3:]:");
        var slice3 = JsonQueryable<NumberItem>.FromString(numbersJson, "$[-3:]")
            .ToList();
        
        foreach (var item in slice3)
        {
            Console.WriteLine($"      - ID {item.Id}: {item.Value}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates nested JSONPath with RFC 9535 filters.
    /// [PURE JSONPATH] - Uses only JSONPath navigation and filters.
    /// </summary>
    private static void NestedPaths()
    {
        Console.WriteLine("4. Nested JSONPath with RFC 9535 Filters [PURE JSONPATH]");
        
        var apiResponse = """
        {
            "status": "success",
            "data": {
                "users": [
                    {"id": 1, "name": "Alice", "age": 28, "premium": true, "score": 95},
                    {"id": 2, "name": "Bob", "age": 32, "premium": false, "score": 78},
                    {"id": 3, "name": "Charlie", "age": 25, "premium": true, "score": 88},
                    {"id": 4, "name": "Diana", "age": 35, "premium": true, "score": 92},
                    {"id": 5, "name": "Eve", "age": 29, "premium": false, "score": 85}
                ]
            }
        }
        """;

        // Navigate nested path + filter
        Console.WriteLine("   a) Premium users with score > 90:");
        var topPremium = JsonQueryable<User>.FromString(apiResponse, "$.data.users[?@.premium == true && @.score > 90]")
            .ToList();
        
        foreach (var user in topPremium)
        {
            Console.WriteLine($"      - {user.Name} (Score: {user.Score})");
        }

        // Deep nesting with complex filter
        Console.WriteLine("\n   b) Users aged 25-30 with high scores:");
        var youngHighScorers = JsonQueryable<User>.FromString(apiResponse, "$.data.users[?@.age >= 25 && @.age <= 30 && @.score >= 85]")
            .ToList();
        
        foreach (var user in youngHighScorers)
        {
            Console.WriteLine($"      - {user.Name}, Age {user.Age}, Score: {user.Score}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates combining JSONPath filters with LINQ operations.
    /// [MIXED JSONPATH + LINQ] - JSONPath filters followed by LINQ GroupBy, Select, OrderBy, and aggregation methods.
    /// </summary>
    private static void CombiningJsonPathWithLinq()
    {
        Console.WriteLine("5. Combining JSONPath Filters with LINQ Operations [MIXED JSONPATH + LINQ]");
        
        var ordersJson = """
        [
            {"orderId": 1, "customer": "Alice", "amount": 150.00, "status": "shipped", "items": 3},
            {"orderId": 2, "customer": "Bob", "amount": 89.99, "status": "pending", "items": 2},
            {"orderId": 3, "customer": "Charlie", "amount": 250.00, "status": "shipped", "items": 5},
            {"orderId": 4, "customer": "Diana", "amount": 120.00, "status": "shipped", "items": 2},
            {"orderId": 5, "customer": "Eve", "amount": 75.50, "status": "pending", "items": 1},
            {"orderId": 6, "customer": "Frank", "amount": 200.00, "status": "shipped", "items": 4},
            {"orderId": 7, "customer": "Grace", "amount": 95.00, "status": "cancelled", "items": 2}
        ]
        """;

        // [MIXED] JSONPath pre-filter + LINQ GroupBy + LINQ OrderByDescending
        Console.WriteLine("   a) Shipped orders (JSONPath) grouped by customer (LINQ):");
        var shippedByCustomer = JsonQueryable<JsonPathOrder>.FromString(ordersJson, "$[?@.status == 'shipped']")
            .GroupBy(o => o.Customer)
            .Select(g => new { 
                Customer = g.Key, 
                OrderCount = g.Count(), 
                TotalAmount = g.Sum(o => o.Amount) 
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();
        
        foreach (var group in shippedByCustomer)
        {
            Console.WriteLine($"      - {group.Customer}: {group.OrderCount} orders, ${group.TotalAmount:F2} total");
        }

        // [MIXED] JSONPath filter + LINQ aggregation
        Console.WriteLine("\n   b) High-value orders (JSONPath) with LINQ statistics:");
        var highValueOrders = JsonQueryable<JsonPathOrder>.FromString(ordersJson, "$[?@.amount > 100]")
            .ToList();
        
        var avgAmount = highValueOrders.Average(o => o.Amount);
        var maxAmount = highValueOrders.Max(o => o.Amount);
        var totalItems = highValueOrders.Sum(o => o.Items);
        
        Console.WriteLine($"      - Count: {highValueOrders.Count}");
        Console.WriteLine($"      - Average: ${avgAmount:F2}");
        Console.WriteLine($"      - Maximum: ${maxAmount:F2}");
        Console.WriteLine($"      - Total Items: {totalItems}");

        // [MIXED] JSONPath filter + LINQ GroupBy + Multiple OrderBy (Asc & Desc)
        Console.WriteLine("\n   c) Orders by status (JSONPath) grouped and multi-sorted (LINQ):");
        var ordersByStatus = JsonQueryable<JsonPathOrder>.FromString(ordersJson, "$[?@.amount > 50]")
            .GroupBy(o => o.Status)
            .Select(g => new {
                Status = g.Key,
                OrderCount = g.Count(),
                TotalAmount = g.Sum(o => o.Amount),
                AvgItems = g.Average(o => o.Items)
            })
            .OrderBy(x => x.Status)           // First: sort by status (ascending)
            .ThenByDescending(x => x.TotalAmount)  // Then: sort by total amount (descending)
            .ToList();
        
        foreach (var statusGroup in ordersByStatus)
        {
            Console.WriteLine($"      - {statusGroup.Status}: {statusGroup.OrderCount} orders, " +
                            $"${statusGroup.TotalAmount:F2} total, {statusGroup.AvgItems:F1} avg items");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Real-world scenarios using advanced JSONPath features.
    /// [MIXED JSONPATH + LINQ] - Combines JSONPath filtering with LINQ operations.
    /// </summary>
    private static void RealWorldScenarios()
    {
        Console.WriteLine("6. Real-World Scenarios [MIXED JSONPATH + LINQ]");
        
        var employeesJson = """
        [
            {"id": 1, "name": "Alice Johnson", "department": "Engineering", "salary": 95000, "yearsEmployed": 5, "skills": ["C#", "Azure", "SQL"]},
            {"id": 2, "name": "Bob Smith", "department": "Engineering", "salary": 105000, "yearsEmployed": 8, "skills": ["Java", "AWS", "Kubernetes"]},
            {"id": 3, "name": "Charlie Brown", "department": "Sales", "salary": 75000, "yearsEmployed": 3, "skills": ["Negotiation", "CRM"]},
            {"id": 4, "name": "Diana Prince", "department": "Engineering", "salary": 120000, "yearsEmployed": 10, "skills": ["C#", "Python", "Azure", "ML"]},
            {"id": 5, "name": "Eve Davis", "department": "Marketing", "salary": 70000, "yearsEmployed": 2, "skills": ["SEO", "Content Writing"]},
            {"id": 6, "name": "Frank Miller", "department": "Engineering", "salary": 98000, "yearsEmployed": 6, "skills": ["Go", "Docker", "Terraform"]}
        ]
        """;

        // Scenario 1: [PURE JSONPATH] Find senior engineers for promotion review
        Console.WriteLine("   a) Senior Engineers (salary > $100K, 7+ years) [PURE JSONPATH]:");
        var seniorEngineers = JsonQueryable<Employee>
            .FromString(employeesJson, "$[?@.department == 'Engineering' && @.salary > 100000 && @.yearsEmployed >= 7]")
            .ToList();
        
        foreach (var emp in seniorEngineers)
        {
            Console.WriteLine($"      - {emp.Name}: ${emp.Salary:N0}, {emp.YearsEmployed} years");
        }

        // Scenario 2: [MIXED] Entry-level talent across departments with LINQ sorting
        Console.WriteLine("\n   b) Entry-level talent (< 3 years) for mentorship program [MIXED JSONPATH + LINQ]:");
        var entryLevel = JsonQueryable<Employee>
            .FromString(employeesJson, "$[?@.yearsEmployed < 3]")
            .OrderBy(e => e.Department)
            .ThenBy(e => e.Name)
            .ToList();
        
        foreach (var emp in entryLevel)
        {
            Console.WriteLine($"      - {emp.Name} ({emp.Department}): {emp.YearsEmployed} years");
        }

        // Scenario 3: [MIXED] Salary analysis with JSONPath + LINQ
        Console.WriteLine("\n   c) Engineering department salary analysis [MIXED JSONPATH + LINQ]:");
        var engStats = JsonQueryable<Employee>
            .FromString(employeesJson, "$[?@.department == 'Engineering']")
            .Select(e => e.Salary)
            .ToList();
        
        Console.WriteLine($"      - Engineers: {engStats.Count}");
        Console.WriteLine($"      - Average Salary: ${engStats.Average():N0}");
        Console.WriteLine($"      - Median Salary: ${engStats.OrderBy(s => s).ElementAt(engStats.Count / 2):N0}");
        Console.WriteLine($"      - Salary Range: ${engStats.Min():N0} - ${engStats.Max():N0}");

        // Scenario 4: [MIXED] Department analysis with GroupBy + Multiple OrderBy
        Console.WriteLine("\n   d) Department workforce analysis [MIXED JSONPATH + LINQ]:");
        var deptAnalysis = JsonQueryable<Employee>
            .FromString(employeesJson, "$[?@.salary > 60000]")  // JSONPath: Filter employees with salary > $60K
            .GroupBy(e => e.Department)                 // LINQ: Group by department
            .Select(g => new
            {
                Department = g.Key,
                EmployeeCount = g.Count(),
                AvgSalary = g.Average(e => e.Salary),
                TotalYearsExperience = g.Sum(e => e.YearsEmployed),
                TopEarner = g.OrderByDescending(e => e.Salary).First().Name
            })
            .OrderByDescending(x => x.AvgSalary)    // LINQ: Primary sort by avg salary (descending)
        .ThenBy(x => x.Department)                  // LINQ: Secondary sort by department name (ascending)
            .ToList();
        
        foreach (var dept in deptAnalysis)
        {
            Console.WriteLine($"      - {dept.Department}: {dept.EmployeeCount} employees, " +
                            $"${dept.AvgSalary:N0} avg salary, " +
                            $"{dept.TotalYearsExperience} total years, " +
                            $"top earner: {dept.TopEarner}");
        }

        Console.WriteLine();
    }
}

#region Model Classes

public record Book(string Title, string Author, decimal Price, int Year, bool InStock);
public record JsonPathProduct(int Id, string Name, string Category, decimal Price, string Code);
public record NumberItem(int Id, int Value);
public record User(int Id, string Name, int Age, bool Premium, int Score);
public record JsonPathOrder(int OrderId, string Customer, decimal Amount, string Status, int Items);
public record Employee(int Id, string Name, string Department, decimal Salary, int YearsEmployed, string[] Skills);

#endregion

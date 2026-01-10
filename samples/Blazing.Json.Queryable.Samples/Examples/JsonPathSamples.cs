using Blazing.Json.Queryable.Providers;
using System.Text.Json;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates JSONPath filtering with multi-level array wildcards and complex nested structures.
/// </summary>
public static class JsonPathSamples
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static void RunAll()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine(" JSONPath Filtering");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        SimpleArrayPath();
        NestedObjectPath();
        MultiLevelArrayWildcards();
        DeepNestedArrays();
        RealWorldExample_Organization();
        RealWorldExample_ECommerce();
        PathWithLinqFiltering();
        ComplexNestedStructure();
    }

    private static void SimpleArrayPath()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("1. Simple Array Path ($.users[*])");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        {
            "users": [
                {"id": 1, "name": "Alice", "age": 30},
                {"id": 2, "name": "Bob", "age": 35},
                {"id": 3, "name": "Charlie", "age": 28}
            ]
        }
        """;

        var results = JsonQueryable<User>.FromString(json, "$.users[*]", _options)
            .ToList();

        Console.WriteLine($"  Found {results.Count} users:");
        foreach (var user in results)
        {
            Console.WriteLine($"    {user.Name} (ID: {user.Id}, Age: {user.Age})");
        }
        Console.WriteLine();
    }

    private static void NestedObjectPath()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("2. Nested Object Path ($.data.customers[*])");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        {
            "status": "success",
            "data": {
                "customers": [
                    {"id": 1, "name": "Alice", "email": "alice@example.com"},
                    {"id": 2, "name": "Bob", "email": "bob@example.com"}
                ]
            }
        }
        """;

        var results = JsonQueryable<Customer>.FromString(json, "$.data[*].customers[*]", _options)
            .ToList();

        Console.WriteLine($"  Found {results.Count} customers:");
        foreach (var customer in results)
        {
            Console.WriteLine($"    {customer.Name} - {customer.Email}");
        }
        Console.WriteLine();
    }

    private static void MultiLevelArrayWildcards()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("3. Multi-Level Array Wildcards ($.departments[*].employees[*])");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        {
            "departments": [
                {
                    "name": "Engineering",
                    "employees": [
                        {"name": "Alice", "role": "Developer"},
                        {"name": "Bob", "role": "Architect"}
                    ]
                },
                {
                    "name": "Sales",
                    "employees": [
                        {"name": "Charlie", "role": "Manager"},
                        {"name": "Diana", "role": "Representative"}
                    ]
                }
            ]
        }
        """;

        var results = JsonQueryable<Employee>.FromString(json, "$.departments[*].employees[*]", _options)
            .ToList();

        Console.WriteLine($"  Found {results.Count} employees across all departments:");
        foreach (var emp in results)
        {
            Console.WriteLine($"    {emp.Name} - {emp.Role}");
        }
        Console.WriteLine();
    }

    private static void DeepNestedArrays()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("4. Deep Nested Arrays ($.departments[*].teams[*].members[*])");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        {
            "departments": [
                {
                    "name": "Engineering",
                    "teams": [
                        {
                            "name": "Backend",
                            "members": [
                                {"id": 1, "name": "Alice"},
                                {"id": 2, "name": "Bob"}
                            ]
                        },
                        {
                            "name": "Frontend",
                            "members": [
                                {"id": 3, "name": "Charlie"}
                            ]
                        }
                    ]
                },
                {
                    "name": "Sales",
                    "teams": [
                        {
                            "name": "EMEA",
                            "members": [
                                {"id": 4, "name": "Diana"}
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        var results = JsonQueryable<Member>.FromString(json, "$.departments[*].teams[*].members[*]", _options)
            .ToList();

        Console.WriteLine($"  Found {results.Count} members across all teams and departments:");
        foreach (var member in results)
        {
            Console.WriteLine($"    {member.Name} (ID: {member.Id})");
        }
        Console.WriteLine();
    }

    private static void RealWorldExample_Organization()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("5. Real-World: Organization Structure");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        {
            "organization": {
                "name": "TechCorp",
                "divisions": [
                    {
                        "name": "Product Development",
                        "departments": [
                            {
                                "name": "Engineering",
                                "employees": [
                                    {"id": 1, "name": "Alice Johnson", "title": "Senior Engineer", "salary": 120000},
                                    {"id": 2, "name": "Bob Smith", "title": "Lead Engineer", "salary": 140000}
                                ]
                            },
                            {
                                "name": "Design",
                                "employees": [
                                    {"id": 3, "name": "Carol White", "title": "UX Designer", "salary": 95000}
                                ]
                            }
                        ]
                    },
                    {
                        "name": "Business Operations",
                        "departments": [
                            {
                                "name": "Sales",
                                "employees": [
                                    {"id": 4, "name": "David Brown", "title": "Sales Manager", "salary": 110000},
                                    {"id": 5, "name": "Eve Davis", "title": "Account Executive", "salary": 85000}
                                ]
                            }
                        ]
                    }
                ]
            }
        }
        """;

        var allEmployees = JsonQueryable<EmployeeDetail>
            .FromString(json, "$.organization[*].divisions[*].departments[*].employees[*]", _options)
            .OrderByDescending(e => e.Salary)
            .ToList();

        Console.WriteLine($"  All employees (ordered by salary):");
        foreach (var emp in allEmployees)
        {
            Console.WriteLine($"    {emp.Name} - {emp.Title} - ${emp.Salary:N0}");
        }

        var avgSalary = allEmployees.Average(e => e.Salary);
        Console.WriteLine($"\n  Average Salary: ${avgSalary:N0}");
        Console.WriteLine();
    }

    private static void RealWorldExample_ECommerce()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("6. Real-World: E-Commerce Orders");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        {
            "orders": [
                {
                    "orderId": "ORD-001",
                    "customer": "Alice",
                    "items": [
                        {"product": "Laptop", "price": 1200, "quantity": 1},
                        {"product": "Mouse", "price": 25, "quantity": 2}
                    ]
                },
                {
                    "orderId": "ORD-002",
                    "customer": "Bob",
                    "items": [
                        {"product": "Monitor", "price": 400, "quantity": 2},
                        {"product": "Keyboard", "price": 75, "quantity": 1}
                    ]
                },
                {
                    "orderId": "ORD-003",
                    "customer": "Charlie",
                    "items": [
                        {"product": "Desk", "price": 350, "quantity": 1}
                    ]
                }
            ]
        }
        """;

        var allItems = JsonQueryable<OrderItem>.FromString(json, "$.orders[*].items[*]", _options)
            .ToList();

        Console.WriteLine($"  All order items ({allItems.Count} total):");
        
        var productSummary = allItems
            .GroupBy(i => i.Product)
            .Select(g => new
            {
                Product = g.Key,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.Price * i.Quantity)
            })
            .OrderByDescending(s => s.TotalRevenue)
            .ToList();

        foreach (var summary in productSummary)
        {
            Console.WriteLine($"    {summary.Product}: {summary.TotalQuantity} units, ${summary.TotalRevenue:N0} revenue");
        }

        var grandTotal = allItems.Sum(i => i.Price * i.Quantity);
        Console.WriteLine($"\n  Grand Total Revenue: ${grandTotal:N0}");
        Console.WriteLine();
    }

    private static void PathWithLinqFiltering()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("7. JSONPath + LINQ Filtering");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        {
            "stores": [
                {
                    "name": "Store A",
                    "products": [
                        {"id": 1, "name": "Laptop", "price": 1200, "stock": 5},
                        {"id": 2, "name": "Mouse", "price": 25, "stock": 50}
                    ]
                },
                {
                    "name": "Store B",
                    "products": [
                        {"id": 3, "name": "Monitor", "price": 400, "stock": 0},
                        {"id": 4, "name": "Keyboard", "price": 75, "stock": 20}
                    ]
                }
            ]
        }
        """;

        var results = JsonQueryable<Product>.FromString(json, "$.stores[*].products[*]", _options)
            .Where(p => p.Stock > 0 && p.Price < 500)
            .OrderBy(p => p.Price)
            .ToList();

        Console.WriteLine("  In-stock products under $500:");
        foreach (var product in results)
        {
            Console.WriteLine($"    {product.Name} - ${product.Price} (Stock: {product.Stock})");
        }
        Console.WriteLine();
    }

    private static void ComplexNestedStructure()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("8. Complex Nested Structure (Blog Posts with Comments)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var json = """
        {
            "blog": {
                "posts": [
                    {
                        "id": 1,
                        "title": "Getting Started with JSON",
                        "comments": [
                            {"author": "Alice", "text": "Great post!"},
                            {"author": "Bob", "text": "Very helpful"}
                        ]
                    },
                    {
                        "id": 2,
                        "title": "Advanced JSONPath",
                        "comments": [
                            {"author": "Charlie", "text": "Excellent examples"},
                            {"author": "Diana", "text": "Learned a lot"},
                            {"author": "Eve", "text": "Thanks!"}
                        ]
                    }
                ]
            }
        }
        """;

        var allComments = JsonQueryable<Comment>.FromString(json, "$.blog[*].posts[*].comments[*]", _options)
            .ToList();

        Console.WriteLine($"  Found {allComments.Count} comments:");
        
        var commentsByAuthor = allComments
            .GroupBy(c => c.Author)
            .Select(g => new { Author = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        foreach (var authorStat in commentsByAuthor)
        {
            Console.WriteLine($"    {authorStat.Author}: {authorStat.Count} comment(s)");
        }
        Console.WriteLine();
    }

    #region Model Classes

    private class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    private class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    private class Employee
    {
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
    }

    private class Member
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class EmployeeDetail
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Title { get; set; } = "";
        public decimal Salary { get; set; }
    }

    private class OrderItem
    {
        public string Product { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    private class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    private class Comment
    {
        public string Author { get; set; } = "";
        public string Text { get; set; } = "";
    }

    #endregion
}

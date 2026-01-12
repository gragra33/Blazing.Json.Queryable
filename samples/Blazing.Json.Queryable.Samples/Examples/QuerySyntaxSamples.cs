using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Samples.Models;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates LINQ query syntax (query expression syntax) with Blazing.Json.Queryable.
/// Query syntax provides an alternative, SQL-like way to write LINQ queries.
/// Both query syntax and method syntax are fully supported and produce identical results.
/// 
/// This samples file demonstrates all library features with query syntax:
/// - FromString, FromUtf8, FromFile, FromStream
/// - JSONPath pre-filtering with query syntax
/// - Complex grouping and aggregations
/// - Real-world scenarios
/// </summary>
public static class QuerySyntaxSamples
{
    public static void RunAll()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine(" Query Syntax (Query Expression Syntax) Examples");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        BasicQuerySyntax();
        ComparisonWithMethodSyntax();
        SortingWithQuerySyntax();
        GroupingWithQuerySyntax();
        QuerySyntaxWithFromUtf8();
        QuerySyntaxWithFromFile();
        QuerySyntaxWithFromStream().Wait();
        QuerySyntaxWithJSONPath();
        ComplexQueryWithGrouping();
        WhenToUseWhichSyntax();
    }

    private static void BasicQuerySyntax()
    {
        Console.WriteLine("1. Basic Query Syntax (Where + Select)");
        Console.WriteLine("   Query syntax provides a declarative, SQL-like way to query data");
        Console.WriteLine("──────────────────────────────────────────────────────────────────");

        var json = """
        [
            {"Name":"Alice","Age":30,"City":"London","IsActive":true},
            {"Name":"Bob","Age":25,"City":"Paris","IsActive":true},
            {"Name":"Charlie","Age":35,"City":"London","IsActive":false},
            {"Name":"David","Age":28,"City":"Berlin","IsActive":true}
        ]
        """;

        // Query syntax - reads like SQL
        var results = (from p in JsonQueryable<Person>.FromString(json)
                       where p.Age > 25 && p.IsActive
                       select new { p.Name, p.Age, p.City })
                      .ToList();

        Console.WriteLine($"  Found {results.Count} active people over 25:");
        foreach (var person in results)
        {
            Console.WriteLine($"    {person.Name}, {person.Age}, {person.City}");
        }
        Console.WriteLine();
    }

    private static void ComparisonWithMethodSyntax()
    {
        Console.WriteLine("2. Query Syntax vs Method Syntax");
        Console.WriteLine("   Both syntaxes are equivalent - choose what feels natural!");
        Console.WriteLine("──────────────────────────────────────────────────────────────────");

        var json = """
        [
            {"Name":"Alice","Age":30,"City":"London"},
            {"Name":"Bob","Age":25,"City":"Paris"},
            {"Name":"Charlie","Age":35,"City":"London"}
        ]
        """;

        // METHOD SYNTAX (fluent)
        var methodResults = JsonQueryable<Person>.FromString(json)
            .Where(p => p.Age > 25)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Name, p.City })
            .ToList();

        // QUERY SYNTAX (declarative)
        var queryResults = (from p in JsonQueryable<Person>.FromString(json)
                            where p.Age > 25
                            orderby p.Name
                            select new { p.Name, p.City })
                           .ToList();

        Console.WriteLine("  Method syntax results:");
        foreach (var r in methodResults)
        {
            Console.WriteLine($"    {r.Name} from {r.City}");
        }

        Console.WriteLine("\n  Query syntax results (identical!):");
        foreach (var r in queryResults)
        {
            Console.WriteLine($"    {r.Name} from {r.City}");
        }
        Console.WriteLine();
    }

    private static void SortingWithQuerySyntax()
    {
        Console.WriteLine("3. Sorting with Query Syntax");
        Console.WriteLine("   Multi-level sorting is very readable in query syntax");
        Console.WriteLine("──────────────────────────────────────────────────────────────────");

        var json = """
        [
            {"Name":"Laptop Pro","Category":"Computers","Rating":4.5,"Price":1299.99},
            {"Name":"Laptop Air","Category":"Computers","Rating":4.9,"Price":1499.99},
            {"Name":"Wireless Mouse","Category":"Accessories","Rating":4.2,"Price":29.99},
            {"Name":"Mechanical Keyboard","Category":"Accessories","Rating":4.7,"Price":89.99},
            {"Name":"4K Monitor","Category":"Displays","Rating":4.6,"Price":349.99}
        ]
        """;

        // Query syntax with multi-level sorting
        var results = (from p in JsonQueryable<Product>.FromString(json)
                       orderby p.Category, p.Rating descending, p.Price
                       select new { p.Name, p.Category, p.Rating, p.Price })
                      .ToList();

        Console.WriteLine("  Products sorted by Category (asc), Rating (desc), Price (asc):");
        foreach (var product in results)
        {
            Console.WriteLine($"    {product.Name,-25} {product.Category,-15} Rating: {product.Rating} ${product.Price:F2}");
        }
        Console.WriteLine();
    }

    private static void GroupingWithQuerySyntax()
    {
        Console.WriteLine("4. Grouping with Query Syntax");
        Console.WriteLine("   Group by is particularly elegant in query syntax");
        Console.WriteLine("──────────────────────────────────────────────────────────────────");

        var json = """
        [
            {"Name":"Alice","Age":30,"City":"London"},
            {"Name":"Bob","Age":25,"City":"Paris"},
            {"Name":"Charlie","Age":35,"City":"London"},
            {"Name":"David","Age":28,"City":"Paris"},
            {"Name":"Eve","Age":32,"City":"London"}
        ]
        """;

        // Query syntax with grouping
        var results = (from p in JsonQueryable<Person>.FromString(json)
                       group p by p.City into cityGroup
                       select new
                       {
                           City = cityGroup.Key,
                           Count = cityGroup.Count(),
                           AvgAge = cityGroup.Average(p => p.Age),
                           MinAge = cityGroup.Min(p => p.Age),
                           MaxAge = cityGroup.Max(p => p.Age)
                       })
                      .ToList();

        Console.WriteLine("  City-wise summary:");
        foreach (var group in results)
        {
            Console.WriteLine($"    {group.City}:");
            Console.WriteLine($"      Count: {group.Count}");
            Console.WriteLine($"      Avg Age: {group.AvgAge:F1} years");
            Console.WriteLine($"      Age Range: {group.MinAge}-{group.MaxAge}");
        }
        Console.WriteLine();
    }

    private static void QuerySyntaxWithFromUtf8()
    {
        Console.WriteLine("5. Query Syntax with FromUtf8 (Zero-allocation UTF-8)");
        Console.WriteLine("   Process UTF-8 bytes directly without string conversion");
        Console.WriteLine("──────────────────────────────────────────────────────────────────");

        var jsonString = """
        [
            {"Id":1,"Name":"Widget","Price":15.00},
            {"Id":2,"Name":"Gadget","Price":25.00},
            {"Id":3,"Name":"Device","Price":150.00}
        ]
        """;

        byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

        // Query syntax with FromUtf8
        var results = (from p in JsonQueryable<SimpleProduct>.FromUtf8(utf8Bytes)
                       where p.Price < 100
                       orderby p.Price
                       select new { p.Name, p.Price })
                      .ToList();

        Console.WriteLine($"  Affordable products (< $100):");
        foreach (var product in results)
        {
            Console.WriteLine($"    {product.Name}: ${product.Price:F2}");
        }
        Console.WriteLine();
    }

    private static void QuerySyntaxWithFromFile()
    {
        Console.WriteLine("6. Query Syntax with FromFile (Direct file access)");
        Console.WriteLine("   Read JSON directly from file without loading into memory");
        Console.WriteLine("──────────────────────────────────────────────────────────────────");

        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");

        if (File.Exists(jsonPath))
        {
            // Query syntax with FromFile
            var results = (from p in JsonQueryable<Person>.FromFile(jsonPath)
                           where p.Age >= 30
                           orderby p.Name
                           select new { p.Name, p.Age, p.City })
                          .Take(5)
                          .ToList();

            Console.WriteLine($"  People aged 30+ (first 5):");
            foreach (var person in results)
            {
                Console.WriteLine($"    {person.Name}, {person.Age}, {person.City ?? "N/A"}");
            }
        }
        else
        {
            Console.WriteLine("  [Skipped - people.json not found]");
        }
        Console.WriteLine();
    }

    private static async Task QuerySyntaxWithFromStream()
    {
        Console.WriteLine("7. Query Syntax with FromStream (Async streaming)");
        Console.WriteLine("   Stream large files with constant memory usage");
        Console.WriteLine("──────────────────────────────────────────────────────────────────");

        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");

        if (File.Exists(jsonPath))
        {
            await using var stream = File.OpenRead(jsonPath);

            // Query syntax with FromStream + AsAsyncEnumerable
            var results = new List<Person>();
            await foreach (var person in (from p in JsonQueryable<Person>.FromStream(stream)
                                          where p.IsActive
                                          orderby p.Age descending
                                          select p)
                                         .Take(5)
                                         .AsAsyncEnumerable())
            {
                results.Add(person);
            }

            Console.WriteLine($"  Top 5 active people by age:");
            foreach (var person in results)
            {
                Console.WriteLine($"    {person.Name}, Age: {person.Age}");
            }
        }
        else
        {
            Console.WriteLine("  [Skipped - people.json not found]");
        }
        Console.WriteLine();
    }

    private static void QuerySyntaxWithJSONPath()
    {
        Console.WriteLine("8. Query Syntax with JSONPath Pre-filtering");
        Console.WriteLine("   Combine RFC 9535 JSONPath filters with query syntax");
        Console.WriteLine("──────────────────────────────────────────────────────────────────");

        var json = """
        [
            {"Name":"Widget","Price":15.00,"Category":"Tools","Rating":4.5,"Stock":100},
            {"Name":"Gadget","Price":25.00,"Category":"Tools","Rating":4.2,"Stock":200},
            {"Name":"Device","Price":150.00,"Category":"Electronics","Rating":4.8,"Stock":0},
            {"Name":"Doohickey","Price":85.00,"Category":"Tools","Rating":4.6,"Stock":50},
            {"Name":"Gizmo","Price":45.00,"Category":"Electronics","Rating":4.3,"Stock":75}
        ]
        """;

        // JSONPath pre-filter (filters in JSON before deserialization)
        // Note: JSONPath uses case-sensitive property names matching the JSON
        // Then query syntax for grouping and aggregation
        var results = (from p in JsonQueryable<Product>
                           .FromString(json, "$[?@.Price < 100 && @.Stock > 0]")
                       group p by p.Category into catGroup
                       orderby catGroup.Key
                       select new
                       {
                           Category = catGroup.Key,
                           Count = catGroup.Count(),
                           AvgPrice = catGroup.Average(p => p.Price),
                           AvgRating = catGroup.Average(p => p.Rating),
                           Products = catGroup.Select(p => p.Name).OrderBy(n => n).ToList()
                       })
                      .ToList();

        Console.WriteLine("  Affordable in-stock products by category:");
        foreach (var category in results)
        {
            Console.WriteLine($"    {category.Category}:");
            Console.WriteLine($"      Count: {category.Count}, Avg Price: ${category.AvgPrice:F2}, Avg Rating: {category.AvgRating:F1}");
            Console.WriteLine($"      Products: {string.Join(", ", category.Products)}");
        }
        Console.WriteLine();
    }

    private static void ComplexQueryWithGrouping()
    {
        Console.WriteLine("9. Complex Real-World Query - Employee Analysis");
        Console.WriteLine("   Demonstrates advanced query syntax with multiple operations");
        Console.WriteLine("──────────────────────────────────────────────────────────────────");

        var json = """
        [
            {"Id":1,"Name":"Alice Johnson","Department":"Engineering","Salary":95000,"YearsEmployed":5},
            {"Id":2,"Name":"Bob Smith","Department":"Engineering","Salary":105000,"YearsEmployed":8},
            {"Id":3,"Name":"Charlie Brown","Department":"Sales","Salary":75000,"YearsEmployed":3},
            {"Id":4,"Name":"Diana Prince","Department":"Engineering","Salary":120000,"YearsEmployed":10},
            {"Id":5,"Name":"Eve Davis","Department":"Marketing","Salary":70000,"YearsEmployed":2},
            {"Id":6,"Name":"Frank Miller","Department":"Engineering","Salary":98000,"YearsEmployed":6},
            {"Id":7,"Name":"Grace Lee","Department":"Sales","Salary":82000,"YearsEmployed":4}
        ]
        """;

        // Complex query: filter by salary, group by department,
        // filter groups with > 1 employee, order by avg salary
        var results = (from e in JsonQueryable<Employee>.FromString(json)
                       where e.Salary > 60000
                       group e by e.Department into deptGroup
                       where deptGroup.Count() > 1
                       orderby deptGroup.Average(e => e.Salary) descending
                       select new
                       {
                           Department = deptGroup.Key,
                           EmployeeCount = deptGroup.Count(),
                           AvgSalary = deptGroup.Average(e => e.Salary),
                           TotalYearsExperience = deptGroup.Sum(e => e.YearsEmployed),
                           TopEarner = deptGroup.OrderByDescending(e => e.Salary).First().Name,
                           TopSalary = deptGroup.Max(e => e.Salary)
                       })
                      .ToList();

        Console.WriteLine("  Department workforce analysis:");
        foreach (var dept in results)
        {
            Console.WriteLine($"    {dept.Department}:");
            Console.WriteLine($"      Employees: {dept.EmployeeCount}");
            Console.WriteLine($"      Avg Salary: ${dept.AvgSalary:N0}");
            Console.WriteLine($"      Total Experience: {dept.TotalYearsExperience} years");
            Console.WriteLine($"      Top Earner: {dept.TopEarner} (${dept.TopSalary:N0})");
        }
        Console.WriteLine();
    }

    private static void WhenToUseWhichSyntax()
    {
        Console.WriteLine("10. When to Use Query Syntax vs Method Syntax");
        Console.WriteLine("──────────────────────────────────────────────────────────────────");

        Console.WriteLine("\n  * USE QUERY SYNTAX WHEN:");
        Console.WriteLine("    - Complex queries with joins and grouping (more SQL-like)");
        Console.WriteLine("    - Multiple 'from' clauses (SelectMany scenarios)");
        Console.WriteLine("    - Team prefers declarative style");
        Console.WriteLine("    - Using 'let' keyword for intermediate results");

        Console.WriteLine("\n  * USE METHOD SYNTAX WHEN:");
        Console.WriteLine("    - Simple filtering and projection");
        Console.WriteLine("    - Chaining many operations (more fluent)");
        Console.WriteLine("    - Using methods not in query syntax (Take, Skip, Distinct)");
        Console.WriteLine("    - Personal/team preference for fluent style");

        Console.WriteLine("\n  BEST PRACTICE:");
        Console.WriteLine("    -> Both syntaxes are fully supported and equivalent");
        Console.WriteLine("    -> Choose what makes code most readable for your scenario");
        Console.WriteLine("    -> Can mix: query syntax + .Take(10) method at the end");

        Console.WriteLine("\n  LEARN MORE:");
        Console.WriteLine("    - Microsoft Docs: Query expression basics");
        Console.WriteLine("      https://learn.microsoft.com/en-us/dotnet/csharp/linq/get-started/query-expression-basics");
        Console.WriteLine("    - Microsoft Docs: Query syntax vs method syntax");
        Console.WriteLine("      https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/query-syntax-and-method-syntax-in-linq");
        Console.WriteLine();
    }
}

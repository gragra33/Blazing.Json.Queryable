using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Samples.Models;
using System.Diagnostics;
using System.Text;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates UTF-8 optimized queries using Blazing.Json.Queryable.
/// Shows performance benefits of using FromUtf8 vs FromString.
/// </summary>
public static class Utf8Queries
{
    public static void RunAll()
    {
        Console.WriteLine("=== UTF-8 Optimized Queries Examples ===\n");
        
        FromUtf8Example();
        FromFileExample();
        PerformanceComparison();
        DirectByteArrayQuery();
        
        Console.WriteLine("\n=== UTF-8 Queries Complete ===\n");
    }
    
    /// <summary>
    /// Basic FromUtf8 query using byte array.
    /// </summary>
    private static void FromUtf8Example()
    {
        Console.WriteLine("1. FromUtf8 Basic Query");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        var utf8Bytes = File.ReadAllBytes(jsonPath); // Already UTF-8
        
        var results = JsonQueryable<Person>.FromUtf8(utf8Bytes)
            .Where(p => p.City == "London")
            .OrderBy(p => p.Age)
            .Select(p => new { p.Name, p.Age })
            .ToList();
        
        Console.WriteLine($"   People in London ({results.Count}):");
        foreach (var person in results)
        {
            Console.WriteLine($"   - {person.Name}, Age: {person.Age}");
        }
        Console.WriteLine();
    }
    
    /// <summary>
    /// FromFile query (direct UTF-8 read).
    /// </summary>
    private static void FromFileExample()
    {
        Console.WriteLine("2. FromFile Query (Direct UTF-8 Read)");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "products.json");
        
        var results = JsonQueryable<Product>.FromFile(jsonPath)
            .Where(p => p.Category == "Accessories")
            .OrderByDescending(p => p.Rating)
            .Take(5)
            .ToList();
        
        Console.WriteLine($"   Top 5 accessories by rating:");
        foreach (var product in results)
        {
            Console.WriteLine($"   - {product.Name}: ${product.Price:F2} (Rating: {product.Rating})");
        }
        Console.WriteLine();
    }
    
    /// <summary>
    /// Performance comparison: FromString vs FromUtf8 vs FromFile.
    /// </summary>
    private static void PerformanceComparison()
    {
        Console.WriteLine("3. Performance Comparison (FromString vs FromUtf8 vs FromFile)");
        Console.WriteLine("   Running 100 iterations of the same query...\n");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "orders.json");
        var jsonString = File.ReadAllText(jsonPath);
        var utf8Bytes = File.ReadAllBytes(jsonPath);
        
        const int iterations = 100;
        
        // Warmup
        _ = JsonQueryable<Order>.FromString(jsonString).Where(o => o.Status == "Shipped").Count();
        _ = JsonQueryable<Order>.FromUtf8(utf8Bytes).Where(o => o.Status == "Shipped").Count();
        _ = JsonQueryable<Order>.FromFile(jsonPath).Where(o => o.Status == "Shipped").Count();
        
        // FromString benchmark
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var count = JsonQueryable<Order>.FromString(jsonString)
                .Where(o => o.Status == "Shipped")
                .OrderBy(o => o.OrderDate)
                .Count();
        }
        sw1.Stop();
        var fromStringMs = sw1.Elapsed.TotalMilliseconds;
        
        // FromUtf8 benchmark
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var count = JsonQueryable<Order>.FromUtf8(utf8Bytes)
                .Where(o => o.Status == "Shipped")
                .OrderBy(o => o.OrderDate)
                .Count();
        }
        sw2.Stop();
        var fromUtf8Ms = sw2.Elapsed.TotalMilliseconds;
        
        // FromFile benchmark
        var sw3 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var count = JsonQueryable<Order>.FromFile(jsonPath)
                .Where(o => o.Status == "Shipped")
                .OrderBy(o => o.OrderDate)
                .Count();
        }
        sw3.Stop();
        var fromFileMs = sw3.Elapsed.TotalMilliseconds;
        
        Console.WriteLine($"   FromString:  {fromStringMs:F2}ms (Baseline)");
        Console.WriteLine($"   FromUtf8:    {fromUtf8Ms:F2}ms ({(fromUtf8Ms / fromStringMs - 1) * 100:+0.0;-0.0}% vs Baseline)");
        Console.WriteLine($"   FromFile:    {fromFileMs:F2}ms ({(fromFileMs / fromStringMs - 1) * 100:+0.0;-0.0}% vs Baseline)");
        
        Console.WriteLine("\n   * FromUtf8 avoids UTF-16 to UTF-8 conversion overhead");
        Console.WriteLine("   * FromFile reads directly as UTF-8 bytes (best for file sources)");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Direct byte array manipulation query.
    /// </summary>
    private static void DirectByteArrayQuery()
    {
        Console.WriteLine("4. Direct Byte Array Query (HTTP Response Scenario)");
        
        // Simulate receiving UTF-8 bytes from HTTP response
        var jsonString = """
            [
                {"id":1,"name":"Product A","price":99.99,"inStock":true},
                {"id":2,"name":"Product B","price":149.99,"inStock":false},
                {"id":3,"name":"Product C","price":79.99,"inStock":true}
            ]
            """;
        
        var utf8Bytes = Encoding.UTF8.GetBytes(jsonString);
        
        Console.WriteLine("   Simulating HTTP response with UTF-8 bytes...");
        
        var results = JsonQueryable<SimpleProduct>.FromUtf8(utf8Bytes)
            .Where(p => p.InStock)
            .Select(p => new { p.Name, p.Price })
            .ToList();
        
        Console.WriteLine($"   Products in stock ({results.Count}):");
        foreach (var product in results)
        {
            Console.WriteLine($"   - {product.Name}: ${product.Price:F2}");
        }
        
        Console.WriteLine("\n   * Zero overhead - bytes used directly without conversion");
        Console.WriteLine("   * Ideal for API responses, file reads, network streams");
        Console.WriteLine();
    }
}

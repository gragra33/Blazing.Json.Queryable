using Blazing.Json.Queryable.Providers;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates advanced query scenarios including complex predicates,
/// nested property access, error handling, and performance monitoring.
/// </summary>
public static class AdvancedScenarios
{
    public static async Task RunAllAsync()
    {
        Console.WriteLine("=== Advanced Scenarios Examples ===\n");
        
        ComplexPredicates();
        MultipleOrderBy();
        ErrorHandling();
        await PerformanceMonitoring();
        DynamicFiltering();
        
        Console.WriteLine("\n=== Advanced Scenarios Complete ===\n");
    }
    
    /// <summary>
    /// Complex predicates with multiple conditions.
    /// </summary>
    private static void ComplexPredicates()
    {
        Console.WriteLine("1. Complex Predicates (Multiple Conditions)");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        var json = File.ReadAllText(jsonPath);
        
        var results = JsonQueryable<Person>.FromString(json)
            .Where(p => 
                (p.Age >= 25 && p.Age <= 35) && 
                (p.City == "London" || p.City == "New York") &&
                p.IsActive &&
                p.Name.Contains("a", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.City)
            .ThenBy(p => p.Age)
            .ToList();
        
        Console.WriteLine($"   Active 25-35yo in London/NY with 'a' in name ({results.Count}):");
        foreach (var person in results)
        {
            Console.WriteLine($"   - {person.Name}, {person.Age} ({person.City})");
        }
        Console.WriteLine();
    }
    
    /// <summary>
    /// Multiple OrderBy with ThenBy operations.
    /// </summary>
    private static void MultipleOrderBy()
    {
        Console.WriteLine("2. Multiple Sorting Levels (OrderBy + ThenBy + ThenBy)");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "products.json");
        var json = File.ReadAllText(jsonPath);
        
        var results = JsonQueryable<Product>.FromString(json)
            .OrderBy(p => p.Category)
            .ThenByDescending(p => p.Rating)
            .ThenBy(p => p.Price)
            .Select(p => new
            {
                p.Name,
                p.Category,
                p.Price,
                p.Rating
            })
            .ToList();
        
        Console.WriteLine($"   Products sorted by Category, Rating (desc), Price ({results.Count}):");
        var currentCategory = "";
        foreach (var product in results)
        {
            if (product.Category != currentCategory)
            {
                currentCategory = product.Category;
                Console.WriteLine($"\n   {currentCategory}:");
            }
            Console.WriteLine($"     - {product.Name}: ${product.Price:F2} (rating: {product.Rating})");
        }
        Console.WriteLine();
    }
    
    /// <summary>
    /// Error handling for various scenarios.
    /// </summary>
    private static void ErrorHandling()
    {
        Console.WriteLine("3. Error Handling Examples");
        
        // Invalid JSON
        Console.WriteLine("   a) Invalid JSON:");
        try
        {
            var invalidJson = "{ invalid json }";
            var _ = JsonQueryable<Person>.FromString(invalidJson).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      * Caught {ex.GetType().Name}: {ex.Message.Split('\n')[0]}");
        }
        
        // Empty results
        Console.WriteLine("\n   b) Empty results:");
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        var json = File.ReadAllText(jsonPath);
        
        var emptyResults = JsonQueryable<Person>.FromString(json)
            .Where(p => p.Age > 1000)
            .ToList();
        Console.WriteLine($"      * Empty query returned {emptyResults.Count} results (no exception)");
        
        // FirstOrDefault vs First
        Console.WriteLine("\n   c) FirstOrDefault vs First:");
        var nullResult = JsonQueryable<Person>.FromString(json)
            .Where(p => p.City == "Mars")
            .FirstOrDefault();
        Console.WriteLine($"      * FirstOrDefault returned: {nullResult?.Name ?? "null (safe)"}");
        
        try
        {
            var _ = JsonQueryable<Person>.FromString(json)
                .Where(p => p.City == "Mars")
                .First();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"      * First threw exception: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    /// <summary>
    /// Performance monitoring and profiling.
    /// </summary>
    private static async Task PerformanceMonitoring()
    {
        Console.WriteLine("4. Performance Monitoring");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "orders.json");
        
        // Monitor different query approaches
        var metrics = new List<(string Approach, long TimeMs, long MemoryKB)>();
        
        // Approach 1: String-based
        var memBefore1 = GC.GetTotalMemory(true);
        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        
        var json = await File.ReadAllTextAsync(jsonPath);
        var count1 = JsonQueryable<Order>
            .FromString(json)
            .Where(o => o.Price > 100)
            .Count();
        
        sw1.Stop();
        var memAfter1 = GC.GetTotalMemory(false);
        metrics.Add(("FromString", sw1.ElapsedMilliseconds, (memAfter1 - memBefore1) / 1024));
        
        // Approach 2: UTF-8
        GC.Collect();
        var memBefore2 = GC.GetTotalMemory(true);
        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        
        var utf8Bytes = await File.ReadAllBytesAsync(jsonPath);
        var count2 = JsonQueryable<Order>
            .FromUtf8(utf8Bytes)
            .Where(o => o.Price > 100)
            .Count();
        
        sw2.Stop();
        var memAfter2 = GC.GetTotalMemory(false);
        metrics.Add(("FromUtf8", sw2.ElapsedMilliseconds, (memAfter2 - memBefore2) / 1024));
        
        // Approach 3: Stream
        GC.Collect();
        var memBefore3 = GC.GetTotalMemory(true);
        var sw3 = System.Diagnostics.Stopwatch.StartNew();
        
        using (var stream = File.OpenRead(jsonPath))
        {
            var count3 = JsonQueryable<Order>
                .FromStream(stream)
                .Where(o => o.Price > 100)
                .Count();
        }
        
        sw3.Stop();
        var memAfter3 = GC.GetTotalMemory(false);
        metrics.Add(("FromStream", sw3.ElapsedMilliseconds, (memAfter3 - memBefore3) / 1024));
        
        // Approach 4: Async
        GC.Collect();
        var memBefore4 = GC.GetTotalMemory(true);
        var sw4 = System.Diagnostics.Stopwatch.StartNew();
        
        await using (var stream = File.OpenRead(jsonPath))
        {
            var count4 = 0;
            await foreach (var order in JsonQueryable<Order>.FromStream(stream)
                .Where(o => o.Price > 100)
                .AsAsyncEnumerable())
            {
                count4++;
            }
        }
        
        sw4.Stop();
        var memAfter4 = GC.GetTotalMemory(false);
        metrics.Add(("Async", sw4.ElapsedMilliseconds, (memAfter4 - memBefore4) / 1024));
        
        Console.WriteLine("   Performance metrics (same query, different approaches):\n");
        Console.WriteLine("   Approach       Time (ms)    Memory (KB)");
        Console.WriteLine("   ──────────────────────────────────────────");
        foreach (var (approach, time, memory) in metrics)
        {
            Console.WriteLine($"   {approach,-14} {time,5}        {memory,6}");
        }
        
        Console.WriteLine("\n   * Performance metrics collected successfully");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Dynamic filtering based on runtime conditions.
    /// </summary>
    private static void DynamicFiltering()
    {
        Console.WriteLine("5. Dynamic Filtering (Runtime Conditions)");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "products.json");
        var json = File.ReadAllText(jsonPath);
        
        // Simulate user input
        var filterCategory = "Accessories";
        var minPrice = 20m;
        var maxPrice = 100m;
        var minRating = 4.0;
        
        var query = JsonQueryable<Product>.FromString(json);
        
        // Build query dynamically
        if (!string.IsNullOrEmpty(filterCategory))
        {
            query = query.Where(p => p.Category == filterCategory);
        }
        
        if (minPrice > 0)
        {
            query = query.Where(p => p.Price >= minPrice);
        }
        
        if (maxPrice > 0)
        {
            query = query.Where(p => p.Price <= maxPrice);
        }
        
        if (minRating > 0)
        {
            query = query.Where(p => p.Rating >= minRating);
        }
        
        var results = query
            .OrderByDescending(p => p.Rating)
            .Select(p => new
            {
                p.Name,
                p.Price,
                p.Rating
            })
            .ToList();
        
        Console.WriteLine($"   Filters applied:");
        Console.WriteLine($"     Category:   {filterCategory}");
        Console.WriteLine($"     Price:      ${minPrice}-${maxPrice}");
        Console.WriteLine($"     Min Rating: {minRating}");
        Console.WriteLine($"\n   Results ({results.Count}):");
        
        foreach (var product in results)
        {
            Console.WriteLine($"   - {product.Name}: {new string(' ', 20 - product.Name.Length)} ${product.Price:F2} (rating: {product.Rating})");
        }
        
        Console.WriteLine("\n   * Dynamic query building successful");
        Console.WriteLine();
    }
}

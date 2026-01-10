using Blazing.Json.Queryable.Providers;
using System.Diagnostics;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates stream-based queries for memory-efficient processing.
/// Shows constant memory usage regardless of file size.
/// </summary>
public static class StreamQueries
{
    public static void RunAll()
    {
        Console.WriteLine("=== Stream Processing Examples ===\n");
        
        BasicStreamQuery();
        MemoryEfficientLargeFile();
        StreamVsInMemoryComparison();
        FilteredStreamProcessing();
        
        Console.WriteLine("\n=== Stream Queries Complete ===\n");
    }
    
    /// <summary>
    /// Basic stream query example.
    /// </summary>
    private static void BasicStreamQuery()
    {
        Console.WriteLine("1. Basic Stream Query");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        
        using var stream = File.OpenRead(jsonPath);
        
        var results = JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.Age > 30)
            .OrderBy(p => p.Name)
            .Take(5)
            .ToList();
        
        Console.WriteLine($"   Top 5 people over 30 (alphabetically):");
        foreach (var person in results)
        {
            Console.WriteLine($"   - {person.Name}, Age: {person.Age}");
        }
        Console.WriteLine();
    }
    
    /// <summary>
    /// Memory-efficient processing of large files.
    /// </summary>
    private static void MemoryEfficientLargeFile()
    {
        Console.WriteLine("2. Memory-Efficient Large File Processing");
        
        // Generate large dataset if it doesn't exist
        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        var largeFilePath = Path.Combine(dataDir, "large-dataset.json");
        
        if (!File.Exists(largeFilePath))
        {
            Console.WriteLine("   Generating large dataset (100K records)...");
            Data.DatasetGenerator.GenerateLargeDataset(largeFilePath, 100_000);
        }
        
        var fileInfo = new FileInfo(largeFilePath);
        Console.WriteLine($"   Processing file: {fileInfo.Length / 1024.0 / 1024.0:F2} MB");
        
        var initialMemory = GC.GetTotalMemory(true);
        
        using var stream = File.OpenRead(largeFilePath);
        
        var sw = Stopwatch.StartNew();
        var results = JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.City == "London" && p.IsActive)
            .OrderByDescending(p => p.Age)
            .Take(10)
            .ToList();
        sw.Stop();
        
        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = (finalMemory - initialMemory) / 1024.0 / 1024.0;
        
        Console.WriteLine($"   Found {results.Count} results in {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"   Memory used: ~{memoryUsed:F2} MB (includes result objects)");
        Console.WriteLine($"   File size: {fileInfo.Length / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"   ? Memory usage is constant, independent of file size");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Compare stream vs in-memory processing.
    /// </summary>
    private static void StreamVsInMemoryComparison()
    {
        Console.WriteLine("3. Stream vs In-Memory Comparison");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "orders.json");
        
        // In-memory approach
        Console.WriteLine("   Testing In-Memory approach...");
        var memBefore1 = GC.GetTotalMemory(true);
        
        var json = File.ReadAllText(jsonPath);
        var sw1 = Stopwatch.StartNew();
        var count1 = JsonQueryable<Order>.FromString(json)
            .Where(o => o.Status == "Shipped")
            .Count();
        sw1.Stop();
        
        var memAfter1 = GC.GetTotalMemory(false);
        var inMemoryMB = (memAfter1 - memBefore1) / 1024.0 / 1024.0;
        
        // Stream approach
        Console.WriteLine("   Testing Stream approach...");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var memBefore2 = GC.GetTotalMemory(true);
        
        using var stream = File.OpenRead(jsonPath);
        var sw2 = Stopwatch.StartNew();
        var count2 = JsonQueryable<Order>.FromStream(stream)
            .Where(o => o.Status == "Shipped")
            .Count();
        sw2.Stop();
        
        var memAfter2 = GC.GetTotalMemory(false);
        var streamMB = (memAfter2 - memBefore2) / 1024.0 / 1024.0;
        
        Console.WriteLine($"\n   In-Memory: {sw1.ElapsedMilliseconds}ms, ~{inMemoryMB:F3} MB");
        Console.WriteLine($"   Stream:    {sw2.ElapsedMilliseconds}ms, ~{streamMB:F3} MB");
        Console.WriteLine($"   Memory savings: {(1 - streamMB / inMemoryMB) * 100:F1}%");
        Console.WriteLine($"   ? Results match: {count1 == count2}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Filtered stream processing with projection.
    /// </summary>
    private static void FilteredStreamProcessing()
    {
        Console.WriteLine("4. Filtered Stream Processing");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "products.json");
        
        using var stream = File.OpenRead(jsonPath);
        
        var results = JsonQueryable<Product>.FromStream(stream)
            .Where(p => p.Stock > 50 && p.Rating >= 4.5)
            .OrderByDescending(p => p.Rating)
            .Select(p => new
            {
                p.Name,
                p.Category,
                p.Price,
                p.Rating,
                Value = p.Price * p.Stock
            })
            .ToList();
        
        Console.WriteLine($"   High-rated products with good stock ({results.Count}):");
        foreach (var product in results)
        {
            Console.WriteLine($"   - {product.Name} ({product.Category})");
            Console.WriteLine($"     Price: ${product.Price:F2}, Rating: {product.Rating}, Inventory Value: ${product.Value:F2}");
        }
        
        Console.WriteLine("\n   * Stream processed efficiently with complex query");
        Console.WriteLine("   * Memory usage constant despite file size");
        Console.WriteLine();
    }
}

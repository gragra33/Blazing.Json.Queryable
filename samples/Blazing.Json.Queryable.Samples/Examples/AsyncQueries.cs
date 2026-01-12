using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Samples.Models;
using System.Diagnostics;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates async enumeration and async LINQ operations using .NET 10's System.Linq.AsyncEnumerable.
/// Shows proper cancellation token usage and async I/O benefits.
/// </summary>
public static class AsyncQueries
{
    public static async Task RunAllAsync()
    {
        Console.WriteLine("=== Async Query Examples ===\n");
        
        await BasicAsyncEnumeration();
        await AsyncWithCancellation();
        await AsyncLinqOperations();
        await AsyncLargeFileProcessing();
        await ParallelAsyncProcessing();
        
        Console.WriteLine("\n=== Async Queries Complete ===\n");
    }
    
    /// <summary>
    /// Basic async enumeration with await foreach.
    /// </summary>
    private static async Task BasicAsyncEnumeration()
    {
        Console.WriteLine("1. Basic Async Enumeration (await foreach)");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        
        await using var stream = File.OpenRead(jsonPath);
        
        var count = 0;
        await foreach (var person in JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .AsAsyncEnumerable())
        {
            count++;
            if (count <= 5)
            {
                Console.WriteLine($"   - {person.Name} ({person.City})");
            }
        }
        
        Console.WriteLine($"   Total active people: {count}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Async enumeration with cancellation token.
    /// </summary>
    private static async Task AsyncWithCancellation()
    {
        Console.WriteLine("2. Async with Cancellation Token");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "orders.json");
        
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100)); // Cancel after 100ms
        
        try
        {
            await using var stream = File.OpenRead(jsonPath);
            
            var count = 0;
            await foreach (var order in JsonQueryable<Order>.FromStream(stream)
                .Where(o => o.Status == "Shipped")
                .AsAsyncEnumerable()
                .WithCancellation(cts.Token))
            {
                count++;
                await Task.Delay(10, cts.Token); // Simulate async work
            }
            
            Console.WriteLine($"   Processed {count} orders");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("   * Operation cancelled gracefully");
        }
        
        Console.WriteLine("   * Cancellation token properly handled");
        Console.WriteLine();
    }
    
    /// <summary>
    /// .NET 10 async LINQ operations with async predicates.
    /// </summary>
    private static async Task AsyncLinqOperations()
    {
        Console.WriteLine("3. Async LINQ Operations (.NET 10)");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "products.json");
        
        await using var stream = File.OpenRead(jsonPath);
        
        // Using .NET 10's async LINQ extensions
        var results = new List<Product>();
        await foreach (var product in JsonQueryable<Product>.FromStream(stream)
            .AsAsyncEnumerable()
            .Where(async (p, ct) => await IsHighValueProductAsync(p, ct))
            .OrderByDescending(p => p.Rating)
            .Take(5))
        {
            results.Add(product);
        }
        
        Console.WriteLine($"   High-value products ({results.Count}):");
        foreach (var product in results)
        {
            Console.WriteLine($"   - {product.Name}: ${product.Price:F2} (Rating: {product.Rating})");
        }
        
        Console.WriteLine("\n   * Async predicates work with .NET 10 async LINQ");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Async processing of large files with progress reporting.
    /// </summary>
    private static async Task AsyncLargeFileProcessing()
    {
        Console.WriteLine("4. Async Large File Processing with Progress");
        
        // Generate large dataset if it doesn't exist
        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        var largeFilePath = Path.Combine(dataDir, "large-dataset.json");
        
        if (!File.Exists(largeFilePath))
        {
            Console.WriteLine("   Generating large dataset (100K records)...");
            Data.DatasetGenerator.GenerateLargeDataset(largeFilePath, 100_000);
        }
        
        var fileInfo = new FileInfo(largeFilePath);
        Console.WriteLine($"   Processing file: {fileInfo.Length / 1024.0 / 1024.0:F2} MB asynchronously");
        
        var sw = Stopwatch.StartNew();
        var count = 0;
        var matched = 0;
        
        await using var stream = File.OpenRead(largeFilePath);
        
        await foreach (var person in JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.City == "Tokyo")
            .AsAsyncEnumerable())
        {
            count++;
            matched++;
            
            // Simulate async I/O work (e.g., database insert, API call)
            if (count % 100 == 0)
            {
                await Task.Delay(1); // Brief async work
            }
            
            // Progress reporting
            if (matched % 1000 == 0)
            {
                Console.WriteLine($"   Processed {matched:N0} matching records...");
            }
        }
        
        sw.Stop();
        
        Console.WriteLine($"\n   * Processed {matched:N0} records in {sw.ElapsedMilliseconds:N0}ms");
        Console.WriteLine("   * Async I/O allows non-blocking processing");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Parallel async processing demonstration.
    /// </summary>
    private static async Task ParallelAsyncProcessing()
    {
        Console.WriteLine("5. Parallel Async Processing");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "orders.json");
        
        await using var stream = File.OpenRead(jsonPath);
        
        var tasks = new List<Task<decimal>>();
        
        await foreach (var order in JsonQueryable<Order>.FromStream(stream)
            .Where(o => o.Status == "Delivered")
            .AsAsyncEnumerable())
        {
            // Process each order asynchronously (simulated)
            tasks.Add(ProcessOrderAsync(order));
        }
        
        var results = await Task.WhenAll(tasks);
        var totalRevenue = results.Sum();
        
        Console.WriteLine($"   Processed {tasks.Count:N0} delivered orders in parallel");
        Console.WriteLine($"   Total revenue: ${totalRevenue:N2}");
        Console.WriteLine("\n   * Async enumeration enables parallel processing");
        Console.WriteLine("   * Non-blocking I/O improves throughput");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Simulates async validation of a product.
    /// </summary>
    private static async Task<bool> IsHighValueProductAsync(Product product, CancellationToken ct)
    {
        await Task.Delay(1, ct); // Simulate async API call or database lookup
        return product is { Price: > 100, Stock: > 20 };
    }
    
    /// <summary>
    /// Simulates async order processing.
    /// </summary>
    private static async Task<decimal> ProcessOrderAsync(Order order)
    {
        await Task.Delay(5); // Simulate async work (API call, database write, etc.)
        return order.Price * order.Quantity;
    }
}

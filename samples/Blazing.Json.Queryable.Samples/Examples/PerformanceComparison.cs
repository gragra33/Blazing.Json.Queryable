using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Samples.Models;
using System.Diagnostics;
using System.Text.Json;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates REALISTIC performance comparisons between traditional approaches and Blazing.Json.Queryable.
/// Uses proper benchmarking techniques: warmup, multiple iterations, averaging.
/// </summary>
public static class PerformanceComparison
{
    public static async Task RunAllAsync()
    {
        Console.WriteLine("=== Performance Comparison Examples ===\n");
        
        SmallDataComparison();
        await LargeFileEarlyTerminationAsync();
        await HugeFileStreamingShowcaseAsync();
        ComparisonSummary();
        
        Console.WriteLine("\n=== Performance Comparisons Complete ===\n");
    }
    
    /// <summary>
    /// Small data comparison with proper averaging.
    /// </summary>
    private static void SmallDataComparison()
    {
        Console.WriteLine("1. Small In-Memory Data (10 records)");
        Console.WriteLine("   WARNING: For small data, traditional LINQ is typically better!\n");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        var jsonString = File.ReadAllText(jsonPath);
        
        const int warmupIterations = 100;
        const int iterations = 1000;
        
        // Warmup
        for (int i = 0; i < warmupIterations; i++)
        {
            var _ = JsonSerializer.Deserialize<List<Person>>(jsonString)!.Where(p => p.Age > 25).ToList();
            var __ = JsonQueryable<Person>.FromString(jsonString).Where(p => p.Age > 25).ToList();
        }
        
        // Measure Traditional
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var people = JsonSerializer.Deserialize<List<Person>>(jsonString)!;
            var filtered = people.Where(p => p.Age > 25).OrderBy(p => p.Name).ToList();
        }
        sw1.Stop();
        var traditionalAvg = sw1.ElapsedMilliseconds / (double)iterations;
        
        // Measure JsonQueryable
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var filtered = JsonQueryable<Person>.FromString(jsonString)
                .Where(p => p.Age > 25)
                .OrderBy(p => p.Name)
                .ToList();
        }
        sw2.Stop();
        var jsonQueryableAvg = sw2.ElapsedMilliseconds / (double)iterations;
        
        Console.WriteLine($"   Traditional LINQ:  {traditionalAvg:F3}ms average ({iterations} iterations)");
        Console.WriteLine($"   JsonQueryable:     {jsonQueryableAvg:F3}ms average ({iterations} iterations)");
        
        if (traditionalAvg < jsonQueryableAvg)
        {
            var ratio = jsonQueryableAvg / traditionalAvg;
            Console.WriteLine($"\n   >> Traditional is {ratio:F1}x faster (expected for small data)");
            Console.WriteLine("   >> Use traditional JsonSerializer + LINQ for datasets < 1MB");
        }
        else
        {
            var ratio = traditionalAvg / jsonQueryableAvg;
            Console.WriteLine($"\n   >> JsonQueryable is {ratio:F1}x faster (unusual - may be measurement noise)");
        }
        
        Console.WriteLine("\n   KEY TAKEAWAY:");
        Console.WriteLine("   * For small data (<1MB), use traditional JsonSerializer + LINQ");
        Console.WriteLine("   * JsonQueryable excels with LARGE files + EARLY TERMINATION");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Large file with early termination - multiple runs for accuracy.
    /// </summary>
    private static async Task LargeFileEarlyTerminationAsync()
    {
        Console.WriteLine("2. Large File with Early Exit - THE KILLER FEATURE!");
        Console.WriteLine("   Scenario: 100K records, find first 10 matching\n");
        
        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        var largeFilePath = Path.Combine(dataDir, "large-dataset.json");
        
        if (!File.Exists(largeFilePath))
        {
            Console.WriteLine("   Generating large dataset (100K records)...");
            Data.DatasetGenerator.GenerateLargeDataset(largeFilePath, 100_000);
        }
        
        var fileInfo = new FileInfo(largeFilePath);
        Console.WriteLine($"   File size: {fileInfo.Length / 1024.0 / 1024.0:F2} MB");
        
        const int iterations = 10;
        
        // Warmup
        {
            var json = await File.ReadAllTextAsync(largeFilePath);
            var _ = JsonSerializer.Deserialize<List<Person>>(json)!.Where(p => p.Age > 25).Take(10).ToList();
        }
        
        await using (var stream = File.OpenRead(largeFilePath))
        {
            var __ = new List<Person>();
            await foreach (var p in JsonQueryable<Person>.FromStream(stream).Where(p => p.Age > 25).Take(10).AsAsyncEnumerable())
            {
                __.Add(p);
            }
        }
        
        // Measure Traditional
        long traditionalTotal = 0;
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var json = await File.ReadAllTextAsync(largeFilePath);
            var all = JsonSerializer.Deserialize<List<Person>>(json)!;
            var results = all.Where(p => p.Age > 25 && p.City == "London").Take(10).ToList();
            sw.Stop();
            traditionalTotal += sw.ElapsedMilliseconds;
        }
        var traditionalAvg = traditionalTotal / (double)iterations;
        
        // Measure Async Streaming
        long streamingTotal = 0;
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var results = new List<Person>();
            await using (var stream = File.OpenRead(largeFilePath))
            {
                await foreach (var person in JsonQueryable<Person>.FromStream(stream)
                    .Where(p => p.Age > 25 && p.City == "London")
                    .Take(10)
                    .AsAsyncEnumerable())
                {
                    results.Add(person);
                }
            }
            sw.Stop();
            streamingTotal += sw.ElapsedMilliseconds;
        }
        var streamingAvg = streamingTotal / (double)iterations;
        
        Console.WriteLine($"\n   Traditional (Load All):  {traditionalAvg:F1}ms average");
        Console.WriteLine($"   Async Streaming:         {streamingAvg:F1}ms average");
        Console.WriteLine($"   ({iterations} iterations each)");
        
        if (streamingAvg < traditionalAvg)
        {
            var ratio = traditionalAvg / streamingAvg;
            var improvement = (1 - streamingAvg / traditionalAvg) * 100;
            Console.WriteLine($"\n   RESULTS:");
            Console.WriteLine($"   * Streaming is {ratio:F1}x faster ({improvement:F1}% improvement)");
            Console.WriteLine("   * Stops reading after finding 10 matches (doesn't process 100K records)");
        }
        else
        {
            var ratio = streamingAvg / traditionalAvg;
            Console.WriteLine($"\n   RESULTS:");
            Console.WriteLine($"   * Streaming is {ratio:F1}x slower for this workload");
            Console.WriteLine("   * Memory benefit still exists (doesn't load entire file)");
        }
        
        Console.WriteLine("\n   KEY INSIGHT:");
        Console.WriteLine("   * Early termination (Take) is where streaming shines");
        Console.WriteLine("   * The fewer matches needed, the bigger the speedup");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Huge file showcase.
    /// </summary>
    private static async Task HugeFileStreamingShowcaseAsync()
    {
        Console.WriteLine("3. Huge Dataset Streaming (1 MILLION Records)");
        Console.WriteLine("   Scenario: Find first 100 active users in New York\n");
        
        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        var hugeFilePath = Path.Combine(dataDir, "huge-dataset.json");
        
        if (!File.Exists(hugeFilePath))
        {
            Console.WriteLine("   Generating huge dataset (1M records)... this may take 30-60 seconds");
            Data.DatasetGenerator.GenerateLargeDataset(hugeFilePath, 1_000_000);
        }
        
        var fileInfo = new FileInfo(hugeFilePath);
        Console.WriteLine($"   File size: {fileInfo.Length / 1024.0 / 1024.0:F2} MB");
        
        Console.WriteLine("\n   Traditional approach would:");
        Console.WriteLine($"   * Load ~{fileInfo.Length / 1024.0 / 1024.0:F0} MB into RAM");
        Console.WriteLine("   * Deserialize all 1M records");
        Console.WriteLine("   * Risk OutOfMemoryException on memory-constrained systems");
        
        // Warmup
        await using (var stream = File.OpenRead(hugeFilePath))
        {
            var warmup = new List<Person>();
            await foreach (var p in JsonQueryable<Person>.FromStream(stream)
                .Where(p => p.IsActive && p.City == "New York")
                .Take(100)
                .AsAsyncEnumerable())
            {
                warmup.Add(p);
                if (warmup.Count >= 10) break; // Partial warmup
            }
        }
        
        // Measure Async Streaming
        const int iterations = 5;
        long totalMs = 0;
        int totalFound = 0;
        
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var results = new List<Person>();
            await using (var stream = File.OpenRead(hugeFilePath))
            {
                await foreach (var person in JsonQueryable<Person>.FromStream(stream)
                    .Where(p => p.IsActive && p.City == "New York")
                    .Take(100)
                    .AsAsyncEnumerable())
                {
                    results.Add(person);
                }
            }
            sw.Stop();
            totalMs += sw.ElapsedMilliseconds;
            totalFound = results.Count;
        }
        
        var avgMs = totalMs / (double)iterations;
        
        Console.WriteLine($"\n   Async Streaming Results ({iterations} runs averaged):");
        Console.WriteLine($"   * Time: {avgMs:F1}ms average");
        Console.WriteLine($"   * Found {totalFound} results per run");
        Console.WriteLine("   * Stopped reading after finding enough matches");
        Console.WriteLine("   * Memory usage: ~constant (only current element in RAM)");
        
        Console.WriteLine("\n   KEY ADVANTAGE:");
        Console.WriteLine("   * Can process files LARGER than available RAM!");
        Console.WriteLine("   * Perfect for log file analysis, data exports, batch processing");
        Console.WriteLine("   * Stops reading as soon as Take limit is reached");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Summary of when to use what.
    /// </summary>
    private static void ComparisonSummary()
    {
        Console.WriteLine("4. When to Use Each Approach");
        Console.WriteLine();
        Console.WriteLine("   USE TRADITIONAL (JsonSerializer + LINQ):");
        Console.WriteLine("   * Small files (< 1 MB)");
        Console.WriteLine("   * All data needed (no early termination)");
        Console.WriteLine("   * Simplicity is priority");
        Console.WriteLine();
        Console.WriteLine("   USE JSONQUERYABLE STREAMING:");
        Console.WriteLine("   * Large files (> 10 MB)");
        Console.WriteLine("   * Early termination (Take, First, Any)");
        Console.WriteLine("   * Memory-constrained environments");
        Console.WriteLine("   * Processing files larger than available RAM");
        Console.WriteLine("   * Async/await required (web APIs, services)");
        Console.WriteLine();
        Console.WriteLine("   PERFORMANCE FACTORS:");
        Console.WriteLine("   * File size: Bigger = more advantage for streaming");
        Console.WriteLine("   * Take count: Smaller = more advantage for streaming");
        Console.WriteLine("   * Disk speed: SSD helps both, but streaming benefits more");
        Console.WriteLine("   * RAM available: Streaming works with limited RAM");
        Console.WriteLine();
    }
}

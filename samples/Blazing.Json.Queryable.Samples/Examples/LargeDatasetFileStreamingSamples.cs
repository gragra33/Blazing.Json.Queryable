using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Samples.Models;
using System.Diagnostics;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates true file I/O streaming with large datasets.
/// Shows how streaming avoids loading entire files into memory while processing.
/// Compares file-based streaming vs. in-memory deserialization approaches.
/// </summary>
public static class LargeDatasetFileStreamingSamples
{
    public static async Task RunAllAsync()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("LARGE DATASET FILE STREAMING SAMPLES");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("[GOAL] Demonstrate true file I/O streaming benefits:");
        Console.WriteLine("  * No full file load into memory");
        Console.WriteLine("  * Constant memory usage regardless of file size");
        Console.WriteLine("  * Process files larger than available RAM");
        Console.WriteLine();
        
        // Ensure datasets exist
        EnsureDatasetsExist();
        
        Console.WriteLine("FILE I/O STREAMING EXAMPLES");
        Console.WriteLine("───────────────────────────────────────────────────────────────");
        FileStreamingWithCount();
        await FileStreamingAsyncProcessing();
        FileStreamingMemoryProfile();
        
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("FILE STREAMING SAMPLES COMPLETE");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Ensure large-dataset.json exists.
    /// </summary>
    private static void EnsureDatasetsExist()
    {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDir);
        
        var largeDatasetPath = Path.Combine(dataDir, "large-dataset.json");
        
        if (!File.Exists(largeDatasetPath))
        {
            Console.WriteLine("WARNING: large-dataset.json not found. Generating 100K records...");
            Data.DatasetGenerator.GenerateLargeDataset(largeDatasetPath, 100_000);
            Console.WriteLine();
        }
    }
    
    /// <summary>
    /// Example 1: File streaming with Count() - true constant memory.
    /// </summary>
    private static void FileStreamingWithCount()
    {
        Console.WriteLine();
        Console.WriteLine("Example 1: File Streaming with Count() - TRUE I/O STREAMING");
        Console.WriteLine();
        
        var largeDatasetPath = Path.Combine(AppContext.BaseDirectory, "Data", "large-dataset.json");
        var fileInfo = new FileInfo(largeDatasetPath);
        
        Console.WriteLine("* File: large-dataset.json");
        Console.WriteLine($"* Size: {fileInfo.Length / 1024.0 / 1024.0:F2} MB ({fileInfo.Length:N0} bytes)");
        Console.WriteLine();
        
        Console.WriteLine("[WHAT THIS DEMONSTRATES]");
        Console.WriteLine("   * File is read in small chunks (~4KB buffers from ArrayPool)");
        Console.WriteLine("   * Each JSON object is deserialized, checked, counted, then discarded");
        Console.WriteLine("   * NO full file load into memory");
        Console.WriteLine("   * Memory usage: ONLY the tiny buffers (constant ~4-8 KB)");
        Console.WriteLine();
        
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        // === FILE STREAMING APPROACH ===
        Console.WriteLine("[FILE STREAMING] Direct from file:");
        
        var runs = new List<(long Time, double Memory, int Count)>();
        
        for (int run = 0; run < 3; run++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var memBefore = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew();
            
            int count;
            using (var fileStream = File.OpenRead(largeDatasetPath))
            {
                count = JsonQueryable<Person>.FromStream(fileStream, jsonOptions)
                    .Where(p => p.City == "London" && p.IsActive)
                    .Count();
            }
            
            sw.Stop();
            var memAfter = GC.GetTotalMemory(false);
            var memUsedMB = (memAfter - memBefore) / 1024.0 / 1024.0;
            
            runs.Add((sw.ElapsedMilliseconds, memUsedMB, count));
            Console.WriteLine($"   Run {run + 1}: {count:N0} records, {sw.ElapsedMilliseconds:N0}ms, Memory: {memUsedMB:F3} MB");
        }
        
        var avgTime = runs.Average(r => r.Time);
        var avgMemory = runs.Average(r => r.Memory);
        
        Console.WriteLine();
        Console.WriteLine("[FILE STREAMING RESULTS]");
        Console.WriteLine($"   * Average time: {avgTime:F0}ms");
        Console.WriteLine($"   * Average memory: {avgMemory:F3} MB");
        Console.WriteLine($"   * Records found: {runs[0].Count:N0}");
        Console.WriteLine();
        
        // === IN-MEMORY APPROACH (for comparison) ===
        Console.WriteLine("[IN-MEMORY APPROACH] Load entire file:");
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var inMemMemBefore = GC.GetTotalMemory(true);
        var swInMem = Stopwatch.StartNew();
        
        var jsonText = File.ReadAllText(largeDatasetPath);
        var allPeople = System.Text.Json.JsonSerializer.Deserialize<List<Person>>(jsonText, jsonOptions);
        var inMemCount = allPeople!.Count(p => p.City == "London" && p.IsActive);
        
        swInMem.Stop();
        var inMemMemAfter = GC.GetTotalMemory(false);
        var inMemMemUsedMB = (inMemMemAfter - inMemMemBefore) / 1024.0 / 1024.0;
        
        Console.WriteLine($"   * {inMemCount:N0} records, {swInMem.ElapsedMilliseconds:N0}ms, Memory: {inMemMemUsedMB:F3} MB");
        Console.WriteLine();
        
        // === COMPARISON ===
        var memorySavings = inMemMemUsedMB - avgMemory;
        var memorySavingsPercent = (memorySavings / inMemMemUsedMB) * 100;
        
        Console.WriteLine("[COMPARISON - FILE STREAMING vs IN-MEMORY]");
        Console.WriteLine("───────────────────────────────────────────────────────────────");
        Console.WriteLine($"   File Streaming:  {avgMemory:F3} MB  ({avgTime:F0}ms)");
        Console.WriteLine($"   In-Memory Load:  {inMemMemUsedMB:F2} MB  ({swInMem.ElapsedMilliseconds}ms)");
        Console.WriteLine($"   Memory Savings:  {memorySavings:F2} MB ({memorySavingsPercent:F1}% reduction)");
        Console.WriteLine("───────────────────────────────────────────────────────────────");
        Console.WriteLine();
        
        Console.WriteLine("[KEY INSIGHTS]");
        Console.WriteLine("   * File streaming uses ~4-8 KB buffers (constant)");
        Console.WriteLine($"   * Avoided loading {fileInfo.Length / 1024.0 / 1024.0:F1} MB file into memory");
        Console.WriteLine("   * Memory usage independent of file size");
        Console.WriteLine("   * Can process files 100x larger than RAM");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Example 2: Async file streaming for log processing scenarios.
    /// </summary>
    private static async Task FileStreamingAsyncProcessing()
    {
        Console.WriteLine("Example 2: Async File Streaming - Log Processing Pattern");
        Console.WriteLine();
        
        var largeDatasetPath = Path.Combine(AppContext.BaseDirectory, "Data", "large-dataset.json");
        var fileInfo = new FileInfo(largeDatasetPath);
        
        Console.WriteLine("* File: large-dataset.json");
        Console.WriteLine($"* Size: {fileInfo.Length / 1024.0 / 1024.0:F2} MB ({fileInfo.Length:N0} bytes)");
        Console.WriteLine();
        
        Console.WriteLine("[SCENARIO] Process log entries, extract alerts, write to output");
        Console.WriteLine("   * Read from file stream (no full load)");
        Console.WriteLine("   * Filter for 'alerts' (Age >= 65 in our example)");
        Console.WriteLine("   * Process each alert individually");
        Console.WriteLine("   * Memory: Only current alert + tiny buffers");
        Console.WriteLine();
        
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memBefore = GC.GetTotalMemory(true);
        
        var sw = Stopwatch.StartNew();
        var alertCount = 0;
        var sampleAlerts = new List<string>();
        
        await using (var fileStream = File.OpenRead(largeDatasetPath))
        {
            await foreach (var person in JsonQueryable<Person>.FromStream(fileStream, jsonOptions)
                .Where(p => p.Age >= 65) // "Alert" condition
                .AsAsyncEnumerable())
            {
                // Process alert (in real scenario: log, send notification, etc.)
                alertCount++;
                
                if (sampleAlerts.Count < 5)
                {
                    sampleAlerts.Add($"{person.Name} (Age {person.Age}, {person.City})");
                }
            }
        }
        
        sw.Stop();
        var memAfter = GC.GetTotalMemory(false);
        var memUsedMB = (memAfter - memBefore) / 1024.0 / 1024.0;
        
        Console.WriteLine("[ASYNC STREAMING RESULTS]");
        Console.WriteLine($"   * Alerts processed: {alertCount:N0}");
        Console.WriteLine($"   * Time: {sw.ElapsedMilliseconds:N0}ms");
        Console.WriteLine($"   * Memory used: {memUsedMB:F3} MB");
        Console.WriteLine();
        
        Console.WriteLine("[SAMPLE ALERTS]");
        foreach (var alert in sampleAlerts)
        {
            Console.WriteLine($"   - {alert}");
        }
        Console.WriteLine();
        
        Console.WriteLine("[KEY INSIGHTS]");
        Console.WriteLine("   * Processed alerts one-at-a-time from file stream");
        Console.WriteLine("   * No full file load required");
        Console.WriteLine("   * True async I/O (non-blocking file reads)");
        Console.WriteLine($"   * Memory constant at ~{memUsedMB:F1} MB regardless of alert count");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Example 3: Memory profiling - streaming vs loading multiple file sizes.
    /// </summary>
    private static void FileStreamingMemoryProfile()
    {
        Console.WriteLine("Example 3: Memory Profiling - File Size Independence");
        Console.WriteLine();
        
        var largeDatasetPath = Path.Combine(AppContext.BaseDirectory, "Data", "large-dataset.json");
        var fileInfo = new FileInfo(largeDatasetPath);
        
        Console.WriteLine("[DEMONSTRATION]");
        Console.WriteLine("   * Same file, different query limits (10, 100, 1000, 10000 records)");
        Console.WriteLine("   * File streaming memory should grow linearly with results");
        Console.WriteLine("   * In-memory approach should be constant (always loads entire file)");
        Console.WriteLine();
        
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        var limits = new[] { 10, 100, 1000, 10000 };
        var streamingResults = new List<(int Limit, double Memory)>();
        var inMemoryResults = new List<(int Limit, double Memory)>();
        
        Console.WriteLine("[FILE STREAMING] Memory usage by result count:");
        foreach (var limit in limits)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var memBefore = GC.GetTotalMemory(true);
            
            using (var stream = File.OpenRead(largeDatasetPath))
            {
                var results = JsonQueryable<Person>.FromStream(stream, jsonOptions)
                    .Where(p => p.IsActive)
                    .Take(limit)
                    .ToList();
                
                var memAfter = GC.GetTotalMemory(false);
                var memUsedMB = (memAfter - memBefore) / 1024.0 / 1024.0;
                
                streamingResults.Add((limit, memUsedMB));
                Console.WriteLine($"   {limit,6:N0} records: {memUsedMB:F3} MB");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("[IN-MEMORY] Memory usage by result count:");
        foreach (var limit in limits)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var memBefore = GC.GetTotalMemory(true);
            
            var jsonText = File.ReadAllText(largeDatasetPath);
            var allPeople = System.Text.Json.JsonSerializer.Deserialize<List<Person>>(jsonText, jsonOptions);
            var results = allPeople!
                .Where(p => p.IsActive)
                .Take(limit)
                .ToList();
            
            var memAfter = GC.GetTotalMemory(false);
            var memUsedMB = (memAfter - memBefore) / 1024.0 / 1024.0;
            
            inMemoryResults.Add((limit, memUsedMB));
            Console.WriteLine($"   {limit,6:N0} records: {memUsedMB:F3} MB (entire {fileInfo.Length / 1024.0 / 1024.0:F1}MB file loaded)");
        }
        
        Console.WriteLine();
        Console.WriteLine("[ANALYSIS]");
        
        // Calculate memory variation for streaming vs in-memory
        var streamingMin = streamingResults.Min(r => r.Memory);
        var streamingMax = streamingResults.Max(r => r.Memory);
        var streamingRange = streamingMax - streamingMin;
        
        var inMemoryMin = inMemoryResults.Min(r => r.Memory);
        var inMemoryMax = inMemoryResults.Max(r => r.Memory);
        var inMemoryAvg = inMemoryResults.Average(r => r.Memory);
        var inMemoryRange = inMemoryMax - inMemoryMin;
        
        Console.WriteLine($"   File Streaming:");
        Console.WriteLine($"   * Min memory: {streamingMin:F3} MB (10 records)");
        Console.WriteLine($"   * Max memory: {streamingMax:F3} MB (10,000 records)");
        Console.WriteLine($"   * Range: {streamingRange:F3} MB - scales with result size");
        Console.WriteLine();
        Console.WriteLine($"   In-Memory:");
        Console.WriteLine($"   * Min memory: {inMemoryMin:F3} MB");
        Console.WriteLine($"   * Max memory: {inMemoryMax:F3} MB");
        Console.WriteLine($"   * Range: {inMemoryRange:F3} MB - relatively constant");
        Console.WriteLine($"   * Always loads ~{fileInfo.Length / 1024.0 / 1024.0:F1} MB file regardless of limit");
        Console.WriteLine();
        
        Console.WriteLine("[CONCLUSION]");
        Console.WriteLine($"   * File streaming: {streamingMin:F1} MB -> {streamingMax:F1} MB (grows with results)");
        Console.WriteLine($"   * In-memory: ~{inMemoryAvg:F1} MB (constant - always loads entire file)");
        Console.WriteLine();
        Console.WriteLine("   WHEN STREAMING WINS:");
        Console.WriteLine($"   * Small result sets: {streamingResults[0].Memory:F1} MB vs {inMemoryResults[0].Memory:F1} MB ({((inMemoryResults[0].Memory - streamingResults[0].Memory) / inMemoryResults[0].Memory * 100):F0}% savings)");
        Console.WriteLine($"   * Large files with selective queries");
        Console.WriteLine();
        Console.WriteLine("   KEY INSIGHT:");
        Console.WriteLine("   * Streaming: memory = f(result_size) - independent of file size");
        Console.WriteLine("   * In-memory: memory = f(file_size) - independent of result size");
        Console.WriteLine();
    }
}

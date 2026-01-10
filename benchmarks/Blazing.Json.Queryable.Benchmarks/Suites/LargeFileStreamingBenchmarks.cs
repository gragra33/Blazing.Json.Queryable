using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.Text.Json;
using Blazing.Json.Queryable.Providers;

namespace Blazing.Json.Queryable.Benchmarks;

/// <summary>
/// Benchmark Suite 5: Large File Streaming (Constant Memory Validation)
/// Proves that streaming maintains constant memory regardless of file size.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class LargeFileStreamingBenchmarks
{
    private string? _file10MB;
    private string? _file100MB;

    [GlobalSetup]
    public void Setup()
    {
        // Generate large test files
        // 10MB ? 50,000 records, 100MB ? 500,000 records (approx 200 bytes per record)
        var data50K = BenchmarkHelpers.GeneratePersonDataset(50000);
        var data500K = BenchmarkHelpers.GeneratePersonDataset(500000);

        _file10MB = BenchmarkHelpers.CreateTempUtf8JsonFile(data50K, "large_10mb");
        _file100MB = BenchmarkHelpers.CreateTempUtf8JsonFile(data500K, "large_100mb");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        BenchmarkHelpers.CleanupTempFile(_file10MB!);
        BenchmarkHelpers.CleanupTempFile(_file100MB!);
    }

    #region 10MB File Benchmarks

    [Benchmark(Baseline = true, Description = "10MB - LoadEntireFile (scales with size)")]
    public int Traditional_LoadEntireFile_10MB()
    {
        var bytes = File.ReadAllBytes(_file10MB!);
        var people = JsonSerializer.Deserialize<List<Person>>(bytes)!;
        var results = people
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "10MB - StreamingConstantMemory (constant ~4KB)")]
    public int CustomProvider_StreamingConstantMemory_10MB()
    {
        using var stream = File.OpenRead(_file10MB!);
        var results = JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    #endregion

    #region 100MB File Benchmarks

    [Benchmark(Description = "100MB - LoadEntireFile (scales with size)")]
    public int Traditional_LoadEntireFile_100MB()
    {
        var bytes = File.ReadAllBytes(_file100MB!);
        var people = JsonSerializer.Deserialize<List<Person>>(bytes)!;
        var results = people
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "100MB - StreamingConstantMemory (constant ~4KB)")]
    public int CustomProvider_StreamingConstantMemory_100MB()
    {
        using var stream = File.OpenRead(_file100MB!);
        var results = JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    #endregion

    #region Async Streaming Benchmarks

    [Benchmark(Description = "100MB - Async StreamingConstantMemory")]
    public async Task<int> CustomProvider_AsyncStreaming_100MB()
    {
        using var stream = File.OpenRead(_file100MB!);
        var count = 0;
        
        await foreach (var person in JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .AsAsyncEnumerable())
        {
            count++;
        }
        
        return count;
    }

    #endregion
}

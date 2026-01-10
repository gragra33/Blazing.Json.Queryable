using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.Text.Json;
using Blazing.Json.Queryable.Providers;

namespace Blazing.Json.Queryable.Benchmarks;

/// <summary>
/// Benchmark Suite 3: Asynchronous Stream Processing
/// Compares traditional async approaches vs custom async provider.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class AsyncStreamBenchmarks
{
    private string? _file10K;
    private string? _file100K;

    [GlobalSetup]
    public void Setup()
    {
        // Generate test files
        var data10K = BenchmarkHelpers.GeneratePersonDataset(10000);
        var data100K = BenchmarkHelpers.GeneratePersonDataset(100000);

        _file10K = BenchmarkHelpers.CreateTempUtf8JsonFile(data10K, "async_benchmark_10k");
        _file100K = BenchmarkHelpers.CreateTempUtf8JsonFile(data100K, "async_benchmark_100k");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        BenchmarkHelpers.CleanupTempFile(_file10K!);
        BenchmarkHelpers.CleanupTempFile(_file100K!);
    }

    #region 10K Records Async Benchmarks

    [Benchmark(Baseline = true, Description = "10K - ReadAllTextAsync then LINQ")]
    public async Task<int> Traditional_ReadAllTextAsync_10K()
    {
        var json = await File.ReadAllTextAsync(_file10K!);
        var people = JsonSerializer.Deserialize<List<Person>>(json)!;
        var results = people
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "10K - DeserializeAsync then LINQ")]
    public async Task<int> Traditional_DeserializeAsync_10K()
    {
        using var stream = File.OpenRead(_file10K!);
        var people = await JsonSerializer.DeserializeAsync<List<Person>>(stream);
        var results = people!
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "10K - Custom AsAsyncEnumerable")]
    public async Task<int> CustomProvider_AsAsyncEnumerable_10K()
    {
        using var stream = File.OpenRead(_file10K!);
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

    #region 100K Records Async Benchmarks

    [Benchmark(Description = "100K - ReadAllTextAsync then LINQ")]
    public async Task<int> Traditional_ReadAllTextAsync_100K()
    {
        var json = await File.ReadAllTextAsync(_file100K!);
        var people = JsonSerializer.Deserialize<List<Person>>(json)!;
        var results = people
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "100K - DeserializeAsync then LINQ")]
    public async Task<int> Traditional_DeserializeAsync_100K()
    {
        using var stream = File.OpenRead(_file100K!);
        var people = await JsonSerializer.DeserializeAsync<List<Person>>(stream);
        var results = people!
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "100K - Custom AsAsyncEnumerable")]
    public async Task<int> CustomProvider_AsAsyncEnumerable_100K()
    {
        using var stream = File.OpenRead(_file100K!);
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

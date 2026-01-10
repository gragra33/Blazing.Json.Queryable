using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using System.Text.Json;
using Blazing.Json.Queryable.Providers;

namespace Blazing.Json.Queryable.Benchmarks;

/// <summary>
/// Benchmark Suite 6: Comprehensive Comparison Matrix
/// Side-by-side comparison of ALL approaches organized by category.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ComprehensiveComparisonBenchmarks
{
    private List<Person>? _people1000;
    private string? _json1000;
    private byte[]? _utf81000;
    private string? _file1K;

    [GlobalSetup]
    public void Setup()
    {
        _people1000 = BenchmarkHelpers.GeneratePersonDataset(1000);
        _json1000 = BenchmarkHelpers.ToJsonString(_people1000);
        _utf81000 = BenchmarkHelpers.ToUtf8Bytes(_people1000);
        _file1K = BenchmarkHelpers.CreateTempUtf8JsonFile(_people1000, "comprehensive_1k");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        BenchmarkHelpers.CleanupTempFile(_file1K!);
    }

    #region InMemory Category

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("InMemory", "Sync")]
    public int InMemory_Traditional_DeserializeThenLinq()
    {
        var people = JsonSerializer.Deserialize<List<Person>>(_json1000!)!;
        var results = people
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark]
    [BenchmarkCategory("InMemory", "Sync")]
    public int InMemory_Custom_FromString()
    {
        var results = JsonQueryable<Person>.FromString(_json1000!)
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark]
    [BenchmarkCategory("InMemory", "Sync")]
    public int InMemory_Custom_FromUtf8()
    {
        var results = JsonQueryable<Person>.FromUtf8(_utf81000!)
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    #endregion

    #region FileIO Category

    [Benchmark]
    [BenchmarkCategory("FileIO", "Sync")]
    public int FileIO_Traditional_ReadAllText()
    {
        var json = File.ReadAllText(_file1K!);
        var people = JsonSerializer.Deserialize<List<Person>>(json)!;
        var results = people
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark]
    [BenchmarkCategory("FileIO", "Sync")]
    public int FileIO_Custom_FromFile()
    {
        using var query = JsonQueryable<Person>.FromFile(_file1K!);
        var results = query
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark]
    [BenchmarkCategory("FileIO", "Sync")]
    public int FileIO_Custom_FromStream()
    {
        using var stream = File.OpenRead(_file1K!);
        var results = JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    #endregion

    #region Async Category

    [Benchmark]
    [BenchmarkCategory("Async")]
    public async Task<int> Async_Traditional_ReadAllTextAsync()
    {
        var json = await File.ReadAllTextAsync(_file1K!);
        var people = JsonSerializer.Deserialize<List<Person>>(json)!;
        var results = people
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark]
    [BenchmarkCategory("Async")]
    public async Task<int> Async_Custom_AsAsyncEnumerable()
    {
        using var stream = File.OpenRead(_file1K!);
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

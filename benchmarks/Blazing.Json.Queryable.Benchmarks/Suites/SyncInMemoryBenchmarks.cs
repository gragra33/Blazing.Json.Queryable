using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.Text.Json;
using Blazing.Json.Queryable.Providers;

namespace Blazing.Json.Queryable.Benchmarks;

/// <summary>
/// Benchmark Suite 1: Synchronous In-Memory Comparisons
/// Compares traditional deserialization + LINQ vs custom provider approaches.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SyncInMemoryBenchmarks
{
    private List<Person>? _people100;
    private List<Person>? _people1000;
    private List<Person>? _people10000;

    private string? _json100;
    private string? _json1000;
    private string? _json10000;

    private byte[]? _utf8100;
    private byte[]? _utf81000;
    private byte[]? _utf810000;

    [GlobalSetup]
    public void Setup()
    {
        // Generate test datasets
        _people100 = BenchmarkHelpers.GeneratePersonDataset(100);
        _people1000 = BenchmarkHelpers.GeneratePersonDataset(1000);
        _people10000 = BenchmarkHelpers.GeneratePersonDataset(10000);

        // Serialize to JSON strings
        _json100 = BenchmarkHelpers.ToJsonString(_people100);
        _json1000 = BenchmarkHelpers.ToJsonString(_people1000);
        _json10000 = BenchmarkHelpers.ToJsonString(_people10000);

        // Serialize to UTF-8 bytes
        _utf8100 = BenchmarkHelpers.ToUtf8Bytes(_people100);
        _utf81000 = BenchmarkHelpers.ToUtf8Bytes(_people1000);
        _utf810000 = BenchmarkHelpers.ToUtf8Bytes(_people10000);
    }

    #region 100 Records Benchmarks

    [Benchmark(Baseline = true, Description = "100 records - Traditional (Deserialize then LINQ)")]
    public int Traditional_100()
    {
        var people = JsonSerializer.Deserialize<List<Person>>(_json100!)!;
        var results = people
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "100 records - Custom FromString")]
    public int CustomProvider_FromString_100()
    {
        var results = JsonQueryable<Person>.FromString(_json100!)
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "100 records - Custom FromUtf8")]
    public int CustomProvider_FromUtf8_100()
    {
        var results = JsonQueryable<Person>.FromUtf8(_utf8100!)
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "100 records - LINQ on pre-deserialized")]
    public int LinqToObjects_100()
    {
        var results = _people100!
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    #endregion

    #region 1000 Records Benchmarks

    [Benchmark(Description = "1000 records - Traditional (Deserialize then LINQ)")]
    public int Traditional_1000()
    {
        var people = JsonSerializer.Deserialize<List<Person>>(_json1000!)!;
        var results = people
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "1000 records - Custom FromString")]
    public int CustomProvider_FromString_1000()
    {
        var results = JsonQueryable<Person>.FromString(_json1000!)
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "1000 records - Custom FromUtf8")]
    public int CustomProvider_FromUtf8_1000()
    {
        var results = JsonQueryable<Person>.FromUtf8(_utf81000!)
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "1000 records - LINQ on pre-deserialized")]
    public int LinqToObjects_1000()
    {
        var results = _people1000!
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    #endregion

    #region 10000 Records Benchmarks

    [Benchmark(Description = "10000 records - Traditional (Deserialize then LINQ)")]
    public int Traditional_10000()
    {
        var people = JsonSerializer.Deserialize<List<Person>>(_json10000!)!;
        var results = people
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "10000 records - Custom FromString")]
    public int CustomProvider_FromString_10000()
    {
        var results = JsonQueryable<Person>.FromString(_json10000!)
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "10000 records - Custom FromUtf8")]
    public int CustomProvider_FromUtf8_10000()
    {
        var results = JsonQueryable<Person>.FromUtf8(_utf810000!)
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "10000 records - LINQ on pre-deserialized")]
    public int LinqToObjects_10000()
    {
        var results = _people10000!
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    #endregion
}

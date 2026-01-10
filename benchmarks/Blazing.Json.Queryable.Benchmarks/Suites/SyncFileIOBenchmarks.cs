using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.Text.Json;
using Blazing.Json.Queryable.Providers;

namespace Blazing.Json.Queryable.Benchmarks;

/// <summary>
/// Benchmark Suite 2: Synchronous File I/O Comparisons
/// Compares different file reading approaches with LINQ queries.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SyncFileIOBenchmarks
{
    private string? _file1K;
    private string? _file10K;

    [GlobalSetup]
    public void Setup()
    {
        // Generate test files
        var data1K = BenchmarkHelpers.GeneratePersonDataset(1000);
        var data10K = BenchmarkHelpers.GeneratePersonDataset(10000);

        _file1K = BenchmarkHelpers.CreateTempUtf8JsonFile(data1K, "benchmark_1k");
        _file10K = BenchmarkHelpers.CreateTempUtf8JsonFile(data10K, "benchmark_10k");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        BenchmarkHelpers.CleanupTempFile(_file1K!);
        BenchmarkHelpers.CleanupTempFile(_file10K!);
    }

    #region 1K Records File Benchmarks

    [Benchmark(Baseline = true, Description = "1K file - ReadAllText then Deserialize")]
    public int Traditional_ReadAllText_1K()
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

    [Benchmark(Description = "1K file - ReadAllBytes then Deserialize")]
    public int Traditional_ReadAllBytes_1K()
    {
        var bytes = File.ReadAllBytes(_file1K!);
        var people = JsonSerializer.Deserialize<List<Person>>(bytes)!;
        var results = people
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "1K file - Custom FromFile")]
    public int CustomProvider_FromFile_1K()
    {
        using var query = JsonQueryable<Person>.FromFile(_file1K!);
        var results = query
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "1K file - Custom FromStream")]
    public int CustomProvider_FromStream_1K()
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

    #region 10K Records File Benchmarks

    [Benchmark(Description = "10K file - ReadAllText then Deserialize")]
    public int Traditional_ReadAllText_10K()
    {
        var json = File.ReadAllText(_file10K!);
        var people = JsonSerializer.Deserialize<List<Person>>(json)!;
        var results = people
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "10K file - ReadAllBytes then Deserialize")]
    public int Traditional_ReadAllBytes_10K()
    {
        var bytes = File.ReadAllBytes(_file10K!);
        var people = JsonSerializer.Deserialize<List<Person>>(bytes)!;
        var results = people
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "10K file - Custom FromFile")]
    public int CustomProvider_FromFile_10K()
    {
        using var query = JsonQueryable<Person>.FromFile(_file10K!);
        var results = query
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "10K file - Custom FromStream")]
    public int CustomProvider_FromStream_10K()
    {
        using var stream = File.OpenRead(_file10K!);
        var results = JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();
        return results.Count;
    }

    #endregion
}

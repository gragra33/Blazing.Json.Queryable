using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Implementations;

namespace Blazing.Json.Queryable.Benchmarks;

/// <summary>
/// Benchmark Suite 4: Memory Allocation Validation
/// Critical validation of zero-allocation claims.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class MemoryAllocationBenchmarks
{
    private List<Person>? _people1000;
    private string? _json1000;
    private byte[]? _utf81000;
    private Person? _testPerson;

    [GlobalSetup]
    public void Setup()
    {
        _people1000 = BenchmarkHelpers.GeneratePersonDataset(1000);
        _json1000 = BenchmarkHelpers.ToJsonString(_people1000);
        _utf81000 = BenchmarkHelpers.ToUtf8Bytes(_people1000);
        _testPerson = _people1000[0];
        // SpanPropertyAccessor is now static - no need to instantiate
    }

    #region String vs UTF-8 Provider Allocations

    [Benchmark(Baseline = true, Description = "FromString allocation test")]
    public int StringProvider_AllocationTest()
    {
        var results = JsonQueryable<Person>.FromString(_json1000!)
            .Where(p => p.Age > 25)
            .Take(10)
            .ToList();
        return results.Count;
    }

    [Benchmark(Description = "FromUtf8 allocation test (zero conversion target)")]
    public int Utf8Provider_AllocationTest()
    {
        var results = JsonQueryable<Person>.FromUtf8(_utf81000!)
            .Where(p => p.Age > 25)
            .Take(10)
            .ToList();
        return results.Count;
    }

    #endregion

    #region Span-based Property Access Allocations

    [Benchmark(Description = "Span property access (zero alloc target)")]
    public int SpanPropertyAccess_AllocationTest()
    {
        var sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            // Use span-based property access
            var value = SpanPropertyAccessor.GetValue(_testPerson!, "Age".AsSpan());
            if (value is int age)
            {
                sum += age;
            }
        }
        return sum;
    }

    [Benchmark(Description = "String property access")]
    public int StringPropertyAccess_AllocationTest()
    {
        var sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            // Use string-based property access (should still be cached)
            var value = SpanPropertyAccessor.GetValueByName(_testPerson!, "Age");
            if (value is int age)
            {
                sum += age;
            }
        }
        return sum;
    }

    #endregion

    #region Stream Provider Allocations

    [Benchmark(Description = "Stream provider allocation test")]
    public int StreamProvider_AllocationTest()
    {
        using var stream = new MemoryStream(_utf81000!);
        var results = JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.Age > 25)
            .Take(10)
            .ToList();
        return results.Count;
    }

    #endregion

    #region Async Provider Allocations

    [Benchmark(Description = "Async provider allocation test")]
    public async Task<int> AsyncProvider_AllocationTest()
    {
        var count = 0;
        for (int i = 0; i < 100; i++)
        {
            using var stream = new MemoryStream(_utf81000!);
            await foreach (var person in JsonQueryable<Person>.FromStream(stream)
                .Where(p => p.Age > 25)
                .Take(10)
                .AsAsyncEnumerable())
            {
                count++;
            }
        }
        return count;
    }

    #endregion
}

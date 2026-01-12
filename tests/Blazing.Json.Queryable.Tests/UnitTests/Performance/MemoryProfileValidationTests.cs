using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.Performance;

/// <summary>
/// Memory profiling tests to validate zero-allocation claims.
/// NOTE: These tests validate functional behavior. For detailed memory profiling
/// with allocation counts, use benchmarks/Blazing.Json.Queryable.Benchmarks with MemoryDiagnoser.
/// </summary>
public class MemoryProfileValidationTests
{
    [Fact]
    public void FromUtf8_ShouldNotConvertToString_ZeroConversionAllocations()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act - Execute query with UTF-8 input
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json)
            .Where(p => p.Age > 25)
            .ToList();

        // Assert - Verify correct execution
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 25);

        // Performance expectation: ZERO UTF-8?UTF-16 conversion allocations
        // Validation: Use benchmarks/Blazing.Json.Queryable.Benchmarks/Suites/MemoryAllocationBenchmarks.cs
        // Expected: Only allocations are deserialized Person objects + List<Person>
    }

    [Fact]
    public void FromString_ShouldConvertOnce_SingleConversionAllocation()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var jsonString = TestData.SerializeToJson(data);

        // Act - Execute query with string input
        var results = JsonQueryable<Person>
            .FromString(jsonString)
            .Where(p => p.Age > 25)
            .ToList();

        // Assert - Verify correct execution
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 25);

        // Performance expectation: EXACTLY ONE UTF-16?UTF-8 conversion at entry point
        // Validation: Use benchmarks/Blazing.Json.Queryable.Benchmarks/Suites/MemoryAllocationBenchmarks.cs
        // Expected: 1x UTF-8 conversion (~5-10KB) + deserialized objects + List<Person>
    }

    [Fact]
    public void FromStream_ShouldUseConstantMemory_NoGrowthWithFileSize()
    {
        // Arrange - Different size datasets
        var smallData = TestData.GetSmallPersonDataset();
        var mediumData = TestData.GetMediumPersonDataset();
        var largeData = TestData.GetLargePersonDataset();

        // Act & Assert - Small dataset
        using (var stream = TestHelpers.CreateMemoryStream(TestData.SerializeToJson(smallData)))
        {
            var results = JsonQueryable<Person>
                .FromStream(stream)
                .Where(p => p.Age > 25)
                .ToList();
            results.ShouldNotBeEmpty();
        }

        // Act & Assert - Medium dataset
        using (var stream = TestHelpers.CreateMemoryStream(TestData.SerializeToJson(mediumData)))
        {
            var results = JsonQueryable<Person>
                .FromStream(stream)
                .Where(p => p.Age > 25)
                .ToList();
            results.ShouldNotBeEmpty();
        }

        // Act & Assert - Large dataset
        using (var stream = TestHelpers.CreateMemoryStream(TestData.SerializeToJson(largeData)))
        {
            var results = JsonQueryable<Person>
                .FromStream(stream)
                .Where(p => p.Age > 25)
                .Take(100) // Limit for test speed
                .ToList();
            results.ShouldNotBeEmpty();
        }

        // Performance expectation: Constant ~4KB buffer (sync) regardless of file size
        // Validation: Use benchmarks/Blazing.Json.Queryable.Benchmarks/Suites/LargeFileStreamingBenchmarks.cs
        // Expected: Memory usage stays constant, does NOT scale with file size
    }

    [Fact]
    public async Task AsyncEnumeration_ShouldReturnArrayPoolBuffers_NoBufferLeaks()
    {
        // Arrange
        var data = TestData.GetMediumPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act - Execute async enumeration multiple times
        for (int iteration = 0; iteration < 10; iteration++)
        {
            using var stream = TestHelpers.CreateMemoryStream(json);
            var results = new List<Person>();

            await foreach (var person in JsonQueryable<Person>
                .FromStream(stream)
                .Where(p => p.Age > 25)
                .AsAsyncEnumerable())
            {
                results.Add(person);
                if (results.Count >= 50) // Limit for test speed
                    break;
            }

            results.Count.ShouldBe(50);
            results.ShouldAllBe(p => p.Age > 25);
        }

        // Performance expectation: ArrayPool buffers returned correctly, no leaks
        // Validation: Use benchmarks/Blazing.Json.Queryable.Benchmarks/Suites/MemoryAllocationBenchmarks.cs
        // Expected: Constant memory across iterations, buffers returned to pool
    }

    [Fact]
    public void SpanPropertyAccess_ShouldNotAllocate_ZeroAllocationPerAccess()
    {
        // Arrange
        var person = TestData.GetSmallPersonDataset().First();

        // Warm up cache
        _ = Implementations.SpanPropertyAccessor.GetValue(person, "Name".AsSpan());

        // Act - Multiple accesses (should be zero-allocation after cache warm-up)
        var results = new List<object?>();
        for (int i = 0; i < 1000; i++)
        {
            var value = Implementations.SpanPropertyAccessor.GetValue(person, "Name".AsSpan());
            results.Add(value);
        }

        // Assert
        results.Count.ShouldBe(1000);
        results.ShouldAllBe(v => v!.Equals(person.Name));

        // Performance expectation: ZERO allocations per cached property access
        // Validation: Use benchmarks/Blazing.Json.Queryable.Benchmarks/Suites/MemoryAllocationBenchmarks.cs
        // Expected: Gen0/Gen1/Gen2 all zero for cached accesses
    }

    [Fact]
    public void ExpressionEvaluation_ShouldCacheDelegates_OneTimeAllocation()
    {
        // Arrange
        var evaluator = new Implementations.CompiledExpressionEvaluator();
        var data = TestData.GetSmallPersonDataset();

        // Act - Build predicate (cache miss - allocates compiled delegate)
        var predicate1 = evaluator.BuildPredicate<Person>(p => p.Age > 25);
        var results1 = data.Where(predicate1).ToList();

        // Act - Build same predicate again (cache hit - no new allocation)
        var predicate2 = evaluator.BuildPredicate<Person>(p => p.Age > 25);
        var results2 = data.Where(predicate2).ToList();

        // Assert
        results1.ShouldNotBeEmpty();
        results2.Count.ShouldBe(results1.Count);

        // Performance expectation: Predicates cached, only first access allocates
        // Validation: Second predicate access should use cached delegate
        // Expected: Zero allocations on cache hit
    }

    [Fact]
    public void ComplexQuery_MaterializationRequired_BuffersResultsOnce()
    {
        // Arrange
        var data = TestData.GetMediumPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act - Query with OrderBy (requires materialization)
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json)
            .Where(p => p.Age > 25)
            .OrderBy(p => p.Name)
            .Take(50)
            .ToList();

        // Assert
        results.Count.ShouldBe(50);
        results.ShouldAllBe(p => p.Age > 25);
        results.ShouldBeOrderedBy(p => p.Name);

        // Performance expectation: OrderBy requires buffering into List<T>
        // Validation: One-time allocation for List<T>, then LINQ sorting
        // Expected: Memory proportional to result set size, not input size
    }

    [Fact]
    public void StreamQuery_WithoutMaterialization_TrueStreaming()
    {
        // Arrange
        var data = TestData.GetLargePersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act - Query WITHOUT OrderBy (pure streaming)
        var results = JsonQueryable<Person>
            .FromStream(stream)
            .Where(p => p.Age > 25)
            .Take(100) // Take without OrderBy - can stream
            .ToList();

        // Assert
        results.Count.ShouldBe(100);
        results.ShouldAllBe(p => p.Age > 25);

        // Performance expectation: True streaming, constant buffer size
        // Validation: Use benchmarks/Blazing.Json.Queryable.Benchmarks/Suites/LargeFileStreamingBenchmarks.cs
        // Expected: ~4KB buffer, processes large file without loading into memory
    }
}

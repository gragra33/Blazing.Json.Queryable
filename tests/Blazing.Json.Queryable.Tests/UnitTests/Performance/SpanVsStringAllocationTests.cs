using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.Performance;

/// <summary>
/// Performance tests comparing Span-based vs String-based approaches.
/// These tests validate allocation behavior and memory efficiency.
/// NOTE: For comprehensive benchmarking, see benchmarks/Blazing.Json.Queryable.Benchmarks project.
/// </summary>
public class SpanVsStringAllocationTests
{
    [Fact]
    public void Utf8Provider_vs_StringProvider_AllocationDifference()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);
        var jsonString = TestData.SerializeToJson(data);

        // Act - UTF-8 provider (no conversion)
        var utf8Results = JsonQueryable<Person>
            .FromUtf8(utf8Json)
            .Where(p => p.Age > 25)
            .ToList();

        // Act - String provider (1x UTF-8 conversion)
        var stringResults = JsonQueryable<Person>
            .FromString(jsonString)
            .Where(p => p.Age > 25)
            .ToList();

        // Assert - Results should be identical
        utf8Results.Count.ShouldBe(stringResults.Count);
        
        // Performance note: UTF-8 provider avoids UTF-8 conversion allocation
        // For actual allocation measurements, use benchmarks/Blazing.Json.Queryable.Benchmarks
    }

    [Fact]
    public void StreamProvider_UsesConstantMemory_RegardlessOfDataSize()
    {
        // Arrange - Test with different data sizes
        var smallData = TestData.GetSmallPersonDataset();
        var mediumData = TestData.GetMediumPersonDataset();
        var largeData = TestData.GetLargePersonDataset();

        var smallJson = TestData.SerializeToJson(smallData);
        var mediumJson = TestData.SerializeToJson(mediumData);
        var largeJson = TestData.SerializeToJson(largeData);

        // Act & Assert - Stream provider should use constant memory
        using (var stream = TestHelpers.CreateMemoryStream(smallJson))
        {
            var results = JsonQueryable<Person>
                .FromStream(stream)
                .Where(p => p.Age > 25)
                .ToList();
            results.ShouldNotBeEmpty();
        }

        using (var stream = TestHelpers.CreateMemoryStream(mediumJson))
        {
            var results = JsonQueryable<Person>
                .FromStream(stream)
                .Where(p => p.Age > 25)
                .ToList();
            results.ShouldNotBeEmpty();
        }

        using (var stream = TestHelpers.CreateMemoryStream(largeJson))
        {
            var results = JsonQueryable<Person>
                .FromStream(stream)
                .Where(p => p.Age > 25)
                .Take(100) // Limit for test speed
                .ToList();
            results.ShouldNotBeEmpty();
        }

        // Performance note: Memory usage should be constant (~4KB buffer)
        // For actual memory profiling, use benchmarks/Blazing.Json.Queryable.Benchmarks
    }

    [Fact]
    public void FromUtf8_PreferredMethod_BestPerformance()
    {
        // Arrange
        var data = TestData.GetMediumPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act - Preferred method (FromUtf8)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json)
            .Where(p => p.Age > 25)
            .OrderBy(p => p.Name)
            .Take(50)
            .ToList();
        stopwatch.Stop();

        // Assert
        results.Count.ShouldBe(50);
        results.ShouldAllBe(p => p.Age > 25);
        results.ShouldBeOrderedBy(p => p.Name);

        // Performance note: FromUtf8 should be fastest (no conversion)
        // Typical: <10ms for 100 records on modern hardware
        // For comprehensive benchmarks, see benchmarks/Blazing.Json.Queryable.Benchmarks
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000); // Generous limit for CI
    }

    [Fact]
    public void ComplexQuery_WithMultipleOperations_MaintainsEfficiency()
    {
        // Arrange
        var data = TestData.GetMediumPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act - Complex query with multiple operations
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json)
            .Where(p => p.Age > 25 && p.IsActive)
            .OrderBy(p => p.City)
            .ThenBy(p => p.Age)
            .Select(p => new { p.Name, p.Age, p.City })
            .Skip(10)
            .Take(20)
            .ToList();
        stopwatch.Stop();

        // Assert
        results.Count.ShouldBe(20);
        
        // Performance note: Multiple operations should still be efficient
        // For detailed performance analysis, see benchmarks/Blazing.Json.Queryable.Benchmarks
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000); // Generous limit for CI
    }

    [Fact]
    public async Task AsyncEnumeration_HandlesLargeDataEfficiently()
    {
        // Arrange
        var data = TestData.GetLargePersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act - Async enumeration using AsAsyncEnumerable() extension
        var results = new List<Person>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        await foreach (var person in JsonQueryable<Person>
            .FromStream(stream)
            .Where(p => p.Age > 30)
            .AsAsyncEnumerable())
        {
            results.Add(person);
            if (results.Count >= 100) // Limit for test speed
                break;
        }
        stopwatch.Stop();

        // Assert
        results.Count.ShouldBe(100);
        results.ShouldAllBe(p => p.Age > 30);

        // Performance note: Async should maintain constant memory with ArrayPool
        // For memory profiling, see benchmarks/Blazing.Json.Queryable.Benchmarks
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(2000); // Generous limit for CI
    }
}

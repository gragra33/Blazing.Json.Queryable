using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.IntegrationTests;

/// <summary>
/// Integration tests for async enumeration scenarios.
/// </summary>
public class AsyncEnumerationTests
{
    [Fact]
    public async Task AsAsyncEnumerable_FromUtf8_EnumeratesAsynchronously()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = new List<Person>();
        await foreach (var person in JsonQueryable<Person>.FromUtf8(utf8Json).AsAsyncEnumerable())
        {
            results.Add(person);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(data.Count);
    }

    [Fact]
    public async Task AsAsyncEnumerable_FromString_EnumeratesAsynchronously()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act
        var results = new List<Person>();
        await foreach (var person in JsonQueryable<Person>.FromString(json).AsAsyncEnumerable())
        {
            results.Add(person);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(data.Count);
    }

    [Fact]
    public async Task AsAsyncEnumerable_WithWhere_FiltersAsynchronously()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = new List<Person>();
        await foreach (var person in JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Age > 30)
            .AsAsyncEnumerable())
        {
            results.Add(person);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 30);
    }

    [Fact]
    public async Task AsAsyncEnumerable_WithComplexQuery_AppliesAllOperations()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = new List<Person>();
        await foreach (var person in JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Age > 25)
            .OrderBy(p => p.Name)
            .Take(5)
            .AsAsyncEnumerable())
        {
            results.Add(person);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBeLessThanOrEqualTo(5);
        results.ShouldAllBe(p => p.Age > 25);
        results.ShouldBeOrderedBy(p => p.Name);
    }

    [Fact]
    public async Task AsAsyncEnumerable_WithCancellation_SupportsCancellation()
    {
        // Arrange
        var data = TestData.GetMediumPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in JsonQueryable<Person>.FromUtf8(utf8Json)
                .AsAsyncEnumerable()
                .WithCancellation(cts.Token))
            {
                // Should not execute
            }
        });
    }

    [Fact]
    public async Task AsAsyncEnumerable_WithDeferredCancellation_CancelsPartially()
    {
        // Arrange
        var data = TestData.GetMediumPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);
        using var cts = new CancellationTokenSource();
        var count = 0;

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in JsonQueryable<Person>.FromUtf8(utf8Json)
                .AsAsyncEnumerable()
                .WithCancellation(cts.Token))
            {
                count++;
                if (count >= 5)
                {
                    cts.Cancel();
                }
            }
        });

        count.ShouldBeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task AsAsyncEnumerable_FromStream_EnumeratesAsynchronously()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = new List<Person>();
        await foreach (var person in JsonQueryable<Person>.FromStream(stream)
            .AsAsyncEnumerable())
        {
            results.Add(person);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(data.Count);
    }

    [Fact]
    public async Task AsAsyncEnumerable_EmptyResults_ReturnsEmpty()
    {
        // Arrange
        var utf8Json = TestData.GetEmptyJsonArrayUtf8();

        // Act
        var results = new List<Person>();
        await foreach (var person in JsonQueryable<Person>.FromUtf8(utf8Json)
            .AsAsyncEnumerable())
        {
            results.Add(person);
        }

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task AsAsyncEnumerable_LargeDataset_HandlesEfficiently()
    {
        // Arrange
        var data = TestData.GetLargePersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);
        var count = 0;

        // Act
        await foreach (var _ in JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Age > 30)
            .Take(100)
            .AsAsyncEnumerable())
        {
            count++;
        }

        // Assert
        count.ShouldBeLessThanOrEqualTo(100);
    }
}

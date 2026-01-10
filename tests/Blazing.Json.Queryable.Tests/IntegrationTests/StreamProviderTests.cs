using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.IntegrationTests;

/// <summary>
/// Integration tests for Stream provider (FromStream, FromFile).
/// </summary>
public class StreamProviderTests
{
    [Fact]
    public void FromStream_SimpleWhereQuery_ReturnsFilteredResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.Age > 30)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 30);
    }

    [Fact]
    public void FromStream_OrderByQuery_ReturnsSortedResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = JsonQueryable<Person>.FromStream(stream)
            .OrderBy(p => p.Name)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldBeOrderedBy(p => p.Name);
    }

    [Fact]
    public void FromStream_ComplexQuery_AppliesAllOperations()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.Age > 25)
            .OrderBy(p => p.Name)
            .Skip(1)
            .Take(3)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBeLessThanOrEqualTo(3);
        results.ShouldAllBe(p => p.Age > 25);
        results.ShouldBeOrderedBy(p => p.Name);
    }

    [Fact]
    public void FromFile_SimpleWhereQuery_ReturnsFilteredResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var tempFile = TestHelpers.CreateTempJsonFile(json);

        // Act
        var results = JsonQueryable<Person>.FromFile(tempFile.Path)
            .Where(p => p.Age > 30)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 30);
    }

    [Fact]
    public void FromFile_OrderByQuery_ReturnsSortedResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var tempFile = TestHelpers.CreateTempJsonFile(json);

        // Act
        var results = JsonQueryable<Person>.FromFile(tempFile.Path)
            .OrderBy(p => p.Name)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldBeOrderedBy(p => p.Name);
    }

    [Fact]
    public void FromFile_ComplexQuery_AppliesAllOperations()
    {
        // Arrange
        var data = TestData.GetMediumPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var tempFile = TestHelpers.CreateTempJsonFile(json);

        // Act
        var results = JsonQueryable<Person>.FromFile(tempFile.Path)
            .Where(p => p.Age > 25 && p.IsActive)
            .OrderBy(p => p.Name)
            .Skip(5)
            .Take(10)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBeLessThanOrEqualTo(10);
        results.ShouldAllBe(p => p.Age > 25 && p.IsActive);
        results.ShouldBeOrderedBy(p => p.Name);
    }

    [Fact]
    public async Task FromStream_AsyncEnumeration_ReturnsResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = new List<Person>();
        await foreach (var person in JsonQueryable<Person>.FromStream(stream).AsAsyncEnumerable())
        {
            results.Add(person);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(data.Count);
    }

    [Fact]
    public async Task FromFile_AsyncEnumeration_ReturnsResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var tempFile = TestHelpers.CreateTempJsonFile(json);

        // Act
        var results = new List<Person>();
        await foreach (var person in JsonQueryable<Person>.FromFile(tempFile.Path).AsAsyncEnumerable())
        {
            results.Add(person);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(data.Count);
    }

    [Fact]
    public async Task FromStream_AsyncWithCancellation_SupportsCancellation()
    {
        // Arrange
        var data = TestData.GetMediumPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in JsonQueryable<Person>.FromStream(stream).AsAsyncEnumerable().WithCancellation(cts.Token))
            {
                // Should not execute
            }
        });
    }

    [Fact]
    public void FromStream_LargeDataset_HandlesEfficiently()
    {
        // Arrange
        var data = TestData.GetLargePersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.Age > 30)
            .Take(50)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBeLessThanOrEqualTo(50);
        results.ShouldAllBe(p => p.Age > 30);
    }

    [Fact]
    public void FromStream_EmptyStream_ReturnsEmptyResults()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = JsonQueryable<Person>.FromStream(stream)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }
}

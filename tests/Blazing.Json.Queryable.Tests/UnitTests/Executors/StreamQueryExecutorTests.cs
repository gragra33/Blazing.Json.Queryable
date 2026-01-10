using Blazing.Json.Queryable.Core;
using Blazing.Json.Queryable.Execution;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.Executors;

/// <summary>
/// Unit tests for StreamQueryExecutor.
/// </summary>
public class StreamQueryExecutorTests
{
    private readonly JsonSerializerOptions _options;

    public StreamQueryExecutorTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    [Fact]
    public void Execute_SimpleQuery_ReturnsResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);
        var executor = new StreamQueryExecutor(stream, _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person)
        };

        // Act
        var results = executor.Execute<Person>(plan).ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(data.Count);
    }

    [Fact]
    public void Execute_WithWherePredicate_FiltersResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);
        var executor = new StreamQueryExecutor(stream, _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            Predicates = new Delegate[] { (Person p) => p.Age > 30 }
        };

        // Act
        var results = executor.Execute<Person>(plan).ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 30);
    }

    [Fact]
    public void Execute_WithOrderBy_SortsResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);
        var executor = new StreamQueryExecutor(stream, _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            SortPropertyPaths = new[] { "Age".AsMemory() },
            SortDirections = new[] { true }
        };

        // Act
        var results = executor.Execute<Person>(plan).ToList();

        // Assert
        results.ShouldBeOrderedBy(p => p.Age);
    }

    [Fact]
    public void Execute_WithTakeAndSkip_AppliesPaging()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);
        var executor = new StreamQueryExecutor(stream, _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            Skip = 2,
            Take = 3
        };

        // Act
        var results = executor.Execute<Person>(plan).ToList();

        // Assert
        results.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteAsync_SimpleQuery_ReturnsResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);
        var executor = new StreamQueryExecutor(stream, _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person)
        };

        // Act
        var results = await executor.ExecuteAsync<Person>(plan).ToListAsync();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(data.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_SupportsCancellation()
    {
        // Arrange
        var data = TestData.GetMediumPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);
        var executor = new StreamQueryExecutor(stream, _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person)
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in executor.ExecuteAsync<Person>(plan, cts.Token))
            {
                // Should not execute
            }
        });
    }

    [Fact]
    public async Task ExecuteAsync_WithWherePredicate_FiltersResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);
        var executor = new StreamQueryExecutor(stream, _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            Predicates = new Delegate[] { (Person p) => p.Age > 30 }
        };

        // Act
        var results = await executor.ExecuteAsync<Person>(plan).ToListAsync();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 30);
    }

    [Fact]
    public void Execute_ComplexQuery_AppliesAllOperations()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = TestHelpers.CreateMemoryStream(json);
        var executor = new StreamQueryExecutor(stream, _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            Predicates = new Delegate[] { (Person p) => p.Age > 25 },
            SortPropertyPaths = new[] { "Name".AsMemory() },
            SortDirections = new[] { true },
            Skip = 1,
            Take = 3
        };

        // Act
        var results = executor.Execute<Person>(plan).ToList();

        // Assert
        results.ShouldAllBe(p => p.Age > 25);
        results.Count.ShouldBeLessThanOrEqualTo(3);
        results.ShouldBeOrderedBy(p => p.Name);
    }

    [Fact]
    public void Execute_EmptyStream_ReturnsEmptyResults()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();
        using var stream = TestHelpers.CreateMemoryStream(json);
        var executor = new StreamQueryExecutor(stream, _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person)
        };

        // Act
        var results = executor.Execute<Person>(plan).ToList();

        // Assert
        results.ShouldBeEmpty();
    }
}

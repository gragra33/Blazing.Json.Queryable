using Blazing.Json.Queryable.Core;
using Blazing.Json.Queryable.Execution;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.Executors;

/// <summary>
/// Unit tests for StringQueryExecutor.
/// </summary>
public class StringQueryExecutorTests
{
    private readonly JsonSerializerOptions _options;

    public StringQueryExecutorTests()
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
        var utf8Json = TestData.SerializeToUtf8(data);
        var executor = new StringQueryExecutor(utf8Json, _options);

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
        var utf8Json = TestData.SerializeToUtf8(data);
        var executor = new StringQueryExecutor(utf8Json, _options);

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
        results.Count.ShouldBeLessThan(data.Count);
    }

    [Fact]
    public void Execute_WithTake_LimitsResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);
        var executor = new StringQueryExecutor(utf8Json, _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            Take = 3
        };

        // Act
        var results = executor.Execute<Person>(plan).ToList();

        // Assert
        results.Count.ShouldBe(3);
    }

    [Fact]
    public void Execute_WithSkip_SkipsResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);
        var executor = new StringQueryExecutor(utf8Json, _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            Skip = 5
        };

        // Act
        var results = executor.Execute<Person>(plan).ToList();

        // Assert
        results.Count.ShouldBe(data.Count - 5);
    }

    [Fact]
    public void Execute_WithOrderBy_SortsResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);
        var executor = new StringQueryExecutor(utf8Json, _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            SortPropertyPaths = new[] { "Age".AsMemory() },
            SortDirections = new[] { true } // Ascending
        };

        // Act
        var results = executor.Execute<Person>(plan).ToList();

        // Assert
        results.ShouldBeOrderedBy(p => p.Age);
    }

    [Fact]
    public void Execute_WithOrderByDescending_SortsDescending()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);
        var executor = new StringQueryExecutor(utf8Json, _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            SortPropertyPaths = new[] { "Age".AsMemory() },
            SortDirections = new[] { false } // Descending
        };

        // Act
        var results = executor.Execute<Person>(plan).ToList();

        // Assert
        results.ShouldBeOrderedBy(p => p.Age, descending: true);
    }

    [Fact]
    public void Execute_ComplexQuery_AppliesAllOperations()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);
        var executor = new StringQueryExecutor(utf8Json, _options);

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
    public async Task ExecuteAsync_SimpleQuery_ReturnsResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);
        var executor = new StringQueryExecutor(utf8Json, _options);

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
        var data = TestData.GetMediumPersonDataset(); // Larger dataset
        var utf8Json = TestData.SerializeToUtf8(data);
        var executor = new StringQueryExecutor(utf8Json, _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person)
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

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
    public void Execute_EmptyArray_ReturnsEmptyResults()
    {
        // Arrange
        var utf8Json = TestData.GetEmptyJsonArrayUtf8();
        var executor = new StringQueryExecutor(utf8Json, _options);

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

using Blazing.Json.Queryable.Core;
using Blazing.Json.Queryable.Execution;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.Executors;

/// <summary>
/// Unit tests for TokenFilteredMemoryExecutor - memory-based JSONPath token filtering.
/// </summary>
public class TokenFilteredMemoryExecutorTests
{
    private readonly JsonSerializerOptions _options;

    public TokenFilteredMemoryExecutorTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    #region Helper Methods

    private static ReadOnlyMemory<byte> CreateUtf8Json<T>(List<T> data, string wrapperProperty)
    {
        var innerJson = JsonSerializer.Serialize(data);
        var json = $$"""{"{{wrapperProperty}}": {{innerJson}}}""";
        return Encoding.UTF8.GetBytes(json);
    }

    #endregion

    #region UTF-8 Source Tests

    [Fact]
    public async Task ExecuteAsync_Utf8Source_SimpleArrayPath_ExtractsItems()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset().Take(5).ToList();
        var utf8Json = CreateUtf8Json(data, "data");
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.data[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person)
        };

        // Act
        var results = await executor.ExecuteAsync<Person>(plan).ToListAsync();

        // Assert
        results.Count.ShouldBe(data.Count);
        results[0].Name.ShouldBe(data[0].Name);
        results[0].Age.ShouldBe(data[0].Age);
    }

    [Fact]
    public void Execute_Utf8Source_SimpleArrayPath_ExtractsItems()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset().Take(5).ToList();
        var utf8Json = CreateUtf8Json(data, "data");
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.data[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person)
        };

        // Act
        var results = executor.Execute<Person>(plan).ToList();

        // Assert
        results.Count.ShouldBe(data.Count);
        results[0].Name.ShouldBe(data[0].Name);
    }

    [Fact]
    public async Task ExecuteAsync_Utf8Source_WithWhereFilter_FiltersResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = CreateUtf8Json(data, "data");
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.data[*]", _options);

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
    public async Task ExecuteAsync_Utf8Source_NestedPath_ExtractsCorrectly()
    {
        // Arrange: Nested JSONPath
        var data = TestData.GetSmallPersonDataset().Take(3).ToList();
        var innerJson = JsonSerializer.Serialize(data);
        var json = "{\"result\": {\"customers\": " + innerJson + "}}";
        var utf8Json = Encoding.UTF8.GetBytes(json);
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.result[*].customers[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person)
        };

        // Act
        var results = await executor.ExecuteAsync<Person>(plan).ToListAsync();

        // Assert
        results.Count.ShouldBe(data.Count);
    }

    #endregion

    #region String Source Tests (via UTF-8 conversion)

    [Fact]
    public async Task ExecuteAsync_StringConvertedToUtf8_SimpleArrayPath_ExtractsItems()
    {
        // Arrange: Simulate string source (converted to UTF-8)
        var data = TestData.GetSmallPersonDataset().Take(5).ToList();
        var innerJson = JsonSerializer.Serialize(data);
        var jsonString = $$"""{"data": {{innerJson}}}""";
        var utf8Json = Encoding.UTF8.GetBytes(jsonString);
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.data[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person)
        };

        // Act
        var results = await executor.ExecuteAsync<Person>(plan).ToListAsync();

        // Assert
        results.Count.ShouldBe(data.Count);
        results[0].Name.ShouldBe(data[0].Name);
    }

    #endregion

    #region LINQ Integration Tests

    [Fact]
    public async Task ExecuteAsync_WithTakeAndSkip_AppliesPaging()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = CreateUtf8Json(data, "data");
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.data[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            Skip = 2,
            Take = 3
        };

        // Act
        var results = await executor.ExecuteAsync<Person>(plan).ToListAsync();

        // Assert
        results.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteAsync_WithOrderBy_SortsResults()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset().Take(5).ToList();
        var utf8Json = CreateUtf8Json(data, "data");
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.data[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            SortPropertyPaths = new[] { "Age".AsMemory() },
            SortDirections = new[] { true }
        };

        // Act
        var results = await executor.ExecuteAsync<Person>(plan).ToListAsync();

        // Assert
        results.ShouldBeOrderedBy(p => p.Age);
    }

    [Fact]
    public async Task ExecuteAsync_ComplexQuery_AppliesAllOperations()
    {
        // Arrange: JSONPath + WHERE + OrderBy + Skip + Take
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = CreateUtf8Json(data, "data");
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.data[*]", _options);

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
        var results = await executor.ExecuteAsync<Person>(plan).ToListAsync();

        // Assert
        results.ShouldAllBe(p => p.Age > 25);
        results.Count.ShouldBeLessThanOrEqualTo(3);
        results.ShouldBeOrderedBy(p => p.Name);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ExecuteAsync_EmptyArray_ReturnsEmpty()
    {
        // Arrange
        var json = """{"data": []}""";
        var utf8Json = Encoding.UTF8.GetBytes(json);
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.data[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person)
        };

        // Act
        var results = await executor.ExecuteAsync<Person>(plan).ToListAsync();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_PathNotFound_ReturnsEmpty()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset().Take(3).ToList();
        var utf8Json = CreateUtf8Json(data, "data");
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.nonexistent[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person)
        };

        // Act
        var results = await executor.ExecuteAsync<Person>(plan).ToListAsync();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_EmptyUtf8Memory_ReturnsEmpty()
    {
        // Arrange
        var utf8Json = ReadOnlyMemory<byte>.Empty;
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.data[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person)
        };

        // Act & Assert - should handle gracefully
        var results = await executor.ExecuteAsync<Person>(plan).ToListAsync();
        results.ShouldBeEmpty();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Constructor_NullJsonPath_ThrowsArgumentNullException()
    {
        // Arrange
        var utf8Json = ReadOnlyMemory<byte>.Empty;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new TokenFilteredMemoryExecutor(utf8Json, null!, _options));
    }

    [Fact]
    public void Execute_NullPlan_ThrowsArgumentNullException()
    {
        // Arrange
        var utf8Json = ReadOnlyMemory<byte>.Empty;
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.data[*]", _options);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            executor.Execute<Person>(null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullPlan_ThrowsArgumentNullException()
    {
        // Arrange
        var utf8Json = ReadOnlyMemory<byte>.Empty;
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.data[*]", _options);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
        {
            await foreach (var _ in executor.ExecuteAsync<Person>(null!))
            {
                // Should not execute
            }
        });
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var data = TestData.GetMediumPersonDataset();
        var utf8Json = CreateUtf8Json(data, "data");
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.data[*]", _options);

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

    #endregion

    #region Performance Tests

    [Fact]
    public async Task ExecuteAsync_LargeDataset_HandlesEfficiently()
    {
        // Arrange
        var data = TestData.GetLargePersonDataset();
        var utf8Json = CreateUtf8Json(data, "data");
        var executor = new TokenFilteredMemoryExecutor(utf8Json, "$.data[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            Take = 100 // Only take first 100 to keep test fast
        };

        // Act
        var results = await executor.ExecuteAsync<Person>(plan).ToListAsync();

        // Assert
        results.Count.ShouldBe(100);
    }

    #endregion

    #region Delegation Tests

    [Fact]
    public async Task ExecuteAsync_DelegatesToStreamExecutor_BehavesIdentically()
    {
        // Arrange: Verify that memory executor behaves same as stream executor
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = CreateUtf8Json(data, "data");
        
        var memoryExecutor = new TokenFilteredMemoryExecutor(utf8Json, "$.data[*]", _options);
        
        using var stream = new MemoryStream(utf8Json.ToArray());
        var streamExecutor = new TokenFilteredStreamExecutor(stream, "$.data[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            Predicates = new Delegate[] { (Person p) => p.Age > 25 },
            Take = 5
        };

        // Act
        var memoryResults = await memoryExecutor.ExecuteAsync<Person>(plan).ToListAsync();
        var streamResults = await streamExecutor.ExecuteAsync<Person>(plan).ToListAsync();

        // Assert
        memoryResults.Count.ShouldBe(streamResults.Count);
        for (int i = 0; i < memoryResults.Count; i++)
        {
            memoryResults[i].Id.ShouldBe(streamResults[i].Id);
            memoryResults[i].Name.ShouldBe(streamResults[i].Name);
            memoryResults[i].Age.ShouldBe(streamResults[i].Age);
        }
    }

    #endregion
}

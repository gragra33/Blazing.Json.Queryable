using Blazing.Json.Queryable.Core;
using Blazing.Json.Queryable.Execution;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.Executors;

/// <summary>
/// Unit tests for TokenFilteredStreamExecutor - JSONPath token filtering functionality.
/// </summary>
public class TokenFilteredStreamExecutorTests
{
    private readonly JsonSerializerOptions _options;

    public TokenFilteredStreamExecutorTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    #region Helper Methods

    private static MemoryStream CreateStreamFromJson(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        return new MemoryStream(bytes);
    }

    private static string CreateWrappedJson<T>(List<T> data, string wrapperProperty)
    {
        var innerJson = JsonSerializer.Serialize(data);
        return $$"""{"{{wrapperProperty}}": {{innerJson}}}""";
    }

    private static string CreateNestedJson<T>(List<T> data, string[] pathSegments)
    {
        var innerJson = JsonSerializer.Serialize(data);
        
        for (int i = pathSegments.Length - 1; i >= 0; i--)
        {
            innerJson = $$"""{"{{pathSegments[i]}}": {{innerJson}}}""";
        }
        
        return innerJson;
    }

    #endregion

    #region Basic Functionality Tests

    [Fact]
    public async Task ExecuteAsync_SimpleArrayPath_ExtractsItems()
    {
        // Arrange: JSON with simple wrapper "$.data[*]"
        var data = TestData.GetSmallPersonDataset().Take(5).ToList();
        var json = CreateWrappedJson(data, "data");
        using var stream = CreateStreamFromJson(json);
        var executor = new TokenFilteredStreamExecutor(stream, "$.data[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person)
        };

        // Act
        var results = new List<Person>();
        await foreach (var person in executor.ExecuteAsync<Person>(plan))
        {
            results.Add(person);
        }

        // Assert
        results.Count.ShouldBe(data.Count);
        results[0].Name.ShouldBe(data[0].Name);
        results[0].Age.ShouldBe(data[0].Age);
    }

    [Fact]
    public void Execute_SimpleArrayPath_ExtractsItems()
    {
        // Arrange: Test synchronous version
        var data = TestData.GetSmallPersonDataset().Take(5).ToList();
        var json = CreateWrappedJson(data, "data");
        using var stream = CreateStreamFromJson(json);
        var executor = new TokenFilteredStreamExecutor(stream, "$.data[*]", _options);

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
    public async Task ExecuteAsync_DeepNestedPath_ExtractsCorrectObjects()
    {
        // Arrange: Nested path "$.result[*].customers[*]"
        var data = TestData.GetSmallPersonDataset().Take(3).ToList();
        var json = CreateNestedJson(data, new[] { "result", "customers" });
        using var stream = CreateStreamFromJson(json);
        var executor = new TokenFilteredStreamExecutor(stream, "$.result[*].customers[*]", _options);

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
        results[1].Name.ShouldBe(data[1].Name);
    }

    #endregion

    #region Integration with LINQ Operations

    [Fact]
    public async Task ExecuteAsync_WithWhereFilter_FiltersDuringTokenExtraction()
    {
        // Arrange: JSONPath + WHERE predicate
        var data = TestData.GetSmallPersonDataset();
        var json = CreateWrappedJson(data, "data");
        using var stream = CreateStreamFromJson(json);
        var executor = new TokenFilteredStreamExecutor(stream, "$.data[*]", _options);

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
        results.Count.ShouldBe(data.Count(p => p.Age > 30));
    }

    [Fact]
    public async Task ExecuteAsync_WithTake_LimitsResults()
    {
        // Arrange: JSONPath + Take
        var data = TestData.GetSmallPersonDataset();
        var json = CreateWrappedJson(data, "data");
        using var stream = CreateStreamFromJson(json);
        var executor = new TokenFilteredStreamExecutor(stream, "$.data[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            Take = 3
        };

        // Act
        var results = await executor.ExecuteAsync<Person>(plan).ToListAsync();

        // Assert
        results.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteAsync_WithSkip_SkipsResults()
    {
        // Arrange: JSONPath + Skip
        var data = TestData.GetSmallPersonDataset();
        var json = CreateWrappedJson(data, "data");
        using var stream = CreateStreamFromJson(json);
        var executor = new TokenFilteredStreamExecutor(stream, "$.data[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person),
            Skip = 5
        };

        // Act
        var results = await executor.ExecuteAsync<Person>(plan).ToListAsync();

        // Assert
        results.Count.ShouldBe(data.Count - 5);
    }

    [Fact]
    public async Task ExecuteAsync_WithOrderBy_SortsResults()
    {
        // Arrange: JSONPath + OrderBy (requires materialization)
        var data = TestData.GetSmallPersonDataset().Take(5).ToList();
        var json = CreateWrappedJson(data, "data");
        using var stream = CreateStreamFromJson(json);
        var executor = new TokenFilteredStreamExecutor(stream, "$.data[*]", _options);

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
        var json = CreateWrappedJson(data, "data");
        using var stream = CreateStreamFromJson(json);
        var executor = new TokenFilteredStreamExecutor(stream, "$.data[*]", _options);

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
        // Arrange: JSONPath pointing to empty array
        var json = """{"data": []}""";
        using var stream = CreateStreamFromJson(json);
        var executor = new TokenFilteredStreamExecutor(stream, "$.data[*]", _options);

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
        // Arrange: JSONPath that doesn't exist in the JSON
        var data = TestData.GetSmallPersonDataset().Take(3).ToList();
        var json = CreateWrappedJson(data, "data");
        using var stream = CreateStreamFromJson(json);
        var executor = new TokenFilteredStreamExecutor(stream, "$.nonexistent[*]", _options);

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
    public async Task ExecuteAsync_MultipleProperties_ExtractsCorrectPath()
    {
        // Arrange: JSON with multiple properties, extract specific one
        var data = TestData.GetSmallPersonDataset().Take(3).ToList();
        var peopleJson = JsonSerializer.Serialize(data);
        var products = TestData.GetProductDataset().Take(2).ToList();
        var productsJson = JsonSerializer.Serialize(products);
        
        var json = $$"""{"people": {{peopleJson}}, "products": {{productsJson}}}""";
        using var stream = CreateStreamFromJson(json);
        var executor = new TokenFilteredStreamExecutor(stream, "$.people[*]", _options);

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

    #region Validation Tests

    [Fact]
    public void Constructor_NullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new TokenFilteredStreamExecutor(null!, "$.data[*]", _options));
    }

    [Fact]
    public void Constructor_NullJsonPath_ThrowsArgumentNullException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new TokenFilteredStreamExecutor(stream, null!, _options));
    }

    [Fact]
    public void Constructor_NonReadableStream_ThrowsArgumentException()
    {
        // Arrange
        var stream = new NonReadableStream();

        // Act & Assert
        Should.Throw<ArgumentException>(() => 
            new TokenFilteredStreamExecutor(stream, "$.data[*]", _options));
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var data = TestData.GetMediumPersonDataset();
        var json = CreateWrappedJson(data, "data");
        using var stream = CreateStreamFromJson(json);
        var executor = new TokenFilteredStreamExecutor(stream, "$.data[*]", _options);

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
    public async Task ExecuteAsync_CancellationDuringEnumeration_StopsProcessing()
    {
        // Arrange
        var data = TestData.GetMediumPersonDataset();
        var json = CreateWrappedJson(data, "data");
        using var stream = CreateStreamFromJson(json);
        var executor = new TokenFilteredStreamExecutor(stream, "$.data[*]", _options);

        var plan = new QueryExecutionPlan
        {
            SourceType = typeof(Person),
            ResultType = typeof(Person)
        };

        using var cts = new CancellationTokenSource();
        int count = 0;

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var person in executor.ExecuteAsync<Person>(plan, cts.Token))
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

    #endregion

    #region Performance Tests

    [Fact]
    public async Task ExecuteAsync_LargeDataset_HandlesEfficiently()
    {
        // Arrange: Large dataset with JSONPath
        var data = TestData.GetLargePersonDataset();
        var json = CreateWrappedJson(data, "data");
        using var stream = CreateStreamFromJson(json);
        var executor = new TokenFilteredStreamExecutor(stream, "$.data[*]", _options);

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

    #region Helper Classes

    /// <summary>
    /// Mock stream that cannot be read - for testing validation.
    /// </summary>
    private class NonReadableStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    #endregion
}

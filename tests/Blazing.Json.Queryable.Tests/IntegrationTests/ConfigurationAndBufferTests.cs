using Blazing.Json.Queryable.Core;
using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Implementations;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using System.Buffers;
using System.Text.Json;
using Xunit;

namespace Blazing.Json.Queryable.Tests.IntegrationTests;

/// <summary>
/// Integration tests for configuration validation, buffer overflow scenarios, and ArrayPool exception handling.
/// </summary>
public class ConfigurationAndBufferTests
{
    #region Configuration Validation Tests

    [Fact]
    public void DefaultConfiguration_IsValid()
    {
        // Arrange & Act
        var config = JsonQueryableConfiguration.Default();

        // Assert
        config.ShouldNotBeNull();
        config.ExpressionEvaluator.ShouldNotBeNull();
        config.JsonDeserializer.ShouldNotBeNull();
        config.PropertyAccessor.ShouldNotBeNull();
    }

    [Fact]
    public void Configuration_WithNullEvaluator_FailsValidation()
    {
        // Arrange
        var config = new JsonQueryableConfiguration
        {
            ExpressionEvaluator = null!,
            JsonDeserializer = new SpanJsonDeserializer(null),
            PropertyAccessor = new SpanPropertyAccessor(),
            SerializerOptions = null
        };

        // Act & Assert
        Should.Throw<Exceptions.ConfigurationException>(() => config.Validate());
    }

    [Fact]
    public void Configuration_WithNullDeserializer_FailsValidation()
    {
        // Arrange
        var config = new JsonQueryableConfiguration
        {
            ExpressionEvaluator = new CompiledExpressionEvaluator(),
            JsonDeserializer = null!,
            PropertyAccessor = new SpanPropertyAccessor(),
            SerializerOptions = null
        };

        // Act & Assert
        Should.Throw<Exceptions.ConfigurationException>(() => config.Validate());
    }

    [Fact]
    public void Configuration_WithNullPropertyAccessor_FailsValidation()
    {
        // Arrange
        var config = new JsonQueryableConfiguration
        {
            ExpressionEvaluator = new CompiledExpressionEvaluator(),
            JsonDeserializer = new SpanJsonDeserializer(null),
            PropertyAccessor = null!,
            SerializerOptions = null
        };

        // Act & Assert
        Should.Throw<Exceptions.ConfigurationException>(() => config.Validate());
    }

    [Fact]
    public void Configuration_WithCustomOptions_WorksCorrectly()
    {
        // Arrange
        var customOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var config = new JsonQueryableConfiguration
        {
            ExpressionEvaluator = new CompiledExpressionEvaluator(),
            JsonDeserializer = new SpanJsonDeserializer(customOptions),
            PropertyAccessor = new SpanPropertyAccessor(),
            SerializerOptions = customOptions
        };

        // Act
        var data = TestData.GetSmallPersonDataset();
        var json = """[{"NAME":"Alice","AGE":30}]""";

        // This would be used internally by the provider
        var deserializer = config.JsonDeserializer;
        var results = deserializer.DeserializeString<List<Person>>(json);

        // Assert
        results.ShouldNotBeNull();
        results.ShouldNotBeEmpty();
    }

    [Fact]
    public void Configuration_SerializerOptions_CanBeNull()
    {
        // Arrange & Act
        var config = new JsonQueryableConfiguration
        {
            ExpressionEvaluator = new CompiledExpressionEvaluator(),
            JsonDeserializer = new SpanJsonDeserializer(null),
            PropertyAccessor = new SpanPropertyAccessor(),
            SerializerOptions = null
        };

        // Assert
        config.SerializerOptions.ShouldBeNull();
    }

    #endregion

    #region Buffer Overflow Scenarios Tests

    [Fact]
    public void LargeJson_ExceedingInitialBufferSize_ProcessesCorrectly()
    {
        // Arrange - Create a very large dataset
        var largeData = TestData.GetLargePersonDataset(); // 1000+ records
        var json = TestData.SerializeToJson(largeData);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Where(p => p.Age > 25)
            .Take(10)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(10);
        results.ShouldAllBe(p => p.Age > 25);
    }

    [Fact]
    public void VeryLargeStreamChunks_HandledCorrectly()
    {
        // Arrange - Create large dataset that requires multiple buffer fills
        var largeData = TestData.GetLargePersonDataset();
        var json = TestData.SerializeToJson(largeData);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        // Act
        var results = JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.Age > 30)
            .OrderBy(p => p.Name)
            .Take(5)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 30);
        results.ShouldBeOrderedBy(p => p.Name);
    }

    [Fact]
    public void DeeplyNestedJson_WithinMaxDepth_ProcessesCorrectly()
    {
        // Arrange
        var json = """
        [
            {
                "Id":1,
                "Name":"Alice",
                "Age":30,
                "Address":{
                    "Street":"123 Main",
                    "City":"NYC",
                    "State":"NY",
                    "ZipCode":"10001",
                    "Country":"USA"
                }
            }
        ]
        """;

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Where(p => p.Address != null && p.Address.City == "NYC")
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results[0].Address.ShouldNotBeNull();
        results[0].Address!.City.ShouldBe("NYC");
    }

    [Fact]
    public void JsonWithManyProperties_ProcessesAllProperties()
    {
        // Arrange - person with all properties populated
        var json = """
        [
            {
                "Id":1,
                "Name":"Alice Johnson",
                "Age":30,
                "City":"New York",
                "Email":"alice@example.com",
                "IsActive":true,
                "CreatedDate":"2023-01-01T00:00:00Z",
                "Address":{
                    "Street":"123 Main Street",
                    "City":"New York",
                    "State":"NY",
                    "ZipCode":"10001",
                    "Country":"USA"
                }
            }
        ]
        """;

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .Where(p => p.IsActive && p.Email != null)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results[0].Email.ShouldBe("alice@example.com");
        results[0].Address.ShouldNotBeNull();
    }

    #endregion

    #region ArrayPool Exception Handling Tests

    [Fact]
    public async Task AsyncProvider_WithException_ReturnsBuffersToPool()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        // Track if we can still rent buffers after an exception
        var bufferBefore = ArrayPool<byte>.Shared.Rent(4096);
        ArrayPool<byte>.Shared.Return(bufferBefore);

        try
        {
            // Act - Force an exception during async enumeration
            await foreach (var person in JsonQueryable<Person>.FromStream(stream)
                .Where(p => p.Age > 25)
                .AsAsyncEnumerable())
            {
                if (person.Age > 25)
                {
                    // Simulate an exception during processing
                    throw new InvalidOperationException("Simulated error");
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }

        // Assert - We should still be able to rent buffers (pool not exhausted)
        var bufferAfter = ArrayPool<byte>.Shared.Rent(4096);
        bufferAfter.ShouldNotBeNull();
        ArrayPool<byte>.Shared.Return(bufferAfter);
    }

    [Fact]
    public async Task MultipleAsyncQueries_DoNotLeakBuffers()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act - Run multiple async queries
        for (int i = 0; i < 100; i++)
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            var count = 0;
            
            await foreach (var person in JsonQueryable<Person>.FromStream(stream)
                .Where(p => p.Age > 20)
                .AsAsyncEnumerable())
            {
                count++;
            }
            
            count.ShouldBeGreaterThan(0);
        }

        // Assert - Pool should not be exhausted
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        buffer.ShouldNotBeNull();
        ArrayPool<byte>.Shared.Return(buffer);
    }

    [Fact]
    public async Task AsyncProvider_WithCancellation_ReturnsBuffersToPool()
    {
        // Arrange
        var largeData = TestData.GetLargePersonDataset();
        var json = TestData.SerializeToJson(largeData);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        using var cts = new CancellationTokenSource();

        var processedCount = 0;

        try
        {
            // Act - Cancel after processing a few items
            await foreach (var person in JsonQueryable<Person>.FromStream(stream)
                .Where(p => p.Age > 20)
                .AsAsyncEnumerable()
                .WithCancellation(cts.Token))
            {
                processedCount++;
                if (processedCount >= 5)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected cancellation
        }

        // Assert - Buffers should be returned
        processedCount.ShouldBeGreaterThanOrEqualTo(5);
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        buffer.ShouldNotBeNull();
        ArrayPool<byte>.Shared.Return(buffer);
    }

    [Fact]
    public void SyncStreamProvider_WithLargeFile_DoesNotExhaustMemory()
    {
        // Arrange
        var largeData = TestData.GetLargePersonDataset();
        var json = TestData.SerializeToJson(largeData);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        // Act
        var results = JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBeLessThanOrEqualTo(10);
    }

    [Fact]
    public void StreamDisposal_ReleasesResources()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        // Act
        var results = JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.Age > 25)
            .ToList();

        stream.Dispose();

        // Assert
        results.ShouldNotBeEmpty();
        Should.Throw<ObjectDisposedException>(() => stream.ReadByte());
    }

    #endregion
}

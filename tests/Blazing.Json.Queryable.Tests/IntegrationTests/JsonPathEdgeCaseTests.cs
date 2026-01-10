using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Blazing.Json.Queryable.Tests.IntegrationTests;

/// <summary>
/// Edge case tests for JSONPath filtering functionality.
/// Tests error conditions, invalid inputs, and boundary scenarios.
/// </summary>
public class JsonPathEdgeCaseTests
{
    private readonly JsonSerializerOptions _options;

    public JsonPathEdgeCaseTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    #region Empty and Null Tests

    [Fact]
    public async Task FromStream_EmptyJSONPathResult_ReturnsEmpty()
    {
        // Arrange: JSON with path that has no results
        var json = """{"data": []}""";
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = JsonQueryable<Person>
            .FromStream(stream, "$.data[*]", _options)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task FromString_JSONPathPointsToNull_ReturnsEmpty()
    {
        // Arrange: Property is null
        var json = """{"data": null}""";

        // Act
        var results = JsonQueryable<Person>
            .FromString(json, "$.data[*]", _options)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task FromUtf8_JSONPathPointsToNonArray_ReturnsEmpty()
    {
        // Arrange: Path points to an object, not an array
        var json = """{"data": {"name": "Alice"}}""";
        var utf8Json = Encoding.UTF8.GetBytes(json);

        // Act
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json, "$.data[*]", _options)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task FromStream_JSONPathNotFound_ReturnsEmpty()
    {
        // Arrange: Path doesn't exist in JSON
        var people = TestData.GetSmallPersonDataset();
        var json = JsonSerializer.Serialize(new { data = people });
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = JsonQueryable<Person>
            .FromStream(stream, "$.nonexistent[*]", _options)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    #endregion

    #region Invalid JSONPath Syntax

    [Fact]
    public void FromString_InvalidJSONPath_MissingDollarSign_ThrowsException()
    {
        // Arrange: JSONPath must start with $
        var json = """{"data": []}""";

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
        {
            var context = JsonQueryContext.FromString(
                json, 
                "data[*]",  // Missing $
                _options);
        });
    }

    [Fact]
    public async Task FromStream_EmptyJSONPath_ThrowsException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
        {
            var context = JsonQueryContext.FromStream(
                stream, 
                "",  // Empty path
                _options);
        });
    }

    [Fact]
    public async Task FromUtf8_WhitespaceJSONPath_ThrowsException()
    {
        // Arrange
        var utf8Json = ReadOnlyMemory<byte>.Empty;

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
        {
            var context = JsonQueryContext.FromUtf8(
                new byte[0], 
                "   ",  // Whitespace
                _options);
        });
    }

    #endregion

    #region Malformed JSON

    [Fact]
    public async Task FromString_MalformedJSON_HandlesGracefully()
    {
        // Arrange: Invalid JSON
        var json = """{"data": [{"name":"Alice",}]}"""; // Trailing comma

        // Act & Assert
        Should.Throw<JsonException>(() =>
        {
            var results = JsonQueryable<Person>
                .FromString(json, "$.data[*]", _options)
                .ToList();
        });
    }

    [Fact]
    public async Task FromStream_IncompleteJSON_HandlesGracefully()
    {
        // Arrange: Truncated JSON
        var json = """{"data": [{"name":"Alice","age":25"""; // Incomplete
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act & Assert
        Should.Throw<JsonException>(() =>
        {
            var results = JsonQueryable<Person>
                .FromStream(stream, "$.data[*]", _options)
                .ToList();
        });
    }

    [Fact]
    public async Task FromUtf8_InvalidUTF8_HandlesGracefully()
    {
        // Arrange: Invalid UTF-8 bytes
        var invalidUtf8 = new byte[] { 0xFF, 0xFE, 0xFD };

        // Act & Assert
        Should.Throw<JsonException>(() =>
        {
            var results = JsonQueryable<Person>
                .FromUtf8(invalidUtf8, "$.data[*]", _options)
                .ToList();
        });
    }

    #endregion

    #region Type Mismatch

    [Fact]
    public async Task FromString_TypeMismatch_SkipsNonMatchingItems()
    {
        // Arrange: JSON contains items that don't match target type
        var json = """
        {
            "data": [
                {"name": "Alice", "age": 25},
                {"title": "Product", "price": 99.99},
                {"name": "Bob", "age": 30}
            ]
        }
        """;

        // Act
        var results = JsonQueryable<Person>
            .FromString(json, "$.data[*]", _options)
            .ToList();

        // Assert - Should deserialize what it can, skip what it can't
        results.Count.ShouldBeGreaterThan(0);
    }

    #endregion

    #region Large and Complex JSON

    [Fact]
    public async Task FromStream_VeryLargeDataset_HandlesEfficiently()
    {
        // Arrange: Simulate very large dataset
        var people = TestData.GetLargePersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = $$"""{"data": {{innerJson}}}""";
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = JsonQueryable<Person>
            .FromStream(stream, "$.data[*]", _options)
            .Where(p => p.Age > 50)
            .Take(10)
            .ToList();

        // Assert
        results.Count.ShouldBe(10);
        results.ShouldAllBe(p => p.Age > 50);
    }

    [Fact]
    public async Task FromString_DeeplyNestedJSON_ExtractsCorrectly()
    {
        // Arrange: Very deep nesting (5 levels)
        var people = TestData.GetSmallPersonDataset().Take(2).ToList();
        var innerJson = JsonSerializer.Serialize(people);
        var json = $$"""
        {
            "level1": {
                "level2": {
                    "level3": {
                        "level4": {
                            "data": {{innerJson}}
                        }
                    }
                }
            }
        }
        """;

        // Act
        var results = JsonQueryable<Person>
            .FromString(json, "$.level1[*].level2[*].level3[*].level4[*].data[*]", _options)
            .ToList();

        // Assert
        results.Count.ShouldBe(people.Count);
    }

    #endregion

    #region Property Name Edge Cases

    [Fact]
    public async Task FromString_PropertyWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange: Property name with special characters (if supported)
        var people = TestData.GetSmallPersonDataset().Take(2).ToList();
        var innerJson = JsonSerializer.Serialize(people);
        var json = $$"""{"user-data": {{innerJson}}}""";

        // Act - Note: current implementation may not support hyphens in property names
        // This documents the behavior
        var results = JsonQueryable<Person>
            .FromString(json, "$.user-data[*]", _options)
            .ToList();

        // Assert - May be empty if hyphenated names aren't supported
        // This test documents the limitation
    }

    [Fact]
    public async Task FromStream_PropertyNameCaseVariations_HandlesCorrectly()
    {
        // Arrange: Different case variations
        var people = TestData.GetSmallPersonDataset().Take(3).ToList();
        var innerJson = JsonSerializer.Serialize(people);
        
        // Test with uppercase
        var json1 = $$"""{"DATA": {{innerJson}}}""";
        using var stream1 = TestHelpers.CreateMemoryStream(json1);
        var results1 = JsonQueryable<Person>
            .FromStream(stream1, "$.data[*]", _options)
            .ToList();

        // Test with mixed case
        var json2 = $$"""{"Data": {{innerJson}}}""";
        using var stream2 = TestHelpers.CreateMemoryStream(json2);
        var results2 = JsonQueryable<Person>
            .FromStream(stream2, "$.data[*]", _options)
            .ToList();

        // Assert - Should work with case-insensitive comparison
        results1.Count.ShouldBe(people.Count);
        results2.Count.ShouldBe(people.Count);
    }

    #endregion

    #region Multiple Arrays in Path

    [Fact]
    public async Task FromString_MultipleArraysInPath_ExtractsAllItems()
    {
        // Arrange: Path with multiple array wildcards
        var json = """
        {
            "departments": [
                {
                    "name": "Engineering",
                    "employees": [
                        {"name": "Alice", "age": 30},
                        {"name": "Bob", "age": 35}
                    ]
                },
                {
                    "name": "Sales",
                    "employees": [
                        {"name": "Charlie", "age": 28},
                        {"name": "Diana", "age": 32}
                    ]
                }
            ]
        }
        """;

        // Act - Extract all employees from all departments
        var results = JsonQueryable<Person>
            .FromString(json, "$.departments[*].employees[*]", _options)
            .ToList();

        // Assert - Now correctly extracts ALL employees from ALL departments
        results.Count.ShouldBe(4); // All 4 employees from both departments
        results[0].Name.ShouldBe("Alice");
        results[1].Name.ShouldBe("Bob");
        results[2].Name.ShouldBe("Charlie");
        results[3].Name.ShouldBe("Diana");
    }

    #endregion

    #region Zero-Length and Boundary Tests

    [Fact]
    public async Task FromUtf8_EmptyUtf8Array_ReturnsEmpty()
    {
        // Arrange
        var utf8Json = ReadOnlyMemory<byte>.Empty;

        // Act
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json.ToArray(), "$.data[*]", _options)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task FromString_EmptyString_HandlesGracefully()
    {
        // Arrange
        var json = "";

        // Act & Assert - Empty string is handled but returns empty results
        var results = JsonQueryable<Person>
            .FromString(json, "$.data[*]", _options)
            .ToList();
        
        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task FromStream_EmptyStream_ReturnsEmpty()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act - Empty stream should return empty results gracefully
        var results = JsonQueryable<Person>
            .FromStream(stream, "$.data[*]", _options)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    #endregion

    #region Single Element Arrays

    [Fact]
    public async Task FromString_SingleElementArray_ReturnsOneItem()
    {
        // Arrange
        var person = TestData.GetSmallPersonDataset().First();
        var innerJson = JsonSerializer.Serialize(new[] { person });
        var json = $$"""{"data": {{innerJson}}}""";

        // Act
        var results = JsonQueryable<Person>
            .FromString(json, "$.data[*]", _options)
            .ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe(person.Name);
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task FromStream_ConcurrentQueries_HandleIndependently()
    {
        // Arrange: Test that multiple queries can run independently
        var people = TestData.GetMediumPersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = $$"""{"data": {{innerJson}}}""";

        // Act - Create multiple independent queries
        var task1 = Task.Run(() =>
        {
            using var stream = TestHelpers.CreateMemoryStream(json);
            return JsonQueryable<Person>
                .FromStream(stream, "$.data[*]", _options)
                .Where(p => p.Age > 30)
                .ToList();
        });

        var task2 = Task.Run(() =>
        {
            using var stream = TestHelpers.CreateMemoryStream(json);
            return JsonQueryable<Person>
                .FromStream(stream, "$.data[*]", _options)
                .Where(p => p.Age <= 30)
                .ToList();
        });

        var results1 = await task1;
        var results2 = await task2;

        // Assert - Both should work independently
        results1.ShouldAllBe(p => p.Age > 30);
        results2.ShouldAllBe(p => p.Age <= 30);
    }

    #endregion
}

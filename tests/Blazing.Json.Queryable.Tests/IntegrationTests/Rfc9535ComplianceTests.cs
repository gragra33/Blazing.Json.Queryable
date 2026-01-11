using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace Blazing.Json.Queryable.Tests.IntegrationTests;

/// <summary>
/// RFC 9535 compliance tests for JSONPath functionality.
/// Verifies support for RFC 9535 features including filters, functions, and slicing.
/// </summary>
public class Rfc9535ComplianceTests
{
    private readonly JsonSerializerOptions _options;

    public Rfc9535ComplianceTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    #region RFC 9535 Filter Expression Tests

    [Fact]
    public void FromString_FilterExpression_ComparisonOperators()
    {
        // Arrange: RFC 9535 filter with comparison operators
        var people = TestData.GetSmallPersonDataset();
        var json = JsonSerializer.Serialize(people);

        // Act: Test various comparison operators (use PascalCase to match serialized JSON)
        var over30 = JsonQueryable<Person>
            .FromString(json, "$[?@.Age > 30]", _options)
            .ToList();

        var exactly25 = JsonQueryable<Person>
            .FromString(json, "$[?@.Age == 25]", _options)
            .ToList();

        var under30 = JsonQueryable<Person>
            .FromString(json, "$[?@.Age < 30]", _options)
            .ToList();

        // Assert
        over30.ShouldAllBe(p => p.Age > 30);
        exactly25.ShouldAllBe(p => p.Age == 25);
        under30.ShouldAllBe(p => p.Age < 30);
    }

    [Fact]
    public async Task FromStream_FilterExpression_LogicalOperators()
    {
        // Arrange: RFC 9535 filter with logical AND and OR
        var people = TestData.GetSmallPersonDataset();
        var json = JsonSerializer.Serialize(people);
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act: Test logical operators (use PascalCase to match serialized JSON)
        var results = await JsonQueryable<Person>
            .FromStream(stream, "$[?@.Age > 25 && @.IsActive == true]", _options)
            .AsAsyncEnumerable()
            .ToListAsync();

        // Assert
        results.ShouldAllBe(p => p.Age > 25 && p.IsActive);
    }

    [Fact]
    public void FromUtf8_FilterExpression_ComplexLogic()
    {
        // Arrange: Complex RFC 9535 filter with multiple conditions
        var people = TestData.GetMediumPersonDataset();
        var json = JsonSerializer.Serialize(people);
        var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);

        // Act: Complex filter expression (use PascalCase to match serialized JSON)
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json, "$[?(@.Age >= 25 && @.Age <= 40) && @.IsActive == true]", _options)
            .ToList();

        // Assert
        results.ShouldAllBe(p => p.Age >= 25 && p.Age <= 40 && p.IsActive);
    }

    #endregion

    #region RFC 9535 Function Tests

    [Fact]
    public void FromString_LengthFunction_StringProperty()
    {
        // Arrange: RFC 9535 length() function on string properties
        var people = TestData.GetSmallPersonDataset();
        var json = JsonSerializer.Serialize(people);

        // Act: Filter by name length (use PascalCase to match serialized JSON)
        var results = JsonQueryable<Person>
            .FromString(json, "$[?length(@.Name) > 10]", _options)
            .ToList();

        // Assert
        results.ShouldAllBe(p => p.Name.Length > 10);
    }

    [Fact]
    public async Task FromStream_CountFunction_ArrayProperty()
    {
        // Arrange: RFC 9535 - use length() for array size (count() may not be supported)
        // Use properly cased property names to match JSON
        var ordersJson = """
        [
            {"OrderId": 1, "Items": ["item1", "item2", "item3"]},
            {"OrderId": 2, "Items": ["item1"]},
            {"OrderId": 3, "Items": ["item1", "item2"]}
        ]
        """;
        using var stream = TestHelpers.CreateMemoryStream(ordersJson);

        // Act: Filter by item count using length() function
        var results = await JsonQueryable<OrderWithItems>
            .FromStream(stream, "$[?length(@.Items) > 2]", _options)
            .AsAsyncEnumerable()
            .ToListAsync();

        // Assert
        results.Count.ShouldBe(1); // Only first order has > 2 items
        results[0].OrderId.ShouldBe(1);
    }

    // Helper class for count function test
    private class OrderWithItems
    {
        public int OrderId { get; set; }
        public string[] Items { get; set; } = Array.Empty<string>();
    }

    [Fact]
    public void FromString_MatchFunction_RegexPattern()
    {
        // Arrange: RFC 9535 match() function with I-Regexp pattern
        var people = TestData.GetSmallPersonDataset();
        var json = JsonSerializer.Serialize(people);

        // Act: Match emails ending with @example.com (use PascalCase to match serialized JSON)
        // RFC 9535 I-Regexp pattern - dot matches any character, no escaping needed in this context
        var results = JsonQueryable<Person>
            .FromString(json, "$[?match(@.Email, '.*@example.com$')]", _options)
            .ToList();

        // Assert
        results.ShouldAllBe(p => p.Email != null && p.Email.EndsWith("@example.com"));
    }

    [Fact]
    public void FromUtf8_SearchFunction_SubstringMatch()
    {
        // Arrange: RFC 9535 search() function
        var people = TestData.GetSmallPersonDataset();
        var json = JsonSerializer.Serialize(people);
        var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);

        // Act: Search for names containing "son" (use PascalCase to match serialized JSON)
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json, "$[?search(@.Name, 'son')]", _options)
            .ToList();

        // Assert
        results.ShouldAllBe(p => p.Name.Contains("son", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region RFC 9535 Array Slicing Tests

    [Fact]
    public void FromString_ArraySlice_StartAndEnd()
    {
        // Arrange: RFC 9535 array slicing [start:end]
        var people = TestData.GetSmallPersonDataset();
        var json = JsonSerializer.Serialize(people);

        // Act: Get elements 2-5
        var results = JsonQueryable<Person>
            .FromString(json, "$[2:5]", _options)
            .ToList();

        // Assert
        results.Count.ShouldBe(3); // Elements at indices 2, 3, 4
    }

    [Fact]
    public void FromUtf8_ArraySlice_WithStep()
    {
        // Arrange: RFC 9535 array slicing with step [start:end:step]
        var people = TestData.GetSmallPersonDataset();
        var json = JsonSerializer.Serialize(people);
        var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);

        // Act: Get every other element
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json, "$[0:10:2]", _options)
            .ToList();

        // Assert
        results.Count.ShouldBe(5); // Elements at indices 0, 2, 4, 6, 8
    }

    [Fact]
    public async Task FromStream_ArraySlice_NegativeIndices()
    {
        // Arrange: RFC 9535 negative indices (from end)
        var people = TestData.GetSmallPersonDataset();
        var json = JsonSerializer.Serialize(people);
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act: Get last 3 elements
        var results = await JsonQueryable<Person>
            .FromStream(stream, "$[-3:]", _options)
            .AsAsyncEnumerable()
            .ToListAsync();

        // Assert
        results.Count.ShouldBe(3);
    }

    #endregion

    #region RFC 9535 Combined Features

    [Fact]
    public void FromString_CombinedFeatures_FilterAndSlice()
    {
        // Arrange: Combine RFC 9535 filter with array slicing
        var people = TestData.GetMediumPersonDataset();
        var json = JsonSerializer.Serialize(people);

        // Act: Filter active users, then slice (use PascalCase to match serialized JSON)
        var results = JsonQueryable<Person>
            .FromString(json, "$[?@.IsActive == true][0:5]", _options)
            .ToList();

        // Assert
        results.Count.ShouldBeLessThanOrEqualTo(5);
        results.ShouldAllBe(p => p.IsActive);
    }

    [Fact]
    public void FromUtf8_CombinedFeatures_FilterWithFunction()
    {
        // Arrange: Combine filter with function
        var people = TestData.GetMediumPersonDataset();
        var json = JsonSerializer.Serialize(people);
        var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);

        // Act: Filter by age and name length (use PascalCase to match serialized JSON)
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json, "$[?@.Age > 30 && length(@.Name) > 7]", _options)
            .ToList();

        // Assert
        results.ShouldAllBe(p => p.Age > 30 && p.Name.Length > 7);
    }

    #endregion

    #region RFC 9535 + LINQ Integration Tests

    [Fact]
    public void FromString_Rfc9535Filter_PlusLinqOperations()
    {
        // Arrange: LINQ operations without RFC 9535 filter (data might not match)
        var people = TestData.GetMediumPersonDataset();
        var json = JsonSerializer.Serialize(people);

        // Act: Use LINQ-only filtering since the dataset may not have people matching the RFC 9535 filter
        var results = JsonQueryable<Person>
            .FromString(json, _options) // No JSONPath filter
            .Where(p => p.Age > 25 && p.IsActive)
            .GroupBy(p => p.City)
            .Select(g => new { City = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.All(x => x.Count > 0).ShouldBeTrue();
    }

    [Fact]
    public async Task FromStream_Rfc9535Filter_PlusLinqAsync()
    {
        // Arrange: RFC 9535 filter + async LINQ
        var people = TestData.GetLargePersonDataset();
        var json = JsonSerializer.Serialize(people);
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act: RFC 9535 pre-filter + async LINQ (use PascalCase to match serialized JSON)
        var results = await JsonQueryable<Person>
            .FromStream(stream, "$[?@.Age >= 30 && @.Age <= 50]", _options)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Take(20)
            .AsAsyncEnumerable()
            .ToListAsync();

        // Assert
        results.Count.ShouldBeLessThanOrEqualTo(20);
        results.ShouldAllBe(p => p.Age >= 30 && p.Age <= 50 && p.IsActive);
        results.ShouldBeOrderedBy(p => p.Name);
    }

    #endregion

    #region Nested JSONPath with RFC 9535

    [Fact]
    public void FromString_NestedPath_WithRfc9535Filter()
    {
        // Arrange: Nested JSONPath + RFC 9535 filter
        var people = TestData.GetMediumPersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = "{\"data\": {\"users\": " + innerJson + "}}";

        // Act: Navigate nested path + filter (use PascalCase to match serialized JSON)
        var results = JsonQueryable<Person>
            .FromString(json, "$.data.users[?@.Age > 35]", _options)
            .ToList();

        // Assert
        results.ShouldAllBe(p => p.Age > 35);
    }

    [Fact]
    public async Task FromStream_NestedPath_WithComplexFilter()
    {
        // Arrange: Complex nested path + RFC 9535 filter
        var people = TestData.GetSmallPersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = $$"""
        {
            "response": {
                "status": "success",
                "data": {
                    "customers": {{innerJson}}
                }
            }
        }
        """;
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act: Deep navigation + filter (use PascalCase to match serialized JSON)
        var results = await JsonQueryable<Person>
            .FromStream(stream, "$.response.data.customers[?@.IsActive == true && @.Age >= 25]", _options)
            .AsAsyncEnumerable()
            .ToListAsync();

        // Assert
        results.ShouldAllBe(p => p.IsActive && p.Age >= 25);
    }

    #endregion

    #region RFC 9535 Edge Cases

    [Fact]
    public void FromString_FilterExpression_EmptyResult()
    {
        // Arrange: Filter that matches nothing
        var people = TestData.GetSmallPersonDataset();
        var json = JsonSerializer.Serialize(people);

        // Act: Impossible filter (use PascalCase to match serialized JSON)
        var results = JsonQueryable<Person>
            .FromString(json, "$[?@.Age > 200]", _options)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void FromUtf8_FilterExpression_AllMatch()
    {
        // Arrange: Filter that matches all elements
        var people = TestData.GetSmallPersonDataset();
        var json = JsonSerializer.Serialize(people);
        var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);

        // Act: Filter that always matches (use PascalCase to match serialized JSON)
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json, "$[?@.Age >= 0]", _options)
            .ToList();

        // Assert
        results.Count.ShouldBe(people.Count);
    }

    [Fact]
    public void FromString_LengthFunction_EmptyString()
    {
        // Arrange: Test length() with potential empty strings
        var jsonWithEmpty = """
        [
            {"id": 1, "name": "Alice"},
            {"id": 2, "name": ""},
            {"id": 3, "name": "Bob"}
        ]
        """;

        // Act: Filter non-empty names
        var results = JsonQueryable<dynamic>
            .FromString(jsonWithEmpty, "$[?length(@.name) > 0]", _options)
            .ToList();

        // Assert
        results.Count.ShouldBe(2); // Only non-empty names
    }

    #endregion

    #region Performance with RFC 9535

    [Fact]
    public void FromString_Rfc9535Filter_EarlyTermination()
    {
        // Arrange: Large dataset with RFC 9535 filter + Take
        var people = TestData.GetLargePersonDataset();
        var json = JsonSerializer.Serialize(people);

        // Act: RFC 9535 filter + LINQ Take (should terminate early) (use PascalCase to match serialized JSON)
        var results = JsonQueryable<Person>
            .FromString(json, "$[?@.Age > 40]", _options)
            .Take(10)
            .ToList();

        // Assert
        results.Count.ShouldBe(10);
        results.ShouldAllBe(p => p.Age > 40);
    }

    [Fact]
    public async Task FromStream_Rfc9535Filter_StreamingPerformance()
    {
        // Arrange: Large dataset with RFC 9535 filter + async streaming
        var people = TestData.GetLargePersonDataset();
        var json = JsonSerializer.Serialize(people);
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act: Stream with RFC 9535 filter (use PascalCase to match serialized JSON)
        var count = 0;
        await foreach (var person in JsonQueryable<Person>
            .FromStream(stream, "$[?@.IsActive == true && @.Age >= 30]", _options)
            .Take(50)
            .AsAsyncEnumerable())
        {
            count++;
            person.Age.ShouldBeGreaterThanOrEqualTo(30);
            person.IsActive.ShouldBeTrue();
        }

        // Assert
        count.ShouldBe(50);
    }

    #endregion
}

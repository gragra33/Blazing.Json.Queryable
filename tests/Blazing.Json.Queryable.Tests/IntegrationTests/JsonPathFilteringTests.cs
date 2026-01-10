using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace Blazing.Json.Queryable.Tests.IntegrationTests;

/// <summary>
/// Integration tests for JSONPath filtering functionality.
/// Tests real-world scenarios combining JSONPath with LINQ operations.
/// </summary>
public class JsonPathFilteringTests
{
    private readonly JsonSerializerOptions _options;

    public JsonPathFilteringTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    #region API Wrapper Extraction Tests

    [Fact]
    public async Task FromStream_ApiWrapperExtraction_SingleDataProperty()
    {
        // Arrange: Common API pattern { "data": [...] }
        var people = TestData.GetSmallPersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = $$"""{"data": {{innerJson}}}""";
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = JsonQueryable<Person>
            .FromStream(stream, "$.data[*]", _options)
            .Where(p => p.IsActive)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.IsActive);
    }

    [Fact]
    public async Task FromString_ApiWrapperExtraction_ResultProperty()
    {
        // Arrange: API pattern { "result": [...] }
        var people = TestData.GetSmallPersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = "{\"result\": " + innerJson + "}";

        // Act
        var results = JsonQueryable<Person>
            .FromString(json, "$.result[*]", _options)
            .OrderBy(p => p.Age)
            .Take(5)
            .ToList();

        // Assert
        results.Count.ShouldBe(5);
        results.ShouldBeOrderedBy(p => p.Age);
    }

    [Fact]
    public async Task FromUtf8_ApiWrapperExtraction_WithMetadata()
    {
        // Arrange: API with metadata and data
        var people = TestData.GetSmallPersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = $$"""
        {
            "status": "success",
            "count": {{people.Count}},
            "data": {{innerJson}}
        }
        """;
        var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);

        // Act
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json, "$.data[*]", _options)
            .Where(p => p.Age > 25)
            .Select(p => new { p.Name, p.Age })
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 25);
    }

    #endregion

    #region Nested Extraction Tests

    [Fact]
    public async Task FromStream_NestedExtraction_TwoLevels()
    {
        // Arrange: { "response": { "customers": [...] } }
        var people = TestData.GetSmallPersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = "{\"response\": {\"customers\": " + innerJson + "}}";
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = JsonQueryable<Person>
            .FromStream(stream, "$.response[*].customers[*]", _options)
            .ToList();

        // Assert
        results.Count.ShouldBe(people.Count);
    }

    [Fact]
    public async Task FromString_DeepNestedExtraction_ThreeLevels()
    {
        // Arrange: { "data": { "result": { "items": [...] } } }
        var people = TestData.GetSmallPersonDataset().Take(5).ToList();
        var innerJson = JsonSerializer.Serialize(people);
        var json = "{\"data\": {\"result\": {\"items\": " + innerJson + "}}}";

        // Act
        var results = JsonQueryable<Person>
            .FromString(json, "$.data[*].result[*].items[*]", _options)
            .ToList();

        // Assert
        results.Count.ShouldBe(people.Count);
    }

    #endregion

    #region Complex Query Combinations

    [Fact]
    public async Task FromStream_ComplexQuery_WhereOrderByTake()
    {
        // Arrange
        var people = TestData.GetMediumPersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = $$"""{"data": {{innerJson}}}""";
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = JsonQueryable<Person>
            .FromStream(stream, "$.data[*]", _options)
            .Where(p => p.Age > 30 && p.IsActive)
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();

        // Assert
        results.Count.ShouldBe(10);
        results.ShouldAllBe(p => p.Age > 30 && p.IsActive);
        results.ShouldBeOrderedBy(p => p.Name);
    }

    [Fact]
    public async Task FromString_ComplexQuery_WhereSelectOrderBy()
    {
        // Arrange
        var people = TestData.GetMediumPersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = "{\"result\": " + innerJson + "}";

        // Act
        var results = JsonQueryable<Person>
            .FromString(json, "$.result[*]", _options)
            .Where(p => p.Age >= 25 && p.Age <= 45)
            .Select(p => new { p.Name, p.Age, p.City })
            .OrderByDescending(p => p.Age)
            .Take(15)
            .ToList();

        // Assert
        results.Count.ShouldBe(15);
        results.ShouldAllBe(p => p.Age >= 25 && p.Age <= 45);
        // Verify ordering manually (Shouldly doesn't have ShouldBeOrderedByDescending for anonymous types)
        for (int i = 0; i < results.Count - 1; i++)
        {
            results[i].Age.ShouldBeGreaterThanOrEqualTo(results[i + 1].Age);
        }
    }

    [Fact]
    public async Task FromUtf8_ComplexQuery_SkipTakeWhere()
    {
        // Arrange
        var people = TestData.GetMediumPersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = $$"""{"data": {{innerJson}}}""";
        var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);

        // Act
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json, "$.data[*]", _options)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id)
            .Skip(10)
            .Take(20)
            .ToList();

        // Assert
        results.Count.ShouldBe(20);
        results.ShouldAllBe(p => p.IsActive);
    }

    #endregion

    #region Multi-Source Extraction

    [Fact]
    public async Task FromString_MultiSourceJson_ExtractSpecificArray()
    {
        // Arrange: JSON with multiple arrays, extract only one
        var people = TestData.GetSmallPersonDataset();
        var products = TestData.GetProductDataset();
        var peopleJson = JsonSerializer.Serialize(people);
        var productsJson = JsonSerializer.Serialize(products);
        
        var json = $$"""
        {
            "people": {{peopleJson}},
            "products": {{productsJson}}
        }
        """;

        // Act - Extract only people
        var peopleResults = JsonQueryable<Person>
            .FromString(json, "$.people[*]", _options)
            .ToList();

        // Assert
        peopleResults.Count.ShouldBe(people.Count);
        peopleResults[0].Name.ShouldBe(people[0].Name);
    }

    [Fact]
    public async Task FromStream_MultiSourceJson_ExtractDifferentArrays()
    {
        // Arrange: Same JSON, extract products this time
        var people = TestData.GetSmallPersonDataset();
        var products = TestData.GetProductDataset();
        var peopleJson = JsonSerializer.Serialize(people);
        var productsJson = JsonSerializer.Serialize(products);
        
        var json = $$"""
        {
            "people": {{peopleJson}},
            "products": {{productsJson}}
        }
        """;
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act - Extract only products
        var productResults = JsonQueryable<Product>
            .FromStream(stream, "$.products[*]", _options)
            .Where(p => p.InStock)
            .ToList();

        // Assert
        productResults.ShouldNotBeEmpty();
        productResults.ShouldAllBe(p => p.InStock);
    }

    #endregion

    #region Async Enumeration Tests

    [Fact]
    public async Task FromStream_AsyncEnumeration_StreamsResults()
    {
        // Arrange
        var people = TestData.GetMediumPersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = $$"""{"data": {{innerJson}}}""";
        using var stream = TestHelpers.CreateMemoryStream(json);

        var query = JsonQueryable<Person>
            .FromStream(stream, "$.data[*]", _options)
            .Where(p => p.Age > 30);

        // Act
        var results = new List<Person>();
        await foreach (var person in query.AsAsyncEnumerable())
        {
            results.Add(person);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 30);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task FromStream_LargeDataset_JSONPathPerformance()
    {
        // Arrange
        var people = TestData.GetLargePersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = $$"""{"data": {{innerJson}}}""";
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = JsonQueryable<Person>
            .FromStream(stream, "$.data[*]", _options)
            .Where(p => p.Age > 40)
            .Take(100)
            .ToList();

        // Assert
        results.Count.ShouldBe(100);
        results.ShouldAllBe(p => p.Age > 40);
    }

    [Fact]
    public async Task FromUtf8_LargeDataset_MemoryEfficiency()
    {
        // Arrange
        var people = TestData.GetLargePersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = $$"""{"result": {{innerJson}}}""";
        var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);

        // Act
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json, "$.result[*]", _options)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Age)
            .Take(50)
            .ToList();

        // Assert
        results.Count.ShouldBe(50);
        results.ShouldBeOrderedBy(p => p.Age);
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public async Task FromStream_WithoutJSONPath_StillWorks()
    {
        // Arrange: Verify backward compatibility - no JSONPath parameter
        var people = TestData.GetSmallPersonDataset();
        var json = JsonSerializer.Serialize(people);
        using var stream = TestHelpers.CreateMemoryStream(json);

        // Act - Old API without JSONPath
        var results = JsonQueryable<Person>
            .FromStream(stream, options: _options)
            .Where(p => p.Age > 25)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 25);
    }

    [Fact]
    public async Task FromString_WithoutJSONPath_StillWorks()
    {
        // Arrange: Backward compatibility for string source
        var people = TestData.GetSmallPersonDataset();
        var json = JsonSerializer.Serialize(people);

        // Act - Old API without JSONPath
        var results = JsonQueryable<Person>
            .FromString(json, options: _options)
            .OrderBy(p => p.Name)
            .Take(5)
            .ToList();

        // Assert
        results.Count.ShouldBe(5);
        results.ShouldBeOrderedBy(p => p.Name);
    }

    [Fact]
    public async Task FromUtf8_WithoutJSONPath_StillWorks()
    {
        // Arrange: Backward compatibility for UTF-8 source
        var people = TestData.GetSmallPersonDataset();
        var json = JsonSerializer.Serialize(people);
        var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);

        // Act - Old API without JSONPath
        var results = JsonQueryable<Person>
            .FromUtf8(utf8Json, options: _options)
            .Where(p => p.IsActive)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.IsActive);
    }

    #endregion

    #region Case Sensitivity Tests

    [Fact]
    public async Task FromString_JSONPath_CaseInsensitive()
    {
        // Arrange: Test case insensitivity in JSONPath
        var people = TestData.GetSmallPersonDataset();
        var innerJson = JsonSerializer.Serialize(people);
        var json = $$"""{"DATA": {{innerJson}}}"""; // Uppercase property

        // Act - Lowercase path
        var results = JsonQueryable<Person>
            .FromString(json, "$.data[*]", _options)
            .ToList();

        // Assert
        results.Count.ShouldBe(people.Count);
    }

    #endregion
}

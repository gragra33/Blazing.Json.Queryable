using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Blazing.Json.Queryable.Tests.IntegrationTests;

/// <summary>
/// Integration tests for JsonSerializerOptions support.
/// Validates that custom serializer options are properly respected.
/// </summary>
public class JsonSerializerOptionsTests
{
    [Fact]
    public void PropertyNameCaseInsensitive_WithMixedCaseProperties_DeserializesCorrectly()
    {
        // Arrange
        var json = """[{"NAME":"Alice","AGE":30},{"name":"Bob","age":25}]""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Act
        var results = JsonQueryable<Person>.FromString(json, options)
            .Where(p => p.Age > 20)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(2);
        results[0].Name.ShouldBe("Alice");
        results[1].Name.ShouldBe("Bob");
    }

    [Fact]
    public void PropertyNamingPolicy_WithCamelCase_DeserializesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var json = JsonSerializer.Serialize(data, options);

        // Act
        var results = JsonQueryable<Person>.FromString(json, options)
            .Where(p => p.Age > 25)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 25);
    }

    [Fact]
    public void PropertyNamingPolicy_WithSnakeCase_DeserializesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
        var json = JsonSerializer.Serialize(data, options);

        // Act
        var results = JsonQueryable<Person>.FromString(json, options)
            .OrderBy(p => p.Name)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldBeOrderedBy(p => p.Name);
    }

    [Fact]
    public void AllowTrailingCommas_WithTrailingCommas_DeserializesCorrectly()
    {
        // Arrange
        var json = """[{"Id":1,"Name":"Alice","Age":30,},{"Id":2,"Name":"Bob","Age":25,}]""";
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true
        };

        // Act
        var results = JsonQueryable<Person>.FromString(json, options)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(2);
    }

    [Fact]
    public void ReadCommentHandling_WithComments_IgnoresComments()
    {
        // Arrange
        var json = """
        [
            // First person
            {"Id":1,"Name":"Alice","Age":30},
            /* Second person */
            {"Id":2,"Name":"Bob","Age":25}
        ]
        """;
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        // Act
        var results = JsonQueryable<Person>.FromString(json, options)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(2);
    }

    [Fact]
    public void NumberHandling_WithStringsAsNumbers_DeserializesCorrectly()
    {
        // Arrange
        var json = """[{"Id":"1","Name":"Alice","Age":"30"},{"Id":"2","Name":"Bob","Age":"25"}]""";
        var options = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        // Act
        var results = JsonQueryable<Person>.FromString(json, options)
            .Where(p => p.Age > 20)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 20);
    }

    [Fact]
    public void MaxDepth_WithNestedObjects_RespectsDepthLimit()
    {
        // Arrange
        var json = """[{"Id":1,"Name":"Alice","Age":30,"Address":{"Street":"123 Main","City":"NYC","State":"NY","ZipCode":"10001"}}]""";
        var options = new JsonSerializerOptions
        {
            MaxDepth = 64
        };

        // Act
        var results = JsonQueryable<Person>.FromString(json, options)
            .Where(p => p.Address != null)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results[0].Address.ShouldNotBeNull();
        results[0].Address!.City.ShouldBe("NYC");
    }

    [Fact]
    public void Utf8Provider_WithCustomOptions_RespectsOptions()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var utf8Json = JsonSerializer.SerializeToUtf8Bytes(data, options);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json, options)
            .Where(p => p.Age > 25)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 25);
    }

    [Fact]
    public void StreamProvider_WithCustomOptions_RespectsOptions()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var json = JsonSerializer.Serialize(data, options);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        // Act
        var results = JsonQueryable<Person>.FromStream(stream, options)
            .OrderBy(p => p.Name)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldBeOrderedBy(p => p.Name);
    }

    [Fact]
    public void FileProvider_WithCustomOptions_RespectsOptions()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var json = JsonSerializer.Serialize(data, options);
        var tempFile = Path.GetTempFileName();
        
        try
        {
            File.WriteAllText(tempFile, json);

            // Act - Use using to properly dispose file stream
            using var query = JsonQueryable<Person>.FromFile(tempFile, options);
            var results = query
                .Where(p => p.Age > 20)
                .ToList();

            // Assert
            results.ShouldNotBeEmpty();
            results.ShouldAllBe(p => p.Age > 20);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void DefaultOptions_WithStandardJson_DeserializesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act - using default options (null)
        var results = JsonQueryable<Person>.FromString(json, null)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(data.Count);
    }
}

using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Blazing.Json.Queryable.Tests.IntegrationTests;

/// <summary>
/// Integration tests for custom JsonConverter support.
/// Validates that custom converters are properly used during deserialization.
/// </summary>
public class CustomJsonConverterTests
{
    /// <summary>
    /// Custom converter for DateTime that handles Unix timestamps.
    /// </summary>
    private class UnixTimestampConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                var unixTime = reader.GetInt64();
                return DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
            }
            return DateTime.Parse(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var unixTime = new DateTimeOffset(value).ToUnixTimeSeconds();
            writer.WriteNumberValue(unixTime);
        }
    }

    /// <summary>
    /// Custom converter for boolean that handles "yes"/"no" strings.
    /// </summary>
    private class YesNoConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                return value?.ToLowerInvariant() == "yes";
            }
            return reader.GetBoolean();
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value ? "yes" : "no");
        }
    }

    [Fact]
    public void CustomConverter_UnixTimestamp_DeserializesCorrectly()
    {
        // Arrange
        var json = """[{"Id":1,"Name":"Alice","Age":30,"CreatedDate":1640995200}]"""; // 2022-01-01
        var options = new JsonSerializerOptions();
        options.Converters.Add(new UnixTimestampConverter());

        // Act
        var results = JsonQueryable<Person>.FromString(json, options)
            .Where(p => p.CreatedDate > new DateTime(2021, 1, 1))
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results[0].CreatedDate.Year.ShouldBe(2022);
    }

    [Fact]
    public void CustomConverter_YesNoBoolean_DeserializesCorrectly()
    {
        // Arrange
        var json = """[{"Id":1,"Name":"Alice","Age":30,"IsActive":"yes"},{"Id":2,"Name":"Bob","Age":25,"IsActive":"no"}]""";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new YesNoConverter());

        // Act
        var results = JsonQueryable<Person>.FromString(json, options)
            .Where(p => p.IsActive)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("Alice");
    }

    [Fact]
    public void MultipleCustomConverters_WorkTogether()
    {
        // Arrange
        var json = """[{"Id":1,"Name":"Alice","Age":30,"IsActive":"yes","CreatedDate":1640995200}]""";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new UnixTimestampConverter());
        options.Converters.Add(new YesNoConverter());

        // Act
        var results = JsonQueryable<Person>.FromString(json, options)
            .Where(p => p.IsActive && p.CreatedDate.Year >= 2022)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results[0].Name.ShouldBe("Alice");
    }

    [Fact]
    public void CustomConverter_WithUtf8Provider_WorksCorrectly()
    {
        // Arrange
        var json = """[{"Id":1,"Name":"Alice","Age":30,"IsActive":"yes"}]""";
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new YesNoConverter());

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Bytes, options)
            .Where(p => p.IsActive)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results[0].IsActive.ShouldBeTrue();
    }

    [Fact]
    public void CustomConverter_WithStreamProvider_WorksCorrectly()
    {
        // Arrange
        var json = """[{"Id":1,"Name":"Alice","Age":30,"CreatedDate":1640995200}]""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var options = new JsonSerializerOptions();
        options.Converters.Add(new UnixTimestampConverter());

        // Act
        var results = JsonQueryable<Person>.FromStream(stream, options)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results[0].CreatedDate.Year.ShouldBe(2022);
    }

    [Fact]
    public void CustomConverter_WithFileProvider_WorksCorrectly()
    {
        // Arrange
        var json = """[{"Id":1,"Name":"Alice","Age":30,"IsActive":"yes"}]""";
        var tempFile = Path.GetTempFileName();
        var options = new JsonSerializerOptions();
        options.Converters.Add(new YesNoConverter());

        try
        {
            File.WriteAllText(tempFile, json);

            // Act - Use using to properly dispose file stream
            using var query = JsonQueryable<Person>.FromFile(tempFile, options);
            var results = query
                .Where(p => p.IsActive)
                .ToList();

            // Assert
            results.ShouldNotBeEmpty();
            results[0].IsActive.ShouldBeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CustomConverter_WithComplexQuery_AppliesAllOperations()
    {
        // Arrange
        var json = """
        [
            {"Id":1,"Name":"Alice","Age":30,"IsActive":"yes","CreatedDate":1640995200},
            {"Id":2,"Name":"Bob","Age":25,"IsActive":"no","CreatedDate":1609459200},
            {"Id":3,"Name":"Charlie","Age":35,"IsActive":"yes","CreatedDate":1672531200}
        ]
        """;
        var options = new JsonSerializerOptions();
        options.Converters.Add(new UnixTimestampConverter());
        options.Converters.Add(new YesNoConverter());

        // Act
        var results = JsonQueryable<Person>.FromString(json, options)
            .Where(p => p.IsActive && p.Age > 25)
            .OrderBy(p => p.CreatedDate)
            .Select(p => new { p.Name, p.CreatedDate })
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(2);
        results[0].Name.ShouldBe("Alice");
        results[1].Name.ShouldBe("Charlie");
    }

    [Fact]
    public async Task CustomConverter_WithAsyncEnumeration_WorksCorrectly()
    {
        // Arrange
        var json = """[{"Id":1,"Name":"Alice","Age":30,"IsActive":"yes"},{"Id":2,"Name":"Bob","Age":25,"IsActive":"no"}]""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var options = new JsonSerializerOptions();
        options.Converters.Add(new YesNoConverter());

        // Act
        var activeCount = 0;
        await foreach (var person in JsonQueryable<Person>.FromStream(stream, options)
            .Where(p => p.IsActive)
            .AsAsyncEnumerable())
        {
            activeCount++;
        }

        // Assert
        activeCount.ShouldBe(1);
    }

    [Fact]
    public void BuiltInConverters_JsonStringEnumConverter_WorksCorrectly()
    {
        // Arrange - using a hypothetical enum in JSON
        var json = """[{"Id":1,"Name":"Alice","Age":30}]""";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());

        // Act
        var results = JsonQueryable<Person>.FromString(json, options)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results[0].Name.ShouldBe("Alice");
    }

    [Fact]
    public void NoCustomConverter_StandardJson_DeserializesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);

        // Act - no custom converters
        var results = JsonQueryable<Person>.FromString(json)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(data.Count);
    }
}

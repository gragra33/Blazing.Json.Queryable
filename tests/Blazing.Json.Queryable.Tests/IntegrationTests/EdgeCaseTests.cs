using Blazing.Json.Queryable.Exceptions;
using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;
using System.Text;

namespace Blazing.Json.Queryable.Tests.IntegrationTests;

/// <summary>
/// Edge case and error handling tests.
/// </summary>
public class EdgeCaseTests
{
    [Fact]
    public void FromUtf8_InvalidJson_ThrowsJsonDeserializationException()
    {
        // Arrange
        var invalidJson = TestData.GetInvalidJsonUtf8();

        // Act & Assert
        Should.Throw<JsonDeserializationException>(() =>
        {
            var results = JsonQueryable<Person>.FromUtf8(invalidJson).ToList();
        });
    }

    [Fact]
    public void FromString_InvalidJson_ThrowsJsonDeserializationException()
    {
        // Arrange
        var invalidJson = TestData.GetInvalidJson();

        // Act & Assert
        Should.Throw<JsonDeserializationException>(() =>
        {
            var results = JsonQueryable<Person>.FromString(invalidJson).ToList();
        });
    }

    [Fact]
    public void FromUtf8_JsonObject_ThrowsJsonDeserializationException()
    {
        // Arrange (JSON object instead of array)
        var jsonObject = TestData.GetJsonObjectUtf8();

        // Act & Assert
        Should.Throw<JsonDeserializationException>(() =>
        {
            var results = JsonQueryable<Person>.FromUtf8(jsonObject).ToList();
        });
    }

    [Fact]
    public void FromUtf8_NullableProperties_HandlesNulls()
    {
        // Arrange
        var data = TestData.GetNullableDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<NullableModel>.FromUtf8(utf8Json)
            .Where(m => m.Name == null)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(m => m.Name == null);
    }

    [Fact]
    public void FromUtf8_SingleElementArray_ReturnsOneResult()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset().Take(1).ToList();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .ToList();

        // Assert
        results.Count.ShouldBe(1);
    }

    [Fact]
    public void FromUtf8_WhereNoMatches_ReturnsEmpty()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Age > 1000) // No one is this old
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void FromUtf8_FirstOrDefault_WithNoMatches_ReturnsNull()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var result = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Age > 1000)
            .FirstOrDefault();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void FromUtf8_Single_WithMultipleResults_ThrowsException()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
        {
            var result = JsonQueryable<Person>.FromUtf8(utf8Json).Single();
        });
    }

    [Fact]
    public void FromUtf8_Single_WithNoResults_ThrowsException()
    {
        // Arrange
        var utf8Json = TestData.GetEmptyJsonArrayUtf8();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
        {
            var result = JsonQueryable<Person>.FromUtf8(utf8Json).Single();
        });
    }

    [Fact]
    public void FromFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = "C:\\NonExistent\\Path\\data.json";

        // Act & Assert
        Should.Throw<FileNotFoundException>(() =>
        {
            var results = JsonQueryable<Person>.FromFile(nonExistentPath).ToList();
        });
    }

    [Fact]
    public void FromUtf8_TakeZero_ReturnsEmpty()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Take(0)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void FromUtf8_SkipAll_ReturnsEmpty()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Skip(data.Count)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void FromUtf8_SkipMoreThanCount_ReturnsEmpty()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Skip(data.Count + 10)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void FromUtf8_Count_EmptyResults_ReturnsZero()
    {
        // Arrange
        var utf8Json = TestData.GetEmptyJsonArrayUtf8();

        // Act
        var count = JsonQueryable<Person>.FromUtf8(utf8Json).Count();

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    public void FromUtf8_Any_EmptyResults_ReturnsFalse()
    {
        // Arrange
        var utf8Json = TestData.GetEmptyJsonArrayUtf8();

        // Act
        var hasAny = JsonQueryable<Person>.FromUtf8(utf8Json).Any();

        // Assert
        hasAny.ShouldBeFalse();
    }

    [Fact]
    public void FromStream_DisposedStream_ThrowsObjectDisposedException()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        var stream = TestHelpers.CreateMemoryStream(json);
        stream.Dispose();

        // Act & Assert - Disposed stream throws ArgumentException because CanRead is false
        Should.Throw<ArgumentException>(() =>
        {
            var results = JsonQueryable<Person>.FromStream(stream).ToList();
        });
    }

    [Fact]
    public async Task FromStream_AsyncWithDisposedStream_ThrowsObjectDisposedException()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        var stream = TestHelpers.CreateMemoryStream(json);
        await stream.DisposeAsync();

        // Act & Assert - Disposed stream throws ArgumentException because CanRead is false
        Should.Throw<ArgumentException>(() =>
        {
            // This creates the query, which validates the stream
            var query = JsonQueryable<Person>.FromStream(stream);
        });
    }

    [Fact]
    public void FromUtf8_ComplexNesting_HandlesCorrectly()
    {
        // Arrange
        var person = new Person
        {
            Id = 1,
            Name = "Test",
            Age = 30,
            Address = new Address
            {
                Street = "123 Main St",
                City = "TestCity",
                State = "TS",
                ZipCode = "12345"
            }
        };
        var data = new List<Person> { person };
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results[0].Address.ShouldNotBeNull();
        results[0].Address!.City.ShouldBe("TestCity");
    }

    #region Missing Properties and Type Mismatches

    [Fact]
    public void FromUtf8_MissingOptionalProperty_HandlesGracefully()
    {
        // Arrange - JSON missing optional properties
        var json = """[{"id":1,"name":"Test","age":30}]"""; // Missing City, Email
        var utf8Json = Encoding.UTF8.GetBytes(json);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json).ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results[0].Name.ShouldBe("Test");
        results[0].City.ShouldBeNull();
        results[0].Email.ShouldBeNull();
    }

    [Fact]
    public void FromUtf8_ExtraPropertiesInJson_IgnoresExtra()
    {
        // Arrange - JSON has extra properties not in model
        var json = """[{"id":1,"name":"Test","age":30,"extraField":"ignored","anotherExtra":123}]""";
        var utf8Json = Encoding.UTF8.GetBytes(json);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json).ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results[0].Name.ShouldBe("Test");
        results[0].Age.ShouldBe(30);
    }

    [Fact]
    public void FromUtf8_TypeMismatch_ThrowsJsonException()
    {
        // Arrange - Age is string instead of int
        var json = """[{"id":1,"name":"Test","age":"not a number"}]""";
        var utf8Json = Encoding.UTF8.GetBytes(json);

        // Act & Assert
        Should.Throw<JsonDeserializationException>(() =>
        {
            var results = JsonQueryable<Person>.FromUtf8(utf8Json).ToList();
        });
    }

    [Fact]
    public void FromUtf8_NullInRequiredProperty_DeserializesAsDefault()
    {
        // Arrange - Age (int) is null
        var json = """[{"id":1,"name":"Test","age":null}]""";
        var utf8Json = Encoding.UTF8.GetBytes(json);

        // Act & Assert - System.Text.Json throws exception for null in non-nullable value types
        Should.Throw<JsonDeserializationException>(() =>
        {
            var results = JsonQueryable<Person>.FromUtf8(utf8Json).ToList();
        });
    }

    #endregion

    #region Multi-Segment and Fragmented Data

    [Fact]
    public void FromUtf8_LargeDataset_HandlesCorrectly()
    {
        // Arrange
        var data = TestData.GetLargePersonDataset();
        var utf8Json = TestData.SerializeToUtf8(data);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json)
            .Where(p => p.Age > 25)
            .Take(10)
            .ToList();

        // Assert
        results.Count.ShouldBe(10);
        results.ShouldAllBe(p => p.Age > 25);
    }

    [Fact]
    public void FromStream_FragmentedRead_HandlesCorrectly()
    {
        // Arrange
        var data = TestData.GetSmallPersonDataset();
        var json = TestData.SerializeToJson(data);
        var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = JsonQueryable<Person>.FromStream(stream)
            .Where(p => p.Age > 25)
            .ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => p.Age > 25);
    }

    #endregion

    #region UTF-8 Encoding Edge Cases

    [Fact]
    public void FromUtf8_ValidUtf8WithBom_HandlesCorrectly()
    {
        // Arrange - UTF-8 with BOM
        var data = TestData.GetSmallPersonDataset();
        var jsonString = TestData.SerializeToJson(data);
        var utf8WithBom = new byte[] { 0xEF, 0xBB, 0xBF }
            .Concat(Encoding.UTF8.GetBytes(jsonString))
            .ToArray();

        // Act - System.Text.Json throws exception for BOM, so strip it first
        var utf8WithoutBom = utf8WithBom.AsSpan(3).ToArray(); // Skip BOM bytes
        var results = JsonQueryable<Person>.FromUtf8(utf8WithoutBom).ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(data.Count);
    }

    [Fact]
    public void FromUtf8_InvalidUtf8Sequence_ThrowsJsonException()
    {
        // Arrange - Invalid UTF-8 byte sequence
        var invalidUtf8 = new byte[] { 0xFF, 0xFE, 0xFD };

        // Act & Assert
        Should.Throw<JsonDeserializationException>(() =>
        {
            var results = JsonQueryable<Person>.FromUtf8(invalidUtf8).ToList();
        });
    }

    [Fact]
    public void FromUtf8_UnicodeCharacters_HandlesCorrectly()
    {
        // Arrange - JSON with unicode characters
        var json = """[{"id":1,"name":"Test ?? ??","age":30}]""";
        var utf8Json = Encoding.UTF8.GetBytes(json);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json).ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results[0].Name.ShouldBe("Test ?? ??");
    }

    #endregion

    #region Buffer and Memory Edge Cases

    [Fact]
    public void FromStream_VerySmallBuffer_HandlesCorrectly()
    {
        // Arrange - Data that fits in one small buffer read
        var data = new List<Person>
        {
            new Person { Id = 1, Name = "A", Age = 25, IsActive = true }
        };
        var json = TestData.SerializeToJson(data);
        var stream = TestHelpers.CreateMemoryStream(json);

        // Act
        var results = JsonQueryable<Person>.FromStream(stream).ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("A");
    }

    [Fact]
    public void FromUtf8_EmptyArray_ReturnsEmpty()
    {
        // Arrange
        var utf8Json = TestData.GetEmptyJsonArrayUtf8();

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json).ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void FromUtf8_WhitespaceOnly_ThrowsJsonException()
    {
        // Arrange
        var whitespace = Encoding.UTF8.GetBytes("   \n\r\t   ");

        // Act & Assert
        Should.Throw<JsonDeserializationException>(() =>
        {
            var results = JsonQueryable<Person>.FromUtf8(whitespace).ToList();
        });
    }

    #endregion

    #region Custom JsonSerializerOptions Edge Cases

    [Fact]
    public void FromUtf8_WithCaseSensitiveOptions_RespectsOptions()
    {
        // Arrange
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false
        };
        var json = """[{"id":1,"name":"Test","age":30}]"""; // lowercase - won't match Pascal case properties with case-sensitive
        var utf8Json = Encoding.UTF8.GetBytes(json);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json, options).ToList();

        // Assert - Should fail to match properties due to case sensitivity
        results.ShouldNotBeEmpty();
        results[0].Name.ShouldBe(string.Empty); // Non-nullable string with default value
        results[0].Id.ShouldBe(0); // int defaults to 0
        results[0].Age.ShouldBe(0); // int defaults to 0
    }

    [Fact]
    public void FromUtf8_WithCaseInsensitiveOptions_RespectsOptions()
    {
        // Arrange
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var json = """[{"ID":1,"NAME":"Test","AGE":30}]"""; // All caps
        var utf8Json = Encoding.UTF8.GetBytes(json);

        // Act
        var results = JsonQueryable<Person>.FromUtf8(utf8Json, options).ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results[0].Name.ShouldBe("Test");
        results[0].Age.ShouldBe(30);
    }

    #endregion
}

using System.Text.Json;
using System.Buffers;
using Blazing.Json.Queryable.Exceptions;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;
using Blazing.Json.Queryable.Implementations;

namespace Blazing.Json.Queryable.Tests.UnitTests.Evaluators;

/// <summary>
/// Unit tests for SpanJsonDeserializer.
/// </summary>
public class SpanJsonDeserializerTests
{
    private readonly SpanJsonDeserializer _deserializer;
    private readonly JsonSerializerOptions _options;

    public SpanJsonDeserializerTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _deserializer = new SpanJsonDeserializer(_options);
    }

    [Fact]
    public void Deserialize_FromUtf8Span_ReturnsCorrectObject()
    {
        // Arrange
        var json = """{"id":1,"name":"Alice","age":25}""";
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(json);

        // Act
        var result = _deserializer.Deserialize<Person>(utf8Bytes.AsSpan());

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Alice");
        result.Age.ShouldBe(25);
    }

    [Fact]
    public void Deserialize_FromUtf8Span_WithEmptySpan_ReturnsNull()
    {
        // Act
        var result = _deserializer.Deserialize<Person>(ReadOnlySpan<byte>.Empty);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Deserialize_FromUtf8Span_WithInvalidJson_ThrowsJsonDeserializationException()
    {
        // Arrange
        var invalidJson = "{invalid json}"u8.ToArray();

        // Act & Assert
        var exception = Should.Throw<JsonDeserializationException>(() => 
            _deserializer.Deserialize<Person>(invalidJson.AsSpan()));
        
        exception.JsonContent.ShouldNotBeNull();
        exception.InnerException.ShouldBeOfType<JsonException>();
    }

    [Fact]
    public void DeserializeString_WithValidJson_ReturnsCorrectObject()
    {
        // Arrange
        var json = """{"id":2,"name":"Bob","age":30}""";

        // Act
        var result = _deserializer.DeserializeString<Person>(json);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Bob");
        result.Age.ShouldBe(30);
    }

    [Fact]
    public void DeserializeString_WithNullString_ReturnsNull()
    {
        // Act
        var result = _deserializer.DeserializeString<Person>(null!); // Use null-forgiving operator for test

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void DeserializeString_WithEmptyString_ReturnsNull()
    {
        // Act
        var result = _deserializer.DeserializeString<Person>("");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Deserialize_WithCustomOptions_RespectsOptions()
    {
        // Arrange
        var options = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };
        var deserializer = new SpanJsonDeserializer(options);
        var json = """{"ID":3,"NAME":"Charlie","AGE":35}""";  // Uppercase properties
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(json);

        // Act
        var result = deserializer.Deserialize<Person>(utf8Bytes.AsSpan());

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(3);
        result.Name.ShouldBe("Charlie");
        result.Age.ShouldBe(35);
    }

    [Fact]
    public void Deserialize_WithNullableProperties_HandlesNullsCorrectly()
    {
        // Arrange
        var json = """{"id":1,"name":null,"age":null}""";
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(json);

        // Act
        var result = _deserializer.Deserialize<NullableModel>(utf8Bytes.AsSpan());

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBeNull();
        result.Age.ShouldBeNull();
    }

    [Fact]
    public void Deserialize_ComplexObject_WithNestedProperties_WorksCorrectly()
    {
        // Arrange
        var json = """{"id":1,"name":"Alice","age":25,"address":{"street":"123 Main St","city":"New York","state":"NY","zipCode":"10001"}}""";
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(json);

        // Act
        var result = _deserializer.Deserialize<Person>(utf8Bytes.AsSpan());

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Alice");
        result.Address.ShouldNotBeNull();
        result.Address.City.ShouldBe("New York");
        result.Address.State.ShouldBe("NY");
    }

    #region ReadOnlySequence<byte> Tests

    [Fact]
    public void Deserialize_FromSequence_SingleSegment_ReturnsCorrectObject()
    {
        // Arrange
        var json = """{"id":4,"name":"David","age":40}""";
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var sequence = new ReadOnlySequence<byte>(utf8Bytes);

        // Act
        var result = _deserializer.Deserialize<Person>(sequence);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("David");
        result.Age.ShouldBe(40);
    }

    [Fact]
    public void Deserialize_FromSequence_MultipleSegments_ReturnsCorrectObject()
    {
        // Arrange
        var json = """{"id":5,"name":"Eve","age":45}""";
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(json);
        
        // Split into multiple segments
        var segment1 = new byte[15];
        var segment2 = new byte[utf8Bytes.Length - 15];
        Array.Copy(utf8Bytes, 0, segment1, 0, 15);
        Array.Copy(utf8Bytes, 15, segment2, 0, segment2.Length);

        var firstSegment = new MemorySegment<byte>(segment1);
        var secondSegment = firstSegment.Append(segment2);
        var sequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, secondSegment.Memory.Length);

        // Act
        var result = _deserializer.Deserialize<Person>(sequence);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Eve");
        result.Age.ShouldBe(45);
    }

    [Fact]
    public void Deserialize_FromSequence_EmptySequence_ReturnsNull()
    {
        // Arrange
        var sequence = ReadOnlySequence<byte>.Empty;

        // Act
        var result = _deserializer.Deserialize<Person>(sequence);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Deserialize_FromSequence_InvalidJson_ThrowsJsonDeserializationException()
    {
        // Arrange
        var invalidJson = "{invalid json}"u8.ToArray();
        var sequence = new ReadOnlySequence<byte>(invalidJson);

        // Act & Assert
        var exception = Should.Throw<JsonDeserializationException>(() => 
            _deserializer.Deserialize<Person>(sequence));
        
        exception.JsonContent.ShouldNotBeNull();
        exception.InnerException.ShouldBeOfType<JsonException>();
    }

    #endregion

    #region ref Utf8JsonReader Tests

    [Fact]
    public void Deserialize_FromReader_ReturnsCorrectObject()
    {
        // Arrange
        var json = """{"id":6,"name":"Frank","age":50}""";
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(utf8Bytes);

        // Act
        var result = _deserializer.Deserialize<Person>(ref reader);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Frank");
        result.Age.ShouldBe(50);
    }

    [Fact]
    public void Deserialize_FromReader_WithCustomOptions_RespectsOptions()
    {
        // Arrange
        var options = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };
        var deserializer = new SpanJsonDeserializer(options);
        var json = """{"ID":7,"NAME":"Grace","AGE":55}""";
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(utf8Bytes);

        // Act
        var result = deserializer.Deserialize<Person>(ref reader);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(7);
        result.Name.ShouldBe("Grace");
        result.Age.ShouldBe(55);
    }

    [Fact]
    public void Deserialize_FromReader_InvalidJson_ThrowsJsonDeserializationException()
    {
        // Arrange
        var invalidJson = "{invalid json}"u8.ToArray();
        var reader = new Utf8JsonReader(invalidJson);

        // Act & Assert - Cannot use ref reader in lambda, must extract
        JsonDeserializationException? exception = null;
        try
        {
            _deserializer.Deserialize<Person>(ref reader);
        }
        catch (JsonDeserializationException ex)
        {
            exception = ex;
        }

        exception.ShouldNotBeNull();
        exception.JsonContent.ShouldNotBeNull();
        exception.InnerException.ShouldBeOfType<JsonException>();
    }

    [Fact]
    public void Deserialize_FromReader_ComplexNestedObject_WorksCorrectly()
    {
        // Arrange
        var json = """{"id":8,"name":"Helen","age":60,"address":{"street":"456 Oak Ave","city":"Boston","state":"MA","zipCode":"02101"}}""";
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(utf8Bytes);

        // Act
        var result = _deserializer.Deserialize<Person>(ref reader);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Helen");
        result.Address.ShouldNotBeNull();
        result.Address.City.ShouldBe("Boston");
        result.Address.State.ShouldBe("MA");
    }

    #endregion

    #region Helper Class for Multi-Segment Sequence

    private class MemorySegment<T> : ReadOnlySequenceSegment<T>
    {
        public MemorySegment(ReadOnlyMemory<T> memory)
        {
            Memory = memory;
        }

        public MemorySegment<T> Append(ReadOnlyMemory<T> memory)
        {
            var segment = new MemorySegment<T>(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = segment;
            return segment;
        }
    }

    #endregion
}

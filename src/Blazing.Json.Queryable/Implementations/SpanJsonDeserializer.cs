using System.Buffers;
using System.Text.Json;
using Blazing.Json.Queryable.Exceptions;

namespace Blazing.Json.Queryable.Implementations;

/// <summary>
/// Implementation of <see cref="Core.IJsonDeserializer"/> that provides span-optimized JSON parsing
/// using System.Text.Json's Utf8JsonReader and JsonSerializer.
/// Supports multiple input formats with zero-allocation UTF-8 processing as the preferred path.
/// </summary>
public sealed class SpanJsonDeserializer : Core.IJsonDeserializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="SpanJsonDeserializer"/>.
    /// </summary>
    /// <param name="options">Optional <see cref="JsonSerializerOptions"/> for deserialization behavior.</param>
    public SpanJsonDeserializer(JsonSerializerOptions? options = null)
    {
        if (options == null)
        {
            // Default options with case-insensitive matching
            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }
        else
        {
            // Use provided options as-is to respect user settings
            _options = options;
        }
    }

    /// <summary>
    /// Deserializes UTF-8 encoded JSON from a span (preferred method).
    /// Zero allocations for the span itself.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Json">UTF-8 encoded JSON data.</param>
    /// <returns>Deserialized object or null.</returns>
    /// <exception cref="JsonDeserializationException">Thrown when JSON is invalid.</exception>
    public T? Deserialize<T>(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.IsEmpty)
        {
            return default;
        }

        try
        {
            // Use the span-based Deserialize overload for optimal performance
            return JsonSerializer.Deserialize<T>(utf8Json, _options);
        }
        catch (JsonException ex)
        {
            string jsonPreview = System.Text.Encoding.UTF8.GetString(utf8Json.Length > 200 ? utf8Json[..200] : utf8Json);
            throw new JsonDeserializationException("Failed to deserialize JSON from UTF-8 span.", ex, jsonPreview);
        }
    }

    /// <summary>
    /// Deserializes UTF-8 JSON from a multi-segment sequence (for large streams).
    /// Handles fragmented buffers efficiently without copying.
    /// Used with PipeReader or large streaming scenarios.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8JsonSequence">Multi-segment UTF-8 JSON sequence.</param>
    /// <returns>Deserialized object or null.</returns>
    /// <exception cref="JsonDeserializationException">Thrown when JSON is invalid.</exception>
    public T? Deserialize<T>(ReadOnlySequence<byte> utf8JsonSequence)
    {
        if (utf8JsonSequence.IsEmpty)
        {
            return default;
        }

        try
        {
            // Create a reader from the sequence
            var reader = new Utf8JsonReader(utf8JsonSequence);
            // Deserialize using the reader
            return JsonSerializer.Deserialize<T>(ref reader, _options);
        }
        catch (JsonException ex)
        {
            string jsonPreview = GetSequencePreview(utf8JsonSequence);
            throw new JsonDeserializationException("Failed to deserialize JSON from sequence.", ex, jsonPreview);
        }
    }

    /// <summary>
    /// Deserializes from an active <see cref="Utf8JsonReader"/>.
    /// MUST be passed by reference (ref struct is mutable).
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="reader">Active <see cref="Utf8JsonReader"/> positioned at JSON to deserialize (passed by reference).</param>
    /// <returns>Deserialized object or null.</returns>
    /// <exception cref="JsonDeserializationException">Thrown when JSON is invalid.</exception>
    /// <remarks>
    /// The reader must be passed by reference because Utf8JsonReader is a mutable ref struct.
    /// The reader's position will be advanced during deserialization.
    /// Cannot be used across await boundaries due to ref struct limitations.
    /// </remarks>
    public T? Deserialize<T>(ref Utf8JsonReader reader)
    {
        try
        {
            // JsonSerializer.Deserialize handles the reader correctly
            return JsonSerializer.Deserialize<T>(ref reader, _options);
        }
        catch (JsonException ex)
        {
            throw new JsonDeserializationException("Failed to deserialize JSON from reader.", ex, "(JSON content not available from reader)");
        }
    }

    /// <summary>
    /// Convenience overload for string input (converts to UTF-8 internally).
    /// Less efficient than UTF-8 span methods - use sparingly.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">JSON string to deserialize.</param>
    /// <returns>Deserialized object or null.</returns>
    /// <exception cref="JsonDeserializationException">Thrown when JSON is invalid.</exception>
    /// <remarks>
    /// This method converts the string to UTF-8 internally, which has allocation overhead.
    /// Prefer using <see cref="Deserialize{T}(ReadOnlySpan{byte})"/> when possible for better performance.
    /// </remarks>
    public T? DeserializeString<T>(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return default;
        }

        try
        {
            // JsonSerializer.Deserialize(string) handles UTF-8 conversion internally
            return JsonSerializer.Deserialize<T>(json, _options);
        }
        catch (JsonException ex)
        {
            throw new JsonDeserializationException("Failed to deserialize JSON from string.", ex, json);
        }
    }

    /// <summary>
    /// Helper method to deserialize a JSON array from UTF-8 bytes.
    /// Useful for processing array-based JSON documents.
    /// </summary>
    /// <typeparam name="T">The element type in the array.</typeparam>
    /// <param name="utf8Json">UTF-8 encoded JSON array.</param>
    /// <returns>Deserialized array or null.</returns>
    /// <exception cref="JsonDeserializationException">Thrown when JSON is invalid or not an array.</exception>
    public T[]? DeserializeArray<T>(ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            return JsonSerializer.Deserialize<T[]>(utf8Json, _options);
        }
        catch (JsonException ex)
        {
            string jsonPreview = System.Text.Encoding.UTF8.GetString(utf8Json.Length > 200 ? utf8Json[..200] : utf8Json);
            throw new JsonDeserializationException("Failed to deserialize JSON array from UTF-8 span.", ex, jsonPreview);
        }
    }

    /// <summary>
    /// Helper method to deserialize a JSON array from a byte array.
    /// </summary>
    /// <typeparam name="T">The element type in the array.</typeparam>
    /// <param name="utf8Json">UTF-8 encoded JSON array as byte array.</param>
    /// <returns>Deserialized array or null.</returns>
    /// <exception cref="JsonDeserializationException">Thrown when JSON is invalid or not an array.</exception>
    public T[]? DeserializeArray<T>(byte[] utf8Json)
    {
        try
        {
            return JsonSerializer.Deserialize<T[]>(utf8Json, _options);
        }
        catch (JsonException ex)
        {
            string jsonPreview = System.Text.Encoding.UTF8.GetString(utf8Json.Length > 200 ? utf8Json.AsSpan()[..200] : utf8Json);
            throw new JsonDeserializationException("Failed to deserialize JSON array from byte array.", ex, jsonPreview);
        }
    }

    /// <summary>
    /// Helper method to deserialize from a stream.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="stream">The stream containing UTF-8 JSON data.</param>
    /// <returns>Deserialized object or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="JsonDeserializationException">Thrown when JSON is invalid.</exception>
    public T? Deserialize<T>(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        
        try
        {
            return JsonSerializer.Deserialize<T>(stream, _options);
        }
        catch (JsonException ex)
        {
            throw new JsonDeserializationException("Failed to deserialize JSON from stream.", ex, null);
        }
    }

    /// <summary>
    /// Helper method to deserialize a JSON array from a stream.
    /// </summary>
    /// <typeparam name="T">The element type in the array.</typeparam>
    /// <param name="stream">The stream containing UTF-8 JSON array data.</param>
    /// <returns>Deserialized array or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="JsonDeserializationException">Thrown when JSON is invalid or not an array.</exception>
    public T[]? DeserializeArray<T>(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        
        try
        {
            return JsonSerializer.Deserialize<T[]>(stream, _options);
        }
        catch (JsonException ex)
        {
            throw new JsonDeserializationException("Failed to deserialize JSON array from stream.", ex, null);
        }
    }

    /// <summary>
    /// Helper method to deserialize a JSON array from a string.
    /// Less efficient than UTF-8 version.
    /// </summary>
    /// <typeparam name="T">The element type in the array.</typeparam>
    /// <param name="json">JSON array string.</param>
    /// <returns>Deserialized array or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonDeserializationException">Thrown when JSON is invalid or not an array.</exception>
    public T[]? DeserializeArrayString<T>(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        
        try
        {
            return JsonSerializer.Deserialize<T[]>(json, _options);
        }
        catch (JsonException ex)
        {
            throw new JsonDeserializationException("Failed to deserialize JSON array from string.", ex, json);
        }
    }

    /// <summary>
    /// Helper method to get a preview of a <see cref="ReadOnlySequence{T}"/> for error messages.
    /// </summary>
    /// <param name="sequence">The sequence to preview.</param>
    /// <returns>String preview of the sequence (up to 200 bytes).</returns>
    private static string GetSequencePreview(ReadOnlySequence<byte> sequence)
    {
        const int maxPreviewLength = 200;
        
        if (sequence.IsEmpty)
            return string.Empty;
        
        if (sequence.IsSingleSegment)
        {
            var span = sequence.FirstSpan;
            return System.Text.Encoding.UTF8.GetString(span.Length > maxPreviewLength ? span[..maxPreviewLength] : span);
        }
        
        // For multi-segment, convert to array first (only preview portion)
        var length = (int)Math.Min(sequence.Length, maxPreviewLength);
        Span<byte> buffer = stackalloc byte[length];
        sequence.Slice(0, length).CopyTo(buffer);
        return System.Text.Encoding.UTF8.GetString(buffer);
    }
}

using System.Buffers;
using System.Text.Json;

namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Handles JSON deserialization with zero-allocation span-based APIs.
/// Primary methods use UTF-8 encoded bytes for optimal performance.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Performance-First Design:</strong> This interface prioritizes UTF-8 span-based methods
/// following Microsoft's best practices for high-performance JSON processing:
/// </para>
/// <list type="bullet">
/// <item>"Read JSON payloads already encoded as UTF-8 text rather than as UTF-16 strings"</item>
/// <item>"Pass Utf8JsonReader by reference rather than by value"</item>
/// <item>"Use ReadOnlySpan&lt;byte&gt; for UTF-8 JSON to avoid allocations"</item>
/// </list>
/// <para>
/// <strong>Method Priority:</strong>
/// <list type="number">
/// <item><strong>Preferred:</strong> <c>Deserialize(ReadOnlySpan&lt;byte&gt;)</c> - Zero conversion overhead</item>
/// <item><strong>Streaming:</strong> <c>Deserialize(ReadOnlySequence&lt;byte&gt;)</c> - For fragmented buffers</item>
/// <item><strong>Advanced:</strong> <c>Deserialize(ref Utf8JsonReader)</c> - For custom reading scenarios</item>
/// <item><strong>Convenience:</strong> <c>DeserializeString(string)</c> - When source is already a string</item>
/// </list>
/// </para>
/// </remarks>
public interface IJsonDeserializer
{
    /// <summary>
    /// Deserializes UTF-8 encoded JSON from a span (PREFERRED METHOD).
    /// Provides zero allocations for the span itself - only deserialized objects are allocated.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="utf8Json">The UTF-8 encoded JSON bytes</param>
    /// <returns>The deserialized object, or null if the JSON is null</returns>
    /// <remarks>
    /// <para>
    /// <strong>Performance:</strong> This is the fastest deserial ization method as it:
    /// <list type="bullet">
    /// <item>Avoids UTF-16 string allocation</item>
    /// <item>Avoids encoding conversion</item>
    /// <item>Works directly with UTF-8 bytes (JSON's native encoding)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>When to Use:</strong>
    /// <list type="bullet">
    /// <item>File.ReadAllBytes() - Files are read as UTF-8 by default</item>
    /// <item>HTTP responses - Often UTF-8 encoded</item>
    /// <item>Memory-mapped files or shared memory scenarios</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Memory:</strong> Zero allocations for the span parameter. Only the deserialized
    /// object graph is allocated on the heap.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // From file bytes (already UTF-8)
    /// byte[] utf8Bytes = File.ReadAllBytes("data.json");
    /// var person = deserializer.Deserialize&lt;Person&gt;(utf8Bytes);
    /// 
    /// // From HTTP response
    /// byte[] responseBytes = await httpClient.GetByteArrayAsync(url);
    /// var products = deserializer.Deserialize&lt;Product[]&gt;(responseBytes);
    /// 
    /// // From stackalloc (small data)
    /// Span&lt;byte&gt; buffer = stackalloc byte[256];
    /// // ... write JSON to buffer ...
    /// var config = deserializer.Deserialize&lt;Config&gt;(buffer);
    /// </code>
    /// </example>
    T? Deserialize<T>(ReadOnlySpan<byte> utf8Json);

    /// <summary>
    /// Deserializes UTF-8 JSON from a multi-segment sequence (for large streams).
    /// Handles fragmented buffers efficiently without copying data.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="utf8JsonSequence">The fragmented UTF-8 JSON data</param>
    /// <returns>The deserialized object, or null if the JSON is null</returns>
    /// <remarks>
    /// <para>
    /// <strong>When to Use:</strong>
    /// <list type="bullet">
    /// <item>PipeReader scenarios (network streams, large files)</item>
    /// <item>Fragmented memory buffers</item>
    /// <item>Streaming scenarios where data arrives in chunks</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Performance:</strong> Processes multi-segment buffers without copying,
    /// maintaining zero-allocation principles for the buffer structure itself.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // From PipeReader
    /// var reader = PipeReader.Create(stream);
    /// var readResult = await reader.ReadAsync();
    /// var data = deserializer.Deserialize&lt;Data&gt;(readResult.Buffer);
    /// reader.AdvanceTo(readResult.Buffer.End);
    /// 
    /// // From fragmented memory
    /// var sequence = new ReadOnlySequence&lt;byte&gt;(firstSegment);
    /// var result = deserializer.Deserialize&lt;Result&gt;(sequence);
    /// </code>
    /// </example>
    T? Deserialize<T>(ReadOnlySequence<byte> utf8JsonSequence);

    /// <summary>
    /// Deserializes from an active Utf8JsonReader.
    /// MUST be passed by reference (ref struct is mutable and should not be copied).
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="reader">The Utf8JsonReader positioned at the start of the JSON value</param>
    /// <returns>The deserialized object, or null if the JSON is null</returns>
    /// <remarks>
    /// <para>
    /// <strong>CRITICAL LIMITATIONS:</strong>
    /// <list type="bullet">
    /// <item>Utf8JsonReader is a ref struct and CANNOT be stored as a field</item>
    /// <item>Utf8JsonReader CANNOT cross await boundaries (async methods)</item>
    /// <item>Must be passed by reference (<c>ref</c>) to avoid expensive copies</item>
    /// <item>Can only live on the stack - cannot be boxed or used with generics directly</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>When to Use:</strong>
    /// <list type="bullet">
    /// <item>Custom JSON parsing scenarios</item>
    /// <item>When you need fine-grained control over the reading process</item>
    /// <item>Advanced streaming scenarios with manual buffer management</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Async Workaround:</strong> For async scenarios, process the reader synchronously
    /// within the async method but use async I/O for buffer reading:
    /// <code>
    /// public async IAsyncEnumerable&lt;T&gt; DeserializeAsync&lt;T&gt;(Stream stream)
    /// {
    ///     byte[] buffer = ArrayPool&lt;byte&gt;.Shared.Rent(4096);
    ///     try
    ///     {
    ///         while (await stream.ReadAsync(buffer) > 0)  // Async I/O
    ///         {
    ///             var reader = new Utf8JsonReader(buffer);  // Sync reader
    ///             yield return Deserialize&lt;T&gt;(ref reader);  // No await here
    ///         }
    ///     }
    ///     finally
    ///     {
    ///         ArrayPool&lt;byte&gt;.Shared.Return(buffer);
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create reader and deserialize
    /// var reader = new Utf8JsonReader(utf8Json);
    /// var person = deserializer.Deserialize&lt;Person&gt;(ref reader);
    /// 
    /// // Streaming scenario
    /// Span&lt;byte&gt; buffer = stackalloc byte[4096];
    /// int bytesRead = stream.Read(buffer);
    /// var reader = new Utf8JsonReader(buffer.Slice(0, bytesRead));
    /// var item = deserializer.Deserialize&lt;Item&gt;(ref reader);
    /// </code>
    /// </example>
    T? Deserialize<T>(ref Utf8JsonReader reader);

    /// <summary>
    /// Convenience overload for string input (converts to UTF-8 internally).
    /// LESS EFFICIENT than UTF-8 span methods - use sparingly.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="json">The JSON string (UTF-16 encoded)</param>
    /// <returns>The deserialized object, or null if the JSON is null</returns>
    /// <remarks>
    /// <para>
    /// <strong>Performance Impact:</strong> This method requires UTF-16 → UTF-8 conversion,
    /// which incurs approximately 5-10KB allocation overhead for typical payloads.
    /// </para>
    /// <para>
    /// <strong>When to Use:</strong>
    /// <list type="bullet">
    /// <item>Source data is already a string (e.g., from legacy APIs)</item>
    /// <item>Convenience is more important than optimal performance</item>
    /// <item>Small payloads where conversion overhead is negligible</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Recommendation:</strong> Prefer <c>Deserialize(ReadOnlySpan&lt;byte&gt;)</c> when possible.
    /// If you have a string, consider keeping the source as UTF-8 bytes throughout your pipeline.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // From string literal (convenience)
    /// var json = """{"name":"Alice","age":25}""";
    /// var person = deserializer.DeserializeString&lt;Person&gt;(json);
    /// 
    /// // From File.ReadAllText (not recommended - prefer File.ReadAllBytes)
    /// var jsonString = File.ReadAllText("data.json");  // ⚠️ Creates UTF-16 string
    /// var data = deserializer.DeserializeString&lt;Data&gt;(jsonString);
    /// 
    /// // Better alternative:
    /// byte[] utf8Bytes = File.ReadAllBytes("data.json");  // Already UTF-8
    /// var data = deserializer.Deserialize&lt;Data&gt;(utf8Bytes);  // No conversion
    /// </code>
    /// </example>
    T? DeserializeString<T>(string json);

    /// <summary>
    /// Deserializes a JSON array from UTF-8 bytes.
    /// Helper method for array-based JSON documents.
    /// </summary>
    /// <typeparam name="T">The element type in the array</typeparam>
    /// <param name="utf8Json">UTF-8 encoded JSON array</param>
    /// <returns>Deserialized array or null</returns>
    T[]? DeserializeArray<T>(ReadOnlySpan<byte> utf8Json);

    /// <summary>
    /// Deserializes a JSON array from a byte array.
    /// </summary>
    /// <typeparam name="T">The element type in the array</typeparam>
    /// <param name="utf8Json">UTF-8 encoded JSON array as byte array</param>
    /// <returns>Deserialized array or null</returns>
    T[]? DeserializeArray<T>(byte[] utf8Json);

    /// <summary>
    /// Deserializes a JSON array from a stream.
    /// </summary>
    /// <typeparam name="T">The element type in the array</typeparam>
    /// <param name="stream">The stream containing UTF-8 JSON array data</param>
    /// <returns>Deserialized array or null</returns>
    T[]? DeserializeArray<T>(Stream stream);
}

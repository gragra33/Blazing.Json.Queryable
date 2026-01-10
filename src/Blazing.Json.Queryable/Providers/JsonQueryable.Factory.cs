using System.Text.Json;

namespace Blazing.Json.Queryable.Providers;

/// <summary>
/// Static factory methods for creating <see cref="JsonQueryable{T}"/> instances from various sources.
/// </summary>
public partial class JsonQueryable<T>
{
    #region Static Factory Methods

    /// <summary>
    /// Creates a queryable from UTF-8 encoded JSON bytes (preferred for best performance).
    /// Zero allocations for JSON processing after initial read.
    /// </summary>
    /// <param name="utf8Json">UTF-8 encoded JSON byte array.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>A queryable over the JSON data.</returns>
    public static IQueryable<T> FromUtf8(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? options = null)
    {
        byte[] buffer = utf8Json.ToArray();
        return FromUtf8(buffer, null, options);
    }

    /// <summary>
    /// Creates a queryable from UTF-8 encoded JSON bytes with optional JSONPath filtering.
    /// Zero allocations for JSON processing after initial read.
    /// </summary>
    /// <param name="utf8Json">UTF-8 encoded JSON byte array.</param>
    /// <param name="jsonPath">Optional JSONPath expression to filter tokens during streaming (e.g., "$.data[*]", "$.result[*].customer").</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>A queryable over the JSON data.</returns>
    /// <remarks>
    /// <para><strong>Performance Characteristics:</strong></para>
    /// <list type="bullet">
    /// <item><strong>Without JSONPath (jsonPath = null):</strong> Uses optimized in-memory deserialization.</item>
    /// <item><strong>With JSONPath:</strong> Uses token-based filtering with Utf8JsonAsyncStreamReader.</item>
    /// </list>
    /// </remarks>
    public static IQueryable<T> FromUtf8(ReadOnlySpan<byte> utf8Json, string? jsonPath, JsonSerializerOptions? options = null)
    {
        byte[] buffer = utf8Json.ToArray();
        return FromUtf8(buffer, jsonPath, options);
    }

    /// <summary>
    /// Creates a queryable from UTF-8 encoded JSON byte array.
    /// </summary>
    /// <param name="utf8Json">UTF-8 encoded JSON byte array.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>A queryable over the JSON data.</returns>
    public static IQueryable<T> FromUtf8(byte[] utf8Json, JsonSerializerOptions? options = null)
    {
        return FromUtf8(utf8Json, null, options);
    }

    /// <summary>
    /// Creates a queryable from UTF-8 encoded JSON byte array with optional JSONPath filtering.
    /// </summary>
    /// <param name="utf8Json">UTF-8 encoded JSON byte array.</param>
    /// <param name="jsonPath">Optional JSONPath expression to filter tokens (e.g., "$.data[*]").</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>A queryable over the JSON data.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="utf8Json"/> is null.</exception>
    public static IQueryable<T> FromUtf8(byte[] utf8Json, string? jsonPath, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(utf8Json);
        
        var context = JsonQueryContext.FromUtf8(utf8Json, jsonPath, options);
        var provider = new JsonQueryProvider(context);
        return new JsonQueryable<T>(provider);
    }

    /// <summary>
    /// Creates a queryable from a JSON string (converts to UTF-8 internally).
    /// Less efficient than <see cref="FromUtf8(ReadOnlySpan{byte}, JsonSerializerOptions?)"/> - use when source data is already a string.
    /// </summary>
    /// <param name="json">JSON string.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>A queryable over the JSON data.</returns>
    public static IQueryable<T> FromString(string json, JsonSerializerOptions? options = null)
    {
        return FromString(json, null, options);
    }

    /// <summary>
    /// Creates a queryable from a JSON string with optional JSONPath filtering.
    /// Converts string to UTF-8 bytes then applies optional token filtering.
    /// </summary>
    /// <param name="json">JSON string.</param>
    /// <param name="jsonPath">Optional JSONPath expression to filter tokens (e.g., "$.data[*]").</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>A queryable over the JSON data.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    public static IQueryable<T> FromString(string json, string? jsonPath, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        
        var context = JsonQueryContext.FromString(json, jsonPath, options);
        var provider = new JsonQueryProvider(context);
        return new JsonQueryable<T>(provider);
    }

    /// <summary>
    /// Creates a queryable from a stream (forward-only, constant memory).
    /// Uses stackalloc for sync (4KB buffer) or ArrayPool for async.
    /// </summary>
    /// <param name="stream">Stream containing JSON data.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>A queryable over the JSON data.</returns>
    /// <remarks>
    /// The stream is NOT owned by the queryable and will not be disposed.
    /// Caller is responsible for disposing the stream.
    /// </remarks>
    public static IQueryable<T> FromStream(Stream stream, JsonSerializerOptions? options = null)
    {
        return FromStream(stream, null, options);
    }

    /// <summary>
    /// Creates a queryable from a stream with optional JSONPath filtering.
    /// Uses constant memory streaming with optional token-based filtering.
    /// </summary>
    /// <param name="stream">Stream containing JSON data.</param>
    /// <param name="jsonPath">Optional JSONPath expression to filter tokens during streaming (e.g., "$.data[*]", "$.customers[*].orders[*]").</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>A queryable over the JSON data.</returns>
    /// <remarks>
    /// <para><strong>Performance Characteristics:</strong></para>
    /// <list type="bullet">
    /// <item><strong>Without JSONPath:</strong> Constant ~4KB memory (sync) or ~50KB (async).</item>
    /// <item><strong>With JSONPath:</strong> Uses Utf8JsonAsyncStreamReader for token extraction, constant memory.</item>
    /// </list>
    /// <para>
    /// The stream is NOT owned by the queryable and will not be disposed.
    /// Caller is responsible for disposing the stream.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Without JSONPath - simple array
    /// await using var stream = File.OpenRead("people.json");
    /// var adults = JsonQueryable.FromStream&lt;Person&gt;(stream)
    ///     .Where(p => p.Age >= 18)
    ///     .ToList();
    /// 
    /// // With JSONPath - API wrapper: { "data": [...] }
    /// await using var stream = httpClient.GetStreamAsync("/api/people");
    /// var people = JsonQueryable.FromStream&lt;Person&gt;(stream, "$.data[*]")
    ///     .Where(p => p.IsActive)
    ///     .ToList();
    /// </code>
    /// </example>
    public static IQueryable<T> FromStream(Stream stream, string? jsonPath, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        
        var context = JsonQueryContext.FromStream(stream, jsonPath, options);
        var provider = new JsonQueryProvider(context);
        return new JsonQueryable<T>(provider);
    }

    /// <summary>
    /// Creates a queryable from a file (reads as UTF-8 bytes directly).
    /// Optimal performance for file-based queries.
    /// </summary>
    /// <param name="filePath">Path to JSON file.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>A queryable over the JSON data.</returns>
    /// <remarks>
    /// <para>
    /// <strong>IMPORTANT:</strong> File-based queries open a file stream that must be disposed.
    /// Always use a 'using' statement or call Dispose/DisposeAsync explicitly.
    /// </para>
    /// <code>
    /// // ✓ Correct - using statement ensures disposal
    /// using var query = JsonQueryable&lt;Person&gt;.FromFile("data.json");
    /// var results = query.Where(p => p.Age > 25).ToList();
    /// </code>
    /// </remarks>
    public static JsonQueryable<T> FromFile(string filePath, JsonSerializerOptions? options = null)
    {
        return FromFile(filePath, null, options);
    }

    /// <summary>
    /// Creates a queryable from a file with optional JSONPath filtering.
    /// Reads file as UTF-8 bytes directly with optional token-based filtering.
    /// </summary>
    /// <param name="filePath">Path to JSON file.</param>
    /// <param name="jsonPath">Optional JSONPath expression to filter tokens (e.g., "$.data[*]").</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>A queryable over the JSON data.</returns>
    /// <remarks>
    /// <para>
    /// <strong>IMPORTANT:</strong> File-based queries open a file stream that must be disposed.
    /// Always use a 'using' statement or call Dispose/DisposeAsync explicitly.
    /// </para>
    /// <code>
    /// // ✓ Correct - using statement ensures disposal
    /// using var query = JsonQueryable&lt;Person&gt;.FromFile("data.json");
    /// var results = query.Where(p => p.Age > 25).ToList();
    /// 
    /// // ✓ Correct - with JSONPath filtering
    /// using var query = JsonQueryable&lt;Person&gt;.FromFile("data.json", "$.result[*]");
    /// var results = query.Where(p => p.Age > 25).ToList();
    /// </code>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if <paramref name="filePath"/> is null or whitespace.</exception>
    public static JsonQueryable<T> FromFile(string filePath, string? jsonPath, JsonSerializerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        
        var context = JsonQueryContext.FromFile(filePath, jsonPath, options);
        var provider = new JsonQueryProvider(context);
        return new JsonQueryable<T>(provider);
    }

    /// <summary>
    /// Returns an empty <see cref="IQueryable{T}"/>.
    /// </summary>
    /// <returns>An empty <see cref="IQueryable{T}"/>.</returns>
    public static IQueryable<T> Empty()
    {
        return Enumerable.Empty<T>().AsQueryable();
    }

    #endregion
}

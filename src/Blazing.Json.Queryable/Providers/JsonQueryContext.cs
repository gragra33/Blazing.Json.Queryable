using System.Text.Json;
using Blazing.Json.Queryable.Core;
using Blazing.Json.Queryable.Utilities;
using Blazing.Json.Queryable.Exceptions;

namespace Blazing.Json.Queryable.Providers;

/// <summary>
/// Manages the context for JSON query execution including source data,
/// configuration, and lifecycle management.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Resource Management:</strong>
/// <list type="bullet">
/// <item>File-based and stream-based contexts may own and dispose streams.</item>
/// <item>Always dispose the context when done to release resources.</item>
/// </list>
/// </para>
/// </remarks>
public class JsonQueryContext : IDisposable, IAsyncDisposable
{
    private readonly JsonSourceType _sourceType;
    private ReadOnlyMemory<byte>? _utf8Bytes;
    private string? _jsonString;
    private Stream? _stream;
    private string? _filePath;
    private bool _ownsStream;
    private bool _disposed;

    /// <summary>
    /// Gets the type of JSON source.
    /// </summary>
    public JsonSourceType SourceType => _sourceType;

    /// <summary>
    /// Gets the query configuration.
    /// </summary>
    public JsonQueryableConfiguration Configuration { get; }

    /// <summary>
    /// Optional JSONPath expression for token filtering.
    /// When set, uses Utf8JsonAsyncStreamReader for token-based extraction.
    /// </summary>
    public string? JsonPath { get; private set; }

    private JsonQueryContext(
        JsonSourceType sourceType,
        JsonSerializerOptions? options)
    {
        _sourceType = sourceType;
        Configuration = JsonQueryableConfiguration.Default(options);
    }

    #region Factory Methods

    /// <summary>
    /// Creates a context from UTF-8 encoded bytes.
    /// </summary>
    /// <param name="utf8Json">UTF-8 encoded JSON byte array.</param>
    /// <param name="jsonPath">Optional JSONPath expression for token filtering (e.g., "$.data[*]", "$.result[*].customer").</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>Configured context for querying.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="utf8Json"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="jsonPath"/> is invalid.</exception>
    public static JsonQueryContext FromUtf8(
        byte[] utf8Json,
        string? jsonPath = null,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(utf8Json);

        if (jsonPath != null)
        {
            ValidateJsonPath(jsonPath);
        }

        var context = new JsonQueryContext(JsonSourceType.Utf8Bytes, options)
        {
            _utf8Bytes = new ReadOnlyMemory<byte>(utf8Json),
            JsonPath = jsonPath
        };

        return context;
    }

    /// <summary>
    /// Creates a context from a JSON string (converts to UTF-8).
    /// </summary>
    /// <param name="json">JSON string.</param>
    /// <param name="jsonPath">Optional JSONPath expression for token filtering (e.g., "$.data[*]", "$.result[*].customer").</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>Configured context for querying.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="jsonPath"/> is invalid.</exception>
    public static JsonQueryContext FromString(
        string json,
        string? jsonPath = null,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (jsonPath != null)
        {
            ValidateJsonPath(jsonPath);
        }

        var context = new JsonQueryContext(JsonSourceType.String, options)
        {
            _jsonString = json,
            JsonPath = jsonPath
        };

        return context;
    }

    /// <summary>
    /// Creates a context from a stream.
    /// </summary>
    /// <param name="stream">Stream containing JSON data.</param>
    /// <param name="jsonPath">Optional JSONPath expression for token filtering (e.g., "$.data[*]", "$.result[*].customer").</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>Configured context for querying.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="stream"/> is not readable or <paramref name="jsonPath"/> is invalid.</exception>
    public static JsonQueryContext FromStream(
        Stream stream,
        string? jsonPath = null,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable.", nameof(stream));
        }

        if (jsonPath != null)
        {
            ValidateJsonPath(jsonPath);
        }

        var context = new JsonQueryContext(JsonSourceType.Stream, options)
        {
            _stream = stream,
            JsonPath = jsonPath,
            _ownsStream = false
        };

        return context;
    }

    /// <summary>
    /// Creates a context from a file path.
    /// </summary>
    /// <param name="filePath">Path to JSON file.</param>
    /// <param name="jsonPath">Optional JSONPath expression for token filtering (e.g., "$.data[*]", "$.result[*].customer").</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>Configured context for querying.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="filePath"/> is null, whitespace, or file does not exist, or if <paramref name="jsonPath"/> is invalid.</exception>
    public static JsonQueryContext FromFile(
        string filePath,
        string? jsonPath = null,
        JsonSerializerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }

        if (jsonPath != null)
        {
            ValidateJsonPath(jsonPath);
        }

        var context = new JsonQueryContext(JsonSourceType.File, options)
        {
            _filePath = filePath,
            JsonPath = jsonPath
        };

        return context;
    }

    #endregion

    #region Source Access Methods

    /// <summary>
    /// Gets the UTF-8 encoded JSON source.
    /// For string sources, converts to UTF-8 on first access (cached).
    /// </summary>
    /// <returns>The UTF-8 encoded JSON as ReadOnlyMemory{byte}.</returns>
    /// <exception cref="InvalidQueryException">Thrown for stream-based or file-based sources.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the context has been disposed.</exception>
    public ReadOnlyMemory<byte> GetUtf8Source()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _sourceType switch
        {
            JsonSourceType.Utf8Bytes => _utf8Bytes!.Value,
            
            JsonSourceType.String => GetOrConvertStringToUtf8(),
            
            JsonSourceType.Stream => throw new InvalidQueryException(
                "Cannot get UTF-8 source for stream-based queries. Use GetStream() instead.",
                "GetUtf8Source"),
            
            JsonSourceType.File => throw new InvalidQueryException(
                "Cannot get UTF-8 source for file-based queries. Use GetStream() instead.",
                "GetUtf8Source"),
            
            _ => throw new InvalidQueryException($"Source type {_sourceType} is not supported.", "GetUtf8Source")
        };
    }

    /// <summary>
    /// Gets the stream for stream-based or file-based sources.
    /// For file sources, opens the file on first access.
    /// </summary>
    /// <returns>The <see cref="Stream"/> for the source.</returns>
    /// <exception cref="InvalidQueryException">Thrown for non-stream sources.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the context has been disposed.</exception>
    public Stream GetStream()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _sourceType switch
        {
            JsonSourceType.Stream => _stream!,
            
            JsonSourceType.File => GetOrOpenFileStream(),
            
            JsonSourceType.Utf8Bytes => throw new InvalidQueryException(
                "Cannot get stream for UTF-8 byte array source.",
                "GetStream"),
            
            JsonSourceType.String => throw new InvalidQueryException(
                "Cannot get stream for string source.",
                "GetStream"),
            
            _ => throw new InvalidQueryException($"Source type {_sourceType} is not supported.", "GetStream")
        };
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Converts the JSON string to UTF-8 bytes (cached after first call).
    /// </summary>
    /// <returns>The UTF-8 encoded JSON as ReadOnlyMemory{byte}.</returns>
    private ReadOnlyMemory<byte> GetOrConvertStringToUtf8()
    {
        _utf8Bytes ??= new ReadOnlyMemory<byte>(
            Utf8Helper.ConvertToUtf8(_jsonString!));

        return _utf8Bytes.Value;
    }

    /// <summary>
    /// Opens the file stream (created on first access, owned by context).
    /// </summary>
    /// <returns>The opened <see cref="Stream"/> for the file.</returns>
    private Stream GetOrOpenFileStream()
    {
        if (_stream == null)
        {
            _stream = File.OpenRead(_filePath!);
            _ownsStream = true;
        }

        return _stream;
    }

    /// <summary>
    /// Validates JSONPath syntax.
    /// </summary>
    /// <param name="jsonPath">The JSONPath expression to validate.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="jsonPath"/> is null, whitespace, or does not start with '$'.</exception>
    private static void ValidateJsonPath(string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            throw new ArgumentException(
                "JSONPath expression cannot be empty or whitespace.",
                nameof(jsonPath));
        }

        if (!jsonPath.StartsWith('$'))
        {
            throw new ArgumentException(
                "JSONPath expression must start with '$' (e.g., '$.data[*]', '$.result[*].customer')",
                nameof(jsonPath));
        }
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes the context and releases resources (synchronous).
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_ownsStream && _stream != null)
        {
            _stream.Dispose();
            _stream = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the context and releases resources (asynchronous).
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        if (_ownsStream && _stream != null)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
            _stream = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}

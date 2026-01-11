using System.Runtime.CompilerServices;
using System.Text.Json;
using Blazing.Json.Queryable.Core;

namespace Blazing.Json.Queryable.Execution;

/// <summary>
/// Executes queries against in-memory JSON (string or UTF-8 bytes) with JSONPath filtering.
/// Supports both simple path navigation and RFC 9535 compliant JSONPath queries.
/// Converts memory source to stream, then delegates to TokenFilteredStreamExecutor.
/// </summary>
public sealed class TokenFilteredMemoryExecutor : IQueryExecutor
{
    private readonly ReadOnlyMemory<byte> _utf8Json;
    private readonly string _jsonPath;
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of TokenFilteredMemoryExecutor.
    /// </summary>
    /// <param name="utf8Json">UTF-8 encoded JSON bytes.</param>
    /// <param name="jsonPath">JSONPath expression (supports RFC 9535 syntax).</param>
    /// <param name="options">JSON serializer options.</param>
    public TokenFilteredMemoryExecutor(
        ReadOnlyMemory<byte> utf8Json,
        string jsonPath,
        JsonSerializerOptions? options)
    {
        _utf8Json = utf8Json;
        _jsonPath = jsonPath ?? throw new ArgumentNullException(nameof(jsonPath));
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Executes a query synchronously against the in-memory JSON with JSONPath filtering.
    /// </summary>
    /// <typeparam name="T">The type of elements in the result.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>Enumerable of query results.</returns>
    public IEnumerable<T> Execute<T>(QueryExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.Validate();

        // Convert memory to stream
        using var stream = new MemoryStream(_utf8Json.ToArray());
        var streamExecutor = new TokenFilteredStreamExecutor(stream, _jsonPath, _options);
        return streamExecutor.Execute<T>(plan);
    }

    /// <summary>
    /// Executes a query asynchronously against the in-memory JSON with JSONPath filtering.
    /// </summary>
    /// <typeparam name="T">The type of elements in the result.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Async enumerable of query results.</returns>
    public async IAsyncEnumerable<T> ExecuteAsync<T>(
        QueryExecutionPlan plan,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.Validate();

        // Convert memory to stream
        using var stream = new MemoryStream(_utf8Json.ToArray());
        var streamExecutor = new TokenFilteredStreamExecutor(stream, _jsonPath, _options);

        await foreach (var item in streamExecutor.ExecuteAsync<T>(plan, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }
}

using System.Runtime.CompilerServices;
using System.Text.Json;
using Blazing.Json.Queryable.Core;
using System.Text.Json.Stream;
using Blazing.Json.JSONPath.Parser;
using Blazing.Json.JSONPath.Evaluator;
using Blazing.Json.JSONPath.Utilities;

namespace Blazing.Json.Queryable.Execution;

/// <summary>
/// Executes queries against JSON streams with JSONPath filtering support.
/// Supports both simple path navigation and RFC 9535 compliant JSONPath queries.
/// Extended to support all LINQ operations with automatic materialization when needed.
/// </summary>
public sealed class TokenFilteredStreamExecutor : IQueryExecutor
{
    private readonly Stream _stream;
    private readonly string _jsonPath;
    private readonly JsonSerializerOptions _options;
    private readonly QueryOperationExecutor _operations = new();

    /// <summary>
    /// Initializes a new instance of <see cref="TokenFilteredStreamExecutor"/>.
    /// </summary>
    /// <param name="stream">The JSON stream to read from.</param>
    /// <param name="jsonPath">JSONPath expression (supports RFC 9535 syntax).</param>
    /// <param name="options">JSON serializer options. If null, uses case-insensitive property names.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> or <paramref name="jsonPath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="stream"/> is not readable.</exception>
    public TokenFilteredStreamExecutor(
        Stream stream,
        string jsonPath,
        JsonSerializerOptions? options)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _jsonPath = jsonPath ?? throw new ArgumentNullException(nameof(jsonPath));
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable.", nameof(stream));
        }
    }

    /// <summary>
    /// Synchronous execution - materializes tokens into memory then filters.
    /// For truly constant memory, use <see cref="ExecuteAsync"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the result.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of query results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="plan"/> is null.</exception>
    public IEnumerable<T> Execute<T>(QueryExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.Validate();

        if (plan.AggregationType.HasValue)
        {
            return ExecuteAggregation<T>(plan);
        }

        if (plan.QuantifierType.HasValue)
        {
            return ExecuteQuantifier<T>(plan);
        }

        if (plan.ConversionType.HasValue)
        {
            return ExecuteConversion<T>(plan);
        }

        var task = ExecuteAsyncCore<T>(plan, CancellationToken.None).ToListAsync();
        return task.AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronous execution with token-based filtering.
    /// Maintains constant memory usage via ArrayPool.
    /// </summary>
    /// <typeparam name="T">The type of elements in the result.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of query results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="plan"/> is null.</exception>
    public async IAsyncEnumerable<T> ExecuteAsync<T>(
        QueryExecutionPlan plan,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.Validate();

        await foreach (var item in ExecuteAsyncCore<T>(plan, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Core async execution logic for token-based filtering and streaming.
    /// </summary>
    /// <typeparam name="T">The type of elements in the result.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of query results.</returns>
    private async IAsyncEnumerable<T> ExecuteAsyncCore<T>(
        QueryExecutionPlan plan,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sourceType = plan.SourceType!;
        var executeMethod = typeof(TokenFilteredStreamExecutor)
            .GetMethod(nameof(ExecuteAsyncTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(sourceType, typeof(T));

        var asyncEnumerable = (IAsyncEnumerable<T>)executeMethod.Invoke(this, [plan, cancellationToken])!;

        await foreach (var item in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Executes the query asynchronously with the specified source and result types.
    /// Automatically selects between RFC 9535 JSONPath and simple navigation.
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An <see cref="IAsyncEnumerable{TResult}"/> of query results.</returns>
    private async IAsyncEnumerable<TResult> ExecuteAsyncTyped<TSource, TResult>(
        QueryExecutionPlan plan,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var predicates = plan.Predicates?.Cast<Func<TSource, bool>>().ToList();

        // Check if query requires materialization (can't stream these operations)
        if (plan.RequiresMaterialization)
        {
            await foreach (var item in ExecuteWithMaterializationAsync<TSource, TResult>(plan, cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
            yield break;
        }

        // Check if this is an RFC 9535 expression using JsonPathHelper from Blazing.Json.JSONPath
        if (JsonPathHelper.HasFeatures(_jsonPath.AsSpan()))
        {
            // Use Blazing.Json.JSONPath for RFC 9535 queries
            await foreach (var item in ExecuteWithJsonPathAsync<TSource, TResult>(plan, predicates, cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
            yield break;
        }

        // Use simple navigation for basic paths
        await foreach (var item in ExecuteWithSimpleNavigationAsync<TSource, TResult>(plan, predicates, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Executes query using RFC 9535 JSONPath evaluation.
    /// </summary>
    private async IAsyncEnumerable<TResult> ExecuteWithJsonPathAsync<TSource, TResult>(
        QueryExecutionPlan plan,
        List<Func<TSource, bool>>? predicates,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        int skip = plan.Skip ?? 0;
        int? take = plan.Take;

        // Load JSON into JsonDocument for RFC 9535 evaluation
        if (_stream.CanSeek)
        {
            _stream.Position = 0;
        }

        using var doc = await JsonDocument.ParseAsync(_stream, cancellationToken: cancellationToken);
        
        // Parse and evaluate RFC 9535 JSONPath
        var query = JsonPathParser.Parse(_jsonPath);
        var evaluator = new JsonPathEvaluator();
        var results = evaluator.Evaluate(query, doc.RootElement);

        foreach (var result in results)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Deserialize matched element
            TSource? item;
            try
            {
                item = JsonSerializer.Deserialize<TSource>(
                    result.Value.GetRawText(),
                    _options
                );
            }
            catch (JsonException)
            {
                continue;
            }

            if (item is null)
                continue;

            // Apply type filtering (OfType)
            if (plan.TypeFilter is not null && item.GetType() != plan.TypeFilter)
                continue;

            // Apply additional LINQ WHERE predicates
            bool matches = true;
            if (predicates is not null)
            {
                foreach (var predicate in predicates)
                {
                    if (!predicate(item))
                    {
                        matches = false;
                        break;
                    }
                }
            }

            if (!matches)
                continue;

            // Apply SKIP
            if (skip > 0)
            {
                skip--;
                continue;
            }

            // Apply projection
            TResult resultItem;
            if (plan.ProjectionSelector is not null && plan.ResultType != typeof(TSource))
            {
                var projector = (Func<TSource, TResult>)plan.ProjectionSelector;
                resultItem = projector(item);
            }
            else
            {
                resultItem = (TResult)(object)item!;
            }

            yield return resultItem;

            // EARLY TERMINATION
            if (take.HasValue && --take <= 0)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Executes query using simple path navigation (existing logic).
    /// </summary>
    private async IAsyncEnumerable<TResult> ExecuteWithSimpleNavigationAsync<TSource, TResult>(
        QueryExecutionPlan plan,
        List<Func<TSource, bool>>? predicates,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Streaming path with simple navigation
        using var reader = new Utf8JsonAsyncStreamReader(_stream, leaveOpen: true);

        int skip = plan.Skip ?? 0;
        int? take = plan.Take;

        // Parse JSONPath to extract target array path
        var pathSegments = ParseJsonPath(_jsonPath);

        // Navigate to the target array using the reader
        bool navigated = await NavigateToArrayAsync(reader, pathSegments, cancellationToken);
        
        if (!navigated)
        {
            // Navigation failed - fall back to materialization
            if (_stream.CanSeek)
            {
                _stream.Position = 0;
            }
            
            var items = await MaterializeAllAsync<TSource>(cancellationToken);
            var results = _operations.ExecuteQuery<TSource, TResult>(items, plan);
            
            foreach (var item in results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
            
            yield break;
        }

        // Now we're at the start of the target array, read each element
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Deserialize the current object/array to source type
                TSource? item;
                try
                {
                    item = await reader.DeserializeAsync<TSource>(_options, cancellationToken);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (item is null)
                    continue;

                // Apply type filtering (OfType)
                if (plan.TypeFilter is not null && item.GetType() != plan.TypeFilter)
                    continue;

                // Apply WHERE predicates
                bool matches = true;
                if (predicates is not null)
                {
                    foreach (var predicate in predicates)
                    {
                        if (!predicate(item))
                        {
                            matches = false;
                            break;
                        }
                    }
                }

                if (!matches)
                    continue;

                // Apply SKIP
                if (skip > 0)
                {
                    skip--;
                    continue;
                }

                // Apply projection
                TResult result;
                if (plan.ProjectionSelector is not null && plan.ResultType != typeof(TSource))
                {
                    var projector = (Func<TSource, TResult>)plan.ProjectionSelector;
                    result = projector(item);
                }
                else
                {
                    result = (TResult)(object)item!;
                }

                yield return result;

                // EARLY TERMINATION
                if (take.HasValue && --take <= 0)
                {
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// Executes the query with materialization (loads all elements before processing) asynchronously.
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An <see cref="IAsyncEnumerable{TResult}"/> of query results.</returns>
    private async IAsyncEnumerable<TResult> ExecuteWithMaterializationAsync<TSource, TResult>(
        QueryExecutionPlan plan,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var items = await MaterializeAllAsync<TSource>(cancellationToken);
        var results = _operations.ExecuteQuery<TSource, TResult>(items, plan);
        
        foreach (var item in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    #region Terminal Operations

    /// <summary>
    /// Executes an aggregation operation and returns the result as a single-element enumerable.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> containing the aggregation result.</returns>
    private IEnumerable<T> ExecuteAggregation<T>(QueryExecutionPlan plan)
    {
        var items = MaterializeAllAsync<object>(CancellationToken.None).GetAwaiter().GetResult();
        object result = QueryOperationExecutor.ExecuteAggregation(items, plan);
        return [(T)result];
    }

    /// <summary>
    /// Executes a quantifier operation and returns the result as a single-element enumerable.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> containing the quantifier result.</returns>
    private IEnumerable<T> ExecuteQuantifier<T>(QueryExecutionPlan plan)
    {
        var items = MaterializeAllAsync<object>(CancellationToken.None).GetAwaiter().GetResult();
        bool result = QueryOperationExecutor.ExecuteQuantifier(items, plan);
        return [(T)(object)result];
    }

    /// <summary>
    /// Executes a conversion operation and returns the result as a single-element enumerable.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> containing the conversion result.</returns>
    private IEnumerable<T> ExecuteConversion<T>(QueryExecutionPlan plan)
    {
        var items = MaterializeAllAsync<object>(CancellationToken.None).GetAwaiter().GetResult();
        object result = QueryOperationExecutor.ExecuteConversion(items, plan);
        return [(T)result];
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Parses JSONPath expression into path segments.
    /// Supports simple paths like <c>$.data[*]</c> or <c>$.result[*].items[*]</c>.
    /// </summary>
    /// <param name="jsonPath">The JSONPath expression.</param>
    /// <returns>Array of path segments.</returns>
    private static string[] ParseJsonPath(string jsonPath)
    {
        if (!jsonPath.StartsWith('$'))
        {
            throw new ArgumentException("JSONPath must start with '$'", nameof(jsonPath));
        }

        var segments = jsonPath.TrimStart('$', '.')
            .Split('.')
            .Select(s => s.Replace("[*]", "").Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
        
        return segments;
    }

    /// <summary>
    /// Navigates to the target array in the JSON stream using the reader.
    /// Handles nested object properties and multiple array wildcards.
    /// Collects all items from all array elements at each level.
    /// </summary>
    /// <param name="reader">The JSON stream reader.</param>
    /// <param name="pathSegments">The path segments to navigate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if navigation succeeded and at the target array; otherwise, false.</returns>
    private static async Task<bool> NavigateToArrayAsync(
        Utf8JsonAsyncStreamReader reader,
        string[] pathSegments,
        CancellationToken cancellationToken)
    {
        int currentSegment = 0;
        
        while (currentSegment < pathSegments.Length)
        {
            var targetPropertyName = pathSegments[currentSegment];
            bool propertyFound = false;
            
            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    
                    if (string.Equals(propertyName, targetPropertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        propertyFound = true;
                        await reader.ReadAsync(cancellationToken); // Read the value
                        
                        // Check if this is the last segment - should be an array
                        if (currentSegment == pathSegments.Length - 1)
                        {
                            return reader.TokenType == JsonTokenType.StartArray;
                        }
                        
                        // Not the last segment - should be an object, continue to next segment
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            currentSegment++;
                            break; // Break inner loop to process next segment
                        }
                        
                        // CRITICAL: If we encounter an ARRAY in the middle of the path
                        // This means we need to collect from ALL elements, not just the first
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            // We CANNOT handle multi-array wildcards with streaming navigation
                            // The caller needs to use materialization approach instead
                            return false;
                        }
                        
                        // Unexpected token type
                        return false;
                    }
                }
            }
            
            if (!propertyFound)
            {
                return false; // Property not found in JSON
            }
        }
        
        return false; // Shouldn't reach here
    }

    /// <summary>
    /// Materializes all items from the JSON stream using JSONPath filtering.
    /// Handles multi-level array wildcards by recursively collecting from all branches.
    /// </summary>
    /// <typeparam name="T">The type to deserialize each element to.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A <see cref="List{T}"/> containing all deserialized elements.</returns>
    private async Task<List<T>> MaterializeAllAsync<T>(CancellationToken cancellationToken)
    {
        var results = new List<T>();
        
        // Reset stream position if seekable
        if (_stream.CanSeek)
        {
            _stream.Position = 0;
        }

        // Check if this is an RFC 9535 expression using JsonPathHelper
        if (JsonPathHelper.HasFeatures(_jsonPath.AsSpan()))
        {
            // Use Blazing.Json.JSONPath for RFC 9535 queries
            using var doc = await JsonDocument.ParseAsync(_stream, cancellationToken: cancellationToken);
            
            // Parse and evaluate RFC 9535 JSONPath
            var query = JsonPathParser.Parse(_jsonPath);
            var evaluator = new JsonPathEvaluator();
            var jsonResults = evaluator.Evaluate(query, doc.RootElement);

            foreach (var result in jsonResults)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Deserialize matched element
                T? item;
                try
                {
                    item = JsonSerializer.Deserialize<T>(
                        result.Value.GetRawText(),
                        _options
                    );
                }
                catch (JsonException)
                {
                    continue;
                }

                if (item is not null)
                {
                    results.Add(item);
                }
            }
            
            return results;
        }
        
        using var reader = new Utf8JsonAsyncStreamReader(_stream, leaveOpen: true);
        var pathSegments = ParseJsonPath(_jsonPath);

        // Use recursive collection to handle multi-array wildcards
        await CollectItemsRecursiveAsync(reader, pathSegments, 0, results, _options, cancellationToken);
        
        return results;
    }

    /// <summary>
    /// Recursively collects items from JSON by navigating through path segments.
    /// Handles array wildcards by iterating through all array elements.
    /// </summary>
    /// <typeparam name="T">The type to deserialize each element to.</typeparam>
    /// <param name="reader">The JSON stream reader.</param>
    /// <param name="pathSegments">The path segments to navigate.</param>
    /// <param name="currentSegment">The current segment index.</param>
    /// <param name="results">The list to collect results into.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    private static async Task CollectItemsRecursiveAsync<T>(
        Utf8JsonAsyncStreamReader reader,
        string[] pathSegments,
        int currentSegment,
        List<T> results,
        JsonSerializerOptions options,
        CancellationToken cancellationToken)
    {
        if (currentSegment >= pathSegments.Length)
        {
            return;
        }

        var targetPropertyName = pathSegments[currentSegment];
        
        // Navigate to find the target property
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                
                if (string.Equals(propertyName, targetPropertyName, StringComparison.OrdinalIgnoreCase))
                {
                    // Read the property value
                    await reader.ReadAsync(cancellationToken);
                    
                    // Is this the last segment?
                    if (currentSegment == pathSegments.Length - 1)
                    {
                        // This is the final array - collect all items
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            await CollectArrayItemsAsync(reader, results, options, cancellationToken);
                        }
                        return;
                    }
                    
                    // Not the last segment - handle based on token type
                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        // Single object - recurse into it
                        await CollectItemsRecursiveAsync(reader, pathSegments, currentSegment + 1, results, options, cancellationToken);
                        return;
                    }
                    else if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        // Array wildcard - iterate through ALL elements
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            if (reader.TokenType == JsonTokenType.EndArray)
                                break;
                            
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                // Recurse into this array element with the next path segment
                                await CollectItemsRecursiveAsync(reader, pathSegments, currentSegment + 1, results, options, cancellationToken);
                            }
                            else
                            {
                                // Skip non-object elements
                                await SkipCurrentTokenAsync(reader, cancellationToken);
                            }
                        }
                        return;
                    }
                }
            }
            else if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
            {
                // We've reached the end of the current object/array without finding the property
                return;
            }
        }
    }

    /// <summary>
    /// Collects all items from the current array.
    /// </summary>
    /// <typeparam name="T">The type to deserialize each element to.</typeparam>
    /// <param name="reader">The JSON stream reader.</param>
    /// <param name="results">The list to collect results into.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    private static async Task CollectArrayItemsAsync<T>(
        Utf8JsonAsyncStreamReader reader,
        List<T> results,
        JsonSerializerOptions options,
        CancellationToken cancellationToken)
    {
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                cancellationToken.ThrowIfCancellationRequested();

                T? item;
                try
                {
                    item = await reader.DeserializeAsync<T>(options, cancellationToken);
                    if (item is not null)
                    {
                        results.Add(item);
                    }
                }
                catch (JsonException)
                {
                    continue;
                }
            }
        }
    }

    /// <summary>
    /// Skips the current JSON token (object, array, or value).
    /// </summary>
    /// <param name="reader">The JSON stream reader.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    private static async Task SkipCurrentTokenAsync(Utf8JsonAsyncStreamReader reader, CancellationToken cancellationToken)
    {
        var tokenType = reader.TokenType;
        
        if (tokenType == JsonTokenType.StartObject)
        {
            int depth = 1;
            while (depth > 0 && await reader.ReadAsync(cancellationToken))
            {
                if (reader.TokenType == JsonTokenType.StartObject) 
                    depth++;
                else if (reader.TokenType == JsonTokenType.EndObject) 
                    depth--;
            }
        }
        else if (tokenType == JsonTokenType.StartArray)
        {
            int depth = 1;
            while (depth > 0 && await reader.ReadAsync(cancellationToken))
            {
                if (reader.TokenType == JsonTokenType.StartArray) 
                    depth++;
                else if (reader.TokenType == JsonTokenType.EndArray) 
                    depth--;
            }
        }
        // For primitive values, they're already consumed by the Read() call
    }

    #endregion
}

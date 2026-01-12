using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Blazing.Json.Queryable.Core;

namespace Blazing.Json.Queryable.Execution;

/// <summary>
/// Executes queries against JSON streams with TRUE element-by-element streaming using <see cref="PipeReader"/>.
/// Leverages .NET 10's <see cref="JsonSerializer.DeserializeAsyncEnumerable{TValue}(PipeReader, JsonSerializerOptions?, CancellationToken)"/> for optimal performance.
/// Supports early termination (Take/First) - only deserializes elements actually needed!
/// Extended to support all LINQ operations with automatic materialization when needed.
/// </summary>
public sealed class StreamQueryExecutor : IQueryExecutor
{
    // Cache default JsonSerializerOptions to avoid repeated allocation
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Stream _stream;
    private readonly JsonSerializerOptions _options;
    private readonly QueryOperationExecutor _operations = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamQueryExecutor"/> class.
    /// </summary>
    /// <param name="stream">The JSON stream to read from.</param>
    /// <param name="options">JSON serializer options. If null, uses cached default options.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="stream"/> is not readable.</exception>
    public StreamQueryExecutor(
        Stream stream,
        JsonSerializerOptions? options)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _options = options ?? DefaultOptions;

        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable.", nameof(stream));
        }
    }

    #region Synchronous Execution

    /// <summary>
    /// Executes a query synchronously against the stream.
    /// </summary>
    /// <typeparam name="T">The type of elements in the result.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of query results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="plan"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the plan is invalid or source type is null.</exception>
    public IEnumerable<T> Execute<T>(QueryExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.Validate();

        // Handle aggregation operations that return single values
        if (plan.AggregationType.HasValue)
        {
            return ExecuteAggregation<T>(plan);
        }

        // Handle quantifier operations that return bool
        if (plan.QuantifierType.HasValue)
        {
            return ExecuteQuantifier<T>(plan);
        }

        // Handle conversion operations (terminal)
        if (plan.ConversionType.HasValue)
        {
            return ExecuteConversion<T>(plan);
        }

        // Deserialize as SOURCE type (before projection)
        var sourceType = plan.SourceType!;
        var deserializeMethod = typeof(StreamQueryExecutor)
            .GetMethod(nameof(ExecuteTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(sourceType, typeof(T));

        return (IEnumerable<T>)deserializeMethod.Invoke(this, [plan])!;
    }

    /// <summary>
    /// Executes the query with the specified source and result types.
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>An <see cref="IEnumerable{TResult}"/> of query results.</returns>
    private IEnumerable<TResult> ExecuteTyped<TSource, TResult>(QueryExecutionPlan plan)
    {
        if (plan.RequiresMaterialization)
        {
            return ExecuteWithMaterialization<TSource, TResult>(plan);
        }

        return ExecuteSyncStreaming<TSource, TResult>(plan);
    }

    /// <summary>
    /// Executes the query in streaming mode (element-by-element) for the given types.
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>An <see cref="IEnumerable{TResult}"/> of query results.</returns>
    private IEnumerable<TResult> ExecuteSyncStreaming<TSource, TResult>(QueryExecutionPlan plan)
    {
        var items = MaterializeAll<TSource>();
        return _operations.ExecuteQuery<TSource, TResult>(items, plan);
    }

    /// <summary>
    /// Executes the query with materialization (loads all elements before processing).
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>An <see cref="IEnumerable{TResult}"/> of query results.</returns>
    private IEnumerable<TResult> ExecuteWithMaterialization<TSource, TResult>(QueryExecutionPlan plan)
    {
        var items = MaterializeAll<TSource>();
        return _operations.ExecuteQuery<TSource, TResult>(items, plan);
    }

    #endregion

    #region Terminal Operations

    /// <summary>
    /// Executes an aggregation operation and returns the result as a single-element enumerable.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> containing the aggregation result.</returns>
    private IEnumerable<T> ExecuteAggregation<T>(QueryExecutionPlan plan)
    {
        var items = MaterializeAll<object>();
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
        var items = MaterializeAll<object>();
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
        var items = MaterializeAll<object>();
        object result = QueryOperationExecutor.ExecuteConversion(items, plan);
        return [(T)result];
    }

    #endregion

    #region Asynchronous Execution

    /// <summary>
    /// TRUE ASYNC STREAMING using .NET 10's <see cref="JsonSerializer.DeserializeAsyncEnumerable{TValue}(PipeReader, JsonSerializerOptions?, CancellationToken)"/> with <see cref="PipeReader"/>.
    /// Deserializes elements one-at-a-time, supports early termination with Take.
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

        // Deserialize as SOURCE type (before projection)
        var sourceType = plan.SourceType!;
        var executeMethod = typeof(StreamQueryExecutor)
            .GetMethod(nameof(ExecuteAsyncTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(sourceType, typeof(T));

        var asyncEnumerable = (IAsyncEnumerable<T>)executeMethod.Invoke(this, [plan, cancellationToken])!;

        await foreach (var item in asyncEnumerable.WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Executes the query asynchronously with the specified source and result types.
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
        
        // For OrderBy, must load everything first
        if (plan.SortPropertyPaths is { Length: > 0 })
        {
            await foreach (var item in ExecuteWithSortingAsync<TSource, TResult>(plan, cancellationToken))
            {
                yield return item;
            }
            yield break;
        }

        // TRUE STREAMING PATH - element-by-element with early termination!
        var pipeReader = PipeReader.Create(_stream);
        
        try
        {
            int skip = plan.Skip ?? 0;
            int? take = plan.Take;

            // Use .NET 10's built-in streaming deserialization!
            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<TSource>(
                pipeReader,
                _options,
                cancellationToken).ConfigureAwait(false))
            {
                if (item is null)
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

                // EARLY TERMINATION - stop reading stream when Take limit reached!
                if (take.HasValue && --take <= 0)
                {
                    yield break; // STOPS READING THE STREAM!
                }
            }
        }
        finally
        {
            await pipeReader.CompleteAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes the query asynchronously with sorting (loads all elements before processing).
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An <see cref="IAsyncEnumerable{TResult}"/> of sorted query results.</returns>
    private async IAsyncEnumerable<TResult> ExecuteWithSortingAsync<TSource, TResult>(
        QueryExecutionPlan plan,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var items = new List<TSource>();
        var pipeReader = PipeReader.Create(_stream);
        
        try
        {
            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<TSource>(
                pipeReader,
                _options,
                cancellationToken).ConfigureAwait(false))
            {
                if (item is not null)
                {
                    items.Add(item);
                }
            }
        }
        finally
        {
            await pipeReader.CompleteAsync().ConfigureAwait(false);
        }

        var results = _operations.ExecuteQuery<TSource, TResult>(items, plan);
        
        foreach (var item in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Materializes all elements from the stream into a <see cref="List{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements to materialize.</typeparam>
    /// <returns>A <see cref="List{T}"/> containing all deserialized elements from the stream.</returns>
    private List<T> MaterializeAll<T>()
    {
        var items = new List<T>();
        var pipeReader = PipeReader.Create(_stream);
        
        try
        {
            var asyncEnumerable = JsonSerializer.DeserializeAsyncEnumerable<T>(pipeReader, _options);
            var enumerator = asyncEnumerable.GetAsyncEnumerator();
            try
            {
                while (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
                {
                    items.Add(enumerator.Current!);
                }
            }
            finally
            {
                enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
        finally
        {
            pipeReader.Complete();
        }

        return items;
    }

    #endregion
}

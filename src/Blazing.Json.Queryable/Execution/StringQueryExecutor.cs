using System.Runtime.CompilerServices;
using System.Text.Json;
using Blazing.Json.Queryable.Core;
using Blazing.Json.Queryable.Exceptions;

namespace Blazing.Json.Queryable.Execution;

/// <summary>
/// Executes queries against in-memory JSON data (UTF-8 bytes or string).
/// Optimized for scenarios where the entire JSON document fits in memory.
/// Supports both synchronous and asynchronous enumeration.
/// </summary>
public sealed class StringQueryExecutor : IQueryExecutor
{
    private readonly ReadOnlyMemory<byte> _utf8Json;
    private readonly JsonSerializerOptions _options;
    private readonly QueryOperationExecutor _operations = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StringQueryExecutor"/> class with UTF-8 bytes.
    /// </summary>
    /// <param name="utf8Json">UTF-8 encoded JSON data.</param>
    /// <param name="options">JSON serializer options. If null, uses case-insensitive property names.</param>
    public StringQueryExecutor(
        ReadOnlyMemory<byte> utf8Json,
        JsonSerializerOptions? options)
    {
        _utf8Json = utf8Json;
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Executes a query synchronously and returns results.
    /// For in-memory queries, deserializes the entire JSON then applies LINQ operations.
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

        // Use reflection to call the typed execution method
        if (plan.SourceType == null)
        {
            throw new InvalidOperationException("Plan.SourceType is null! Cannot execute query.");
        }
        
        var executeTypedMethod = typeof(StringQueryExecutor)
            .GetMethod(nameof(ExecuteTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(plan.SourceType, typeof(T));
        
        try
        {
            var result = executeTypedMethod.Invoke(this, [plan]);
            return (IEnumerable<T>)result!;
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>
    /// Typed execution method that handles source type and result type separately.
    /// Delegates all query operations to <see cref="QueryOperationExecutor"/>.
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>An <see cref="IEnumerable{TResult}"/> of query results.</returns>
    private IEnumerable<TResult> ExecuteTyped<TSource, TResult>(QueryExecutionPlan plan)
    {
        var items = DeserializeCollection<TSource>(_utf8Json.Span);
        return _operations.ExecuteQuery<TSource, TResult>(items, plan);
    }

    #region Aggregation Operations

    /// <summary>
    /// Executes an aggregation operation and returns the result as a single-element enumerable.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> containing the aggregation result.</returns>
    private IEnumerable<T> ExecuteAggregation<T>(QueryExecutionPlan plan)
    {
        var sourceType = plan.SourceType!;
        var method = typeof(StringQueryExecutor)
            .GetMethod(nameof(DeserializeAndAggregate), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(sourceType, typeof(T));
        
        try
        {
            var result = (T)method.Invoke(this, [plan])!;
            return [result];
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>
    /// Deserializes the JSON and performs the aggregation operation.
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>The aggregation result.</returns>
    private TResult DeserializeAndAggregate<TSource, TResult>(QueryExecutionPlan plan)
    {
        var items = DeserializeCollection<TSource>(_utf8Json.Span);
        IEnumerable<object> query = items.Cast<object>();

        object result = QueryOperationExecutor.ExecuteAggregation(query, plan);
        return (TResult)result;
    }

    #endregion

    #region Quantifier Operations

    /// <summary>
    /// Executes a quantifier operation and returns the result as a single-element enumerable.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> containing the quantifier result.</returns>
    private IEnumerable<T> ExecuteQuantifier<T>(QueryExecutionPlan plan)
    {
        var result = ExecuteQuantifierTyped<T>(plan);
        return [result];
    }

    /// <summary>
    /// Executes a quantifier operation and returns the result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>The quantifier result.</returns>
    private T ExecuteQuantifierTyped<T>(QueryExecutionPlan plan)
    {
        var sourceType = plan.SourceType!;
        var method = typeof(StringQueryExecutor)
            .GetMethod(nameof(DeserializeAndQuantify), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(sourceType);
        
        try
        {
            return (T)method.Invoke(this, [plan])!;
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>
    /// Deserializes the JSON and performs the quantifier operation.
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>The quantifier result as a boolean.</returns>
    private bool DeserializeAndQuantify<TSource>(QueryExecutionPlan plan)
    {
        var items = DeserializeCollection<TSource>(_utf8Json.Span);
        IEnumerable<object> query = items.Cast<object>();

        return QueryOperationExecutor.ExecuteQuantifier(query, plan);
    }

    #endregion

    #region Conversion Operations

    /// <summary>
    /// Executes a conversion operation and returns the result as a single-element enumerable.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> containing the conversion result.</returns>
    private IEnumerable<T> ExecuteConversion<T>(QueryExecutionPlan plan)
    {
        var sourceType = plan.SourceType!;
        var method = typeof(StringQueryExecutor)
            .GetMethod(nameof(DeserializeAndConvert), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(sourceType, typeof(T));
        
        try
        {
            var result = (T)method.Invoke(this, [plan])!;
            return [result];
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>
    /// Deserializes the JSON and performs the conversion operation.
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <returns>The conversion result.</returns>
    private TResult DeserializeAndConvert<TSource, TResult>(QueryExecutionPlan plan)
    {
        var items = DeserializeCollection<TSource>(_utf8Json.Span);
        IEnumerable<object> query = items.Cast<object>();

        object result = QueryOperationExecutor.ExecuteConversion(query, plan);
        return (TResult)result;
    }

    #endregion

    /// <summary>
    /// Executes a query asynchronously and returns results.
    /// For in-memory data, this converts synchronous enumeration to async.
    /// Note: Not truly async I/O, but compatible with async LINQ patterns.
    /// </summary>
    /// <typeparam name="T">The type of elements in the result.</typeparam>
    /// <param name="plan">The query execution plan.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of query results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="plan"/> is null.</exception>
    public async IAsyncEnumerable<T> ExecuteAsync<T>(
        QueryExecutionPlan plan,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.Validate();

        var results = Execute<T>(plan);

        foreach (var item in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }

        await Task.CompletedTask;
    }

    #region Helper Methods

    /// <summary>
    /// Deserializes UTF-8 JSON to a collection of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize each element to.</typeparam>
    /// <param name="utf8Json">The UTF-8 encoded JSON data.</param>
    /// <returns>A <see cref="List{T}"/> containing the deserialized elements.</returns>
    /// <exception cref="JsonDeserializationException">Thrown if deserialization fails or the JSON is not an array.</exception>
    private List<T> DeserializeCollection<T>(ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            var reader = new Utf8JsonReader(utf8Json);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("JSON must be an array.");
            }

            var result = JsonSerializer.Deserialize<List<T>>(utf8Json, _options);
            return result ?? [];
        }
        catch (JsonException ex)
        {
            string jsonPreview = System.Text.Encoding.UTF8.GetString(utf8Json.Length > 200 ? utf8Json[..200] : utf8Json);
            throw new JsonDeserializationException("Failed to deserialize JSON.", ex, jsonPreview);
        }
    }

    #endregion
}

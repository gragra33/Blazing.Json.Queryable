namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Executes queries against JSON data sources with support for both
/// synchronous and asynchronous enumeration.
/// Implementations must be memory-efficient, especially for streaming scenarios.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Dual Execution Modes:</strong>
/// <list type="bullet">
/// <item><strong>Synchronous:</strong> Uses stackalloc for buffers (&lt;4KB), constant stack memory</item>
/// <item><strong>Asynchronous:</strong> Uses ArrayPool for buffers, constant heap memory (~50KB)</item>
/// </list>
/// </para>
/// </remarks>
public interface IQueryExecutor
{
    /// <summary>
    /// Executes a query synchronously and returns results.
    /// For streaming queries, should use constant memory through stackalloc buffers.
    /// </summary>
    /// <typeparam name="T">The type of elements to return</typeparam>
    /// <param name="plan">The query execution plan containing filters, projections, and operations</param>
    /// <returns>An enumerable sequence of results</returns>
    /// <example>
    /// <code>
    /// // String-based execution
    /// var plan = new QueryExecutionPlan
    /// {
    ///     Predicates = new[] { p => p.Age > 18 },
    ///     Skip = 0,
    ///     Take = 10
    /// };
    /// 
    /// var results = executor.Execute&lt;Person&gt;(plan);
    /// foreach (var person in results)
    /// {
    ///     Console.WriteLine(person.Name);
    /// }
    /// </code>
    /// </example>
    IEnumerable<T> Execute<T>(QueryExecutionPlan plan);

    /// <summary>
    /// Executes a query asynchronously and returns results.
    /// For stream-based queries with true async I/O and proper cancellation support.
    /// </summary>
    /// <typeparam name="T">The type of elements to return</typeparam>
    /// <param name="plan">The query execution plan containing filters, projections, and operations</param>
    /// <param name="cancellationToken">Cancellation token to stop enumeration</param>
    /// <returns>An async enumerable sequence of results</returns>
    /// <remarks>
    /// <para>
    /// <strong>Memory Strategy (Async):</strong>
    /// <code>
    /// public async IAsyncEnumerable&lt;T&gt; ExecuteAsync&lt;T&gt;(
    ///     QueryExecutionPlan plan,
    ///     [EnumeratorCancellation] CancellationToken cancellationToken = default)
    /// {
    ///     // ⚠️ CANNOT use stackalloc across await
    ///     // ✅ Use ArrayPool instead
    ///     byte[] buffer = ArrayPool&lt;byte&gt;.Shared.Rent(4096);
    ///     try
    ///     {
    ///         int bytesRead;
    ///         while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
    ///         {
    ///             cancellationToken.ThrowIfCancellationRequested();
    ///             
    ///             // ✅ Create reader locally (ref struct on stack)
    ///             var reader = new Utf8JsonReader(buffer.AsSpan(0, bytesRead));
    ///             
    ///             // ✅ Process synchronously (Utf8JsonReader limitation)
    ///             foreach (var item in ProcessChunk(ref reader, plan))
    ///             {
    ///                 yield return item;
    ///             }
    ///         }
    ///     }
    ///     finally
    ///     {
    ///         // ✅ CRITICAL: Return buffer to pool
    ///         ArrayPool&lt;byte&gt;.Shared.Return(buffer);
    ///     }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Cancellation Support:</strong>
    /// Use [EnumeratorCancellation] attribute to properly flow cancellation tokens:
    /// <list type="bullet">
    /// <item>Check cancellationToken.ThrowIfCancellationRequested() before expensive operations</item>
    /// <item>Pass token to all async I/O operations</item>
    /// <item>Ensure buffer cleanup happens even on cancellation (use try/finally)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Performance Characteristics:</strong>
    /// <list type="bullet">
    /// <item>Async I/O: Non-blocking, better throughput for multiple concurrent operations</item>
    /// <item>Memory: Constant ~50KB from ArrayPool (buffer size + overhead)</item>
    /// <item>CPU: Same processing cost as sync (JSON parsing is CPU-bound)</item>
    /// <item>Best for: I/O-bound scenarios, high concurrency, large files</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Async execution with cancellation
    /// using var cts = new CancellationTokenSource();
    /// cts.CancelAfter(TimeSpan.FromSeconds(30));
    /// 
    /// var plan = new QueryExecutionPlan
    /// {
    ///     Predicates = new[] { p => p.Age > 18 },
    ///     Take = 100
    /// };
    /// 
    /// await foreach (var person in executor.ExecuteAsync&lt;Person&gt;(plan, cts.Token))
    /// {
    ///     await ProcessPersonAsync(person);
    ///     
    ///     // Cancellation is automatically checked during enumeration
    /// }
    /// 
    /// // ArrayPool buffers are automatically returned even if cancelled
    /// </code>
    /// </example>
    IAsyncEnumerable<T> ExecuteAsync<T>(
        QueryExecutionPlan plan,
        CancellationToken cancellationToken = default);
}

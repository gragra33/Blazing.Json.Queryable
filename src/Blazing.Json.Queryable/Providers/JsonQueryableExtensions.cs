using System.Runtime.CompilerServices;

namespace Blazing.Json.Queryable.Providers;

/// <summary>
/// Extension methods for <see cref="JsonQueryable{T}"/> to enable async enumeration and .NET 10 async LINQ support.
/// </summary>
/// <remarks>
/// <para>
/// <strong>.NET 10 Async LINQ Support:</strong>
/// .NET 10 includes System.Linq.AsyncEnumerable built-in, providing full LINQ support for
/// <see cref="IAsyncEnumerable{T}"/> without requiring external packages.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item>Standard LINQ methods work with <see cref="IAsyncEnumerable{T}"/></item>
/// <item>Synchronous predicates: <c>Where(x =&gt; x.Age &gt; 18)</c></item>
/// <item>Asynchronous predicates: <c>Where(async (x, ct) =&gt; await CheckAsync(x, ct))</c></item>
/// <item>All standard operations: Where, Select, OrderBy, Take, Skip, etc.</item>
/// <item>Proper cancellation token support via [EnumeratorCancellation]</item>
/// </list>
/// </para>
/// </remarks>
public static class JsonQueryableExtensions
{
    /// <summary>
    /// Converts an <see cref="IQueryable{T}"/> to <see cref="IAsyncEnumerable{T}"/> for async enumeration.
    /// If the source already implements <see cref="IAsyncEnumerable{T}"/>, returns it directly.
    /// Otherwise, wraps synchronous enumeration in an async iterator.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <returns>An async enumerable sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// <strong>Use Cases:</strong>
    /// <list type="bullet">
    /// <item><strong>Stream Processing:</strong> Process large files asynchronously with constant memory</item>
    /// <item><strong>Non-Blocking I/O:</strong> Free up threads during async operations</item>
    /// <item><strong>Cancellation:</strong> Respond to cancellation tokens during enumeration</item>
    /// <item><strong>.NET 10 Async LINQ:</strong> Use async predicates and transformations</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Optimization:</strong> If the source is already <see cref="IAsyncEnumerable{T}"/> (e.g., <see cref="JsonQueryable{T}"/>
    /// from stream), this method returns it directly without wrapping, ensuring optimal performance.
    /// </para>
    /// <para>
    /// <strong>Cancellation Support:</strong>
    /// <code>
    /// using var cts = new CancellationTokenSource();
    /// cts.CancelAfter(TimeSpan.FromSeconds(30));
    /// 
    /// await foreach (var item in query.AsAsyncEnumerable().WithCancellation(cts.Token))
    /// {
    ///     // Enumeration can be cancelled
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic async enumeration
    /// await using var stream = File.OpenRead("data.json");
    /// var query = JsonQueryable.FromStream&lt;Person&gt;(stream)
    ///     .Where(p =&gt; p.Age &gt; 18);
    /// 
    /// await foreach (var person in query.AsAsyncEnumerable())
    /// {
    ///     await ProcessPersonAsync(person);
    /// }
    /// 
    /// // .NET 10 async LINQ with async predicates
    /// await using var stream = File.OpenRead("data.json");
    /// 
    /// await foreach (var person in JsonQueryable.FromStream&lt;Person&gt;(stream)
    ///     .AsAsyncEnumerable()
    ///     .Where(async (p, ct) =&gt; await IsActiveAsync(p, ct))  // Async predicate
    ///     .OrderBy(p =&gt; p.Name)
    ///     .Take(100))
    /// {
    ///     Console.WriteLine(person.Name);
    /// }
    /// 
    /// // With cancellation
    /// using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
    /// 
    /// await foreach (var item in query.AsAsyncEnumerable().WithCancellation(cts.Token))
    /// {
    ///     // Process with automatic cancellation support
    /// }
    /// </code>
    /// </example>
    public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IQueryable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        // If source is JsonQueryable<T>, use its optimized async implementation
        if (source is JsonQueryable<T> jsonQueryable)
        {
            return ToAsyncEnumerableFromJsonQueryable(jsonQueryable);
        }

        // Otherwise, wrap synchronous enumeration in async iterator
        return ToAsyncEnumerableCore(source);
    }

    /// <summary>
    /// Optimized async enumeration for <see cref="JsonQueryable{T}"/> using internal async enumerator.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="jsonQueryable">The <see cref="JsonQueryable{T}"/> instance.</param>
    /// <param name="cancellationToken">A cancellation token to observe while iterating.</param>
    /// <returns>An async enumerable sequence.</returns>
    private static async IAsyncEnumerable<T> ToAsyncEnumerableFromJsonQueryable<T>(
        JsonQueryable<T> jsonQueryable,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var enumerator = jsonQueryable.GetAsyncEnumeratorInternal(cancellationToken);
        
        while (await enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            yield return enumerator.Current;
        }
    }

    /// <summary>
    /// Core implementation that wraps synchronous enumeration in an async iterator.
    /// Supports cancellation via [EnumeratorCancellation] attribute.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="cancellationToken">A cancellation token to observe while iterating.</param>
    /// <returns>An async enumerable sequence.</returns>
    private static async IAsyncEnumerable<T> ToAsyncEnumerableCore<T>(
        IQueryable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in source)
        {
            // Check for cancellation before yielding each item
            cancellationToken.ThrowIfCancellationRequested();

            yield return item;
        }
    }
}

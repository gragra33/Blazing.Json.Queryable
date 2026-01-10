using System.Collections;
using System.Linq.Expressions;
using SysQueryable = System.Linq.Queryable;

namespace Blazing.Json.Queryable.Providers;

/// <summary>
/// Provides <see cref="IQueryable{T}"/> support for querying JSON data.
/// Implements <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/> for proper resource cleanup, especially for file-based queries.
/// </summary>
/// <typeparam name="T">The type of elements in the query.</typeparam>
/// <remarks>
/// <para>
/// <strong>Resource Management:</strong>
/// <list type="bullet">
/// <item>File-based queries (FromFile) open file streams that must be disposed.</item>
/// <item>Always use 'using' statements or dispose explicitly for file-based queries.</item>
/// <item>String and byte array sources don't require disposal but it's harmless.</item>
/// </list>
/// </para>
/// </remarks>
public partial class JsonQueryable<T> : IOrderedQueryable<T>, IDisposable, IAsyncDisposable
{
    private readonly JsonQueryProvider _provider;
    private readonly Expression _expression;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="JsonQueryable{T}"/> with a provider and expression.
    /// Used for query chaining.
    /// </summary>
    /// <param name="provider">The query provider.</param>
    /// <param name="expression">The expression tree representing the query.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider"/> or <paramref name="expression"/> is null.</exception>
    internal JsonQueryable(JsonQueryProvider provider, Expression expression)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="JsonQueryable{T}"/> with a provider.
    /// Used as the root query.
    /// </summary>
    /// <param name="provider">The query provider.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider"/> is null.</exception>
    internal JsonQueryable(JsonQueryProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _expression = Expression.Constant(this);
    }

    #region IQueryable<T> Implementation

    /// <summary>
    /// Gets the type of the element(s) that are returned when the expression tree is executed.
    /// </summary>
    public Type ElementType => typeof(T);

    /// <summary>
    /// Gets the expression tree associated with this queryable.
    /// </summary>
    public Expression Expression => _expression;

    /// <summary>
    /// Gets the query provider associated with this queryable.
    /// </summary>
    public IQueryProvider Provider => _provider;

    /// <summary>
    /// Returns an enumerator that iterates through the query results (synchronous).
    /// </summary>
    /// <returns>An enumerator for the query results.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the queryable has been disposed.</exception>
    public IEnumerator<T> GetEnumerator()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _provider.Execute<IEnumerable<T>>(_expression).GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the query results (synchronous).
    /// </summary>
    /// <returns>An enumerator for the query results.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion

    #region Async Enumeration Support

    /// <summary>
    /// Gets an async enumerator for this queryable.
    /// Used internally by AsAsyncEnumerable() extension method.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while iterating.</param>
    /// <returns>An async enumerator for the query results.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the queryable has been disposed.</exception>
    internal async IAsyncEnumerator<T> GetAsyncEnumeratorInternal(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        var executor = _provider.GetAsyncExecutor();
        var plan = _provider.BuildExecutionPlan(_expression);

        await foreach (var item in executor.ExecuteAsync<T>(plan, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return item;
        }
    }

    #endregion

    #region Core LINQ Operations

    #region Filtering Operations

    /// <summary>
    /// Filters elements based on a specified type.
    /// Useful when JSON contains heterogeneous arrays.
    /// </summary>
    /// <typeparam name="TResult">The type to filter elements on.</typeparam>
    /// <returns>A queryable containing only elements of type <typeparamref name="TResult"/>.</returns>
    public IQueryable<TResult> OfType<TResult>()
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.OfType),
            [typeof(TResult)],
            Expression);

        return _provider.CreateQuery<TResult>(expression);
    }

    #endregion

    #region Projection Operations

    /// <summary>
    /// Casts elements to the specified type.
    /// Throws <see cref="InvalidCastException"/> if an element cannot be cast.
    /// </summary>
    /// <typeparam name="TResult">The type to cast elements to.</typeparam>
    /// <returns>A queryable with elements cast to <typeparamref name="TResult"/>.</returns>
    public IQueryable<TResult> Cast<TResult>()
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Cast),
            [typeof(TResult)],
            Expression);

        return _provider.CreateQuery<TResult>(expression);
    }

    /// <summary>
    /// Projects each element to an <see cref="IEnumerable{TResult}"/> and flattens the results.
    /// Essential for nested collections.
    /// </summary>
    /// <typeparam name="TResult">The type of the elements in the resulting sequence.</typeparam>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <returns>A queryable whose elements are the result of invoking the one-to-many projection function on each element of the input sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public IQueryable<TResult> SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.SelectMany),
            [typeof(T), typeof(TResult)],
            Expression,
            Expression.Quote(selector));

        return _provider.CreateQuery<TResult>(expression);
    }

    /// <summary>
    /// Projects each element to an <see cref="IEnumerable{TCollection}"/>, flattens the results, and applies a result selector.
    /// </summary>
    /// <typeparam name="TCollection">The type of the intermediate elements collected.</typeparam>
    /// <typeparam name="TResult">The type of the elements in the resulting sequence.</typeparam>
    /// <param name="collectionSelector">A projection function to apply to each element.</param>
    /// <param name="resultSelector">A projection function to apply to each element of the intermediate sequence.</param>
    /// <returns>A queryable whose elements are the result of invoking the projection functions on each element of the input sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
    public IQueryable<TResult> SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(collectionSelector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.SelectMany),
            [typeof(T), typeof(TCollection), typeof(TResult)],
            Expression,
            Expression.Quote(collectionSelector),
            Expression.Quote(resultSelector));

        return _provider.CreateQuery<TResult>(expression);
    }

    #endregion

    #region Sorting Operations

    /// <summary>
    /// Sorts elements in ascending order using the default comparer.
    /// Requires <see cref="IComparable{T}"/> implementation.
    /// </summary>
    /// <returns>An <see cref="IOrderedQueryable{T}"/> whose elements are sorted in ascending order.</returns>
    public IOrderedQueryable<T> Order()
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Order),
            [typeof(T)],
            Expression);

        return (IOrderedQueryable<T>)_provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Sorts elements in descending order using the default comparer.
    /// </summary>
    /// <returns>An <see cref="IOrderedQueryable{T}"/> whose elements are sorted in descending order.</returns>
    public IOrderedQueryable<T> OrderDescending()
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.OrderDescending),
            [typeof(T)],
            Expression);

        return (IOrderedQueryable<T>)_provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Reverses the order of elements.
    /// <b>Warning:</b> Materializes the entire sequence into memory.
    /// </summary>
    /// <returns>A queryable whose elements are in the reverse order.</returns>
    public IQueryable<T> Reverse()
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Reverse),
            [typeof(T)],
            Expression);

        return _provider.CreateQuery<T>(expression);
    }

    #endregion

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes the queryable and releases any underlying resources (e.g., file streams).
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _provider.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the queryable and releases any underlying resources.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await _provider.DisposeAsync().ConfigureAwait(false);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}

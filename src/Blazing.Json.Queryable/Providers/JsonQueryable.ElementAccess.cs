using System.Linq.Expressions;
using SysQueryable = System.Linq.Queryable;

namespace Blazing.Json.Queryable.Providers;

/// <summary>
/// Element access and quantifier operations for <see cref="JsonQueryable{T}"/>.
/// </summary>
public partial class JsonQueryable<T>
{
    #region Element Operations

    /// <summary>
    /// Returns the element at a specified index.
    /// Throws if index is out of range.
    /// <b>Warning:</b> For streams, enumerates up to index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to retrieve.</param>
    /// <returns>The element at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is out of range.</exception>
    public T ElementAt(int index)
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.ElementAt),
            [typeof(T)],
            Expression,
            Expression.Constant(index));

        return _provider.Execute<T>(expression);
    }

    /// <summary>
    /// Returns the element at a specified index using <see cref="Index"/> syntax.
    /// Supports from-end indexing (e.g., ^1 for last element).
    /// </summary>
    /// <param name="index">The index of the element to retrieve (supports from-end).</param>
    /// <returns>The element at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is out of range.</exception>
    public T ElementAt(Index index)
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.ElementAt),
            [typeof(T)],
            Expression,
            Expression.Constant(index));

        return _provider.Execute<T>(expression);
    }

    /// <summary>
    /// Returns the element at a specified index or default if out of range.
    /// </summary>
    /// <param name="index">The zero-based index of the element to retrieve.</param>
    /// <returns>The element at the specified index, or default if out of range.</returns>
    public T? ElementAtOrDefault(int index)
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.ElementAtOrDefault),
            [typeof(T)],
            Expression,
            Expression.Constant(index));

        return _provider.Execute<T>(expression);
    }

    /// <summary>
    /// Returns the element at a specified index or default if out of range using <see cref="Index"/> syntax.
    /// </summary>
    /// <param name="index">The index of the element to retrieve (supports from-end).</param>
    /// <returns>The element at the specified index, or default if out of range.</returns>
    public T? ElementAtOrDefault(Index index)
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.ElementAtOrDefault),
            [typeof(T)],
            Expression,
            Expression.Constant(index));

        return _provider.Execute<T>(expression);
    }

    /// <summary>
    /// Returns the last element in the sequence.
    /// <b>Warning:</b> Enumerates entire sequence.
    /// </summary>
    /// <returns>The last element in the sequence.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the sequence is empty.</exception>
    public T Last()
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Last),
            [typeof(T)],
            Expression);

        return _provider.Execute<T>(expression);
    }

    /// <summary>
    /// Returns the last element matching a predicate.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>The last element that matches the predicate.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="predicate"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if no element matches the predicate.</exception>
    public T Last(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Last),
            [typeof(T)],
            Expression,
            Expression.Quote(predicate));

        return _provider.Execute<T>(expression);
    }

    /// <summary>
    /// Returns the last element or default if the sequence is empty.
    /// </summary>
    /// <returns>The last element, or default if the sequence is empty.</returns>
    public T? LastOrDefault()
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.LastOrDefault),
            [typeof(T)],
            Expression);

        return _provider.Execute<T>(expression);
    }

    /// <summary>
    /// Returns the last element matching a predicate or default if not found.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>The last element that matches the predicate, or default if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="predicate"/> is null.</exception>
    public T? LastOrDefault(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.LastOrDefault),
            [typeof(T)],
            Expression,
            Expression.Quote(predicate));

        return _provider.Execute<T>(expression);
    }

    #endregion

    #region Quantifier Operations

    /// <summary>
    /// Determines whether all elements satisfy a condition.
    /// Short-circuits on first false.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns><c>true</c> if every element passes the test; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="predicate"/> is null.</exception>
    public bool All(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.All),
            [typeof(T)],
            Expression,
            Expression.Quote(predicate));

        return _provider.Execute<bool>(expression);
    }

    /// <summary>
    /// Determines whether the sequence contains a specific element.
    /// </summary>
    /// <param name="item">The element to locate in the sequence.</param>
    /// <returns><c>true</c> if the element is found; otherwise, <c>false</c>.</returns>
    public bool Contains(T item)
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Contains),
            [typeof(T)],
            Expression,
            Expression.Constant(item, typeof(T)));

        return _provider.Execute<bool>(expression);
    }

    /// <summary>
    /// Determines whether the sequence contains a specific element using a specified comparer.
    /// </summary>
    /// <param name="item">The element to locate in the sequence.</param>
    /// <param name="comparer">The equality comparer to use for the search.</param>
    /// <returns><c>true</c> if the element is found; otherwise, <c>false</c>.</returns>
    public bool Contains(T item, IEqualityComparer<T>? comparer)
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Contains),
            [typeof(T)],
            Expression,
            Expression.Constant(item, typeof(T)),
            Expression.Constant(comparer, typeof(IEqualityComparer<T>)));

        return _provider.Execute<bool>(expression);
    }

    /// <summary>
    /// Determines whether two sequences are equal.
    /// </summary>
    /// <param name="second">The sequence to compare to the current sequence.</param>
    /// <returns><c>true</c> if the sequences are equal; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> is null.</exception>
    public bool SequenceEqual(IEnumerable<T> second)
    {
        ArgumentNullException.ThrowIfNull(second);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.SequenceEqual),
            [typeof(T)],
            Expression,
            Expression.Constant(second));

        return _provider.Execute<bool>(expression);
    }

    /// <summary>
    /// Determines whether two sequences are equal using a specified comparer.
    /// </summary>
    /// <param name="second">The sequence to compare to the current sequence.</param>
    /// <param name="comparer">The equality comparer to use for the comparison.</param>
    /// <returns><c>true</c> if the sequences are equal; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> is null.</exception>
    public bool SequenceEqual(IEnumerable<T> second, IEqualityComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(second);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.SequenceEqual),
            [typeof(T)],
            Expression,
            Expression.Constant(second),
            Expression.Constant(comparer, typeof(IEqualityComparer<T>)));

        return _provider.Execute<bool>(expression);
    }

    #endregion
}

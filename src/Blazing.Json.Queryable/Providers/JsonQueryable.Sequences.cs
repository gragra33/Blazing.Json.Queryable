using System.Linq.Expressions;
using SysQueryable = System.Linq.Queryable;

namespace Blazing.Json.Queryable.Providers;

/// <summary>
/// Sequence and partitioning operations for <see cref="JsonQueryable{T}"/>.
/// </summary>
public partial class JsonQueryable<T>
{
    #region Sequence Operations

    /// <summary>
    /// Appends an element to the end of the sequence.
    /// </summary>
    /// <param name="element">The element to append.</param>
    /// <returns>A queryable with the element appended.</returns>
    public IQueryable<T> Append(T element)
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Append),
            [typeof(T)],
            Expression,
            Expression.Constant(element, typeof(T)));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Prepends an element to the beginning of the sequence.
    /// </summary>
    /// <param name="element">The element to prepend.</param>
    /// <returns>A queryable with the element prepended.</returns>
    public IQueryable<T> Prepend(T element)
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Prepend),
            [typeof(T)],
            Expression,
            Expression.Constant(element, typeof(T)));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Concatenates two sequences.
    /// </summary>
    /// <param name="second">The sequence to concatenate to the current sequence.</param>
    /// <returns>A queryable representing the concatenation of the two sequences.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> is null.</exception>
    public IQueryable<T> Concat(IEnumerable<T> second)
    {
        ArgumentNullException.ThrowIfNull(second);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Concat),
            [typeof(T)],
            Expression,
            Expression.Constant(second));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Applies a function to corresponding elements of two sequences.
    /// </summary>
    /// <typeparam name="TSecond">The type of the elements of the second sequence.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="second">The sequence to merge with the current sequence.</param>
    /// <param name="resultSelector">A function that specifies how to merge the elements from the two sequences.</param>
    /// <returns>A queryable of merged results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> or <paramref name="resultSelector"/> is null.</exception>
    public IQueryable<TResult> Zip<TSecond, TResult>(
        IEnumerable<TSecond> second,
        Expression<Func<T, TSecond, TResult>> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(resultSelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Zip),
            [typeof(T), typeof(TSecond), typeof(TResult)],
            Expression,
            Expression.Constant(second),
            Expression.Quote(resultSelector));

        return _provider.CreateQuery<TResult>(expression);
    }

    /// <summary>
    /// Produces tuples of corresponding elements from two sequences.
    /// </summary>
    /// <typeparam name="TSecond">The type of the elements of the second sequence.</typeparam>
    /// <param name="second">The sequence to merge with the current sequence.</param>
    /// <returns>A queryable of tuples containing elements from both sequences.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> is null.</exception>
    public IQueryable<(T First, TSecond Second)> Zip<TSecond>(IEnumerable<TSecond> second)
    {
        ArgumentNullException.ThrowIfNull(second);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Zip),
            [typeof(T), typeof(TSecond)],
            Expression,
            Expression.Constant(second));

        return _provider.CreateQuery<(T First, TSecond Second)>(expression);
    }

    #endregion

    #region Partitioning Operations

    /// <summary>
    /// Returns the last N elements.
    /// <b>Warning:</b> Requires buffering last N elements.
    /// </summary>
    /// <param name="count">The number of elements to return from the end of the sequence.</param>
    /// <returns>A queryable containing the last <paramref name="count"/> elements.</returns>
    public IQueryable<T> TakeLast(int count)
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.TakeLast),
            [typeof(T)],
            Expression,
            Expression.Constant(count));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Skips the last N elements.
    /// <b>Warning:</b> Must enumerate entire sequence to determine count.
    /// </summary>
    /// <param name="count">The number of elements to skip from the end of the sequence.</param>
    /// <returns>A queryable that contains the elements after skipping the last <paramref name="count"/> elements.</returns>
    public IQueryable<T> SkipLast(int count)
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.SkipLast),
            [typeof(T)],
            Expression,
            Expression.Constant(count));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Returns elements while a condition is true.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>A queryable containing the elements taken while the predicate is true.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="predicate"/> is null.</exception>
    public IQueryable<T> TakeWhile(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.TakeWhile),
            [typeof(T)],
            Expression,
            Expression.Quote(predicate));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Returns elements while a condition with index is true.
    /// </summary>
    /// <param name="predicate">A function to test each element and its index for a condition.</param>
    /// <returns>A queryable containing the elements taken while the predicate is true.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="predicate"/> is null.</exception>
    public IQueryable<T> TakeWhile(Expression<Func<T, int, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.TakeWhile),
            [typeof(T)],
            Expression,
            Expression.Quote(predicate));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Skips elements while a condition is true.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>A queryable containing the elements after the predicate returns false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="predicate"/> is null.</exception>
    public IQueryable<T> SkipWhile(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.SkipWhile),
            [typeof(T)],
            Expression,
            Expression.Quote(predicate));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Skips elements while a condition with index is true.
    /// </summary>
    /// <param name="predicate">A function to test each element and its index for a condition.</param>
    /// <returns>A queryable containing the elements after the predicate returns false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="predicate"/> is null.</exception>
    public IQueryable<T> SkipWhile(Expression<Func<T, int, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.SkipWhile),
            [typeof(T)],
            Expression,
            Expression.Quote(predicate));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Splits elements into chunks of specified size.
    /// </summary>
    /// <param name="size">The maximum size of each chunk.</param>
    /// <returns>A queryable of arrays, each containing up to <paramref name="size"/> elements.</returns>
    public IQueryable<T[]> Chunk(int size)
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Chunk),
            [typeof(T)],
            Expression,
            Expression.Constant(size));

        return _provider.CreateQuery<T[]>(expression);
    }

    #endregion
}

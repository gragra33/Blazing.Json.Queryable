using System.Linq.Expressions;
using SysQueryable = System.Linq.Queryable;

namespace Blazing.Json.Queryable.Providers;

/// <summary>
/// Set operations for <see cref="JsonQueryable{T}"/> (Distinct, Union, Intersect, Except).
/// </summary>
public partial class JsonQueryable<T>
{
    #region Set Operations

    /// <summary>
    /// Returns distinct elements using the default equality comparer.
    /// </summary>
    /// <returns>A queryable containing distinct elements.</returns>
    public IQueryable<T> Distinct()
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Distinct),
            [typeof(T)],
            Expression);

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Returns distinct elements using a specified comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer to use for comparing elements.</param>
    /// <returns>A queryable containing distinct elements.</returns>
    public IQueryable<T> Distinct(IEqualityComparer<T>? comparer)
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Distinct),
            [typeof(T)],
            Expression,
            Expression.Constant(comparer, typeof(IEqualityComparer<T>)));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Returns distinct elements based on a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>A queryable containing distinct elements by key.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keySelector"/> is null.</exception>
    public IQueryable<T> DistinctBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.DistinctBy),
            [typeof(T), typeof(TKey)],
            Expression,
            Expression.Quote(keySelector));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Returns distinct elements based on a key selector with a specified comparer.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="comparer">The equality comparer to use for the keys.</param>
    /// <returns>A queryable containing distinct elements by key.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keySelector"/> is null.</exception>
    public IQueryable<T> DistinctBy<TKey>(
        Expression<Func<T, TKey>> keySelector,
        IEqualityComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.DistinctBy),
            [typeof(T), typeof(TKey)],
            Expression,
            Expression.Quote(keySelector),
            Expression.Constant(comparer, typeof(IEqualityComparer<TKey>)));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Produces the set union of two sequences.
    /// <b>Warning:</b> Requires second sequence - must materialize first sequence.
    /// </summary>
    /// <param name="second">The sequence to union with the current sequence.</param>
    /// <returns>A queryable containing the set union of the two sequences.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> is null.</exception>
    public IQueryable<T> Union(IEnumerable<T> second)
    {
        ArgumentNullException.ThrowIfNull(second);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Union),
            [typeof(T)],
            Expression,
            Expression.Constant(second));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Produces the set union of two sequences with a specified comparer.
    /// </summary>
    /// <param name="second">The sequence to union with the current sequence.</param>
    /// <param name="comparer">The equality comparer to use for the elements.</param>
    /// <returns>A queryable containing the set union of the two sequences.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> is null.</exception>
    public IQueryable<T> Union(IEnumerable<T> second, IEqualityComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(second);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Union),
            [typeof(T)],
            Expression,
            Expression.Constant(second),
            Expression.Constant(comparer, typeof(IEqualityComparer<T>)));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Produces the set union of two sequences by using a specified key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="second">The sequence to union with the current sequence.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>A queryable containing the set union of the two sequences by key.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> or <paramref name="keySelector"/> is null.</exception>
    public IQueryable<T> UnionBy<TKey>(
        IEnumerable<T> second,
        Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(keySelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.UnionBy),
            [typeof(T), typeof(TKey)],
            Expression,
            Expression.Constant(second),
            Expression.Quote(keySelector));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Produces the set union of two sequences by using a specified key selector and comparer.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="second">The sequence to union with the current sequence.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="comparer">The equality comparer to use for the keys.</param>
    /// <returns>A queryable containing the set union of the two sequences by key.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> or <paramref name="keySelector"/> is null.</exception>
    public IQueryable<T> UnionBy<TKey>(
        IEnumerable<T> second,
        Expression<Func<T, TKey>> keySelector,
        IEqualityComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(keySelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.UnionBy),
            [typeof(T), typeof(TKey)],
            Expression,
            Expression.Constant(second),
            Expression.Quote(keySelector),
            Expression.Constant(comparer, typeof(IEqualityComparer<TKey>)));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Produces the set intersection of two sequences.
    /// </summary>
    /// <param name="second">The sequence to intersect with the current sequence.</param>
    /// <returns>A queryable containing the set intersection of the two sequences.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> is null.</exception>
    public IQueryable<T> Intersect(IEnumerable<T> second)
    {
        ArgumentNullException.ThrowIfNull(second);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Intersect),
            [typeof(T)],
            Expression,
            Expression.Constant(second));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Produces the set intersection of two sequences with a specified comparer.
    /// </summary>
    /// <param name="second">The sequence to intersect with the current sequence.</param>
    /// <param name="comparer">The equality comparer to use for the elements.</param>
    /// <returns>A queryable containing the set intersection of the two sequences.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> is null.</exception>
    public IQueryable<T> Intersect(IEnumerable<T> second, IEqualityComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(second);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Intersect),
            [typeof(T)],
            Expression,
            Expression.Constant(second),
            Expression.Constant(comparer, typeof(IEqualityComparer<T>)));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Produces the set intersection of two sequences by using a specified key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="second">The sequence to intersect with the current sequence.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>A queryable containing the set intersection of the two sequences by key.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> or <paramref name="keySelector"/> is null.</exception>
    public IQueryable<T> IntersectBy<TKey>(
        IEnumerable<TKey> second,
        Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(keySelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.IntersectBy),
            [typeof(T), typeof(TKey)],
            Expression,
            Expression.Constant(second),
            Expression.Quote(keySelector));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Produces the set intersection of two sequences by using a specified key selector and comparer.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="second">The sequence to intersect with the current sequence.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="comparer">The equality comparer to use for the keys.</param>
    /// <returns>A queryable containing the set intersection of the two sequences by key.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> or <paramref name="keySelector"/> is null.</exception>
    public IQueryable<T> IntersectBy<TKey>(
        IEnumerable<TKey> second,
        Expression<Func<T, TKey>> keySelector,
        IEqualityComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(keySelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.IntersectBy),
            [typeof(T), typeof(TKey)],
            Expression,
            Expression.Constant(second),
            Expression.Quote(keySelector),
            Expression.Constant(comparer, typeof(IEqualityComparer<TKey>)));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Produces the set difference of two sequences.
    /// </summary>
    /// <param name="second">The sequence to compare to the current sequence.</param>
    /// <returns>A queryable containing the set difference of the two sequences.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> is null.</exception>
    public IQueryable<T> Except(IEnumerable<T> second)
    {
        ArgumentNullException.ThrowIfNull(second);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Except),
            [typeof(T)],
            Expression,
            Expression.Constant(second));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Produces the set difference of two sequences with a specified comparer.
    /// </summary>
    /// <param name="second">The sequence to compare to the current sequence.</param>
    /// <param name="comparer">The equality comparer to use for the elements.</param>
    /// <returns>A queryable containing the set difference of the two sequences.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> is null.</exception>
    public IQueryable<T> Except(IEnumerable<T> second, IEqualityComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(second);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Except),
            [typeof(T)],
            Expression,
            Expression.Constant(second),
            Expression.Constant(comparer, typeof(IEqualityComparer<T>)));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Produces the set difference of two sequences by using a specified key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="second">The sequence to compare to the current sequence.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>A queryable containing the set difference of the two sequences by key.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> or <paramref name="keySelector"/> is null.</exception>
    public IQueryable<T> ExceptBy<TKey>(
        IEnumerable<TKey> second,
        Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(keySelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.ExceptBy),
            [typeof(T), typeof(TKey)],
            Expression,
            Expression.Constant(second),
            Expression.Quote(keySelector));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Produces the set difference of two sequences by using a specified key selector and comparer.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="second">The sequence to compare to the current sequence.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="comparer">The equality comparer to use for the keys.</param>
    /// <returns>A queryable containing the set difference of the two sequences by key.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="second"/> or <paramref name="keySelector"/> is null.</exception>
    public IQueryable<T> ExceptBy<TKey>(
        IEnumerable<TKey> second,
        Expression<Func<T, TKey>> keySelector,
        IEqualityComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(keySelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.ExceptBy),
            [typeof(T), typeof(TKey)],
            Expression,
            Expression.Constant(second),
            Expression.Quote(keySelector),
            Expression.Constant(comparer, typeof(IEqualityComparer<TKey>)));

        return _provider.CreateQuery<T>(expression);
    }

    #endregion
}

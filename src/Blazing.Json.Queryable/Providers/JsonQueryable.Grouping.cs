using System.Linq.Expressions;
using SysQueryable = System.Linq.Queryable;

namespace Blazing.Json.Queryable.Providers;

/// <summary>
/// Grouping and join operations for <see cref="JsonQueryable{T}"/>.
/// </summary>
public partial class JsonQueryable<T>
{
    #region Grouping Operations

    /// <summary>
    /// Groups elements by a key selector.
    /// <b>Warning:</b> Materializes entire sequence for grouping.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>A queryable of groupings by key.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keySelector"/> is null.</exception>
    public IQueryable<IGrouping<TKey, T>> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.GroupBy),
            [typeof(T), typeof(TKey)],
            Expression,
            Expression.Quote(keySelector));

        return _provider.CreateQuery<IGrouping<TKey, T>>(expression);
    }

    /// <summary>
    /// Groups elements by a key selector with a specified comparer.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="comparer">The comparer to use for the keys.</param>
    /// <returns>A queryable of groupings by key.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keySelector"/> is null.</exception>
    public IQueryable<IGrouping<TKey, T>> GroupBy<TKey>(
        Expression<Func<T, TKey>> keySelector,
        IEqualityComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.GroupBy),
            [typeof(T), typeof(TKey)],
            Expression,
            Expression.Quote(keySelector),
            Expression.Constant(comparer, typeof(IEqualityComparer<TKey>)));

        return _provider.CreateQuery<IGrouping<TKey, T>>(expression);
    }

    /// <summary>
    /// Groups elements and applies an element selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TElement">The type of the elements in each group.</typeparam>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="elementSelector">A function to map each source element to an element in the group.</param>
    /// <returns>A queryable of groupings by key with projected elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
    public IQueryable<IGrouping<TKey, TElement>> GroupBy<TKey, TElement>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(elementSelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.GroupBy),
            [typeof(T), typeof(TKey), typeof(TElement)],
            Expression,
            Expression.Quote(keySelector),
            Expression.Quote(elementSelector));

        return _provider.CreateQuery<IGrouping<TKey, TElement>>(expression);
    }

    /// <summary>
    /// Groups elements and applies a result selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector"/>.</typeparam>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="resultSelector">A function to create a result value from each group.</param>
    /// <returns>A queryable of projected group results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keySelector"/> or <paramref name="resultSelector"/> is null.</exception>
    public IQueryable<TResult> GroupBy<TKey, TResult>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<TKey, IEnumerable<T>, TResult>> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.GroupBy),
            [typeof(T), typeof(TKey), typeof(TResult)],
            Expression,
            Expression.Quote(keySelector),
            Expression.Quote(resultSelector));

        return _provider.CreateQuery<TResult>(expression);
    }

    /// <summary>
    /// Groups elements with element selector and result selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TElement">The type of the elements in each group.</typeparam>
    /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector"/>.</typeparam>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="elementSelector">A function to map each source element to an element in the group.</param>
    /// <param name="resultSelector">A function to create a result value from each group.</param>
    /// <returns>A queryable of projected group results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keySelector"/>, <paramref name="elementSelector"/>, or <paramref name="resultSelector"/> is null.</exception>
    public IQueryable<TResult> GroupBy<TKey, TElement, TResult>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector,
        Expression<Func<TKey, IEnumerable<TElement>, TResult>> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(elementSelector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.GroupBy),
            [typeof(T), typeof(TKey), typeof(TElement), typeof(TResult)],
            Expression,
            Expression.Quote(keySelector),
            Expression.Quote(elementSelector),
            Expression.Quote(resultSelector));

        return _provider.CreateQuery<TResult>(expression);
    }

    /// <summary>
    /// Creates a <see cref="ILookup{TKey, TElement}"/> from the sequence.
    /// Terminal operation - executes immediately.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>A <see cref="ILookup{TKey, T}"/> containing keys and elements from the sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keySelector"/> is null.</exception>
    public ILookup<TKey, T> ToLookup<TKey>(Expression<Func<T, TKey>> keySelector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        // ToLookup is a terminal operation - execute immediately, don't build expression tree
        var query = _provider.Execute<IEnumerable<T>>(Expression);
        var compiled = keySelector.Compile();
        return query.ToLookup(compiled);
    }

    /// <summary>
    /// Creates a <see cref="ILookup{TKey, TElement}"/> from the sequence with an element selector.
    /// Terminal operation - executes immediately.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="elementSelector">A function to map each source element to a value in the lookup.</param>
    /// <returns>A <see cref="ILookup{TKey, TElement}"/> containing keys and values from the sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
    public ILookup<TKey, TElement> ToLookup<TKey, TElement>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(elementSelector);

        // ToLookup is a terminal operation - execute immediately
        var query = _provider.Execute<IEnumerable<T>>(Expression);
        var compiledKey = keySelector.Compile();
        var compiledElement = elementSelector.Compile();
        return query.ToLookup(compiledKey, compiledElement);
    }

    #endregion

    #region Join Operations

    /// <summary>
    /// Correlates elements of two sequences based on matching keys.
    /// <b>Warning:</b> Requires second sequence - materializes both sequences.
    /// </summary>
    /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
    /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="inner">The sequence to join to the first sequence.</param>
    /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
    /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
    /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
    /// <returns>A queryable of joined results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/>, <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/>, or <paramref name="resultSelector"/> is null.</exception>
    public IQueryable<TResult> Join<TInner, TKey, TResult>(
        IEnumerable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(outerKeySelector);
        ArgumentNullException.ThrowIfNull(innerKeySelector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Join),
            [typeof(T), typeof(TInner), typeof(TKey), typeof(TResult)],
            Expression,
            Expression.Constant(inner),
            Expression.Quote(outerKeySelector),
            Expression.Quote(innerKeySelector),
            Expression.Quote(resultSelector));

        return _provider.CreateQuery<TResult>(expression);
    }

    /// <summary>
    /// Correlates elements of two sequences based on matching keys with a specified comparer.
    /// </summary>
    /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
    /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="inner">The sequence to join to the first sequence.</param>
    /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
    /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
    /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
    /// <param name="comparer">The comparer to use for the keys.</param>
    /// <returns>A queryable of joined results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/>, <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/>, or <paramref name="resultSelector"/> is null.</exception>
    public IQueryable<TResult> Join<TInner, TKey, TResult>(
        IEnumerable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector,
        IEqualityComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(outerKeySelector);
        ArgumentNullException.ThrowIfNull(innerKeySelector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Join),
            [typeof(T), typeof(TInner), typeof(TKey), typeof(TResult)],
            Expression,
            Expression.Constant(inner),
            Expression.Quote(outerKeySelector),
            Expression.Quote(innerKeySelector),
            Expression.Quote(resultSelector),
            Expression.Constant(comparer, typeof(IEqualityComparer<TKey>)));

        return _provider.CreateQuery<TResult>(expression);
    }

    /// <summary>
    /// Correlates elements and groups the results (left outer join).
    /// </summary>
    /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
    /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="inner">The sequence to join to the first sequence.</param>
    /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
    /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
    /// <param name="resultSelector">A function to create a result element from an element of the first sequence and a group of matching elements from the second sequence.</param>
    /// <returns>A queryable of grouped join results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/>, <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/>, or <paramref name="resultSelector"/> is null.</exception>
    public IQueryable<TResult> GroupJoin<TInner, TKey, TResult>(
        IEnumerable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(outerKeySelector);
        ArgumentNullException.ThrowIfNull(innerKeySelector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.GroupJoin),
            [typeof(T), typeof(TInner), typeof(TKey), typeof(TResult)],
            Expression,
            Expression.Constant(inner),
            Expression.Quote(outerKeySelector),
            Expression.Quote(innerKeySelector),
            Expression.Quote(resultSelector));

        return _provider.CreateQuery<TResult>(expression);
    }

    /// <summary>
    /// Correlates elements and groups the results with a specified comparer.
    /// </summary>
    /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
    /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="inner">The sequence to join to the first sequence.</param>
    /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
    /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
    /// <param name="resultSelector">A function to create a result element from an element of the first sequence and a group of matching elements from the second sequence.</param>
    /// <param name="comparer">The comparer to use for the keys.</param>
    /// <returns>A queryable of grouped join results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/>, <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/>, or <paramref name="resultSelector"/> is null.</exception>
    public IQueryable<TResult> GroupJoin<TInner, TKey, TResult>(
        IEnumerable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector,
        IEqualityComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(outerKeySelector);
        ArgumentNullException.ThrowIfNull(innerKeySelector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.GroupJoin),
            [typeof(T), typeof(TInner), typeof(TKey), typeof(TResult)],
            Expression,
            Expression.Constant(inner),
            Expression.Quote(outerKeySelector),
            Expression.Quote(innerKeySelector),
            Expression.Quote(resultSelector),
            Expression.Constant(comparer, typeof(IEqualityComparer<TKey>)));

        return _provider.CreateQuery<TResult>(expression);
    }

    #endregion
}

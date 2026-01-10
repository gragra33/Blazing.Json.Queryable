using System.Linq.Expressions;
using SysQueryable = System.Linq.Queryable;

namespace Blazing.Json.Queryable.Providers;

/// <summary>
/// Aggregation operations for <see cref="JsonQueryable{T}"/>.
/// </summary>
public partial class JsonQueryable<T>
{
    #region Aggregation Operations

    /// <summary>
    /// Computes the sum of a sequence of <see cref="int"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The sum of the projected values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public int Sum(Expression<Func<T, int>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Sum),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<int>(expression);
    }

    /// <summary>
    /// Computes the sum of a sequence of nullable <see cref="int"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The sum of the projected values, or null if the sequence contains only nulls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public int? Sum(Expression<Func<T, int?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Sum),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<int?>(expression);
    }

    /// <summary>
    /// Computes the sum of a sequence of <see cref="long"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The sum of the projected values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public long Sum(Expression<Func<T, long>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Sum),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<long>(expression);
    }

    /// <summary>
    /// Computes the sum of a sequence of nullable <see cref="long"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The sum of the projected values, or null if the sequence contains only nulls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public long? Sum(Expression<Func<T, long?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Sum),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<long?>(expression);
    }

    /// <summary>
    /// Computes the sum of a sequence of <see cref="float"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The sum of the projected values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public float Sum(Expression<Func<T, float>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Sum),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<float>(expression);
    }

    /// <summary>
    /// Computes the sum of a sequence of nullable <see cref="float"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The sum of the projected values, or null if the sequence contains only nulls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public float? Sum(Expression<Func<T, float?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Sum),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<float?>(expression);
    }

    /// <summary>
    /// Computes the sum of a sequence of <see cref="double"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The sum of the projected values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public double Sum(Expression<Func<T, double>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Sum),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<double>(expression);
    }

    /// <summary>
    /// Computes the sum of a sequence of nullable <see cref="double"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The sum of the projected values, or null if the sequence contains only nulls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public double? Sum(Expression<Func<T, double?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Sum),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<double?>(expression);
    }

    /// <summary>
    /// Computes the sum of a sequence of <see cref="decimal"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The sum of the projected values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public decimal Sum(Expression<Func<T, decimal>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Sum),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<decimal>(expression);
    }

    /// <summary>
    /// Computes the sum of a sequence of nullable <see cref="decimal"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The sum of the projected values, or null if the sequence contains only nulls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public decimal? Sum(Expression<Func<T, decimal?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Sum),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<decimal?>(expression);
    }

    /// <summary>
    /// Computes the average of a sequence of <see cref="int"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The average of the projected values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public double Average(Expression<Func<T, int>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Average),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<double>(expression);
    }

    /// <summary>
    /// Computes the average of a sequence of nullable <see cref="int"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The average of the projected values, or null if the sequence contains only nulls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public double? Average(Expression<Func<T, int?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Average),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<double?>(expression);
    }

    /// <summary>
    /// Computes the average of a sequence of <see cref="long"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The average of the projected values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public double Average(Expression<Func<T, long>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Average),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<double>(expression);
    }

    /// <summary>
    /// Computes the average of a sequence of nullable <see cref="long"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The average of the projected values, or null if the sequence contains only nulls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public double? Average(Expression<Func<T, long?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Average),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<double?>(expression);
    }

    /// <summary>
    /// Computes the average of a sequence of <see cref="float"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The average of the projected values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public float Average(Expression<Func<T, float>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Average),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<float>(expression);
    }

    /// <summary>
    /// Computes the average of a sequence of nullable <see cref="float"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The average of the projected values, or null if the sequence contains only nulls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public float? Average(Expression<Func<T, float?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Average),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<float?>(expression);
    }

    /// <summary>
    /// Computes the average of a sequence of <see cref="double"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The average of the projected values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public double Average(Expression<Func<T, double>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Average),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<double>(expression);
    }

    /// <summary>
    /// Computes the average of a sequence of nullable <see cref="double"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The average of the projected values, or null if the sequence contains only nulls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public double? Average(Expression<Func<T, double?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Average),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<double?>(expression);
    }

    /// <summary>
    /// Computes the average of a sequence of <see cref="decimal"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The average of the projected values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public decimal Average(Expression<Func<T, decimal>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Average),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<decimal>(expression);
    }

    /// <summary>
    /// Computes the average of a sequence of nullable <see cref="decimal"/> values.
    /// </summary>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The average of the projected values, or null if the sequence contains only nulls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public decimal? Average(Expression<Func<T, decimal?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Average),
            null,
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<decimal?>(expression);
    }

    /// <summary>
    /// Returns the minimum value in a sequence according to the specified selector.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the selector.</typeparam>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The minimum value in the sequence, or null if the sequence is empty or contains only nulls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public TResult? Min<TResult>(Expression<Func<T, TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Min),
            [typeof(T), typeof(TResult)],
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<TResult>(expression);
    }

    /// <summary>
    /// Returns the maximum value in a sequence according to the specified selector.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the selector.</typeparam>
    /// <param name="selector">A projection function to extract the value from each element.</param>
    /// <returns>The maximum value in the sequence, or null if the sequence is empty or contains only nulls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null.</exception>
    public TResult? Max<TResult>(Expression<Func<T, TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Max),
            [typeof(T), typeof(TResult)],
            Expression,
            Expression.Quote(selector));

        return _provider.Execute<TResult>(expression);
    }

    /// <summary>
    /// Returns the element with the minimum key value according to the specified key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by the selector.</typeparam>
    /// <param name="keySelector">A projection function to extract the key from each element.</param>
    /// <returns>The element with the minimum key value, or null if the sequence is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keySelector"/> is null.</exception>
    public T? MinBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.MinBy),
            [typeof(T), typeof(TKey)],
            Expression,
            Expression.Quote(keySelector));

        return _provider.Execute<T>(expression);
    }

    /// <summary>
    /// Returns the element with the maximum key value according to the specified key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by the selector.</typeparam>
    /// <param name="keySelector">A projection function to extract the key from each element.</param>
    /// <returns>The element with the maximum key value, or null if the sequence is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keySelector"/> is null.</exception>
    public T? MaxBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.MaxBy),
            [typeof(T), typeof(TKey)],
            Expression,
            Expression.Quote(keySelector));

        return _provider.Execute<T>(expression);
    }

    /// <summary>
    /// Applies an accumulator function over a sequence.
    /// </summary>
    /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="func">An accumulator function to be invoked on each element.</param>
    /// <returns>The final accumulator value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="func"/> is null.</exception>
    public TAccumulate Aggregate<TAccumulate>(
        TAccumulate seed,
        Expression<Func<TAccumulate, T, TAccumulate>> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Aggregate),
            [typeof(T), typeof(TAccumulate)],
            Expression,
            Expression.Constant(seed),
            Expression.Quote(func));

        return _provider.Execute<TAccumulate>(expression);
    }

    /// <summary>
    /// Applies an accumulator function over a sequence and applies a result selector to the final accumulator value.
    /// </summary>
    /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="func">An accumulator function to be invoked on each element.</param>
    /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
    /// <returns>The transformed result value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="func"/> or <paramref name="resultSelector"/> is null.</exception>
    public TResult Aggregate<TAccumulate, TResult>(
        TAccumulate seed,
        Expression<Func<TAccumulate, T, TAccumulate>> func,
        Expression<Func<TAccumulate, TResult>> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(resultSelector);

        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.Aggregate),
            [typeof(T), typeof(TAccumulate), typeof(TResult)],
            Expression,
            Expression.Constant(seed),
            Expression.Quote(func),
            Expression.Quote(resultSelector));

        return _provider.Execute<TResult>(expression);
    }

    #endregion
}

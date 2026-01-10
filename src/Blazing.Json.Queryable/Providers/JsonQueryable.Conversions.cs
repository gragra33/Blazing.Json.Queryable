using System.Linq.Expressions;
using SysQueryable = System.Linq.Queryable;

namespace Blazing.Json.Queryable.Providers;

/// <summary>
/// Conversion and utility operations for <see cref="JsonQueryable{T}"/>.
/// </summary>
public partial class JsonQueryable<T>
{
    #region Conversion Operations

    /// <summary>
    /// Converts the sequence to an array.
    /// Terminal operation - executes query immediately.
    /// </summary>
    /// <returns>An array containing the elements of the sequence.</returns>
    public T[] ToArray()
    {
        // ToArray is a terminal operation - execute immediately
        var query = _provider.Execute<IEnumerable<T>>(Expression);
        return [.. query];
    }

    /// <summary>
    /// Converts the sequence to a <see cref="List{T}"/>.
    /// Terminal operation - executes query immediately.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> containing the elements of the sequence.</returns>
    public List<T> ToList()
    {
        // ToList is a terminal operation - execute immediately
        var query = _provider.Execute<IEnumerable<T>>(Expression);
        return [.. query];
    }

    /// <summary>
    /// Creates a <see cref="Dictionary{TKey, TValue}"/> from the sequence using the specified key selector.
    /// Terminal operation - executes immediately. Throws if duplicate keys found.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <returns>A <see cref="Dictionary{TKey, T}"/> containing keys and elements from the sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keySelector"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if duplicate keys are found.</exception>
    public Dictionary<TKey, T> ToDictionary<TKey>(Expression<Func<T, TKey>> keySelector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        // ToDictionary is a terminal operation - execute immediately
        var query = _provider.Execute<IEnumerable<T>>(Expression);
        var compiled = keySelector.Compile();
        return query.ToDictionary(compiled);
    }

    /// <summary>
    /// Creates a <see cref="Dictionary{TKey, TElement}"/> from the sequence using the specified key and element selectors.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="elementSelector">A function to map each element to a value in the dictionary.</param>
    /// <returns>A <see cref="Dictionary{TKey, TElement}"/> containing keys and values from the sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if duplicate keys are found.</exception>
    public Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(elementSelector);

        // ToDictionary is a terminal operation - execute immediately
        var query = _provider.Execute<IEnumerable<T>>(Expression);
        var compiledKey = keySelector.Compile();
        var compiledElement = elementSelector.Compile();
        return query.ToDictionary(compiledKey, compiledElement);
    }

    /// <summary>
    /// Creates a <see cref="Dictionary{TKey, TValue}"/> from the sequence using the specified key selector and comparer.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="comparer">The comparer to use for the dictionary keys.</param>
    /// <returns>A <see cref="Dictionary{TKey, T}"/> containing keys and elements from the sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keySelector"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if duplicate keys are found.</exception>
    public Dictionary<TKey, T> ToDictionary<TKey>(
        Expression<Func<T, TKey>> keySelector,
        IEqualityComparer<TKey>? comparer)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        // ToDictionary is a terminal operation - execute immediately
        var query = _provider.Execute<IEnumerable<T>>(Expression);
        var compiled = keySelector.Compile();
        return query.ToDictionary(compiled, comparer);
    }

    /// <summary>
    /// Creates a <see cref="HashSet{T}"/> from the sequence.
    /// Terminal operation - executes immediately.
    /// </summary>
    /// <returns>A <see cref="HashSet{T}"/> containing the elements of the sequence.</returns>
    public HashSet<T> ToHashSet()
    {
        // ToHashSet is a terminal operation - execute immediately
        var query = _provider.Execute<IEnumerable<T>>(Expression);
        return [.. query];
    }

    /// <summary>
    /// Creates a <see cref="HashSet{T}"/> from the sequence using a specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer to use for the set elements.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing the elements of the sequence.</returns>
    public HashSet<T> ToHashSet(IEqualityComparer<T>? comparer)
    {
        // ToHashSet is a terminal operation - execute immediately
        var query = _provider.Execute<IEnumerable<T>>(Expression);
        return query.ToHashSet(comparer);
    }

    #endregion

    #region Utility Operations

    /// <summary>
    /// Returns the sequence as <see cref="IEnumerable{T}"/>.
    /// Useful for switching from query to local execution.
    /// </summary>
    /// <returns>The sequence as <see cref="IEnumerable{T}"/>.</returns>
    public IEnumerable<T> AsEnumerable()
    {
        return this;
    }

    /// <summary>
    /// Returns the sequence, or a default element if empty.
    /// </summary>
    /// <returns>An <see cref="IQueryable{T}"/> that contains the default value if the sequence is empty; otherwise, the sequence itself.</returns>
    public IQueryable<T?> DefaultIfEmpty()
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.DefaultIfEmpty),
            [typeof(T)],
            Expression);

        return _provider.CreateQuery<T?>(expression);
    }

    /// <summary>
    /// Returns the sequence, or a specified default value if empty.
    /// </summary>
    /// <param name="defaultValue">The value to return if the sequence is empty.</param>
    /// <returns>An <see cref="IQueryable{T}"/> that contains <paramref name="defaultValue"/> if the sequence is empty; otherwise, the sequence itself.</returns>
    public IQueryable<T> DefaultIfEmpty(T defaultValue)
    {
        var expression = Expression.Call(
            typeof(SysQueryable),
            nameof(SysQueryable.DefaultIfEmpty),
            [typeof(T)],
            Expression,
            Expression.Constant(defaultValue, typeof(T)));

        return _provider.CreateQuery<T>(expression);
    }

    /// <summary>
    /// Attempts to get the count without enumerating.
    /// For JSON streams, this typically returns false.
    /// </summary>
    /// <param name="count">When this method returns, contains the count of elements if successful; otherwise, zero.</param>
    /// <returns><c>true</c> if the count could be determined without enumeration; otherwise, <c>false</c>.</returns>
    public bool TryGetNonEnumeratedCount(out int count)
    {
        count = 0;
        return false;
    }

    #endregion
}

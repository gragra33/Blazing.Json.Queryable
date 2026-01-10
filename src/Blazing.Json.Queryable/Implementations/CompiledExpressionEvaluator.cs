using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Blazing.Json.Queryable.Implementations;

/// <summary>
/// Implementation of IExpressionEvaluator that uses Expression.Compile() 
/// for predicate and selector evaluation.
/// Caches compiled delegates for performance.
/// </summary>
public sealed class CompiledExpressionEvaluator : Core.IExpressionEvaluator
{
    // Cache compiled predicates to avoid repeated compilation
    // Key: Expression tree hash, Value: Compiled delegate
    private readonly ConcurrentDictionary<int, Delegate> _predicateCache = new();
    private readonly ConcurrentDictionary<int, Delegate> _selectorCache = new();

    /// <summary>
    /// Builds a predicate function from a LINQ expression using Expression.Compile().
    /// Results are cached to avoid repeated compilation overhead.
    /// </summary>
    /// <typeparam name="T">The type of object the predicate operates on</typeparam>
    /// <param name="expression">The expression to compile into a predicate</param>
    /// <returns>A compiled predicate function</returns>
    /// <exception cref="ArgumentNullException">Thrown when expression is null</exception>
    public Func<T, bool> BuildPredicate<T>(Expression<Func<T, bool>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        // Use expression's GetHashCode for caching
        // Note: This is safe because expression trees are immutable
        int cacheKey = expression.GetHashCode();

        if (_predicateCache.TryGetValue(cacheKey, out var cachedDelegate))
        {
            return (Func<T, bool>)cachedDelegate;
        }

        // Compile the expression tree into executable code
        var compiledPredicate = expression.Compile();

        // Cache for future use
        _predicateCache.TryAdd(cacheKey, compiledPredicate);

        return compiledPredicate;
    }

    /// <summary>
    /// Builds a selector function from a LINQ expression using Expression.Compile().
    /// Results are cached to avoid repeated compilation overhead.
    /// </summary>
    /// <typeparam name="T">The source type</typeparam>
    /// <typeparam name="TResult">The result type after projection</typeparam>
    /// <param name="expression">The expression to compile into a selector</param>
    /// <returns>A compiled selector function</returns>
    /// <exception cref="ArgumentNullException">Thrown when expression is null</exception>
    public Func<T, TResult> BuildSelector<T, TResult>(Expression<Func<T, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        // Use expression's GetHashCode for caching
        int cacheKey = expression.GetHashCode();

        if (_selectorCache.TryGetValue(cacheKey, out var cachedDelegate))
        {
            return (Func<T, TResult>)cachedDelegate;
        }

        // Compile the expression tree into executable code
        var compiledSelector = expression.Compile();

        // Cache for future use
        _selectorCache.TryAdd(cacheKey, compiledSelector);

        return compiledSelector;
    }

    /// <summary>
    /// Gets the current number of cached predicates.
    /// Useful for diagnostics and testing.
    /// </summary>
    public int PredicateCacheCount => _predicateCache.Count;

    /// <summary>
    /// Gets the current number of cached selectors.
    /// Useful for diagnostics and testing.
    /// </summary>
    public int SelectorCacheCount => _selectorCache.Count;

    /// <summary>
    /// Clears all cached delegates.
    /// Useful for testing or memory management in long-running applications.
    /// </summary>
    public void ClearCache()
    {
        _predicateCache.Clear();
        _selectorCache.Clear();
    }
}

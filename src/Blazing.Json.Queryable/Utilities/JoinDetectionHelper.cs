namespace Blazing.Json.Queryable.Utilities;

/// <summary>
/// Shared helper class for join operation detection logic.
/// Centralizes the logic for determining whether a join operation is a Join or GroupJoin
/// based on the result selector's signature.
/// </summary>
internal static class JoinDetectionHelper
{
    /// <summary>
    /// Determines if a join operation is a GroupJoin based on result selector signature.
    /// Handles compiler-generated Closure parameters correctly.
    /// </summary>
    /// <param name="resultSelector">The result selector delegate to inspect.</param>
    /// <returns>True if this is a GroupJoin (second parameter is IEnumerable), false for regular Join.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="resultSelector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when result selector has invalid parameter count.</exception>
    /// <remarks>
    /// <para>
    /// <strong>Join vs GroupJoin Detection Logic:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><strong>Join:</strong> Func&lt;TOuter, TInner, TResult&gt; - second param is single element</item>
    /// <item><strong>GroupJoin:</strong> Func&lt;TOuter, IEnumerable&lt;TInner&gt;, TResult&gt; - second param is IEnumerable</item>
    /// </list>
    /// <para>
    /// <strong>Compiler-Generated Parameters:</strong>
    /// </para>
    /// <para>
    /// Compiled lambdas may have a compiler-generated Closure parameter at index 0:
    /// </para>
    /// <list type="bullet">
    /// <item><strong>Without Closure:</strong> [TOuter, TInner/IEnumerable&lt;TInner&gt;] (2 params)</item>
    /// <item><strong>With Closure:</strong> [Closure, TOuter, TInner/IEnumerable&lt;TInner&gt;] (3 params)</item>
    /// </list>
    /// <para>
    /// We inspect the LAST parameter to handle both cases correctly.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Join: Func&lt;Person, Order, Result&gt;
    /// Func&lt;Person, Order, string&gt; joinSelector = (p, o) => $"{p.Name}: {o.Id}";
    /// bool isGroupJoin = JoinDetectionHelper.IsGroupJoin(joinSelector);
    /// // Returns: false
    /// 
    /// // GroupJoin: Func&lt;Person, IEnumerable&lt;Order&gt;, Result&gt;
    /// Func&lt;Person, IEnumerable&lt;Order&gt;, string&gt; groupJoinSelector = 
    ///     (p, orders) => $"{p.Name}: {orders.Count()}";
    /// isGroupJoin = JoinDetectionHelper.IsGroupJoin(groupJoinSelector);
    /// // Returns: true
    /// </code>
    /// </example>
    public static bool IsGroupJoin(Delegate resultSelector)
    {
        ArgumentNullException.ThrowIfNull(resultSelector);
        
        var parameters = resultSelector.Method.GetParameters();
        
        // Need at least 2 parameters for join operations
        // (or 3 with compiler-generated Closure)
        if (parameters.Length < 2)
        {
            throw new InvalidOperationException(
                $"Result selector must have at least 2 parameters. Found {parameters.Length}.");
        }
        
        // Get the second LOGICAL parameter (last parameter in array)
        // - 2 params: parameters[1] (no Closure)
        // - 3 params: parameters[2] (with Closure at index 0)
        var secondLogicalParam = parameters[^1];
        
        // Check if it's IEnumerable<T> (GroupJoin) vs single element (Join)
        return secondLogicalParam.ParameterType.IsGenericType &&
               secondLogicalParam.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
    }
}

using System.Linq.Expressions;

namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Evaluates LINQ expressions to create predicate and selector functions.
/// This interface enables swappable evaluation strategies for AOT compatibility.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Performance Note:</strong> This interface works identically for both synchronous and
/// asynchronous query execution. Expressions are evaluated once and cached; async execution
/// does not require different expression evaluation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var evaluator = new CompiledExpressionEvaluator();
/// 
/// // Build a predicate
/// Expression&lt;Func&lt;Person, bool&gt;&gt; expr = p => p.Age > 18;
/// var predicate = evaluator.BuildPredicate(expr);
/// 
/// // Use the predicate
/// bool isAdult = predicate(person);
/// </code>
/// </example>
public interface IExpressionEvaluator
{
    /// <summary>
    /// Builds a predicate function from a LINQ expression.
    /// The resulting function can be invoked multiple times with minimal overhead.
    /// </summary>
    /// <typeparam name="T">The type of object to evaluate</typeparam>
    /// <param name="expression">The LINQ expression representing the filter condition</param>
    /// <returns>A compiled predicate function that can be invoked efficiently</returns>
    /// <example>
    /// <code>
    /// // Simple predicate
    /// var predicate = evaluator.BuildPredicate&lt;Person&gt;(p => p.Age > 25);
    /// 
    /// // Complex predicate
    /// var complexPredicate = evaluator.BuildPredicate&lt;Order&gt;(
    ///     o => o.Total > 100 &amp;&amp; o.Status == "Pending"
    /// );
    /// </code>
    /// </example>
    Func<T, bool> BuildPredicate<T>(Expression<Func<T, bool>> expression);

    /// <summary>
    /// Builds a selector function from a LINQ expression for projection operations.
    /// Supports transforming objects, creating anonymous types, and extracting specific properties.
    /// </summary>
    /// <typeparam name="T">The source type to transform</typeparam>
    /// <typeparam name="TResult">The result type after transformation</typeparam>
    /// <param name="expression">The LINQ expression representing the projection</param>
    /// <returns>A compiled selector function that transforms source objects to result objects</returns>
    /// <remarks>
    /// <para>
    /// Selectors are commonly used for:
    /// <list type="bullet">
    /// <item>Property extraction: <c>p => p.Name</c></item>
    /// <item>Anonymous types: <c>p => new { p.Name, p.Age }</c></item>
    /// <item>Complex transformations: <c>p => new PersonDto { FullName = p.FirstName + " " + p.LastName }</c></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Performance:</strong> The compiled selector is cached and reused for all
    /// matching items in the query, minimizing overhead.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple property selection
    /// var nameSelector = evaluator.BuildSelector&lt;Person, string&gt;(p => p.Name);
    /// 
    /// // Anonymous type projection
    /// var dtoSelector = evaluator.BuildSelector&lt;Person, object&gt;(
    ///     p => new { p.Name, IsAdult = p.Age >= 18 }
    /// );
    /// 
    /// // Complex transformation
    /// var summarySelector = evaluator.BuildSelector&lt;Order, OrderSummary&gt;(
    ///     o => new OrderSummary 
    ///     { 
    ///         Id = o.OrderId,
    ///         CustomerName = o.Customer.Name,
    ///         ItemCount = o.Items.Count
    ///     }
    /// );
    /// </code>
    /// </example>
    Func<T, TResult> BuildSelector<T, TResult>(Expression<Func<T, TResult>> expression);
}

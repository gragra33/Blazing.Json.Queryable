using System.Linq.Expressions;

namespace Blazing.Json.Queryable.Visitors;

/// <summary>
/// Base expression visitor for JSON query expression trees.
/// Provides common functionality for walking and analyzing LINQ expression trees.
/// </summary>
public abstract class JsonExpressionVisitor : ExpressionVisitor
{
    /// <summary>
    /// Gets the query execution plan being built and populated by the visitor.
    /// </summary>
    protected Core.QueryExecutionPlan Plan { get; }

    /// <summary>
    /// Gets the expression evaluator used for compiling predicates and selectors.
    /// </summary>
    protected Core.IExpressionEvaluator Evaluator { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonExpressionVisitor"/> class.
    /// </summary>
    /// <param name="plan">The <see cref="Core.QueryExecutionPlan"/> to populate.</param>
    /// <param name="evaluator">The <see cref="Core.IExpressionEvaluator"/> used for compiling expressions.</param>
    protected JsonExpressionVisitor(Core.QueryExecutionPlan plan, Core.IExpressionEvaluator evaluator)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(evaluator);

        Plan = plan;
        Evaluator = evaluator;
    }

    /// <summary>
    /// Visits a <see cref="MethodCallExpression"/> node and delegates to specialized handlers for recognized LINQ methods.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    /// <returns>The visited expression.</returns>
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        ArgumentNullException.ThrowIfNull(node);

        // Get the method name
        string methodName = node.Method.Name;

        // Delegate to appropriate handler based on method name
        return methodName switch
        {
            Core.LinqMethodNames.Where => VisitWhere(node),
            Core.LinqMethodNames.Select => VisitSelect(node),
            Core.LinqMethodNames.OrderBy
                or Core.LinqMethodNames.OrderByDescending
                or Core.LinqMethodNames.ThenBy
                or Core.LinqMethodNames.ThenByDescending => VisitOrderBy(node),
            Core.LinqMethodNames.Take => VisitTake(node),
            Core.LinqMethodNames.Skip => VisitSkip(node),
            Core.LinqMethodNames.First
                or Core.LinqMethodNames.FirstOrDefault
                or Core.LinqMethodNames.Single
                or Core.LinqMethodNames.SingleOrDefault
                or Core.LinqMethodNames.Count
                or Core.LinqMethodNames.LongCount
                or Core.LinqMethodNames.Any => VisitAggregation(node),
            _ => base.VisitMethodCall(node)
        };
    }

    /// <summary>
    /// Visits a Where clause. Override in derived classes to handle filtering logic.
    /// </summary>
    /// <param name="node">The Where method call expression.</param>
    /// <returns>The visited expression.</returns>
    protected virtual Expression VisitWhere(MethodCallExpression node)
    {
        // Default: continue visiting
        return base.VisitMethodCall(node);
    }

    /// <summary>
    /// Visits a Select clause. Override in derived classes to handle projection logic.
    /// </summary>
    /// <param name="node">The Select method call expression.</param>
    /// <returns>The visited expression.</returns>
    protected virtual Expression VisitSelect(MethodCallExpression node)
    {
        // Default: continue visiting
        return base.VisitMethodCall(node);
    }

    /// <summary>
    /// Visits an OrderBy/ThenBy clause. Override in derived classes to handle sorting logic.
    /// </summary>
    /// <param name="node">The OrderBy method call expression.</param>
    /// <returns>The visited expression.</returns>
    protected virtual Expression VisitOrderBy(MethodCallExpression node)
    {
        // Default: continue visiting
        return base.VisitMethodCall(node);
    }

    /// <summary>
    /// Visits a Take clause. Override in derived classes to handle limiting logic.
    /// </summary>
    /// <param name="node">The Take method call expression.</param>
    /// <returns>The visited expression.</returns>
    protected virtual Expression VisitTake(MethodCallExpression node)
    {
        // Default: continue visiting
        return base.VisitMethodCall(node);
    }

    /// <summary>
    /// Visits a Skip clause. Override in derived classes to handle skipping logic.
    /// </summary>
    /// <param name="node">The Skip method call expression.</param>
    /// <returns>The visited expression.</returns>
    protected virtual Expression VisitSkip(MethodCallExpression node)
    {
        // Default: continue visiting
        return base.VisitMethodCall(node);
    }

    /// <summary>
    /// Visits an aggregation method (First, Count, Any, etc.).
    /// Override in derived classes to handle aggregation operations.
    /// </summary>
    /// <param name="node">The aggregation method call expression.</param>
    /// <returns>The visited expression.</returns>
    protected virtual Expression VisitAggregation(MethodCallExpression node)
    {
        // Default: continue visiting
        return base.VisitMethodCall(node);
    }

    /// <summary>
    /// Extracts the lambda expression from a method call argument at the specified index.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    /// <param name="argumentIndex">The index of the lambda argument (typically 1).</param>
    /// <returns>The lambda expression, or <c>null</c> if not found.</returns>
    protected static LambdaExpression? GetLambdaArgument(MethodCallExpression node, int argumentIndex = 1)
    {
        if (node.Arguments.Count <= argumentIndex)
        {
            return null;
        }

        var argument = node.Arguments[argumentIndex];

        // Unwrap unary quote expressions
        if (argument is UnaryExpression { NodeType: ExpressionType.Quote } unary)
        {
            return unary.Operand as LambdaExpression;
        }

        return argument as LambdaExpression;
    }

    /// <summary>
    /// Gets a constant value of type <typeparamref name="T"/> from an expression.
    /// </summary>
    /// <typeparam name="T">The type of the constant value to extract.</typeparam>
    /// <param name="expression">The expression to extract from.</param>
    /// <returns>The constant value, or default if not a constant.</returns>
    protected static T? GetConstantValue<T>(Expression expression)
    {
        if (expression is ConstantExpression { Value: T value })
        {
            return value;
        }

        return default;
    }

    /// <summary>
    /// Extracts the property name from a lambda expression.
    /// Handles simple member access (e.g., <c>p =&gt; p.Name</c>).
    /// </summary>
    /// <param name="lambda">The lambda expression.</param>
    /// <returns>The property name, or <c>null</c> if not a simple member access.</returns>
    protected static string? GetPropertyName(LambdaExpression lambda)
    {
        if (lambda.Body is MemberExpression member)
        {
            return member.Member.Name;
        }

        // Handle conversions (e.g., p => (object)p.Name)
        if (lambda.Body is UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression convertedMember })
        {
            return convertedMember.Member.Name;
        }

        return null;
    }
}

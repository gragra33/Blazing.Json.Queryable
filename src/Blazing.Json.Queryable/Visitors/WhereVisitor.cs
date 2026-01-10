using System.Linq.Expressions;

namespace Blazing.Json.Queryable.Visitors;

/// <summary>
/// Expression visitor that handles Where clause filtering.
/// Extracts filter predicates and property paths from Where expressions.
/// </summary>
public sealed class WhereVisitor(Core.QueryExecutionPlan plan, Core.IExpressionEvaluator evaluator)
    : JsonExpressionVisitor(plan, evaluator)
{
    private readonly List<ReadOnlyMemory<char>> _filterPaths = [];
    private readonly List<Delegate> _predicates = [];

    /// <summary>
    /// Visits a Where clause and extracts the filter predicate.
    /// </summary>
    /// <param name="node">The Where method call expression.</param>
    /// <returns>The visited expression.</returns>
    protected override Expression VisitWhere(MethodCallExpression node)
    {
        ArgumentNullException.ThrowIfNull(node);

        // Extract the lambda expression (predicate)
        var lambda = GetLambdaArgument(node, argumentIndex: 1);

        if (lambda is not null)
        {
            // Extract property names from the lambda body
            ExtractFilterProperties(lambda);

            // Compile the predicate using the evaluator
            CompilePredicate(lambda);
        }

        // Continue visiting the source expression
        if (node.Arguments.Count > 0)
        {
            Visit(node.Arguments[0]);
        }

        return node;
    }

    /// <summary>
    /// Extracts property names referenced in the filter expression.
    /// Stores them as <see cref="ReadOnlyMemory{Char}"/> for zero-allocation access.
    /// </summary>
    /// <param name="lambda">The lambda expression containing the filter.</param>
    private void ExtractFilterProperties(LambdaExpression lambda)
    {
        // For simple member access (e.g., p => p.Age > 25)
        var propertyName = GetPropertyName(lambda);

        if (!string.IsNullOrEmpty(propertyName))
        {
            // Store as ReadOnlyMemory<char> for span-based access
            _filterPaths.Add(propertyName.AsMemory());
        }
        else
        {
            // For complex expressions, extract all member accesses
            var memberVisitor = new MemberExpressionCollector();
            memberVisitor.Visit(lambda.Body);

            foreach (var memberName in memberVisitor.MemberNames)
            {
                _filterPaths.Add(memberName.AsMemory());
            }
        }
    }

    /// <summary>
    /// Compiles the filter predicate using the expression evaluator.
    /// </summary>
    /// <param name="lambda">The lambda expression to compile.</param>
    private void CompilePredicate(LambdaExpression lambda)
    {
        // Get the generic type arguments
        var parameterType = lambda.Parameters[0].Type;
        var returnType = lambda.ReturnType;

        // Only handle boolean predicates
        if (returnType != typeof(bool))
        {
            throw new InvalidOperationException($"Where predicate must return bool, got {returnType.Name}");
        }

        // Use reflection to call the generic BuildPredicate method
        var buildPredicateMethod = Evaluator.GetType()
            .GetMethod(nameof(Core.IExpressionEvaluator.BuildPredicate))
            ?.MakeGenericMethod(parameterType);

        if (buildPredicateMethod is not null)
        {
            // Convert to Expression&lt;Func&lt;T, bool&gt;&gt;

            // Invoke the method
            var compiledPredicate = buildPredicateMethod.Invoke(Evaluator, [lambda]);

            if (compiledPredicate is Delegate predicate)
            {
                _predicates.Add(predicate);
            }
        }
    }

    /// <summary>
    /// Completes the visit and updates the execution plan with collected filter information.
    /// </summary>
    public void Complete()
    {
        if (_filterPaths.Count > 0)
        {
            Plan.FilterPropertyPaths = [.. _filterPaths];
        }

        if (_predicates.Count > 0)
        {
            Plan.Predicates = [.. _predicates];
        }
    }

    /// <summary>
    /// Helper visitor to collect all member accesses in an expression.
    /// </summary>
    private sealed class MemberExpressionCollector : ExpressionVisitor
    {
        /// <summary>
        /// Gets the list of member names found in the expression.
        /// </summary>
        public List<string> MemberNames { get; } = [];

        /// <summary>
        /// Visits a member expression and adds its name to the collection.
        /// </summary>
        /// <param name="node">The member expression node.</param>
        /// <returns>The visited expression.</returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            MemberNames.Add(node.Member.Name);
            return base.VisitMember(node);
        }
    }
}

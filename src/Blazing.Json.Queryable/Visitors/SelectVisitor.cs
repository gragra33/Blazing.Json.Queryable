using System.Linq.Expressions;

namespace Blazing.Json.Queryable.Visitors;

/// <summary>
/// Expression visitor that handles Select clause projections.
/// Extracts projection selectors and property paths from Select expressions.
/// </summary>
public sealed class SelectVisitor(Core.QueryExecutionPlan plan, Core.IExpressionEvaluator evaluator) 
    : JsonExpressionVisitor(plan, evaluator)
{
    private readonly List<ReadOnlyMemory<char>> _projectionPaths = [];
    private Delegate? _projectionSelector;

    /// <summary>
    /// Visits a Select clause and extracts the projection selector.
    /// </summary>
    /// <param name="node">The Select method call expression.</param>
    /// <returns>The visited expression.</returns>
    protected override Expression VisitSelect(MethodCallExpression node)
    {
        ArgumentNullException.ThrowIfNull(node);
        
        // CRITICAL FIX: Only process actual Select calls, not GroupBy with result selectors
        // GroupBy operations should be handled by AdvancedLinqOperationsVisitor
        if (node.Method.Name != Core.LinqMethodNames.Select)
        {
            // This is not a Select operation, skip it
            if (node.Arguments.Count > 0)
            {
                Visit(node.Arguments[0]);
            }
            return node;
        }

        // Extract the lambda expression (selector)
        var lambda = GetLambdaArgument(node, argumentIndex: 1);

        if (lambda is not null)
        {
            // Extract property names from the lambda body
            ExtractProjectionProperties(lambda);

            // Compile the selector using the evaluator
            CompileSelector(lambda);

            // Update result type in plan
            Plan.ResultType = lambda.ReturnType;
        }

        // Continue visiting the source expression
        if (node.Arguments.Count > 0)
        {
            Visit(node.Arguments[0]);
        }

        return node;
    }

    /// <summary>
    /// Extracts property names referenced in the projection expression.
    /// Stores them as <see cref="ReadOnlyMemory{Char}"/> for zero-allocation access.
    /// </summary>
    /// <param name="lambda">The lambda expression containing the projection.</param>
    private void ExtractProjectionProperties(LambdaExpression lambda)
    {
        // Simple property access (e.g., p => p.Name)
        if (lambda.Body is MemberExpression member)
        {
            _projectionPaths.Add(member.Member.Name.AsMemory());
            return;
        }

        // Anonymous type or new expression (e.g., p => new { p.Name, p.Age })
        if (lambda.Body is NewExpression newExpr)
        {
            foreach (var argument in newExpr.Arguments)
            {
                if (argument is MemberExpression memberArg)
                {
                    _projectionPaths.Add(memberArg.Member.Name.AsMemory());
                }
            }
            return;
        }

        // Member initialization (e.g., p => new Person { Name = p.Name })
        if (lambda.Body is MemberInitExpression initExpr)
        {
            foreach (var binding in initExpr.Bindings)
            {
                if (binding is MemberAssignment { Expression: MemberExpression assignMember })
                {
                    _projectionPaths.Add(assignMember.Member.Name.AsMemory());
                }
            }
            return;
        }

        // Complex expressions - collect all member accesses
        var memberVisitor = new MemberExpressionCollector();
        memberVisitor.Visit(lambda.Body);

        foreach (var memberName in memberVisitor.MemberNames)
        {
            _projectionPaths.Add(memberName.AsMemory());
        }
    }

    /// <summary>
    /// Compiles the projection selector using the expression evaluator.
    /// </summary>
    /// <param name="lambda">The lambda expression to compile.</param>
    private void CompileSelector(LambdaExpression lambda)
    {
        // Get the generic type arguments
        var parameterType = lambda.Parameters[0].Type;
        var returnType = lambda.ReturnType;

        // Use reflection to call the generic BuildSelector method
        var buildSelectorMethod = Evaluator.GetType()
            .GetMethod(nameof(Core.IExpressionEvaluator.BuildSelector))
            ?.MakeGenericMethod(parameterType, returnType);

        if (buildSelectorMethod is not null)
        {
            // Invoke the method
            var compiledSelector = buildSelectorMethod.Invoke(Evaluator, [lambda]);

            if (compiledSelector is Delegate selector)
            {
                _projectionSelector = selector;
            }
        }
    }

    /// <summary>
    /// Completes the visit and updates the execution plan with collected projection information.
    /// </summary>
    public void Complete()
    {
        if (_projectionPaths.Count > 0)
        {
            Plan.ProjectionPropertyPaths = [.. _projectionPaths];
        }

        if (_projectionSelector is not null)
        {
            Plan.ProjectionSelector = _projectionSelector;
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

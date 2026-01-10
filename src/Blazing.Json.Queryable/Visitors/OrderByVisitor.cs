using Blazing.Json.Queryable.Core;
using System.Linq.Expressions;

namespace Blazing.Json.Queryable.Visitors;

/// <summary>
/// Expression visitor that handles OrderBy, OrderByDescending, ThenBy, and ThenByDescending clauses.
/// Extracts sort property paths and directions from ordering expressions and updates the query execution plan.
/// </summary>
public sealed class OrderByVisitor(QueryExecutionPlan plan, IExpressionEvaluator evaluator)
    : JsonExpressionVisitor(plan, evaluator)
{
    private readonly List<ReadOnlyMemory<char>> _sortPaths = [];
    private readonly List<bool> _sortDirections = []; // true = ascending, false = descending

    /// <summary>
    /// Visits an OrderBy/ThenBy clause and extracts the sort property and direction.
    /// </summary>
    /// <param name="node">The OrderBy method call expression.</param>
    /// <returns>The visited expression.</returns>
    protected override Expression VisitOrderBy(MethodCallExpression node)
    {
        ArgumentNullException.ThrowIfNull(node);

        string methodName = node.Method.Name;

        // Determine sort direction based on method name
        bool ascending = methodName is LinqMethodNames.OrderBy or LinqMethodNames.ThenBy;

        // Extract the lambda expression (key selector)
        var lambda = GetLambdaArgument(node, argumentIndex: 1);

        if (lambda is not null)
        {
            // Extract property name from the lambda
            var propertyName = GetPropertyName(lambda);

            if (!string.IsNullOrEmpty(propertyName))
            {
                // Store property path as ReadOnlyMemory<char>
                _sortPaths.Add(propertyName.AsMemory());
                _sortDirections.Add(ascending);
            }
            else
            {
                // Handle complex key selectors
                var memberVisitor = new MemberExpressionCollector();
                memberVisitor.Visit(lambda.Body);

                // For now, use the first member found
                // TODO: Handle compound key selectors in future phases
                if (memberVisitor.MemberNames.Count > 0)
                {
                    _sortPaths.Add(memberVisitor.MemberNames[0].AsMemory());
                    _sortDirections.Add(ascending);
                }
            }
        }

        // Continue visiting the source expression
        if (node.Arguments.Count > 0)
        {
            Visit(node.Arguments[0]);
        }

        return node;
    }

    /// <summary>
    /// Completes the visit and updates the execution plan with collected sorting information.
    /// </summary>
    public void Complete()
    {
        if (_sortPaths.Count > 0)
        {
            // Reverse the arrays because we collected them in reverse order (bottom-up tree traversal)
            _sortPaths.Reverse();
            _sortDirections.Reverse();
            
            Plan.SortPropertyPaths = [.. _sortPaths];
            Plan.SortDirections = [.. _sortDirections];
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

using System.Linq.Expressions;
using Blazing.Json.Queryable.Core;

namespace Blazing.Json.Queryable.Visitors;

/// <summary>
/// Provides an <see cref="ExpressionVisitor"/> implementation for analyzing advanced LINQ operations
/// such as aggregations, set operations, grouping, joins, element access, quantifiers, sequence operations,
/// partitioning, and conversions, and populates a <see cref="QueryExecutionPlan"/> accordingly.
/// </summary>
internal sealed class AdvancedLinqOperationsVisitor : ExpressionVisitor
{
    private readonly QueryExecutionPlan _plan;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedLinqOperationsVisitor"/> class.
    /// </summary>
    /// <param name="plan">The <see cref="QueryExecutionPlan"/> to populate with LINQ operation details.</param>
    public AdvancedLinqOperationsVisitor(QueryExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        _plan = plan;
    }

    /// <summary>
    /// Visits a <see cref="MethodCallExpression"/> and processes recognized LINQ operations.
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <returns>The original node after processing.</returns>
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Skip if this method call is inside a lambda expression body
        // We only want to handle top-level LINQ operations, not operations nested inside selectors
        // This prevents incorrectly detecting .Average() inside a GroupBy result selector as a top-level aggregation
        
        switch (node.Method.Name)
        {
            // Filtering Operations
            case LinqMethodNames.OfType:
                HandleOfType(node);
                break;

            case LinqMethodNames.Cast:
                HandleCast(node);
                break;

            // Projection Operations
            case LinqMethodNames.SelectMany:
                HandleSelectMany(node);
                break;

            // Sorting Operations
            case LinqMethodNames.Order:
            case LinqMethodNames.OrderDescending:
                HandleOrder(node);
                break;

            case LinqMethodNames.Reverse:
                _plan.Reverse = true;
                break;

            // Aggregation Operations
            case LinqMethodNames.Sum:
            case LinqMethodNames.Average:
            case LinqMethodNames.Min:
            case LinqMethodNames.Max:
            case LinqMethodNames.MinBy:
            case LinqMethodNames.MaxBy:
            case LinqMethodNames.Aggregate:
                // Only handle if this is a top-level LINQ call, not nested in a lambda
                if (IsLinqExtensionMethod(node))
                {
                    HandleAggregation(node);
                }
                break;

            // Set Operations
            case LinqMethodNames.Distinct:
            case LinqMethodNames.DistinctBy:
            case LinqMethodNames.Union:
            case LinqMethodNames.UnionBy:
            case LinqMethodNames.Intersect:
            case LinqMethodNames.IntersectBy:
            case LinqMethodNames.Except:
            case LinqMethodNames.ExceptBy:
                HandleSetOperation(node);
                break;

            // Grouping Operations
            case LinqMethodNames.GroupBy:
                HandleGroupBy(node);
                // Don't visit arguments for GroupBy - the selectors are compiled separately
                return node;

            // Join Operations
            case LinqMethodNames.Join:
            case LinqMethodNames.GroupJoin:
                HandleJoin(node);
                break;

            // Element Operations
            case LinqMethodNames.ElementAt:
            case LinqMethodNames.ElementAtOrDefault:
                HandleElementAt(node);
                break;

            case LinqMethodNames.Last:
            case LinqMethodNames.LastOrDefault:
                HandleLast(node);
                break;

            // Quantifier Operations
            case LinqMethodNames.All:
                HandleQuantifier(node);
                break;

            case LinqMethodNames.Contains:
                // CRITICAL FIX: Only handle Contains if it's a LINQ extension method (on IEnumerable)
                // NOT if it's an instance method like string.Contains()
                if (IsLinqExtensionMethod(node))
                {
                    HandleQuantifier(node);
                }
                // Otherwise it's an instance method (like string.Contains) - leave it alone
                break;

            case LinqMethodNames.SequenceEqual:
                HandleQuantifier(node);
                break;

            // Sequence Operations
            case LinqMethodNames.Append:
                _plan.SequenceOperationType = SequenceOperationType.Append;
                _plan.AppendElement = GetConstantValue(node.Arguments[1]);
                break;

            case LinqMethodNames.Prepend:
                _plan.SequenceOperationType = SequenceOperationType.Prepend;
                _plan.PrependElement = GetConstantValue(node.Arguments[1]);
                break;

            case LinqMethodNames.Concat:
                _plan.SequenceOperationType = SequenceOperationType.Concat;
                _plan.SecondSequence = GetConstantValue(node.Arguments[1]);
                break;

            case LinqMethodNames.Zip:
                HandleZip(node);
                break;

            // Partitioning Operations
            case LinqMethodNames.TakeLast:
                _plan.PartitioningType = PartitioningType.TakeLast;
                _plan.PartitionCount = GetConstantInt(node.Arguments[1]);
                break;

            case LinqMethodNames.SkipLast:
                _plan.PartitioningType = PartitioningType.SkipLast;
                _plan.PartitionCount = GetConstantInt(node.Arguments[1]);
                break;

            case LinqMethodNames.TakeWhile:
                _plan.PartitioningType = PartitioningType.TakeWhile;
                HandleTakeSkipWhile(node);
                break;

            case LinqMethodNames.SkipWhile:
                _plan.PartitioningType = PartitioningType.SkipWhile;
                HandleTakeSkipWhile(node);
                break;

            case LinqMethodNames.Chunk:
                _plan.PartitioningType = PartitioningType.Chunk;
                _plan.ChunkSize = GetConstantInt(node.Arguments[1]);
                break;

            // Conversion Operations - These are terminal, handled by Execute, not here
            // DefaultIfEmpty
            case LinqMethodNames.DefaultIfEmpty:
                HandleDefaultIfEmpty(node);
                break;
        }

        // Visit source (first argument) only, skip lambda arguments
        if (node.Arguments.Count > 0)
        {
            Visit(node.Arguments[0]);
        }
        
        return node;
    }

    /// <summary>
    /// Determines if a method call is a LINQ extension method (static on Queryable/Enumerable)
    /// or an instance method (such as string.Contains).
    /// </summary>
    /// <param name="node">The method call expression.</param>
    /// <returns><c>true</c> if the method is a LINQ extension method; otherwise, <c>false</c>.</returns>
    private static bool IsLinqExtensionMethod(MethodCallExpression node)
    {
        // Extension methods are static methods where the first parameter is 'this'
        // Instance methods are not static
        if (!node.Method.IsStatic)
        {
            return false; // Instance method like string.Contains()
        }

        // Check if it's from System.Linq.Queryable or System.Linq.Enumerable
        var declaringType = node.Method.DeclaringType;
        return declaringType == typeof(System.Linq.Queryable) || 
               declaringType == typeof(Enumerable);
    }

    /// <summary>
    /// Handles the OfType LINQ operation and sets the type filter in the execution plan.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    private void HandleOfType(MethodCallExpression node)
    {
        if (node.Method.IsGenericMethod)
        {
            var typeArguments = node.Method.GetGenericArguments();
            if (typeArguments.Length > 0)
            {
                _plan.TypeFilter = typeArguments[0];
            }
        }
    }

    /// <summary>
    /// Handles the Cast LINQ operation and sets the result type in the execution plan.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    private void HandleCast(MethodCallExpression node)
    {
        if (node.Method.IsGenericMethod)
        {
            var typeArguments = node.Method.GetGenericArguments();
            if (typeArguments.Length > 0)
            {
                _plan.ResultType = typeArguments[0];
            }
        }
    }

    /// <summary>
    /// Handles the SelectMany LINQ operation and sets selectors in the execution plan.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    private void HandleSelectMany(MethodCallExpression node)
    {
        if (node.Arguments.Count < 2)
        {
            return; // Insufficient arguments for SelectMany
        }

        var collectionSelector = StripQuotes(node.Arguments[1]);
        if (collectionSelector is LambdaExpression collectionLambda)
        {
            _plan.SelectManyCollectionSelector = collectionLambda.Compile();
        }

        if (node.Arguments.Count >= 3)
        {
            var resultSelector = StripQuotes(node.Arguments[2]);
            if (resultSelector is LambdaExpression resultLambda)
            {
                _plan.SelectManyResultSelector = resultLambda.Compile();
            }
        }
    }

    /// <summary>
    /// Handles Order and OrderDescending LINQ operations and sets sort properties in the execution plan.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    private void HandleOrder(MethodCallExpression node)
    {
        bool descending = node.Method.Name == LinqMethodNames.OrderDescending;
        
        // For Order/OrderDescending, there's no key selector - sort by the element itself
        // We'll need to handle this differently in the executor
        _plan.SortPropertyPaths = [ReadOnlyMemory<char>.Empty];
        _plan.SortDirections = [!descending];
    }

    /// <summary>
    /// Handles aggregation LINQ operations and sets aggregation details in the execution plan.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    private void HandleAggregation(MethodCallExpression node)
    {
        _plan.AggregationType = node.Method.Name switch
        {
            LinqMethodNames.Sum => AggregationType.Sum,
            LinqMethodNames.Average => AggregationType.Average,
            LinqMethodNames.Min => AggregationType.Min,
            LinqMethodNames.Max => AggregationType.Max,
            LinqMethodNames.MinBy => AggregationType.MinBy,
            LinqMethodNames.MaxBy => AggregationType.MaxBy,
            LinqMethodNames.Aggregate => AggregationType.Aggregate,
            _ => throw new InvalidOperationException($"Unknown aggregation: {node.Method.Name}")
        };

        // Extract selector if present
        if (node.Arguments.Count >= 2)
        {
            var selector = StripQuotes(node.Arguments[1]);
            
            if (_plan.AggregationType == AggregationType.Aggregate)
            {
                // Aggregate has seed as first argument, func as second
                _plan.AggregateSeed = GetConstantValue(node.Arguments[1]);
                
                if (node.Arguments.Count >= 3)
                {
                    var func = StripQuotes(node.Arguments[2]);
                    if (func is LambdaExpression funcLambda)
                    {
                        _plan.AggregateFunc = funcLambda.Compile();
                    }
                }
                
                if (node.Arguments.Count >= 4)
                {
                    var resultSelector = StripQuotes(node.Arguments[3]);
                    if (resultSelector is LambdaExpression resultLambda)
                    {
                        _plan.AggregateResultSelector = resultLambda.Compile();
                    }
                }
            }
            else if (_plan.AggregationType is AggregationType.MinBy or AggregationType.MaxBy)
            {
                if (selector is LambdaExpression keySelectorLambda)
                {
                    _plan.KeySelector = keySelectorLambda.Compile();
                }
            }
            else
            {
                if (selector is LambdaExpression selectorLambda)
                {
                    _plan.AggregationSelector = selectorLambda.Compile();
                }
            }
        }
    }

    /// <summary>
    /// Handles set operation LINQ methods and sets set operation details in the execution plan.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    private void HandleSetOperation(MethodCallExpression node)
    {
        _plan.SetOperationType = node.Method.Name switch
        {
            LinqMethodNames.Distinct => SetOperationType.Distinct,
            LinqMethodNames.DistinctBy => SetOperationType.DistinctBy,
            LinqMethodNames.Union => SetOperationType.Union,
            LinqMethodNames.UnionBy => SetOperationType.UnionBy,
            LinqMethodNames.Intersect => SetOperationType.Intersect,
            LinqMethodNames.IntersectBy => SetOperationType.IntersectBy,
            LinqMethodNames.Except => SetOperationType.Except,
            LinqMethodNames.ExceptBy => SetOperationType.ExceptBy,
            _ => throw new InvalidOperationException($"Unknown set operation: {node.Method.Name}")
        };

        // Extract second sequence for binary operations
        if (node.Method.Name is LinqMethodNames.Union or LinqMethodNames.UnionBy or 
            LinqMethodNames.Intersect or LinqMethodNames.IntersectBy or 
            LinqMethodNames.Except or LinqMethodNames.ExceptBy)
        {
            if (node.Arguments.Count >= 2)
            {
                _plan.SecondSequence = GetConstantValue(node.Arguments[1]);
            }
        }

        // Extract key selector for *By operations
        if (node.Method.Name.EndsWith("By", StringComparison.Ordinal))
        {
            int keySelectorIndex = node.Method.Name is LinqMethodNames.UnionBy or 
                                   LinqMethodNames.IntersectBy or 
                                   LinqMethodNames.ExceptBy ? 2 : 1;
            
            if (node.Arguments.Count > keySelectorIndex)
            {
                var keySelector = StripQuotes(node.Arguments[keySelectorIndex]);
                if (keySelector is LambdaExpression keySelectorLambda)
                {
                    _plan.KeySelector = keySelectorLambda.Compile();
                }
            }
        }

        // Extract comparer if present (last argument)
        if (node.Arguments.Count > 1)
        {
            var lastArg = node.Arguments[^1];
            if (lastArg.Type.IsGenericType && 
                lastArg.Type.GetGenericTypeDefinition() == typeof(IEqualityComparer<>))
            {
                var comparer = GetConstantValue(lastArg);
                if (node.Method.Name.EndsWith("By", StringComparison.Ordinal))
                {
                    _plan.KeyComparer = comparer;
                }
                else
                {
                    _plan.Comparer = comparer;
                }
            }
        }
    }

    /// <summary>
    /// Handles GroupBy LINQ operation and sets grouping details in the execution plan.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    private void HandleGroupBy(MethodCallExpression node)
    {
        if (node.Arguments.Count < 2)
        {
            return; // No keySelector - invalid GroupBy, skip
        }

        // Argument 1: keySelector (always present)
        var keySelector = StripQuotes(node.Arguments[1]);
        if (keySelector is LambdaExpression keySelectorLambda)
        {
            _plan.KeySelector = keySelectorLambda.Compile();
        }
        else
        {
            return; // Invalid keySelector, skip
        }

        // Argument 2 (if present): could be elementSelector, resultSelector, or comparer
        if (node.Arguments.Count >= 3)
        {
            var secondArg = StripQuotes(node.Arguments[2]);
            
            if (secondArg is LambdaExpression secondLambda)
            {
                // Determine if it's element selector or result selector by parameter count
                // Element selector: Func<TSource, TElement> (1 param)
                // Result selector: Func<TKey, IEnumerable<TSource or TElement>, TResult> (2 params)
                if (secondLambda.Parameters.Count == 2)
                {
                    // It's a result selector (no element selector)
                    _plan.GroupByResultSelector = secondLambda.Compile();
                }
                else if (secondLambda.Parameters.Count == 1)
                {
                    // It's an element selector (1 param)
                    _plan.ElementSelector = secondLambda.Compile();
                }
                // else: invalid parameter count, skip
            }
            else if (secondArg.Type.IsGenericType && 
                     secondArg.Type.GetGenericTypeDefinition() == typeof(IEqualityComparer<>))
            {
                // It's a comparer
                _plan.KeyComparer = GetConstantValue(secondArg);
            }
        }

        // Argument 3 (if present): could be resultSelector or comparer
        if (node.Arguments.Count >= 4)
        {
            var thirdArg = StripQuotes(node.Arguments[3]);
            
            if (thirdArg is LambdaExpression thirdLambda)
            {
                // When we have 4 arguments with lambdas, arg3 is the result selector
                // It should have 2 parameters: Func<TKey, IEnumerable<TElement>, TResult>
                if (thirdLambda.Parameters.Count == 2)
                {
                    _plan.GroupByResultSelector = thirdLambda.Compile();
                }
                // Otherwise it might be an element selector if arg2 was a comparer
                else if (thirdLambda.Parameters.Count == 1 && _plan.ElementSelector == null)
                {
                    _plan.ElementSelector = thirdLambda.Compile();
                }
                // else: invalid parameter count, skip
            }
            else if (thirdArg.Type.IsGenericType && 
                     thirdArg.Type.GetGenericTypeDefinition() == typeof(IEqualityComparer<>))
            {
                // It's a comparer
                _plan.KeyComparer = GetConstantValue(thirdArg);
            }
        }

        // Argument 4 (if present): must be comparer (when we have key + element + result selectors)
        if (node.Arguments.Count >= 5)
        {
            var fourthArg = node.Arguments[4];
            if (fourthArg.Type.IsGenericType && 
                fourthArg.Type.GetGenericTypeDefinition() == typeof(IEqualityComparer<>))
            {
                _plan.KeyComparer = GetConstantValue(fourthArg);
            }
        }
    }

    /// <summary>
    /// Handles Join and GroupJoin LINQ operations and sets join details in the execution plan.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    private void HandleJoin(MethodCallExpression node)
    {
        if (node.Arguments.Count < 5)
        {
            return; // Insufficient arguments for join operation
        }

        // Arguments: source, inner, outerKeySelector, innerKeySelector, resultSelector, [comparer]
        _plan.InnerSequence = GetConstantValue(node.Arguments[1]);
        
        var outerKeySelector = StripQuotes(node.Arguments[2]);
        if (outerKeySelector is LambdaExpression outerLambda)
        {
            _plan.OuterKeySelector = outerLambda.Compile();
        }

        var innerKeySelector = StripQuotes(node.Arguments[3]);
        if (innerKeySelector is LambdaExpression innerLambda)
        {
            _plan.InnerKeySelector = innerLambda.Compile();
        }

        var resultSelector = StripQuotes(node.Arguments[4]);
        if (resultSelector is LambdaExpression resultLambda)
        {
            _plan.JoinResultSelector = resultLambda.Compile();
        }

        // Check for comparer
        if (node.Arguments.Count >= 6)
        {
            _plan.KeyComparer = GetConstantValue(node.Arguments[5]);
        }
    }

    /// <summary>
    /// Handles ElementAt and ElementAtOrDefault LINQ operations and sets element index in the execution plan.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    private void HandleElementAt(MethodCallExpression node)
    {
        if (node.Arguments.Count >= 2)
        {
            var indexArg = node.Arguments[1];
            
            if (indexArg.Type == typeof(int))
            {
                _plan.ElementIndex = GetConstantInt(indexArg);
            }
            else if (indexArg.Type == typeof(Index))
            {
                var indexValue = GetConstantValue(indexArg);
                if (indexValue is Index index)
                {
                    _plan.ElementIndexFromEnd = index;
                }
            }
        }
    }

    /// <summary>
    /// Handles Last and LastOrDefault LINQ operations and sets the predicate in the execution plan.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    private void HandleLast(MethodCallExpression node)
    {
        if (node.Arguments.Count >= 2)
        {
            var predicate = StripQuotes(node.Arguments[1]);
            if (predicate is LambdaExpression predicateLambda)
            {
                _plan.LastPredicate = predicateLambda.Compile();
            }
        }
    }

    /// <summary>
    /// Handles quantifier LINQ operations (All, Contains, SequenceEqual) and sets quantifier details in the execution plan.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    private void HandleQuantifier(MethodCallExpression node)
    {
        _plan.QuantifierType = node.Method.Name switch
        {
            LinqMethodNames.All => QuantifierType.All,
            LinqMethodNames.Contains => QuantifierType.Contains,
            LinqMethodNames.SequenceEqual => QuantifierType.SequenceEqual,
            _ => throw new InvalidOperationException($"Unknown quantifier: {node.Method.Name}")
        };

        switch (node.Method.Name)
        {
            case LinqMethodNames.All:
                if (node.Arguments.Count >= 2)
                {
                    var predicate = StripQuotes(node.Arguments[1]);
                    if (predicate is LambdaExpression predicateLambda)
                    {
                        _plan.Predicates = [predicateLambda.Compile()];
                    }
                }
                break;

            case LinqMethodNames.Contains:
                if (node.Arguments.Count >= 2)
                {
                    _plan.ContainsItem = GetConstantValue(node.Arguments[1]);
                }
                if (node.Arguments.Count >= 3)
                {
                    _plan.Comparer = GetConstantValue(node.Arguments[2]);
                }
                break;

            case LinqMethodNames.SequenceEqual:
                if (node.Arguments.Count >= 2)
                {
                    _plan.SecondSequence = GetConstantValue(node.Arguments[1]);
                }
                if (node.Arguments.Count >= 3)
                {
                    _plan.Comparer = GetConstantValue(node.Arguments[2]);
                }
                break;
        }
    }

    /// <summary>
    /// Handles Zip LINQ operation and sets zip details in the execution plan.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    private void HandleZip(MethodCallExpression node)
    {
        _plan.SequenceOperationType = SequenceOperationType.Zip;

        if (node.Arguments.Count >= 2)
        {
            _plan.SecondSequence = GetConstantValue(node.Arguments[1]);
        }

        if (node.Arguments.Count >= 3)
        {
            var thirdArg = StripQuotes(node.Arguments[2]);
            
            // Check if it's a selector (LambdaExpression) or a third sequence
            if (thirdArg is LambdaExpression resultLambda)
            {
                // It's a selector: Zip(second, selector)
                _plan.ZipSelector = resultLambda.Compile();
            }
            else
            {
                // It's a third sequence: Zip(second, third)
                _plan.ThirdSequence = GetConstantValue(node.Arguments[2]);
            }
        }
    }

    /// <summary>
    /// Handles TakeWhile and SkipWhile LINQ operations and sets partitioning predicates in the execution plan.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    private void HandleTakeSkipWhile(MethodCallExpression node)
    {
        if (node.Arguments.Count >= 2)
        {
            var predicate = StripQuotes(node.Arguments[1]);
            
            if (predicate is LambdaExpression lambda)
            {
                // Check if predicate has index parameter (Func<T, int, bool>)
                if (lambda.Parameters.Count == 2)
                {
                    _plan.PartitionPredicateWithIndex = lambda.Compile();
                }
                else
                {
                    _plan.Predicates = [lambda.Compile()];
                }
            }
        }
    }

    /// <summary>
    /// Handles DefaultIfEmpty LINQ operation and sets default value details in the execution plan.
    /// </summary>
    /// <param name="node">The method call expression.</param>
    private void HandleDefaultIfEmpty(MethodCallExpression node)
    {
        // DefaultIfEmpty is always present if this handler is called
        _plan.HasDefaultValue = true;
        
        // Check if a specific default value was provided
        if (node.Arguments.Count >= 2)
        {
            _plan.DefaultValue = GetConstantValue(node.Arguments[1]);
        }
        // Otherwise DefaultValue remains null, which is correct for DefaultIfEmpty()
    }

    /// <summary>
    /// Removes any <see cref="ExpressionType.Quote"/> wrappers from the given expression.
    /// </summary>
    /// <param name="expression">The expression to strip quotes from.</param>
    /// <returns>The unwrapped <see cref="Expression"/>.</returns>
    private static Expression StripQuotes(Expression expression)
    {
        while (expression.NodeType == ExpressionType.Quote)
        {
            expression = ((UnaryExpression)expression).Operand;
        }
        return expression;
    }

    /// <summary>
    /// Attempts to extract a constant value from an expression.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <returns>The constant value if available; otherwise, <c>null</c>.</returns>
    private static object? GetConstantValue(Expression expression)
    {
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        // Try to evaluate the expression
        try
        {
            var lambda = Expression.Lambda(expression);
            var compiled = lambda.Compile();
            return compiled.DynamicInvoke();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to extract a constant integer value from an expression.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <returns>The constant integer value if available; otherwise, <c>null</c>.</returns>
    private static int? GetConstantInt(Expression? expression)
    {
        if (expression == null)
            return null;

        var value = GetConstantValue(expression);
        return value as int?;
    }

    /// <summary>
    /// Completes any final processing for the visitor. This method is reserved for future use.
    /// </summary>
#pragma warning disable CA1822 // Mark members as static
    public void Complete()
    {
        // Any final processing can go here - do we still need this?
    }
#pragma warning restore CA1822
}

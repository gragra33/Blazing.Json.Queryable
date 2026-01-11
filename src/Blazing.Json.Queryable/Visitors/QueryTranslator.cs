using System.Linq.Expressions;

namespace Blazing.Json.Queryable.Visitors;

/// <summary>
/// Orchestrates the translation of LINQ expression trees into query execution plans.
/// Coordinates multiple specialized visitors (Where, Select, OrderBy, etc.) to build a complete execution plan.
/// </summary>
public sealed class QueryTranslator
{
    private readonly Core.IExpressionEvaluator _evaluator;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryTranslator"/> class.
    /// </summary>
    /// <param name="evaluator">The expression evaluator for compiling predicates and selectors.</param>
    public QueryTranslator(Core.IExpressionEvaluator evaluator)
    {
        ArgumentNullException.ThrowIfNull(evaluator);
        _evaluator = evaluator;
    }

    /// <summary>
    /// Translates a LINQ expression tree into a query execution plan.
    /// </summary>
    /// <param name="expression">The expression tree to translate.</param>
    /// <param name="sourceType">The source element type.</param>
    /// <returns>A <see cref="Core.QueryExecutionPlan"/> ready for execution.</returns>
    public Core.QueryExecutionPlan Translate(Expression expression, Type sourceType)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(sourceType);

        // Find the true source type (before any Select operations)
        var trueSourceType = FindOriginalSourceType(expression) ?? sourceType;

        // Create the execution plan
        var plan = new Core.QueryExecutionPlan
        {
            SourceType = trueSourceType,
            ResultType = sourceType // This is the final result type (after Select if present)
        };

        // Analyze the expression tree to extract operations
        AnalyzeExpression(expression, plan);

        // Extract Skip/Take values
        ExtractPagingOperations(expression, plan);

        // Validate the plan
        plan.Validate();

        return plan;
    }

    /// <summary>
    /// Finds the original source type by walking back through the expression tree to before any Select or GroupBy operations.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <returns>The original source type, or <c>null</c> if not found.</returns>
    private static Type? FindOriginalSourceType(Expression expression)
    {
        var current = expression;

        while (current is MethodCallExpression methodCall)
        {
            // For GroupBy and Select, KEEP WALKING to find the original source
            // Don't stop at these operations - go all the way back to the data source
            if (methodCall.Method.Name is Core.LinqMethodNames.GroupBy or 
                                         Core.LinqMethodNames.Select or 
                                         Core.LinqMethodNames.SelectMany)
            {
                if (methodCall.Arguments.Count > 0)
                {
                    current = methodCall.Arguments[0];
                    continue;
                }
            }
            
            // For all other operations, check the source (first argument)
            if (methodCall.Arguments.Count > 0)
            {
                current = methodCall.Arguments[0];
                continue;
            }

            break;
        }

        // Return the element type of whatever we ended up at
        return GetElementTypeFromExpression(current);
    }

    /// <summary>
    /// Analyzes the expression tree using specialized visitors to populate the execution plan.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <param name="plan">The plan to populate.</param>
    private void AnalyzeExpression(Expression expression, Core.QueryExecutionPlan plan)
    {
        // Check if query needs ordered execution (has operations after projection)
        if (NeedsOrderedExecution(expression))
        {
            // Build execution steps in order
            BuildExecutionSteps(expression, plan);
        }
        else
        {
            // Legacy path: use specialized visitors for simple queries
            // Visit Where clauses
            var whereVisitor = new WhereVisitor(plan, _evaluator);
            whereVisitor.Visit(expression);
            whereVisitor.Complete();

            // Visit Select clauses
            var selectVisitor = new SelectVisitor(plan, _evaluator);
            selectVisitor.Visit(expression);
            selectVisitor.Complete();

            // Visit OrderBy clauses
            var orderByVisitor = new OrderByVisitor(plan, _evaluator);
            orderByVisitor.Visit(expression);
            orderByVisitor.Complete();

            // Visit operations (aggregations, set operations, grouping, joins, etc.)
            var advancedLinqVisitor = new AdvancedLinqOperationsVisitor(plan);
            advancedLinqVisitor.Visit(expression);
            advancedLinqVisitor.Complete();
        }
    }

    /// <summary>
    /// Determines if the query needs ordered execution (operations after projection).
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <returns><c>true</c> if ordered execution is required; otherwise, <c>false</c>.</returns>
    private static bool NeedsOrderedExecution(Expression expression)
    {
        var analyzer = new OperationOrderAnalyzer();
        analyzer.Analyze(expression);
        return analyzer.HasOperationsAfterProjection;
    }

    /// <summary>
    /// Builds execution steps by walking the expression tree in reverse (bottom-up).
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <param name="plan">The plan to populate.</param>
    private static void BuildExecutionSteps(Expression expression, Core.QueryExecutionPlan plan)
    {
        var steps = new List<Core.ExecutionStep>();
        var current = expression;

        // Walk expression tree from outermost to innermost operation
        while (current is MethodCallExpression methodCall)
        {
            var opName = methodCall.Method.Name;
            
            // Skip terminal operations - they're handled separately
            bool isTerminalOp = opName is Core.LinqMethodNames.ToList or Core.LinqMethodNames.ToArray or 
                                         Core.LinqMethodNames.ToDictionary or Core.LinqMethodNames.ToHashSet or 
                                         Core.LinqMethodNames.ToLookup or
                                         Core.LinqMethodNames.Count or Core.LinqMethodNames.LongCount or 
                                         Core.LinqMethodNames.Any or Core.LinqMethodNames.All or 
                                         Core.LinqMethodNames.First or Core.LinqMethodNames.FirstOrDefault or
                                         Core.LinqMethodNames.Single or Core.LinqMethodNames.SingleOrDefault or 
                                         Core.LinqMethodNames.Last or Core.LinqMethodNames.LastOrDefault or
                                         Core.LinqMethodNames.ElementAt or Core.LinqMethodNames.ElementAtOrDefault;
            
            if (!isTerminalOp)
            {
                var step = CreateExecutionStep(methodCall);
                if (step is not null)
                {
                    steps.Add(step);
                }
            }

            // Move to source (first argument)
            if (methodCall.Arguments.Count > 0)
            {
                current = methodCall.Arguments[0];
            }
            else
            {
                break;
            }
        }

        // Reverse to get correct execution order (innermost to outermost)
        steps.Reverse();
        plan.ExecutionSteps = steps;
    }

    /// <summary>
    /// Creates an <see cref="Core.ExecutionStep"/> from a method call expression.
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>, or <c>null</c> if not applicable.</returns>
    private static Core.ExecutionStep? CreateExecutionStep(MethodCallExpression node)
    {
        return node.Method.Name switch
        {
            Core.LinqMethodNames.Where => CreateWhereStep(node),
            Core.LinqMethodNames.Select => CreateSelectStep(node),
            Core.LinqMethodNames.SelectMany => CreateSelectManyStep(node),
            Core.LinqMethodNames.GroupBy => CreateGroupByStep(node),
            Core.LinqMethodNames.Join or Core.LinqMethodNames.GroupJoin => CreateJoinStep(node),
            Core.LinqMethodNames.OfType => CreateOfTypeStep(node),
            Core.LinqMethodNames.OrderBy or Core.LinqMethodNames.OrderByDescending => CreateOrderByStep(node, node.Method.Name == Core.LinqMethodNames.OrderByDescending),
            Core.LinqMethodNames.ThenBy or Core.LinqMethodNames.ThenByDescending => CreateThenByStep(node, node.Method.Name == Core.LinqMethodNames.ThenByDescending),
            Core.LinqMethodNames.Reverse => new Core.ExecutionStep { OperationType = Core.OperationType.Reverse },
            Core.LinqMethodNames.Skip => CreateSkipStep(node),
            Core.LinqMethodNames.Take => CreateTakeStep(node),
            Core.LinqMethodNames.Distinct or Core.LinqMethodNames.DistinctBy or 
            Core.LinqMethodNames.Union or Core.LinqMethodNames.UnionBy or 
            Core.LinqMethodNames.Intersect or Core.LinqMethodNames.IntersectBy or 
            Core.LinqMethodNames.Except or Core.LinqMethodNames.ExceptBy 
                => CreateSetOperationStep(node),
            Core.LinqMethodNames.TakeLast or Core.LinqMethodNames.SkipLast or 
            Core.LinqMethodNames.TakeWhile or Core.LinqMethodNames.SkipWhile or 
            Core.LinqMethodNames.Chunk 
                => CreatePartitioningStep(node),
            Core.LinqMethodNames.Append or Core.LinqMethodNames.Prepend or 
            Core.LinqMethodNames.Concat or Core.LinqMethodNames.Zip 
                => CreateSequenceOperationStep(node),
            Core.LinqMethodNames.DefaultIfEmpty => CreateDefaultIfEmptyStep(node),
            _ => null
        };
    }

    /// <summary>
    /// Creates a Join or GroupJoin execution step.
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>.</returns>
    private static Core.ExecutionStep CreateJoinStep(MethodCallExpression node)
    {
        // Join/GroupJoin arguments: source, inner, outerKeySelector, innerKeySelector, resultSelector, [comparer]
        var innerSequence = GetConstantValue(node.Arguments[1]);
        var outerKeySelector = GetLambdaArgument(node, 2)?.Compile();
        var innerKeySelector = GetLambdaArgument(node, 3)?.Compile();
        var resultSelector = GetLambdaArgument(node, 4)?.Compile();
        object? comparer = node.Arguments.Count >= 6 ? GetConstantValue(node.Arguments[5]) : null;
        
        // Determine result type from the result selector
        var resultType = GetLambdaArgument(node, 4)?.ReturnType ?? GetElementTypeFromExpression(node);
        
        // Package all join data together
        var joinData = new JoinData
        {
            IsGroupJoin = node.Method.Name == Core.LinqMethodNames.GroupJoin,
            InnerSequence = innerSequence,
            OuterKeySelector = outerKeySelector,
            InnerKeySelector = innerKeySelector,
            ResultSelector = resultSelector,
            KeyComparer = comparer
        };
        
        return new Core.ExecutionStep
        {
            OperationType = Core.OperationType.Join,
            Data = joinData,
            ResultType = resultType
        };
    }

    /// <summary>
    /// Creates a GroupBy execution step.
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>.</returns>
    private static Core.ExecutionStep CreateGroupByStep(MethodCallExpression node)
    {
        var keySelector = GetLambdaArgument(node, 1)?.Compile();
        var keyType = GetLambdaArgument(node, 1)?.ReturnType ?? typeof(object);
        var sourceType = GetElementTypeFromExpression(node.Arguments[0]) ?? typeof(object);
        
        // Check for element selector (arg 2 with 1 parameter)
        Delegate? elementSelector = null;
        Type elementType = sourceType; // Default to source type
        
        if (node.Arguments.Count >= 3)
        {
            var secondLambda = GetLambdaArgument(node, 2);
            if (secondLambda is { Parameters.Count: 1 })
            {
                // This is an element selector
                elementSelector = secondLambda.Compile();
                elementType = secondLambda.ReturnType;
            }
        }
        
        var groupingType = typeof(IGrouping<,>).MakeGenericType(keyType, elementType);
        
        return new Core.ExecutionStep 
        { 
            OperationType = Core.OperationType.GroupBy,
            Delegate = keySelector,
            Data = elementSelector, // Store element selector in Data field
            ResultType = groupingType
        };
    }

    /// <summary>
    /// Creates a Where execution step.
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>.</returns>
    private static Core.ExecutionStep CreateWhereStep(MethodCallExpression node)
    {
        var lambda = GetLambdaArgument(node, 1);
        var predicate = lambda?.Compile();
        return new Core.ExecutionStep 
        { 
            OperationType = Core.OperationType.Where,
            Delegate = predicate
        };
    }

    /// <summary>
    /// Creates a Select execution step.
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>.</returns>
    private static Core.ExecutionStep CreateSelectStep(MethodCallExpression node)
    {
        var lambda = GetLambdaArgument(node, 1);
        var selector = lambda?.Compile();
        return new Core.ExecutionStep 
        { 
            OperationType = Core.OperationType.Select,
            Delegate = selector,
            ResultType = lambda?.ReturnType
        };
    }

    /// <summary>
    /// Creates a SelectMany execution step.
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>.</returns>
    private static Core.ExecutionStep CreateSelectManyStep(MethodCallExpression node)
    {
        var collectionSelector = GetLambdaArgument(node, 1)?.Compile();
        var resultSelector = node.Arguments.Count >= 3 ? GetLambdaArgument(node, 2)?.Compile() : null;
        
        return new Core.ExecutionStep 
        { 
            OperationType = Core.OperationType.SelectMany,
            Delegate = collectionSelector,
            Data = resultSelector,
            ResultType = resultSelector?.Method.ReturnType ?? GetElementTypeFromExpression(node)
        };
    }

    /// <summary>
    /// Creates an OfType execution step.
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>.</returns>
    private static Core.ExecutionStep CreateOfTypeStep(MethodCallExpression node)
    {
        var targetType = node.Method.GetGenericArguments()[0];
        return new Core.ExecutionStep 
        { 
            OperationType = Core.OperationType.OfType,
            Data = targetType,
            ResultType = targetType
        };
    }

    /// <summary>
    /// Creates an OrderBy execution step.
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <param name="descending">Indicates if the ordering is descending.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>.</returns>
    private static Core.ExecutionStep CreateOrderByStep(MethodCallExpression node, bool descending)
    {
        var lambda = GetLambdaArgument(node, 1);
        var keySelector = lambda?.Compile();
        return new Core.ExecutionStep 
        { 
            OperationType = Core.OperationType.OrderBy,
            Delegate = keySelector,
            Data = descending
        };
    }

    /// <summary>
    /// Creates a ThenBy execution step.
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <param name="descending">Indicates if the ordering is descending.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>.</returns>
    private static Core.ExecutionStep CreateThenByStep(MethodCallExpression node, bool descending)
    {
        var lambda = GetLambdaArgument(node, 1);
        var keySelector = lambda?.Compile();
        return new Core.ExecutionStep 
        { 
            OperationType = Core.OperationType.ThenBy,
            Delegate = keySelector,
            Data = descending
        };
    }

    /// <summary>
    /// Creates a Skip execution step.
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>.</returns>
    private static Core.ExecutionStep CreateSkipStep(MethodCallExpression node)
    {
        var count = GetConstantInt(node.Arguments[1]);
        return new Core.ExecutionStep 
        { 
            OperationType = Core.OperationType.Skip,
            Count = count
        };
    }

    /// <summary>
    /// Creates a Take execution step.
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>.</returns>
    private static Core.ExecutionStep CreateTakeStep(MethodCallExpression node)
    {
        var count = GetConstantInt(node.Arguments[1]);
        return new Core.ExecutionStep 
        { 
            OperationType = Core.OperationType.Take,
            Count = count
        };
    }

    /// <summary>
    /// Creates a set operation execution step (Distinct, Union, Intersect, Except).
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>.</returns>
    private static Core.ExecutionStep CreateSetOperationStep(MethodCallExpression node)
    {
        var setOpType = node.Method.Name switch
        {
            Core.LinqMethodNames.Distinct => Core.SetOperationType.Distinct,
            Core.LinqMethodNames.DistinctBy => Core.SetOperationType.DistinctBy,
            Core.LinqMethodNames.Union => Core.SetOperationType.Union,
            Core.LinqMethodNames.UnionBy => Core.SetOperationType.UnionBy,
            Core.LinqMethodNames.Intersect => Core.SetOperationType.Intersect,
            Core.LinqMethodNames.IntersectBy => Core.SetOperationType.IntersectBy,
            Core.LinqMethodNames.Except => Core.SetOperationType.Except,
            Core.LinqMethodNames.ExceptBy => Core.SetOperationType.ExceptBy,
            _ => Core.SetOperationType.Distinct
        };

        Delegate? keySelector = null;
        
        // Extract key selector for *By operations
        if (node.Method.Name.EndsWith("By"))
        {
            int keySelectorIndex = node.Method.Name is Core.LinqMethodNames.UnionBy or 
                                                       Core.LinqMethodNames.IntersectBy or 
                                                       Core.LinqMethodNames.ExceptBy ? 2 : 1;
            if (node.Arguments.Count > keySelectorIndex)
            {
                keySelector = GetLambdaArgument(node, keySelectorIndex)?.Compile();
            }
        }

        // Extract second sequence for binary operations
        object? secondSequence = null;
        if (node.Method.Name is Core.LinqMethodNames.Union or Core.LinqMethodNames.UnionBy or 
                                Core.LinqMethodNames.Intersect or Core.LinqMethodNames.IntersectBy or 
                                Core.LinqMethodNames.Except or Core.LinqMethodNames.ExceptBy)
        {
            if (node.Arguments.Count >= 2)
            {
                secondSequence = GetConstantValue(node.Arguments[1]);
            }
        }

        // Store both set op type and second sequence
        var stepData = new SetOperationData
        {
            OperationType = setOpType,
            SecondSequence = secondSequence
        };

        return new Core.ExecutionStep 
        { 
            OperationType = Core.OperationType.SetOperation,
            Delegate = keySelector,
            Data = stepData
        };
    }

    /// <summary>
    /// Creates a partitioning execution step (TakeLast, SkipLast, TakeWhile, SkipWhile, Chunk).
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>.</returns>
    private static Core.ExecutionStep CreatePartitioningStep(MethodCallExpression node)
    {
        var partType = node.Method.Name switch
        {
            Core.LinqMethodNames.TakeLast => Core.PartitioningType.TakeLast,
            Core.LinqMethodNames.SkipLast => Core.PartitioningType.SkipLast,
            Core.LinqMethodNames.TakeWhile => Core.PartitioningType.TakeWhile,
            Core.LinqMethodNames.SkipWhile => Core.PartitioningType.SkipWhile,
            _ => Core.PartitioningType.Chunk
        };

        Delegate? predicate = null;
        int? count = null;

        if (partType is Core.PartitioningType.TakeLast or Core.PartitioningType.SkipLast)
        {
            count = GetConstantInt(node.Arguments[1]);
        }
        else if (partType is Core.PartitioningType.TakeWhile or Core.PartitioningType.SkipWhile)
        {
            predicate = GetLambdaArgument(node, 1)?.Compile();
        }
        else
        {
            count = GetConstantInt(node.Arguments[1]);
        }

        return new Core.ExecutionStep 
        { 
            OperationType = Core.OperationType.Partitioning,
            Delegate = predicate,
            Data = partType,
            Count = count,
            ResultType = partType == Core.PartitioningType.Chunk 
                ? typeof(IEnumerable<>).MakeGenericType(GetElementTypeFromExpression(node.Arguments[0])!) 
                : null
        };
    }

    /// <summary>
    /// Creates a sequence operation execution step (Append, Prepend, Concat, Zip).
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>.</returns>
    private static Core.ExecutionStep CreateSequenceOperationStep(MethodCallExpression node)
    {
        var seqOpType = node.Method.Name switch
        {
            Core.LinqMethodNames.Append => Core.SequenceOperationType.Append,
            Core.LinqMethodNames.Prepend => Core.SequenceOperationType.Prepend,
            Core.LinqMethodNames.Concat => Core.SequenceOperationType.Concat,
            Core.LinqMethodNames.Zip => Core.SequenceOperationType.Zip,
            _ => Core.SequenceOperationType.Append
        };

        object? data = GetConstantValue(node.Arguments[1]);
        Delegate? zipSelector = null;

        if (seqOpType == Core.SequenceOperationType.Zip && node.Arguments.Count >= 3)
        {
            zipSelector = GetLambdaArgument(node, 2)?.Compile();
        }

        var stepData = new SequenceOperationData
        {
            OperationType = seqOpType,
            SecondSequence = data,
            ZipSelector = zipSelector
        };

        return new Core.ExecutionStep 
        { 
            OperationType = Core.OperationType.SequenceOperation,
            Data = stepData
        };
    }

    /// <summary>
    /// Creates a DefaultIfEmpty execution step.
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <returns>The created <see cref="Core.ExecutionStep"/>.</returns>
    private static Core.ExecutionStep CreateDefaultIfEmptyStep(MethodCallExpression node)
    {
        object? defaultValue = node.Arguments.Count >= 2 ? GetConstantValue(node.Arguments[1]) : null;
        return new Core.ExecutionStep 
        { 
            OperationType = Core.OperationType.DefaultIfEmpty,
            Data = defaultValue
        };
    }

    /// <summary>
    /// Gets the lambda argument from a method call expression at the specified index.
    /// </summary>
    /// <param name="node">The method call expression node.</param>
    /// <param name="index">The argument index.</param>
    /// <returns>The lambda expression, or <c>null</c> if not found.</returns>
    private static LambdaExpression? GetLambdaArgument(MethodCallExpression node, int index)
    {
        if (node.Arguments.Count <= index) return null;
        
        var arg = node.Arguments[index];
        while (arg.NodeType == ExpressionType.Quote)
        {
            arg = ((UnaryExpression)arg).Operand;
        }
        
        return arg as LambdaExpression;
    }

    /// <summary>
    /// Gets the constant value from an expression, evaluating if necessary.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <returns>The constant value, or <c>null</c> if not found.</returns>
    private static object? GetConstantValue(Expression expression)
    {
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

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
    /// Gets the constant integer value from an expression.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <returns>The integer value, or <c>null</c> if not found.</returns>
    private static int? GetConstantInt(Expression expression)
    {
        var value = GetConstantValue(expression);
        return value as int?;
    }

    /// <summary>
    /// Extracts the element type from a queryable expression.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <returns>The element type, or <c>null</c> if not determined.</returns>
    private static Type? GetElementTypeFromExpression(Expression expression)
    {
        var type = expression.Type;

        // Handle IQueryable<T>
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();

            if (genericTypeDef == typeof(IQueryable<>) || genericTypeDef == typeof(IEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        // Check interfaces
        var queryableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryable<>));

        return queryableInterface?.GetGenericArguments()[0];
    }

    /// <summary>
    /// Extracts the element type from a queryable expression (static version for external use).
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <returns>The element type, or <c>null</c> if not determined.</returns>
    public static Type? GetElementType(Expression expression)
    {
        var type = expression.Type;

        // Handle IQueryable<T>
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();

            if (genericTypeDef == typeof(IQueryable<>) || genericTypeDef == typeof(IEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        // Check interfaces
        var queryableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryable<>));

        return queryableInterface?.GetGenericArguments()[0];
    }

    /// <summary>
    /// Helper class to track operation data for set operations.
    /// </summary>
    public sealed class SetOperationData
    {
        /// <summary>Type of set operation.</summary>
        public Core.SetOperationType OperationType { get; set; }
        
        /// <summary>Second sequence for binary set operations.</summary>
        public object? SecondSequence { get; set; }
    }

    /// <summary>
    /// Helper class to track operation data for sequence operations.
    /// </summary>
    public sealed class SequenceOperationData
    {
        /// <summary>Type of sequence operation.</summary>
        public Core.SequenceOperationType OperationType { get; set; }
        
        /// <summary>Second sequence for sequence operations.</summary>
        public object? SecondSequence { get; set; }
        
        /// <summary>Zip result selector function.</summary>
        public Delegate? ZipSelector { get; set; }
    }

    /// <summary>
    /// Helper class to track operation data for join operations.
    /// </summary>
    public sealed class JoinData
    {
        /// <summary>Indicates if this is a GroupJoin (true) or Join (false).</summary>
        public bool IsGroupJoin { get; set; }
        
        /// <summary>Inner sequence to join with.</summary>
        public object? InnerSequence { get; set; }
        
        /// <summary>Outer key selector function.</summary>
        public Delegate? OuterKeySelector { get; set; }
        
        /// <summary>Inner key selector function.</summary>
        public Delegate? InnerKeySelector { get; set; }
        
        /// <summary>Result selector function.</summary>
        public Delegate? ResultSelector { get; set; }
        
        /// <summary>Optional key comparer.</summary>
        public object? KeyComparer { get; set; }
    }
    
    /// <summary>
    /// Helper visitor to extract Skip and Take values from an expression tree.
    /// </summary>
    private sealed class PagingExtractionVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Gets the value for Skip operation, if present.
        /// </summary>
        public int? SkipValue { get; private set; }
        /// <summary>
        /// Gets the value for Take operation, if present.
        /// </summary>
        public int? TakeValue { get; private set; }

        /// <summary>
        /// Visits method call expressions to extract Skip and Take values.
        /// </summary>
        /// <param name="node">The method call expression node.</param>
        /// <returns>The visited expression.</returns>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case Core.LinqMethodNames.Skip:
                    SkipValue = GetConstantInt(node.Arguments[1]);
                    break;

                case Core.LinqMethodNames.Take:
                    TakeValue = GetConstantInt(node.Arguments[1]);
                    break;
            }

            return base.VisitMethodCall(node);
        }

        private static int? GetConstantInt(Expression expression)
        {
            if (expression is ConstantExpression { Value: int value })
            {
                return value;
            }

            return null;
        }
    }
    
    /// <summary>
    /// Extracts Skip and Take operations from the expression tree and populates the plan.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <param name="plan">The plan to populate.</param>
    private static void ExtractPagingOperations(Expression expression, Core.QueryExecutionPlan plan)
    {
        var pagingVisitor = new PagingExtractionVisitor();
        pagingVisitor.Visit(expression);

        plan.Skip = pagingVisitor.SkipValue;
        plan.Take = pagingVisitor.TakeValue;
    }

    /// <summary>
    /// Analyzer to detect if query has operations after projection.
    /// Walks the expression tree top-down to detect operation order.
    /// </summary>
    private sealed class OperationOrderAnalyzer : ExpressionVisitor
    {
        /// <summary>
        /// Gets a value indicating whether the query has operations after projection.
        /// </summary>
        public bool HasOperationsAfterProjection { get; private set; }

        /// <summary>
        /// Analyzes the expression by building operation stack from top to bottom.
        /// </summary>
        /// <param name="expression">The expression to analyze.</param>
        public void Analyze(Expression expression)
        {
            // Build operation list from outermost to innermost
            var operations = new List<string>();
            var current = expression;

            while (current is MethodCallExpression methodCall)
            {
                var opName = methodCall.Method.Name;
                
                // Skip terminal operations that don't affect ordering
                if (opName is not (Core.LinqMethodNames.ToList or Core.LinqMethodNames.ToArray or 
                                   Core.LinqMethodNames.ToDictionary or Core.LinqMethodNames.ToHashSet or 
                                   Core.LinqMethodNames.ToLookup or 
                                   Core.LinqMethodNames.Count or Core.LinqMethodNames.LongCount or 
                                   Core.LinqMethodNames.Any or Core.LinqMethodNames.All or 
                                   Core.LinqMethodNames.First or Core.LinqMethodNames.FirstOrDefault or
                                   Core.LinqMethodNames.Single or Core.LinqMethodNames.SingleOrDefault or 
                                   Core.LinqMethodNames.Last or Core.LinqMethodNames.LastOrDefault or
                                   Core.LinqMethodNames.ElementAt or Core.LinqMethodNames.ElementAtOrDefault))
                {
                    operations.Add(opName);
                }
                
                current = methodCall.Arguments.Count > 0 ? methodCall.Arguments[0] : null;
                if (current is null) break;
            }

            // Check if there's GroupBy followed by Select - this MUST use ExecutionSteps
            int groupByIndex = -1;
            int selectIndex = -1;
            
            for (int i = 0; i < operations.Count; i++)
            {
                if (operations[i] == Core.LinqMethodNames.GroupBy)
                {
                    if (groupByIndex == -1) groupByIndex = i;
                }
                else if (operations[i] == Core.LinqMethodNames.Select)
                {
                    if (selectIndex == -1) selectIndex = i;
                }
            }
            
            // If Select comes before GroupBy in the list (after GroupBy in the query)
            // This means: .GroupBy(...).Select(...) which needs ExecutionSteps
            if (groupByIndex >= 0 && selectIndex >= 0 && selectIndex < groupByIndex)
            {
                HasOperationsAfterProjection = true;
                return;
            }

            // Check if OrderBy comes after Join
            // Join changes the result type, so OrderBy must work on the new type via ExecutionSteps
            int joinIndex = -1;
            int orderByIndex = -1;
            
            for (int i = 0; i < operations.Count; i++)
            {
                if (operations[i] is Core.LinqMethodNames.Join or Core.LinqMethodNames.GroupJoin)
                {
                    if (joinIndex == -1) joinIndex = i;
                }
                else if (operations[i] is Core.LinqMethodNames.OrderBy or Core.LinqMethodNames.OrderByDescending or 
                                    Core.LinqMethodNames.ThenBy or Core.LinqMethodNames.ThenByDescending)
                {
                    if (orderByIndex == -1) orderByIndex = i;
                }
            }
            
            // If OrderBy comes before Join in the list (after Join in the query)
            if (orderByIndex >= 0 && joinIndex >= 0 && orderByIndex < joinIndex)
            {
                HasOperationsAfterProjection = true;
                return;
            }

            // Check if there's a projection followed by other operations
            int projectionIndex = -1;
            for (int i = 0; i < operations.Count; i++)
            {
                if (operations[i] is Core.LinqMethodNames.Select or Core.LinqMethodNames.SelectMany)
                {
                    projectionIndex = i;
                    break;
                }
            }

            // Check for operations that need ordered execution AFTER projection
            if (projectionIndex > 0)
            {
                // There are operations BEFORE the projection in the list
                // (which means AFTER in the query chain since we build the list outermost-to-innermost)
                for (int i = 0; i < projectionIndex; i++)
                {
                    if (operations[i] is Core.LinqMethodNames.Distinct or Core.LinqMethodNames.DistinctBy or 
                                    Core.LinqMethodNames.Union or Core.LinqMethodNames.UnionBy or 
                                    Core.LinqMethodNames.Intersect or Core.LinqMethodNames.IntersectBy or 
                                    Core.LinqMethodNames.Except or Core.LinqMethodNames.ExceptBy or
                                    Core.LinqMethodNames.TakeLast or Core.LinqMethodNames.SkipLast or 
                                    Core.LinqMethodNames.TakeWhile or Core.LinqMethodNames.SkipWhile)
                    {
                        HasOperationsAfterProjection = true;
                        return;
                    }
                }
            }

            // NEW: Check if TakeWhile/SkipWhile comes after OrderBy
            // This requires ordered execution because the legacy path applies partitioning before ordering
            int orderByIndex2 = -1;
            int partitioningIndex = -1;
            
            for (int i = 0; i < operations.Count; i++)
            {
                if (operations[i] is Core.LinqMethodNames.OrderBy or Core.LinqMethodNames.OrderByDescending or 
                                Core.LinqMethodNames.ThenBy or Core.LinqMethodNames.ThenByDescending)
                {
                    if (orderByIndex2 == -1) // Record first OrderBy
                    {
                        orderByIndex2 = i;
                    }
                }
                else if (operations[i] is Core.LinqMethodNames.TakeWhile or Core.LinqMethodNames.SkipWhile)
                {
                    if (partitioningIndex == -1) // Record first TakeWhile/SkipWhile
                    {
                        partitioningIndex = i;
                    }
                }
            }

            // If partitioning comes before OrderBy in the list (after OrderBy in the query)
            if (partitioningIndex >= 0 && orderByIndex2 >= 0 && partitioningIndex < orderByIndex2)
            {
                HasOperationsAfterProjection = true;
            }
        }
    }
}

using Blazing.Json.Queryable.Core;
using Blazing.Json.Queryable.Visitors;
using Blazing.Json.Queryable.Utilities;
// ReSharper disable PossibleMultipleEnumeration

namespace Blazing.Json.Queryable.Execution;

/// <summary>
/// Executes LINQ operations on materialized collections.
/// Shared by all executor implementations for consistent behavior.
/// This class contains all shared query operation logic extracted from individual executors,
/// ensuring consistent behavior across all execution strategies.
/// </summary>
internal sealed class QueryOperationExecutor
{
    /// <summary>
    /// Executes a complete query plan on a materialized source collection.
    /// This is the primary entry point for executing queries on already-materialized data.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <typeparam name="TResult">Result element type.</typeparam>
    /// <param name="source">Materialized source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Query results as <see cref="System.Collections.Generic.IEnumerable{TResult}"/>.</returns>
    internal IEnumerable<TResult> ExecuteQuery<TSource, TResult>(
        IEnumerable<TSource> source,
        QueryExecutionPlan plan)
    {
        // Use ordered execution if ExecutionSteps are present
        if (plan.ExecutionSteps is { Count: > 0 })
        {
            return ExecuteStepsTyped<TSource, TResult>(source, plan);
        }

        // LEGACY PATH: Original execution logic for simple queries
        IEnumerable<TSource> query = source;

        // Apply type filtering (OfType)
        if (plan.TypeFilter is not null)
        {
            query = query.Where(item => item is not null && item.GetType() == plan.TypeFilter);
        }

        // Apply SelectMany (flattening) - this changes the element type, so predicates should not be applied before it
        if (plan.SelectManyCollectionSelector is not null)
        {
            return ApplySelectMany<TSource, TResult>(query, plan);
        }

        // Handle Zip operation - changes result type to tuples
        if (plan.SequenceOperationType == SequenceOperationType.Zip)
        {
            return ApplyZipWithResult<TSource, TResult>(query, plan);
        }

        // Apply Where filters (only if we're NOT doing SelectMany, which handles its own filtering)
        if (plan.Predicates is { Length: > 0 })
        {
            // Check if this predicate is used for partitioning - if so, don't apply it here
            bool isPartitioningPredicate = plan.PartitioningType is PartitioningType.TakeWhile or PartitioningType.SkipWhile;
            
            if (!isPartitioningPredicate)
            {
                foreach (var predicate in plan.Predicates)
                {
                    var typedPredicate = (Func<TSource, bool>)predicate;
                    query = query.Where(typedPredicate);
                }
            }
        }

        // Normal path: operations on source type, then projection
        if (plan.SetOperationType.HasValue)
        {
            query = ApplySetOperation(query, plan);
        }

        // Handle GroupBy - check KeySelector presence (but NOT if it's a *By set operation)
        if (plan is { KeySelector: not null, SetOperationType: null })
        {
            // GroupBy with result selector - projects groups
            if (plan.GroupByResultSelector is not null)
            {
                return ExecuteGroupBy<TSource, TResult>(query, plan);
            }

            // GroupBy without result selector - returns IGrouping<TKey, TElement>
            return ExecuteGroupByWithoutResultSelector<TSource, TResult>(query, plan);
        }

        if (plan.InnerSequence is not null)
        {
            return ExecuteJoin<TSource, TResult>(query, plan);
        }

        // Handle non-Zip sequence operations (Append, Prepend, Concat)
        if (plan.SequenceOperationType.HasValue && plan.SequenceOperationType is not SequenceOperationType.Zip)
        {
            query = ApplySequenceOperation(query, plan);
        }

        if (plan.PartitioningType.HasValue)
        {
            return ApplyPartitioning<TSource, TResult>(query, plan);
        }

        // Apply OrderBy sorting
        if (plan.SortPropertyPaths is { Length: > 0 })
        {
            IOrderedEnumerable<TSource>? ordered = null;

            for (int i = 0; i < plan.SortPropertyPaths.Length; i++)
            {
                // Convert span to string once for use in lambda
                string propertyPath = plan.GetSortPath(i).ToString();
                bool ascending = plan.SortDirections?[i] ?? true;

                if (i == 0)
                {
                    ordered = ascending
                        ? query.OrderBy(item => GetPropertyValue(item, propertyPath.AsSpan()))
                        : query.OrderByDescending(item => GetPropertyValue(item, propertyPath.AsSpan()));
                }
                else
                {
                    ordered = ascending
                        ? ordered!.ThenBy(item => GetPropertyValue(item, propertyPath.AsSpan()))
                        : ordered!.ThenByDescending(item => GetPropertyValue(item, propertyPath.AsSpan()));
                }
            }

            query = ordered!;
        }

        // Apply Reverse
        if (plan.Reverse)
        {
            query = query.Reverse();
        }

        // Apply Skip/Take paging
        if (plan.Skip is > 0)
        {
            query = query.Skip(plan.Skip.Value);
        }

        if (plan.Take.HasValue)
        {
            query = query.Take(plan.Take.Value);
        }

        // Handle DefaultIfEmpty - must be applied BEFORE projection
        if (plan.HasDefaultValue)
        {
            if (plan.DefaultValue is not null)
            {
                var defaultValue = (TSource)plan.DefaultValue;
                query = query.DefaultIfEmpty(defaultValue);
            }
            else
            {
                query = query.DefaultIfEmpty()!;
            }
        }

        // Apply projection (Select) if present
        if (plan.ProjectionSelector is not null)
        {
            var selector = (Func<TSource, TResult>)plan.ProjectionSelector;
            return query.Select(selector);
        }

        // No projection - TSource must equal TResult
        return query.Cast<TResult>();
    }

    #region Projection Operations

    /// <summary>
    /// Applies SelectMany (flattening) operation to the source collection.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <typeparam name="TResult">Result element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Flattened and projected results.</returns>
    private static IEnumerable<TResult> ApplySelectMany<TSource, TResult>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        var collectionSelector = plan.SelectManyCollectionSelector!;

        IEnumerable<TResult> result;

        if (plan.SelectManyResultSelector is not null)
        {
            var resultSelector = plan.SelectManyResultSelector;
            
            // Determine collection element type from the collection selector's return type
            Type collectionElementType;
            var collectionReturnType = collectionSelector.Method.ReturnType;
            if (collectionReturnType.IsGenericType && collectionReturnType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                collectionElementType = collectionReturnType.GetGenericArguments()[0];
            }
            else
            {
                var enumerableInterface = collectionReturnType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                collectionElementType = enumerableInterface?.GetGenericArguments()[0] ?? typeof(object);
            }
            
            try
            {
                var intermediate = typeof(Enumerable).GetMethods()
                    .Where(m => m.Name == nameof(Enumerable.SelectMany) && 
                               m.GetParameters().Length == 3 &&
                               m.GetGenericArguments().Length == 3)
                    .Select(m => m.MakeGenericMethod(typeof(TSource), collectionElementType, typeof(TResult)))
                    .FirstOrDefault(m => {
                        try {
                            var ps = m.GetParameters();
                            return ps[1].ParameterType.IsInstanceOfType(collectionSelector) &&
                                   ps[2].ParameterType.IsAssignableFrom(resultSelector.GetType());
                        } catch { return false; }
                    });
                
                if (intermediate is not null)
                {
                    result = (IEnumerable<TResult>)intermediate.Invoke(null, [source, collectionSelector, resultSelector
                    ])!;
                }
                else
                {
                    var selectManyMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == nameof(Enumerable.SelectMany) && 
                                   m.GetParameters().Length == 3 &&
                                   m.GetGenericArguments().Length == 3 &&
                                   !m.GetParameters()[1].ParameterType.GetGenericArguments()[0].Name.Contains("Int32"));
                    
                    result = (IEnumerable<TResult>)selectManyMethod.MakeGenericMethod(typeof(TSource), collectionElementType, typeof(TResult))
                        .Invoke(null, [source, collectionSelector, resultSelector])!;
                }
            }
            catch
            {
                var selectManyMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == nameof(Enumerable.SelectMany) && 
                               m.GetParameters().Length == 3 &&
                               m.GetGenericArguments().Length == 3 &&
                               !m.GetParameters()[1].ParameterType.GetGenericArguments()[0].Name.Contains("Int32"));
                
                result = (IEnumerable<TResult>)selectManyMethod.MakeGenericMethod(typeof(TSource), collectionElementType, typeof(TResult))
                    .Invoke(null, [source, collectionSelector, resultSelector])!;
            }
        }
        else
        {
            Type flattenedElementType = typeof(TResult);
            var collectionReturnType = collectionSelector.Method.ReturnType;
            if (collectionReturnType.IsGenericType && collectionReturnType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                flattenedElementType = collectionReturnType.GetGenericArguments()[0];
            }
            else
            {
                var enumerableInterface = collectionReturnType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                if (enumerableInterface is not null)
                {
                    flattenedElementType = enumerableInterface.GetGenericArguments()[0];
                }
            }
            
            try
            {
                var intermediate = typeof(Enumerable).GetMethods()
                    .Where(m => m.Name == nameof(Enumerable.SelectMany) && 
                               m.GetParameters().Length == 2 &&
                               m.GetGenericArguments().Length == 2)
                    .Select(m => m.MakeGenericMethod(typeof(TSource), flattenedElementType))
                    .FirstOrDefault(m => {
                        try {
                            return m.GetParameters()[1].ParameterType.IsInstanceOfType(collectionSelector);
                        } catch { return false; }
                    });
                
                if (intermediate is not null)
                {
                    var flattenedResult = (System.Collections.IEnumerable)intermediate.Invoke(null, [source, collectionSelector
                    ])!;
                    result = flattenedResult.Cast<TResult>();
                }
                else
                {
                    var selectManyMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == nameof(Enumerable.SelectMany) && 
                                   m.GetParameters().Length == 2 &&
                                   m.GetGenericArguments().Length == 2 &&
                                   !m.GetParameters()[1].ParameterType.GetGenericArguments()[0].Name.Contains("Int32"));
                    
                    var fallbackResult = (System.Collections.IEnumerable)selectManyMethod.MakeGenericMethod(typeof(TSource), flattenedElementType)
                        .Invoke(null, [source, collectionSelector])!;
                    result = fallbackResult.Cast<TResult>();
                }
            }
            catch
            {
                var selectManyMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == nameof(Enumerable.SelectMany) && 
                               m.GetParameters().Length == 2 &&
                               m.GetGenericArguments().Length == 2 &&
                               !m.GetParameters()[1].ParameterType.GetGenericArguments()[0].Name.Contains("Int32"));
                
                var fallbackResult = (System.Collections.IEnumerable)selectManyMethod.MakeGenericMethod(typeof(TSource), flattenedElementType)
                    .Invoke(null, [source, collectionSelector])!;
                result = fallbackResult.Cast<TResult>();
            }
        }

        // Apply predicates that come AFTER SelectMany (they operate on TResult, the flattened type)
        if (plan.Predicates is { Length: > 0 })
        {
            bool isPartitioningPredicate = plan.PartitioningType is PartitioningType.TakeWhile or PartitioningType.SkipWhile;
            
            if (!isPartitioningPredicate)
            {
                foreach (var predicate in plan.Predicates)
                {
                    var typedPredicate = (Func<TResult, bool>)predicate;
                    result = result.Where(typedPredicate);
                }
            }
        }

        return result;
    }

    #endregion

    #region Aggregation Operations

    /// <summary>
    /// Executes an aggregation operation (Count, Sum, Min, Max, etc.) on the source collection.
    /// </summary>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>The aggregation result.</returns>
    internal static object ExecuteAggregation(IEnumerable<object> source, QueryExecutionPlan plan)
    {
        if (plan.Predicates is not null)
        {
            foreach (var predicate in plan.Predicates)
            {
                source = source.Where(item => (bool)predicate.DynamicInvoke(item)!);
            }
        }

        object? result = plan.AggregationType switch
        {
            AggregationType.Count => source.Count(),
            AggregationType.LongCount => source.LongCount(),
            AggregationType.Sum => ExecuteSum(source, plan.AggregationSelector!),
            AggregationType.Average => ExecuteAverage(source, plan.AggregationSelector!),
            AggregationType.Min => ExecuteMin(source, plan.AggregationSelector!),
            AggregationType.Max => ExecuteMax(source, plan.AggregationSelector!),
            AggregationType.MinBy => source.MinBy(item => plan.KeySelector!.DynamicInvoke(item)),
            AggregationType.MaxBy => source.MaxBy(item => plan.KeySelector!.DynamicInvoke(item)),
            AggregationType.Aggregate => ExecuteAggregate(source, plan),
            _ => throw new NotSupportedException($"Aggregation type {plan.AggregationType} not supported")
        };

        return result!;
    }

    /// <summary>
    /// Executes a Sum aggregation using the provided selector.
    /// </summary>
    /// <param name="source">Source collection.</param>
    /// <param name="selector">Selector delegate.</param>
    /// <returns>The sum result.</returns>
    private static object ExecuteSum(IEnumerable<object> source, Delegate selector)
    {
        var returnType = selector.Method.ReturnType;
        
        if (returnType == typeof(int) || returnType == typeof(int?))
            return source.Sum(item => (int?)selector.DynamicInvoke(item) ?? 0);
        if (returnType == typeof(long) || returnType == typeof(long?))
            return source.Sum(item => (long?)selector.DynamicInvoke(item) ?? 0);
        if (returnType == typeof(double) || returnType == typeof(double?))
            return source.Sum(item => (double?)selector.DynamicInvoke(item) ?? 0);
        if (returnType == typeof(decimal) || returnType == typeof(decimal?))
            return source.Sum(item => (decimal?)selector.DynamicInvoke(item) ?? 0);
        if (returnType == typeof(float) || returnType == typeof(float?))
            return source.Sum(item => (float?)selector.DynamicInvoke(item) ?? 0);
        
        throw new NotSupportedException($"Sum not supported for type {returnType}");
    }

    /// <summary>
    /// Executes an Average aggregation using the provided selector.
    /// </summary>
    /// <param name="source">Source collection.</param>
    /// <param name="selector">Selector delegate.</param>
    /// <returns>The average result.</returns>
    private static object ExecuteAverage(IEnumerable<object> source, Delegate selector)
    {
        var returnType = selector.Method.ReturnType;

        if (returnType == typeof(int) || returnType == typeof(int?))
            return source.Average(item => (int?)selector.DynamicInvoke(item) ?? 0);
        if (returnType == typeof(long) || returnType == typeof(long?))
            return source.Average(item => (long?)selector.DynamicInvoke(item) ?? 0);
        if (returnType == typeof(double) || returnType == typeof(double?))
            return source.Average(item => (double?)selector.DynamicInvoke(item) ?? 0);
        if (returnType == typeof(decimal) || returnType == typeof(decimal?))
            return source.Average(item => (decimal?)selector.DynamicInvoke(item) ?? 0);
        if (returnType == typeof(float) || returnType == typeof(float?))
            return source.Average(item => (float?)selector.DynamicInvoke(item) ?? 0);
            
        throw new NotSupportedException($"Average not supported for type {returnType}");
    }

    /// <summary>
    /// Executes a Min aggregation using the provided selector.
    /// </summary>
    /// <param name="source">Source collection.</param>
    /// <param name="selector">Selector delegate.</param>
    /// <returns>The minimum value.</returns>
    private static object ExecuteMin(IEnumerable<object> source, Delegate selector)
    {
        return source.Min(item => selector.DynamicInvoke(item)) ?? throw new InvalidOperationException("Sequence contains no elements");
    }

    /// <summary>
    /// Executes a Max aggregation using the provided selector.
    /// </summary>
    /// <param name="source">Source collection.</param>
    /// <param name="selector">Selector delegate.</param>
    /// <returns>The maximum value.</returns>
    private static object ExecuteMax(IEnumerable<object> source, Delegate selector)
    {
        return source.Max(item => selector.DynamicInvoke(item)) ?? throw new InvalidOperationException("Sequence contains no elements");
    }

    /// <summary>
    /// Executes a custom Aggregate operation using the provided plan.
    /// </summary>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>The aggregation result.</returns>
    private static object ExecuteAggregate(IEnumerable<object> source, QueryExecutionPlan plan
    )
    {
        var seed = plan.AggregateSeed;
        var func = plan.AggregateFunc!;
        var resultSelector = plan.AggregateResultSelector;

        object accumulated = seed!;
        foreach (var item in source)
        {
            accumulated = func.DynamicInvoke(accumulated, item)!;
        }

        return resultSelector is not null ? resultSelector.DynamicInvoke(accumulated)! : accumulated;
    }

    #endregion

    #region Set Operations

    /// <summary>
    /// Applies a set operation (Distinct, Union, Intersect, Except, etc.) to the source collection.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Resulting collection after set operation.</returns>
    private static IEnumerable<TSource> ApplySetOperation<TSource>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        return plan.SetOperationType switch
        {
            SetOperationType.Distinct => plan.Comparer is not null
                ? source.Distinct((IEqualityComparer<TSource>)plan.Comparer)
                : source.Distinct(),

            SetOperationType.DistinctBy => ApplyDistinctBy(source, plan),

            SetOperationType.Union => plan is { Comparer: not null, SecondSequence: not null }
                ? source.Union((IEnumerable<TSource>)plan.SecondSequence, (IEqualityComparer<TSource>)plan.Comparer)
                : plan.SecondSequence is not null
                    ? source.Union((IEnumerable<TSource>)plan.SecondSequence)
                    : source,

            SetOperationType.UnionBy => ApplyUnionBy(source, plan),

            SetOperationType.Intersect => plan is { Comparer: not null, SecondSequence: not null }
                ? source.Intersect((IEnumerable<TSource>)plan.SecondSequence, (IEqualityComparer<TSource>)plan.Comparer)
                : plan.SecondSequence is not null
                    ? source.Intersect((IEnumerable<TSource>)plan.SecondSequence)
                    : source,

            SetOperationType.IntersectBy => ApplyIntersectBy(source, plan),

            SetOperationType.Except => plan is { Comparer: not null, SecondSequence: not null }
                ? source.Except((IEnumerable<TSource>)plan.SecondSequence, (IEqualityComparer<TSource>)plan.Comparer)
                : plan.SecondSequence is not null
                    ? source.Except((IEnumerable<TSource>)plan.SecondSequence)
                    : source,

            SetOperationType.ExceptBy => ApplyExceptBy(source, plan),

            _ => throw new NotSupportedException($"Set operation {plan.SetOperationType} not supported")
        };
    }

    /// <summary>
    /// Applies DistinctBy operation to the source collection.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Distinct elements by key.</returns>
    private static IEnumerable<TSource> ApplyDistinctBy<TSource>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        var keySelector = plan.KeySelector!;
        return plan.KeyComparer is not null
            ? source.DistinctBy(item => keySelector.DynamicInvoke(item), (IEqualityComparer<object?>)plan.KeyComparer)
            : source.DistinctBy(item => keySelector.DynamicInvoke(item));
    }

    /// <summary>
    /// Applies UnionBy operation to the source collection.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Union of elements by key.</returns>
    private static IEnumerable<TSource> ApplyUnionBy<TSource>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        var keySelector = plan.KeySelector!;
        var second = (IEnumerable<TSource>)plan.SecondSequence!;
        return plan.KeyComparer is not null
            ? source.UnionBy(second, item => keySelector.DynamicInvoke(item), (IEqualityComparer<object?>)plan.KeyComparer)
            : source.UnionBy(second, item => keySelector.DynamicInvoke(item));
    }

    /// <summary>
    /// Applies IntersectBy operation to the source collection.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Intersection of elements by key.</returns>
    private static IEnumerable<TSource> ApplyIntersectBy<TSource>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        var keySelector = plan.KeySelector!;
        var secondKeys = ((System.Collections.IEnumerable)plan.SecondSequence!).Cast<object?>();
        return plan.KeyComparer is not null
            ? source.IntersectBy(secondKeys, item => keySelector.DynamicInvoke(item), (IEqualityComparer<object?>)plan.KeyComparer)
            : source.IntersectBy(secondKeys, item => keySelector.DynamicInvoke(item));
    }

    /// <summary>
    /// Applies ExceptBy operation to the source collection.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Elements except those by key.</returns>
    private static IEnumerable<TSource> ApplyExceptBy<TSource>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        var keySelector = plan.KeySelector!;
        var secondKeys = ((System.Collections.IEnumerable)plan.SecondSequence!).Cast<object?>();
        return plan.KeyComparer is not null
            ? source.ExceptBy(secondKeys, item => keySelector.DynamicInvoke(item), (IEqualityComparer<object?>)plan.KeyComparer)
            : source.ExceptBy(secondKeys, item => keySelector.DynamicInvoke(item));
    }

    #endregion

    #region Grouping Operations

    /// <summary>
    /// Executes a GroupBy operation with a result selector.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <typeparam name="TResult">Result element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Grouped results with projection.</returns>
    private static IEnumerable<TResult> ExecuteGroupBy<TSource, TResult>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        var keySelector = plan.KeySelector!;
        var keyType = keySelector.Method.ReturnType;
        
        var elementSelector = plan.ElementSelector;
        var elementType = elementSelector?.Method.ReturnType ?? typeof(TSource);
        
        var resultSelector = plan.GroupByResultSelector!;
        var resultType = typeof(TResult);

        // Use reflection to call the properly typed GroupBy method
        // This is necessary because DynamicInvoke loses type information
        System.Reflection.MethodInfo groupByMethod;
        
        if (elementSelector is not null)
        {
            // GroupBy<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, [comparer])
            groupByMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == LinqMethodNames.GroupBy && 
                           m.GetGenericArguments().Length == 4 &&
                           m.GetParameters().Length == (plan.KeyComparer is not null ? 5 : 4));
            
            var typedMethod = groupByMethod.MakeGenericMethod(typeof(TSource), keyType, elementType, resultType);
            
            var result = plan.KeyComparer is not null
                ? typedMethod.Invoke(null, [source, keySelector, elementSelector, resultSelector, plan.KeyComparer])!
                : typedMethod.Invoke(null, [source, keySelector, elementSelector, resultSelector])!;
            return (IEnumerable<TResult>)result;
        }
        else
        {
            // GroupBy<TSource, TKey, TResult>(source, keySelector, resultSelector, [comparer])
            // CRITICAL: The result selector must have signature Func<TKey, IEnumerable<TSource>, TResult>
            // NOT Func<TSource, TResult> (which would be an element selector)
            
            // Find the correct overload by checking the parameter types explicitly
            var candidateMethods = typeof(Enumerable).GetMethods()
                .Where(m => m.Name == LinqMethodNames.GroupBy && 
                           m.GetGenericArguments().Length == 3 &&
                           m.GetParameters().Length == (plan.KeyComparer is not null ? 4 : 3))
                .ToList();
            
            // The correct overload has parameter 2 (resultSelector) with signature:
            // Func<TKey, IEnumerable<TElement>, TResult> (which is Func<,,>)
            // vs the element selector overload which has:
            // Func<TSource, TElement> (which is Func<,>)
            groupByMethod = candidateMethods.FirstOrDefault(m =>
            {
                var param2Type = m.GetParameters()[2].ParameterType;
                
                // Check if this is a Func with 2 input parameters (Func<TKey, IEnumerable<TElement>, TResult>)
                // The generic type definition for Func<,,> has 3 type arguments (2 inputs + 1 return)
                return param2Type.IsGenericType && 
                       param2Type.GetGenericTypeDefinition() == typeof(Func<,,>);
            }) ?? throw new InvalidOperationException(
                "Could not find GroupBy<TSource, TKey, TResult> overload with result selector. " +
                "Expected Func<TKey, IEnumerable<TSource>, TResult> as third parameter.");
            
            var typedMethod = groupByMethod.MakeGenericMethod(typeof(TSource), keyType, resultType);
            
            var result = plan.KeyComparer is not null
                ? typedMethod.Invoke(null, [source, keySelector, resultSelector, plan.KeyComparer])!
                : typedMethod.Invoke(null, [source, keySelector, resultSelector])!;
            return (IEnumerable<TResult>)result;
        }
    }

    /// <summary>
    /// Executes a GroupBy operation without a result selector.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <typeparam name="TResult">Result element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Grouped results as <see cref="IGrouping{TKey, TElement}"/>.</returns>
    private static IEnumerable<TResult> ExecuteGroupByWithoutResultSelector<TSource, TResult>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        var keySelector = plan.KeySelector!;
        var keyType = keySelector.Method.ReturnType;
        
        var elementSelector = plan.ElementSelector;
        var elementType = elementSelector?.Method.ReturnType ?? typeof(TSource);

        System.Reflection.MethodInfo groupByMethod;
        
        if (elementSelector is not null)
        {
            groupByMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == LinqMethodNames.GroupBy && 
                           m.GetGenericArguments().Length == 3 &&
                           m.GetParameters().Length == (plan.KeyComparer is not null ? 4 : 3));
            groupByMethod = groupByMethod.MakeGenericMethod(typeof(TSource), keyType, elementType);
            
            var groups = plan.KeyComparer is not null
                ? groupByMethod.Invoke(null, [source, keySelector, elementSelector, plan.KeyComparer])!
                : groupByMethod.Invoke(null, [source, keySelector, elementSelector])!;
            return ((System.Collections.IEnumerable)groups).Cast<TResult>();
        }
        else
        {
            groupByMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == LinqMethodNames.GroupBy && 
                           m.GetGenericArguments().Length == 2 &&
                           m.GetParameters().Length == (plan.KeyComparer is not null ? 3 : 2));
            groupByMethod = groupByMethod.MakeGenericMethod(typeof(TSource), keyType);
            
            var groups = plan.KeyComparer is not null
                ? groupByMethod.Invoke(null, [source, keySelector, plan.KeyComparer])!
                : groupByMethod.Invoke(null, [source, keySelector])!;
            return ((System.Collections.IEnumerable)groups).Cast<TResult>();
        }
    }

    #endregion

    #region Join Operations

    /// <summary>
    /// Executes a join operation (Join or GroupJoin) on the source collection.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <typeparam name="TResult">Result element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Joined results.</returns>
    private IEnumerable<TResult> ExecuteJoin<TSource, TResult>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        var outerKeySelector = plan.OuterKeySelector!;
        var innerKeySelector = plan.InnerKeySelector!;
        var resultSelector = plan.JoinResultSelector!;

        if (plan.InnerSequence == null)
            throw new InvalidOperationException("Inner sequence is required for join operations");

        var innerEnumerable = plan.InnerSequence as System.Collections.IEnumerable
            ?? throw new InvalidOperationException("Inner sequence must be enumerable");

        // Use shared helper for Join vs GroupJoin detection
        bool isGroupJoin = JoinDetectionHelper.IsGroupJoin(resultSelector);

        var methodName = isGroupJoin ? nameof(Enumerable.GroupJoin) : nameof(Enumerable.Join);
        var joinMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == methodName && m.GetGenericArguments().Length == 4);

        var innerType = plan.InnerSequence.GetType().GetGenericArguments()[0];
        var keyType = outerKeySelector.Method.ReturnType;

        var typedJoinMethod = joinMethod.MakeGenericMethod(typeof(TSource), innerType, keyType, typeof(TResult));

        var invokeParams = plan.KeyComparer is not null
            ? new[] { source, innerEnumerable, outerKeySelector, innerKeySelector, resultSelector, plan.KeyComparer }
            : [source, innerEnumerable, outerKeySelector, innerKeySelector, resultSelector];

        return (IEnumerable<TResult>)typedJoinMethod.Invoke(null, invokeParams)!;
    }

    #endregion

    #region Quantifier Operations

    /// <summary>
    /// Executes a quantifier operation (All, Any, Contains, SequenceEqual) on the source collection.
    /// </summary>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>True if the quantifier condition is met; otherwise, false.</returns>
    internal static bool ExecuteQuantifier(IEnumerable<object> source, QueryExecutionPlan plan)
    {
        bool result = plan.QuantifierType switch
        {
            QuantifierType.All => source.All(item => (bool)plan.Predicates![0].DynamicInvoke(item)!),
            QuantifierType.Any => plan.Predicates is { Length: > 0 }
                ? source.Any(item => (bool)plan.Predicates[0].DynamicInvoke(item)!)
                : source.Any(),
            QuantifierType.Contains => plan.Comparer is not null
                ? source.Contains(plan.ContainsItem!, (IEqualityComparer<object>)plan.Comparer)
                : source.Contains(plan.ContainsItem!),
            QuantifierType.SequenceEqual => plan.Comparer is not null
                ? source.SequenceEqual((IEnumerable<object>)plan.SecondSequence!, (IEqualityComparer<object>)plan.Comparer)
                : source.SequenceEqual((IEnumerable<object>)plan.SecondSequence!),
            _ => throw new NotSupportedException($"Quantifier type {plan.QuantifierType} not supported")
        };

        return result;
    }

    #endregion

    #region Sequence Operations

    /// <summary>
    /// Applies a sequence operation (Append, Prepend, Concat) to the source collection.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Resulting collection after sequence operation.</returns>
    private static IEnumerable<TSource> ApplySequenceOperation<TSource>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        return plan.SequenceOperationType switch
        {
            SequenceOperationType.Append => source.Append((TSource)plan.AppendElement!),
            SequenceOperationType.Prepend => source.Prepend((TSource)plan.PrependElement!),
            SequenceOperationType.Concat => source.Concat((IEnumerable<TSource>)plan.SecondSequence!),
            _ => throw new NotSupportedException($"Sequence operation {plan.SequenceOperationType} not supported")
        };
    }

    /// <summary>
    /// Applies a Zip operation with a result selector or multiple sequences.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <typeparam name="TResult">Result element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Resulting zipped collection.</returns>
    private static IEnumerable<TResult> ApplyZipWithResult<TSource, TResult>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        if (plan.ZipSelector is not null)
        {
            // Zip with result selector: Zip<TFirst,TSecond,TResult>(first, second, selector)
            var zipMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == LinqMethodNames.Zip && 
                           m.GetParameters().Length == 3 && 
                           m.GetGenericArguments().Length == 3 &&
                           m.GetParameters()[2].ParameterType.Name.Contains("Func"));
            
            var secondType = plan.SecondSequence!.GetType().GetGenericArguments()[0];
            var resultType = plan.ZipSelector.Method.ReturnType;
            
            var typedMethod = zipMethod.MakeGenericMethod(typeof(TSource), secondType, resultType);
            return (IEnumerable<TResult>)typedMethod.Invoke(null, [source, plan.SecondSequence, plan.ZipSelector])!;
        }
        else if (plan.ThirdSequence is not null)
        {
            // Zip with three sequences (returns tuples): Zip<TFirst,TSecond,TThird>(first, second, third)
            var zipMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == LinqMethodNames.Zip && 
                           m.GetParameters().Length == 3 && 
                           m.GetGenericArguments().Length == 3 &&
                           !m.GetParameters()[2].ParameterType.Name.Contains("Func"));
            
            var secondType = plan.SecondSequence!.GetType().GetGenericArguments()[0];
            var thirdType = plan.ThirdSequence.GetType().GetGenericArguments()[0];
            
            var typedMethod = zipMethod.MakeGenericMethod(typeof(TSource), secondType, thirdType);
            var result = typedMethod.Invoke(null, [source, plan.SecondSequence, plan.ThirdSequence])!;
            return ((System.Collections.IEnumerable)result).Cast<TResult>();
        }
        else
        {
            // Zip with two sequences (returns tuples): Zip<TFirst,TSecond>(first, second)
            var zipMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == LinqMethodNames.Zip && 
                           m.GetParameters().Length == 2 && 
                           m.GetGenericArguments().Length == 2);
            
            var secondType = plan.SecondSequence!.GetType().GetGenericArguments()[0];
            
            var typedMethod = zipMethod.MakeGenericMethod(typeof(TSource), secondType);
            var result = typedMethod.Invoke(null, [source, plan.SecondSequence])!;
            return ((System.Collections.IEnumerable)result).Cast<TResult>();
        }
    }

    #endregion

    #region Partitioning Operations

    /// <summary>
    /// Applies a partitioning operation (TakeLast, SkipLast, TakeWhile, SkipWhile, Chunk) to the source collection.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <typeparam name="TResult">Result element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Partitioned results.</returns>
    private static IEnumerable<TResult> ApplyPartitioning<TSource, TResult>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        if (plan.PartitioningType == PartitioningType.Chunk)
        {
            var chunkMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == nameof(Enumerable.Chunk) && m.GetParameters().Length == 2);
            var typedMethod = chunkMethod.MakeGenericMethod(typeof(TSource));
            var chunks = (IEnumerable<TSource[]>)typedMethod.Invoke(null, [source, plan.ChunkSize!])!;
            return chunks.Cast<TResult>();
        }
        
        IEnumerable<TSource> result = plan.PartitioningType switch
        {
            PartitioningType.TakeLast => source.TakeLast(plan.PartitionCount ?? 0),
            PartitioningType.SkipLast => source.SkipLast(plan.PartitionCount ?? 0),
            PartitioningType.TakeWhile => ApplyTakeWhile(source, plan),
            PartitioningType.SkipWhile => ApplySkipWhile(source, plan),
            _ => throw new NotSupportedException($"Partitioning type {plan.PartitioningType} not supported")
        };

        return result.Cast<TResult>();
    }

    /// <summary>
    /// Applies TakeWhile operation to the source collection.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Elements taken while the predicate is true.</returns>
    private static IEnumerable<TSource> ApplyTakeWhile<TSource>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        if (plan.PartitionPredicateWithIndex is not null)
        {
            var predicate = (Func<TSource, int, bool>)plan.PartitionPredicateWithIndex;
            return source.TakeWhile(predicate);
        }
        else if (plan.Predicates is { Length: > 0 })
        {
            var predicate = (Func<TSource, bool>)plan.Predicates[0];
            return source.TakeWhile(predicate);
        }
        
        return source;
    }

    /// <summary>
    /// Applies SkipWhile operation to the source collection.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Elements skipped while the predicate is true.</returns>
    private static IEnumerable<TSource> ApplySkipWhile<TSource>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        if (plan.PartitionPredicateWithIndex is not null)
        {
            var predicate = (Func<TSource, int, bool>)plan.PartitionPredicateWithIndex;
            return source.SkipWhile(predicate);
        }
        else if (plan.Predicates is { Length: > 0 })
        {
            var predicate = (Func<TSource, bool>)plan.Predicates[0];
            return source.SkipWhile(predicate);
        }
        
        return source;
    }

    #endregion

    #region Conversion Operations

    /// <summary>
    /// Executes a conversion operation (ToArray, ToList, ToDictionary, ToHashSet, ToLookup) on the source collection.
    /// </summary>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>The conversion result.</returns>
    internal static object ExecuteConversion(IEnumerable<object> source, QueryExecutionPlan plan)
    {
        if (plan.Predicates is not null)
        {
            foreach (var predicate in plan.Predicates)
            {
                source = source.Where(item => (bool)predicate.DynamicInvoke(item)!);
            }
        }

        object result = plan.ConversionType switch
        {
            ConversionType.ToArray => source.ToArray(),
            ConversionType.ToList => source.ToList(),
            ConversionType.ToDictionary => ExecuteToDictionary(source, plan),
            ConversionType.ToHashSet => plan.Comparer is not null
                ? source.ToHashSet((IEqualityComparer<object>)plan.Comparer)
                : [.. source],
            ConversionType.ToLookup => ExecuteToLookup(source, plan),
            _ => throw new NotSupportedException($"Conversion type {plan.ConversionType} not supported")
        };

        return result;
    }

    /// <summary>
    /// Executes a ToDictionary conversion on the source collection.
    /// </summary>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>A dictionary of key-value pairs.</returns>
    private static Dictionary<object, object> ExecuteToDictionary(IEnumerable<object> source, QueryExecutionPlan plan)
    {
        var keySelector = plan.KeySelector!;
        var elementSelector = plan.ElementSelector;

        if (elementSelector is not null)
        {
            return plan.KeyComparer is not null
                ? source.ToDictionary(
                    item => keySelector.DynamicInvoke(item)!,
                    item => elementSelector.DynamicInvoke(item)!,
                    (IEqualityComparer<object>)plan.KeyComparer)
                : source.ToDictionary(
                    item => keySelector.DynamicInvoke(item)!,
                    item => elementSelector.DynamicInvoke(item)!);
        }
        else
        {
            return plan.KeyComparer is not null
                ? source.ToDictionary(
                    item => keySelector.DynamicInvoke(item)!,
                    (IEqualityComparer<object>)plan.KeyComparer)
                : source.ToDictionary(item => keySelector.DynamicInvoke(item)!);
        }
    }

    /// <summary>
    /// Executes a ToLookup conversion on the source collection.
    /// </summary>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>A lookup of key-value pairs.</returns>
    private static object ExecuteToLookup(IEnumerable<object> source, QueryExecutionPlan plan)
    {
        var keySelector = plan.KeySelector!;
        var elementSelector = plan.ElementSelector;

        if (elementSelector is not null)
        {
            return plan.KeyComparer is not null
                ? source.ToLookup(
                    item => keySelector.DynamicInvoke(item)!,
                    item => elementSelector.DynamicInvoke(item)!,
                    (IEqualityComparer<object>)plan.KeyComparer)
                : source.ToLookup(
                    item => keySelector.DynamicInvoke(item)!,
                    item => elementSelector.DynamicInvoke(item)!);
        }
        else
        {
            return plan.KeyComparer is not null
                ? source.ToLookup(
                    item => keySelector.DynamicInvoke(item)!,
                    (IEqualityComparer<object>)plan.KeyComparer)
                : source.ToLookup(item => keySelector.DynamicInvoke(item)!);
        }
    }

    #endregion

    #region ExecutionSteps Support

    /// <summary>
    /// Executes a sequence of execution steps on the source collection, supporting advanced query plans.
    /// </summary>
    /// <typeparam name="TSource">Source element type.</typeparam>
    /// <typeparam name="TResult">Result element type.</typeparam>
    /// <param name="source">Source collection.</param>
    /// <param name="plan">Query execution plan.</param>
    /// <returns>Query results after applying all execution steps.</returns>
    private static IEnumerable<TResult> ExecuteStepsTyped<TSource, TResult>(IEnumerable<TSource> source, QueryExecutionPlan plan)
    {
        object query = source;
        Type currentType = typeof(TSource);

        for (int i = 0; i < plan.ExecutionSteps!.Count; i++)
        {
            var step = plan.ExecutionSteps[i];
            
            query = ExecuteStep(query, currentType, step, out var newType);
            currentType = newType ?? currentType;
        }

        if (query is IEnumerable<TResult> typedResult)
        {
            return typedResult;
        }

        if (currentType == typeof(TResult))
        {
            return (IEnumerable<TResult>)query;
        }

        var castMethod = typeof(Enumerable).GetMethod("Cast")!.MakeGenericMethod(typeof(TResult));
        return (IEnumerable<TResult>)castMethod.Invoke(null, [query])!;
    }

    /// <summary>
    /// Executes a single execution step on the current query object.
    /// </summary>
    /// <param name="query">Current query object.</param>
    /// <param name="currentType">Current element type.</param>
    /// <param name="step">Execution step to apply.</param>
    /// <param name="newType">Outputs the new element type if changed.</param>
    /// <returns>The query object after applying the step.</returns>
    private static object ExecuteStep(object query, Type currentType, ExecutionStep step, out Type? newType)
    {
        newType = step.ResultType;

        return step.OperationType switch
        {
            OperationType.Where => ExecuteWhereStep(query, currentType, step),
            
            OperationType.Select => UpdateTypeAndExecute(out newType, step.ResultType, 
                () => ExecuteSelectStep(query, currentType, step)),
            
            OperationType.SelectMany => UpdateTypeAndExecute(out newType, step.ResultType,
                () => ExecuteSelectManyStep(query, currentType, step)),
            
            OperationType.GroupBy => UpdateTypeAndExecute(out newType, step.ResultType,
                () => ExecuteGroupByStep(query, currentType, step)),
            
            OperationType.Join => UpdateTypeAndExecute(out newType, step.ResultType,
                () => ExecuteJoinStep(query, currentType, step)),
            
            OperationType.OfType => UpdateTypeAndExecute(out newType, step.Data as Type,
                () => ExecuteOfTypeStep(query, step)),
            
            OperationType.OrderBy => ExecuteOrderByStep(query, currentType, step),
            OperationType.ThenBy => ExecuteThenByStep(query, currentType, step),
            OperationType.Reverse => ExecuteReverseStep(query, currentType),
            OperationType.Skip => ExecuteSkipStep(query, currentType, step),
            OperationType.Take => ExecuteTakeStep(query, currentType, step),
            OperationType.SetOperation => ExecuteSetOperationStep(query, currentType, step),
            
            OperationType.Partitioning => step.Data is PartitioningType.Chunk
                ? UpdateTypeAndExecute(out newType, typeof(IEnumerable<>).MakeGenericType(currentType),
                    () => ExecutePartitioningStep(query, currentType, step))
                : ExecutePartitioningStep(query, currentType, step),
            
            OperationType.SequenceOperation => ExecuteSequenceOperationStep(query, currentType, step),
            OperationType.DefaultIfEmpty => ExecuteDefaultIfEmptyStep(query, currentType, step),
            _ => query
        };
        
        static object UpdateTypeAndExecute(out Type? typeOut, Type? newTypeValue, Func<object> execute)
        {
            typeOut = newTypeValue;
            return execute();
        }
    }

    private static object ExecuteGroupByStep(object query, Type sourceType, ExecutionStep step)
    {
        var keySelector = step.Delegate!;
        var keyType = keySelector.Method.ReturnType;
        
        // Check if there's an element selector in step.Data
        if (step.Data is Delegate elementSelector)
        {
            // GroupBy WITH element selector: GroupBy<TSource, TKey, TElement>(source, keySelector, elementSelector)
            var elementType = elementSelector.Method.ReturnType;
            
            var groupByMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == LinqMethodNames.GroupBy && 
                           m.GetGenericArguments().Length == 3 &&
                           m.GetParameters().Length == 3);
            var typedMethod = groupByMethod.MakeGenericMethod(sourceType, keyType, elementType);
            return typedMethod.Invoke(null, [query, keySelector, elementSelector])!;
        }
        else
        {
            // GroupBy WITHOUT element selector - just group the source
            var groupByMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == LinqMethodNames.GroupBy && 
                           m.GetGenericArguments().Length == 2 &&
                           m.GetParameters().Length == 2);
            var typedMethod = groupByMethod.MakeGenericMethod(sourceType, keyType);
            return typedMethod.Invoke(null, [query, keySelector])!;
        }
    }

    private static object ExecuteWhereStep(object query, Type elementType, ExecutionStep step)
    {
        var whereMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == LinqMethodNames.Where && m.GetParameters().Length == 2 &&
                       m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2);
        var typedMethod = whereMethod.MakeGenericMethod(elementType);
        return typedMethod.Invoke(null, [query, step.Delegate])!;
    }

    private static object ExecuteSelectStep(object query, Type sourceType, ExecutionStep step)
    {
        var resultType = step.ResultType!;
        var selectMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == LinqMethodNames.Select && m.GetParameters().Length == 2 &&
                       m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2);
        var typedMethod = selectMethod.MakeGenericMethod(sourceType, resultType);
        return typedMethod.Invoke(null, [query, step.Delegate])!;
    }

    private static object ExecuteSelectManyStep(object query, Type sourceType, ExecutionStep step)
    {
        var collectionSelector = step.Delegate!;

        Type collectionElementType;
        var collectionReturnType = collectionSelector.Method.ReturnType;
        if (collectionReturnType.IsGenericType && collectionReturnType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            collectionElementType = collectionReturnType.GetGenericArguments()[0];
        }
        else
        {
            var enumerableInterface = collectionReturnType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            collectionElementType = enumerableInterface?.GetGenericArguments()[0] ?? typeof(object);
        }

        if (step.Data is Delegate resultSelector)
        {
            var resultType = resultSelector.Method.ReturnType;
            var selectManyMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == LinqMethodNames.SelectMany && m.GetParameters().Length == 3 && m.GetGenericArguments().Length == 3);
            var typedMethod = selectManyMethod.MakeGenericMethod(sourceType, collectionElementType, resultType);
            return typedMethod.Invoke(null, [query, collectionSelector, resultSelector])!;
        }
        else
        {
            var selectManyMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == LinqMethodNames.SelectMany && m.GetParameters().Length == 2 && m.GetGenericArguments().Length == 2);
            var typedMethod = selectManyMethod.MakeGenericMethod(sourceType, collectionElementType);
            return typedMethod.Invoke(null, [query, collectionSelector])!;
        }
    }

    private static object ExecuteOfTypeStep(object query, ExecutionStep step)
    {
        var targetType = step.Data as Type;
        var ofTypeMethod = typeof(Enumerable).GetMethod(LinqMethodNames.OfType)!;
        var typedMethod = ofTypeMethod.MakeGenericMethod(targetType!);
        return typedMethod.Invoke(null, [query])!;
    }

    private static object ExecuteOrderByStep(object query, Type elementType, ExecutionStep step)
    {
        bool descending = step.Data is true;
        var keySelector = step.Delegate!;
        var keyType = keySelector.Method.ReturnType;

        var methodName = descending ? LinqMethodNames.OrderByDescending : LinqMethodNames.OrderBy;
        var orderByMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2);
        var typedMethod = orderByMethod.MakeGenericMethod(elementType, keyType);
        return typedMethod.Invoke(null, [query, keySelector])!;
    }

    private static object ExecuteThenByStep(object query, Type elementType, ExecutionStep step)
    {
        bool descending = step.Data is true;
        var keySelector = step.Delegate!;
        var keyType = keySelector.Method.ReturnType;

        var methodName = descending ? LinqMethodNames.ThenByDescending : LinqMethodNames.ThenBy;
        var thenByMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2);
        var typedMethod = thenByMethod.MakeGenericMethod(elementType, keyType);
        return typedMethod.Invoke(null, [query, keySelector])!;
    }

    private static object ExecuteReverseStep(object query, Type elementType)
    {
        var reverseMethod = typeof(Enumerable).GetMethod(LinqMethodNames.Reverse)!;
        var typedMethod = reverseMethod.MakeGenericMethod(elementType);
        return typedMethod.Invoke(null, [query])!;
    }

    private static object ExecuteSkipStep(object query, Type elementType, ExecutionStep step)
    {
        var skipMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == LinqMethodNames.Skip && m.GetParameters().Length == 2);
        var typedMethod = skipMethod.MakeGenericMethod(elementType);
        return typedMethod.Invoke(null, [query, step.Count!])!;
    }

    private static object ExecuteTakeStep(object query, Type elementType, ExecutionStep step)
    {
        var takeMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == LinqMethodNames.Take && m.GetParameters().Length == 2 &&
                       m.GetParameters()[1].ParameterType == typeof(int));
        var typedMethod = takeMethod.MakeGenericMethod(elementType);
        return typedMethod.Invoke(null, [query, step.Count!])!;
    }

    private static object ExecuteSetOperationStep(object query, Type elementType, ExecutionStep step)
    {
        var data = step.Data as QueryTranslator.SetOperationData 
                   ?? throw new InvalidOperationException("SetOperation step missing data");

        switch (data.OperationType)
        {
            case SetOperationType.Distinct:
                var distinctMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == LinqMethodNames.Distinct && m.GetParameters().Length == 1);
                return distinctMethod.MakeGenericMethod(elementType).Invoke(null, [query])!;

            case SetOperationType.DistinctBy:
                var distinctByMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == LinqMethodNames.DistinctBy && m.GetParameters().Length == 2);
                var keyType = step.Delegate!.Method.ReturnType;
                return distinctByMethod.MakeGenericMethod(elementType, keyType).Invoke(null, [query, step.Delegate])!;

            case SetOperationType.Union:
                var unionMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == LinqMethodNames.Union && m.GetParameters().Length == 2);
                return unionMethod.MakeGenericMethod(elementType).Invoke(null, [query, data.SecondSequence])!;

            case SetOperationType.UnionBy:
                var unionByMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == LinqMethodNames.UnionBy && m.GetParameters().Length == 3);
                var unionKeyType = step.Delegate!.Method.ReturnType;
                return unionByMethod.MakeGenericMethod(elementType, unionKeyType).Invoke(null, [query, data.SecondSequence, step.Delegate])!;

            case SetOperationType.Intersect:
                var intersectMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == LinqMethodNames.Intersect && m.GetParameters().Length == 2);
                return intersectMethod.MakeGenericMethod(elementType).Invoke(null, [query, data.SecondSequence])!;

            case SetOperationType.IntersectBy:
                var intersectByMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == LinqMethodNames.IntersectBy && m.GetParameters().Length == 3);
                var intersectKeyType = step.Delegate!.Method.ReturnType;
                return intersectByMethod.MakeGenericMethod(elementType, intersectKeyType).Invoke(null, [query, data.SecondSequence, step.Delegate])!;

            case SetOperationType.Except:
                var exceptMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == LinqMethodNames.Except && m.GetParameters().Length == 2);
                return exceptMethod.MakeGenericMethod(elementType).Invoke(null, [query, data.SecondSequence])!;

            case SetOperationType.ExceptBy:
                var exceptByMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == LinqMethodNames.ExceptBy && m.GetParameters().Length == 3);
                var exceptKeyType = step.Delegate!.Method.ReturnType;
                return exceptByMethod.MakeGenericMethod(elementType, exceptKeyType).Invoke(null, [query, data.SecondSequence, step.Delegate])!;

            default:
                return query;
        }
    }

    private static object ExecutePartitioningStep(object query, Type elementType, ExecutionStep step)
    {
        var partType = (PartitioningType)step.Data!;

        switch (partType)
        {
            case PartitioningType.TakeLast:
                var takeLastMethod = typeof(Enumerable).GetMethod(LinqMethodNames.TakeLast)!;
                return takeLastMethod.MakeGenericMethod(elementType).Invoke(null, [query, step.Count!])!;

            case PartitioningType.SkipLast:
                var skipLastMethod = typeof(Enumerable).GetMethod(LinqMethodNames.SkipLast)!;
                return skipLastMethod.MakeGenericMethod(elementType).Invoke(null, [query, step.Count!])!;

            case PartitioningType.TakeWhile:
                var takeWhilePred = step.Delegate ?? throw new InvalidOperationException(
                        $"TakeWhile step missing predicate delegate. elementType: {elementType}");
                var takeWhileParamCount = takeWhilePred.Method.GetParameters().Length;
                
                if (takeWhileParamCount == 2)
                {
                    var takeWhileIndexedMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == LinqMethodNames.TakeWhile && m.GetParameters().Length == 2 &&
                                   m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2);
                    return takeWhileIndexedMethod.MakeGenericMethod(elementType).Invoke(null, [query, takeWhilePred])!;
                }
                else
                {
                    var takeWhileMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == LinqMethodNames.TakeWhile && m.GetParameters().Length == 2 &&
                                   m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 1);
                    return takeWhileMethod.MakeGenericMethod(elementType).Invoke(null, [query, takeWhilePred])!;
                }

            case PartitioningType.SkipWhile:
                var skipWhilePred = step.Delegate!;
                var skipWhileParamCount = skipWhilePred.Method.GetParameters().Length;
                
                if (skipWhileParamCount == 2)
                {
                    var skipWhileIndexedMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == LinqMethodNames.SkipWhile && m.GetParameters().Length == 2 &&
                                   m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2);
                    return skipWhileIndexedMethod.MakeGenericMethod(elementType).Invoke(null, [query, skipWhilePred])!;
                }
                else
                {
                    var skipWhileMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == LinqMethodNames.SkipWhile && m.GetParameters().Length == 2 &&
                                   m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 1);
                    return skipWhileMethod.MakeGenericMethod(elementType).Invoke(null, [query, skipWhilePred])!;
                }

            case PartitioningType.Chunk:
                var chunkMethod = typeof(Enumerable).GetMethod(LinqMethodNames.Chunk)!;
                return chunkMethod.MakeGenericMethod(elementType).Invoke(null, [query, step.Count!])!;

            default:
                return query;
        }
    }

    private static object ExecuteSequenceOperationStep(object query, Type elementType, ExecutionStep step)
    {
        var data = step.Data as QueryTranslator.SequenceOperationData 
                   ?? throw new InvalidOperationException("SequenceOperation step missing data");

        switch (data.OperationType)
        {
            case SequenceOperationType.Append:
                var appendMethod = typeof(Enumerable).GetMethod(LinqMethodNames.Append)!;
                return appendMethod.MakeGenericMethod(elementType).Invoke(null, [query, data.SecondSequence])!;

            case SequenceOperationType.Prepend:
                var prependMethod = typeof(Enumerable).GetMethod(LinqMethodNames.Prepend)!;
                return prependMethod.MakeGenericMethod(elementType).Invoke(null, [query, data.SecondSequence])!;

            case SequenceOperationType.Concat:
                var concatMethod = typeof(Enumerable).GetMethod(LinqMethodNames.Concat)!;
                return concatMethod.MakeGenericMethod(elementType).Invoke(null, [query, data.SecondSequence])!;

            case SequenceOperationType.Zip:
                if (data.ZipSelector is not null)
                {
                    var zipMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == LinqMethodNames.Zip && m.GetParameters().Length == 3 && m.GetGenericArguments().Length == 3);
                    var secondType = data.SecondSequence!.GetType().GetGenericArguments()[0];
                    var resultType = data.ZipSelector.Method.ReturnType;
                    return zipMethod.MakeGenericMethod(elementType, secondType, resultType).Invoke(null, [query, data.SecondSequence, data.ZipSelector])!;
                }
                else
                {
                    var zipMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == LinqMethodNames.Zip && m.GetParameters().Length == 2 && m.GetGenericArguments().Length == 2);
                    var secondType = data.SecondSequence!.GetType().GetGenericArguments()[0];
                    return zipMethod.MakeGenericMethod(elementType, secondType).Invoke(null, [query, data.SecondSequence])!;
                }

            default:
                return query;
        }
    }

    private static object ExecuteDefaultIfEmptyStep(object query, Type elementType, ExecutionStep step)
    {
        var defaultValue = step.Data;
        var defaultIfEmptyMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == LinqMethodNames.DefaultIfEmpty && m.GetParameters().Length == (defaultValue is not null ? 2 : 1));
        
        return defaultValue is not null
            ? defaultIfEmptyMethod.MakeGenericMethod(elementType).Invoke(null, [query, defaultValue])!
            : defaultIfEmptyMethod.MakeGenericMethod(elementType).Invoke(null, [query])!;
    }

    private static object ExecuteJoinStep(object query, Type sourceType, ExecutionStep step)
    {
        var joinData = step.Data as QueryTranslator.JoinData 
                       ?? throw new InvalidOperationException("Join step missing data");

        var outerKeySelector = joinData.OuterKeySelector!;
        var innerKeySelector = joinData.InnerKeySelector!;
        var resultSelector = joinData.ResultSelector!;
        var innerEnumerable = joinData.InnerSequence as System.Collections.IEnumerable
                              ?? throw new InvalidOperationException("Inner sequence must be enumerable");

        // Use the join classification determined during query translation
        bool isGroupJoin = joinData.IsGroupJoin;

        var methodName = isGroupJoin ? nameof(Enumerable.GroupJoin) : nameof(Enumerable.Join);
        var joinMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == methodName && m.GetGenericArguments().Length == 4);

        var innerType = joinData.InnerSequence!.GetType().GetGenericArguments()[0];
        var keyType = outerKeySelector.Method.ReturnType;

        var typedJoinMethod = joinMethod.MakeGenericMethod(sourceType, innerType, keyType, step.ResultType!);

        var invokeParams = joinData.KeyComparer is not null
            ? new[] { query, innerEnumerable, outerKeySelector, innerKeySelector, resultSelector, joinData.KeyComparer }
            : [query, innerEnumerable, outerKeySelector, innerKeySelector, resultSelector];

        return typedJoinMethod.Invoke(null, invokeParams)!;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the value of a property by name from the specified item.
    /// Uses span-based property access for zero-allocation lookups with global shared cache.
    /// </summary>
    /// <typeparam name="T">Type of the item.</typeparam>
    /// <param name="item">The item to get the property value from.</param>
    /// <param name="propertyPath">The property name or path.</param>
    /// <returns>The property value, or null if not found.</returns>
    private static object? GetPropertyValue<T>(T? item, ReadOnlySpan<char> propertyPath)
    {
        if (item is null)
            return null;

        // Use static SpanPropertyAccessor for global cached, zero-allocation property access
        return Implementations.SpanPropertyAccessor.GetValue(item, propertyPath);
    }

    #endregion
}

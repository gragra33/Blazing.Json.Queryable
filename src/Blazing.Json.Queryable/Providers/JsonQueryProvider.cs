using System.Linq.Expressions;
using Blazing.Json.Queryable.Core;
using Blazing.Json.Queryable.Execution;
using Blazing.Json.Queryable.Visitors;
using Blazing.Json.Queryable.Exceptions;

namespace Blazing.Json.Queryable.Providers;

/// <summary>
/// Query provider for JSON-based LINQ queries.
/// Translates expression trees into query execution plans.
/// Implements <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/> to manage underlying resources.
/// </summary>
public class JsonQueryProvider : IQueryProvider, IDisposable, IAsyncDisposable
{
    private readonly JsonQueryContext _context;
    private readonly IQueryExecutor _executor;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="JsonQueryProvider"/>.
    /// </summary>
    /// <param name="context">The query context containing configuration and source data.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is null.</exception>
    internal JsonQueryProvider(JsonQueryContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _executor = CreateExecutor(context);
    }

    /// <summary>
    /// Creates the appropriate executor based on the source type.
    /// </summary>
    /// <param name="context">The query context.</param>
    /// <returns>An <see cref="IQueryExecutor"/> for the context.</returns>
    /// <exception cref="InvalidQueryException">Thrown if the source type is not supported.</exception>
    private static IQueryExecutor CreateExecutor(JsonQueryContext context)
    {
        // Use provided options or create default with case-insensitive matching
        var options = context.Configuration.SerializerOptions ??
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        
        // Check if JSONPath filtering is requested
        var hasJsonPath = !string.IsNullOrWhiteSpace(context.JsonPath);
        
        return context.SourceType switch
        {
            JsonSourceType.Utf8Bytes when hasJsonPath => new TokenFilteredMemoryExecutor(
                context.GetUtf8Source(),
                context.JsonPath!,
                options),
            
            JsonSourceType.Utf8Bytes => new StringQueryExecutor(
                context.GetUtf8Source(),
                options),
            
            JsonSourceType.String when hasJsonPath => new TokenFilteredMemoryExecutor(
                context.GetUtf8Source(),
                context.JsonPath!,
                options),
            
            JsonSourceType.String => new StringQueryExecutor(
                context.GetUtf8Source(),
                options),
            
            JsonSourceType.Stream when hasJsonPath => new TokenFilteredStreamExecutor(
                context.GetStream(),
                context.JsonPath!,
                options),
            
            JsonSourceType.Stream => new StreamQueryExecutor(
                context.GetStream(),
                options),
            
            JsonSourceType.File when hasJsonPath => new TokenFilteredStreamExecutor(
                context.GetStream(),
                context.JsonPath!,
                options),
            
            JsonSourceType.File => new StreamQueryExecutor(
                context.GetStream(),
                options),
            
            _ => throw new InvalidQueryException($"Source type {context.SourceType} is not supported.", "CreateExecutor")
        };
    }

    #region IQueryProvider Implementation

    /// <summary>
    /// Constructs a new queryable for a given expression tree.
    /// Used for query chaining (e.g., after Where, Select, etc.).
    /// </summary>
    /// <typeparam name="TElement">The type of elements in the query.</typeparam>
    /// <param name="expression">The expression tree representing the query.</param>
    /// <returns>A new <see cref="JsonQueryable{TElement}"/> for the expression.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the provider has been disposed.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="expression"/> is null.</exception>
    /// <exception cref="InvalidQueryException">Thrown if the expression does not represent an <see cref="IQueryable{TElement}"/>.</exception>
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(expression);
        
        if (!typeof(IQueryable<TElement>).IsAssignableFrom(expression.Type))
        {
            throw new InvalidQueryException(
                $"Expression must represent an IQueryable<{typeof(TElement).Name}>.",
                "CreateQuery");
        }

        return new JsonQueryable<TElement>(this, expression);
    }

    /// <summary>
    /// Constructs a new queryable for a given expression tree (non-generic).
    /// </summary>
    /// <param name="expression">The expression tree representing the query.</param>
    /// <returns>A new <see cref="JsonQueryable{T}"/> for the expression.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the provider has been disposed.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="expression"/> is null.</exception>
    /// <exception cref="InvalidQueryException">Thrown if the element type cannot be determined from the expression.</exception>
    public IQueryable CreateQuery(Expression expression)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(expression);

        Type elementType = expression.Type.GetGenericArguments().FirstOrDefault()
            ?? throw new InvalidQueryException("Cannot determine element type from expression.", "CreateQuery");

        Type queryableType = typeof(JsonQueryable<>).MakeGenericType(elementType);
        
        return (IQueryable)Activator.CreateInstance(
            queryableType,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null,
            [this, expression],
            null)!;
    }

    /// <summary>
    /// Executes the query represented by the expression tree.
    /// Translates the expression into a query plan and executes it synchronously.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="expression">The expression tree representing the query.</param>
    /// <returns>The result of executing the query.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the provider has been disposed.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="expression"/> is null.</exception>
    /// <exception cref="InvalidQueryException">Thrown for invalid aggregate or query expressions.</exception>
    /// <exception cref="InvalidOperationException">Thrown for execution errors or if the executor is not initialized.</exception>
    public TResult Execute<TResult>(Expression expression)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(expression);

        // Check if this is an aggregate operation method call
        if (expression is MethodCallExpression methodCall)
        {
            var methodName = methodCall.Method.Name;
            
            // Handle aggregate operations: Count, LongCount, Any, First, FirstOrDefault, Single, SingleOrDefault, ElementAt, ElementAtOrDefault, Last, LastOrDefault
            if (methodName is LinqMethodNames.Count or LinqMethodNames.LongCount or LinqMethodNames.Any or 
                LinqMethodNames.First or LinqMethodNames.FirstOrDefault or LinqMethodNames.Single or LinqMethodNames.SingleOrDefault or 
                LinqMethodNames.ElementAt or LinqMethodNames.ElementAtOrDefault or LinqMethodNames.Last or LinqMethodNames.LastOrDefault)
            {
                // Get the source expression (the query before the aggregate)
                var sourceExpression = methodCall.Arguments[0];
                
                // Check if there's a predicate (2nd argument) - for First, Single, Last
                if (methodCall.Arguments.Count == 2 && methodName is not LinqMethodNames.ElementAt and not LinqMethodNames.ElementAtOrDefault)
                {
                    // Extract the predicate lambda from the second argument
                    var predicateArg = methodCall.Arguments[1];
                    if (predicateArg is UnaryExpression { Operand: LambdaExpression })
                    {
                        // The predicate is wrapped in a UnaryExpression (Quote)
                        // We need to apply it as a Where clause
                        var whereMethod = typeof(System.Linq.Queryable)
                            .GetMethods()
                            .First(m => m.Name == LinqMethodNames.Where && m.GetParameters().Length == 2)
                            .MakeGenericMethod(GetElementType(sourceExpression.Type));
                        
                        sourceExpression = Expression.Call(whereMethod, sourceExpression, predicateArg);
                    }
                }
                
                // Build execution plan for the source (with predicate applied if present)
                var sourcePlan = BuildExecutionPlan(sourceExpression);
                var sourceElementType = GetElementType(sourceExpression.Type);
                
                // Execute the source query to get IEnumerable
                var sourceExecuteMethod = typeof(IQueryExecutor)
                    .GetMethod(nameof(IQueryExecutor.Execute))!
                    .MakeGenericMethod(sourceElementType);
                
                object? queryResult;
                try
                {
                    queryResult = sourceExecuteMethod.Invoke(_executor, [sourcePlan]);
                }
                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
                
                if (queryResult is System.Collections.IEnumerable)
                {
                    // Apply the aggregate operation
                    switch (methodName)
                    {
                        case LinqMethodNames.Count:
                            {
                                var countMethod = typeof(Enumerable)
                                    .GetMethods()
                                    .First(m => m.Name == LinqMethodNames.Count && m.GetParameters().Length == 1)
                                    .MakeGenericMethod(sourceElementType);
                                try
                                {
                                    return (TResult)countMethod.Invoke(null, [queryResult])!;
                                }
                                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
                                {
                                    throw ex.InnerException;
                                }
                            }
                        
                        case LinqMethodNames.LongCount:
                            {
                                var longCountMethod = typeof(Enumerable)
                                    .GetMethods()
                                    .First(m => m.Name == LinqMethodNames.LongCount && m.GetParameters().Length == 1)
                                    .MakeGenericMethod(sourceElementType);
                                try
                                {
                                    return (TResult)longCountMethod.Invoke(null, [queryResult])!;
                                }
                                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
                                {
                                    throw ex.InnerException;
                                }
                            }
                        
                        case LinqMethodNames.Any:
                            {
                                var anyMethod = typeof(Enumerable)
                                    .GetMethods()
                                    .First(m => m.Name == LinqMethodNames.Any && m.GetParameters().Length == 1)
                                    .MakeGenericMethod(sourceElementType);
                                try
                                {
                                    return (TResult)anyMethod.Invoke(null, [queryResult])!;
                                }
                                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
                                {
                                    throw ex.InnerException;
                                }
                            }
                        
                        case LinqMethodNames.First:
                            {
                                var firstMethod = typeof(Enumerable)
                                    .GetMethods()
                                    .First(m => m.Name == LinqMethodNames.First && m.GetParameters().Length == 1)
                                    .MakeGenericMethod(sourceElementType);
                                try
                                {
                                    return (TResult)firstMethod.Invoke(null, [queryResult])!;
                                }
                                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
                                {
                                    throw ex.InnerException;
                                }
                            }
                        
                        case LinqMethodNames.FirstOrDefault:
                            {
                                var firstOrDefaultMethod = typeof(Enumerable)
                                    .GetMethods()
                                    .First(m => m.Name == LinqMethodNames.FirstOrDefault && m.GetParameters().Length == 1)
                                    .MakeGenericMethod(sourceElementType);
                                try
                                {
                                    var firstResult = firstOrDefaultMethod.Invoke(null, [queryResult]);
                                    return firstResult is not null ? (TResult)firstResult : default!;
                                }
                                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
                                {
                                    throw ex.InnerException;
                                }
                            }
                        
                        case LinqMethodNames.Single:
                            {
                                var singleMethod = typeof(Enumerable)
                                    .GetMethods()
                                    .First(m => m.Name == LinqMethodNames.Single && m.GetParameters().Length == 1)
                                    .MakeGenericMethod(sourceElementType);
                                try
                                {
                                    return (TResult)singleMethod.Invoke(null, [queryResult])!;
                                }
                                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
                                {
                                    throw ex.InnerException;
                                }
                            }
                        
                        case LinqMethodNames.SingleOrDefault:
                            {
                                var singleOrDefaultMethod = typeof(Enumerable)
                                    .GetMethods()
                                    .First(m => m.Name == LinqMethodNames.SingleOrDefault && m.GetParameters().Length == 1)
                                    .MakeGenericMethod(sourceElementType);
                                try
                                {
                                    var singleResult = singleOrDefaultMethod.Invoke(null, [queryResult]);
                                    return singleResult is not null ? (TResult)singleResult : default!;
                                }
                                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
                                {
                                    throw ex.InnerException;
                                }
                            }
                        
                        case LinqMethodNames.ElementAt:
                            {
                                // Get the index from the second argument
                                var indexArg = methodCall.Arguments[1];
                                int index;
                                if (indexArg is ConstantExpression { Value: int intIndex })
                                {
                                    index = intIndex;
                                }
                                else if (indexArg.Type == typeof(Index))
                                {
                                    // Handle Index type (^n notation)
                                    var indexValue = Expression.Lambda(indexArg).Compile().DynamicInvoke();
                                    if (indexValue is Index idx)
                                    {
                                        var list = queryResult as System.Collections.IList ?? 
                                                  typeof(Enumerable).GetMethod(LinqMethodNames.ToList)!.MakeGenericMethod(sourceElementType)
                                                  .Invoke(null, [queryResult]) as System.Collections.IList;
                                        index = idx.GetOffset(list!.Count);
                                    }
                                    else
                                    {
                                        throw new ArgumentException("Invalid index argument");
                                    }
                                }
                                else
                                {
                                    throw new ArgumentException("Invalid index argument");
                                }
                                
                                var elementAtMethod = typeof(Enumerable)
                                    .GetMethods()
                                    .First(m => m.Name == LinqMethodNames.ElementAt && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(int))
                                    .MakeGenericMethod(sourceElementType);
                                try
                                {
                                    return (TResult)elementAtMethod.Invoke(null, [queryResult, index])!;
                                }
                                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
                                {
                                    throw ex.InnerException;
                                }
                            }
                        
                        case LinqMethodNames.ElementAtOrDefault:
                            {
                                // Get the index from the second argument
                                var indexArg = methodCall.Arguments[1];
                                int index;
                                if (indexArg is ConstantExpression { Value: int intIndex })
                                {
                                    index = intIndex;
                                }
                                else if (indexArg.Type == typeof(Index))
                                {
                                    // Handle Index type (^n notation)
                                    var indexValue = Expression.Lambda(indexArg).Compile().DynamicInvoke();
                                    if (indexValue is Index idx)
                                    {
                                        var list = queryResult as System.Collections.IList ?? 
                                                  typeof(Enumerable).GetMethod(LinqMethodNames.ToList)!.MakeGenericMethod(sourceElementType)
                                                  .Invoke(null, [queryResult]) as System.Collections.IList;
                                        index = idx.GetOffset(list!.Count);
                                    }
                                    else
                                    {
                                        throw new ArgumentException("Invalid index argument");
                                    }
                                }
                                else
                                {
                                    throw new ArgumentException("Invalid index argument");
                                }
                                
                                var elementAtOrDefaultMethod = typeof(Enumerable)
                                    .GetMethods()
                                    .First(m => m.Name == LinqMethodNames.ElementAtOrDefault && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(int))
                                    .MakeGenericMethod(sourceElementType);
                                try
                                {
                                    var elementResult = elementAtOrDefaultMethod.Invoke(null, [queryResult, index]);
                                    return elementResult is not null ? (TResult)elementResult : default!;
                                }
                                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
                                {
                                    throw ex.InnerException;
                                }
                            }
                        
                        case LinqMethodNames.Last:
                            {
                                var lastMethod = typeof(Enumerable)
                                    .GetMethods()
                                    .First(m => m.Name == LinqMethodNames.Last && m.GetParameters().Length == 1)
                                    .MakeGenericMethod(sourceElementType);
                                try
                                {
                                    return (TResult)lastMethod.Invoke(null, [queryResult])!;
                                }
                                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
                                {
                                    throw ex.InnerException;
                                }
                            }
                        
                        case LinqMethodNames.LastOrDefault:
                            {
                                var lastOrDefaultMethod = typeof(Enumerable)
                                    .GetMethods()
                                    .First(m => m.Name == LinqMethodNames.LastOrDefault && m.GetParameters().Length == 1)
                                    .MakeGenericMethod(sourceElementType);
                                try
                                {
                                    var lastResult = lastOrDefaultMethod.Invoke(null, [queryResult]);
                                    return lastResult is not null ? (TResult)lastResult : default!;
                                }
                                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
                                {
                                    throw ex.InnerException;
                                }
                            }
                    }
                }
            }
        }

        // If not an aggregate operation, proceed with normal query execution
        
        var plan = BuildExecutionPlan(expression) ?? throw new InvalidOperationException($"BuildExecutionPlan returned null for expression: {expression}");
        if (_executor == null)
        {
            throw new InvalidOperationException("Executor is null - provider may not be initialized correctly");
        }
        
        // Execute the query
        var elementType = GetElementType(expression.Type) ?? throw new InvalidOperationException($"Could not determine element type from expression: {expression}");
        var executeMethod = typeof(IQueryExecutor)
            .GetMethod(nameof(IQueryExecutor.Execute))!
            .MakeGenericMethod(elementType) ?? throw new InvalidOperationException($"Could not create Execute method for element type: {elementType}");
        object result;
        try
        {
            result = executeMethod.Invoke(_executor, [plan])!;
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Unwrap TargetInvocationException to preserve the original exception
            throw ex.InnerException;
        }
        catch (NullReferenceException ex)
        {
            throw new InvalidOperationException(
                $"NullReferenceException during execution. Expression: {expression}, ElementType: {elementType}, Plan: {true}", ex);
        }

        // Handle different result types

        // If types match directly, return as-is
        if (result is TResult result1)
        {
            return result1;
        }

        // Handle IEnumerable<T> results - check if result implements IEnumerable<elementType>
        var resultEnumerableType = result.GetType().GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && 
                               i.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                               i.GetGenericArguments()[0] == GetElementType(expression.Type));
        
        if (resultEnumerableType != null && typeof(TResult).IsAssignableFrom(resultEnumerableType))
        {
            return (TResult)result;
        }

        // Handle IEnumerable results
        if (result is System.Collections.IEnumerable)
        {
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(GetElementType(expression.Type));
            if (enumerableType.IsInstanceOfType(result))
            {
                // Try to materialize the enumerable
                var toListMethod = typeof(Enumerable)
                    .GetMethod("ToList")!
                    .MakeGenericMethod(GetElementType(expression.Type));

                if (toListMethod.Invoke(null, [result]) is System.Collections.IList list)
                {
                    // For empty results with element type matching TResult
                    if (list.Count == 0 && (typeof(TResult) == GetElementType(expression.Type) || typeof(TResult).IsAssignableFrom(GetElementType(expression.Type))))
                    {
                        return default!;
                    }

                    if (list.Count > 0)
                    {
                        var firstItem = list[0];
                        if (firstItem is TResult item)
                        {
                            return item;
                        }
                        // Handle null items for reference types
                        if (!typeof(TResult).IsValueType && firstItem == null)
                        {
                            return default!;
                        }
                    }
                }
            }
        }

        throw new InvalidOperationException(
            $"Cannot convert query result to {typeof(TResult).Name}.");
    }

    /// <summary>
    /// Executes the query represented by the expression tree (non-generic).
    /// </summary>
    /// <param name="expression">The expression tree representing the query.</param>
    /// <returns>The result of executing the query.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the provider has been disposed.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="expression"/> is null.</exception>
    public object? Execute(Expression expression)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(expression);

        GetElementType(expression.Type);
        
        var executeMethod = typeof(JsonQueryProvider)
            .GetMethod(nameof(Execute), [typeof(Expression)])!
            .MakeGenericMethod(expression.Type);

        return executeMethod.Invoke(this, [expression]);
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Builds a query execution plan from an expression tree.
    /// Used by <see cref="JsonQueryable{T}"/> for async enumeration.
    /// </summary>
    /// <param name="expression">The expression tree representing the query.</param>
    /// <returns>The <see cref="QueryExecutionPlan"/> for the query.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the provider has been disposed.</exception>
    internal QueryExecutionPlan BuildExecutionPlan(Expression expression)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var translator = new QueryTranslator(_context.Configuration.ExpressionEvaluator);
        var sourceType = GetRootSourceType(expression);
        return translator.Translate(expression, sourceType);
    }

    /// <summary>
    /// Gets the executor for async operations.
    /// Used by <see cref="JsonQueryable{T}"/> for async enumeration.
    /// </summary>
    /// <returns>The <see cref="IQueryExecutor"/> for async operations.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the provider has been disposed.</exception>
    internal IQueryExecutor GetAsyncExecutor()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _executor;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the root source type by walking back through the expression tree to find the original <see cref="JsonQueryable{T}"/>.
    /// </summary>
    /// <param name="expression">The expression tree to analyze.</param>
    /// <returns>The root source type.</returns>
    private static Type GetRootSourceType(Expression expression)
    {
        var current = expression;
        
        // Walk back through the expression tree
        while (true)
        {
            // Check if this is a MethodCallExpression (like Select, Where, GroupBy, etc.)
            if (current is MethodCallExpression { Arguments.Count: > 0 } methodCall)
            {
                // The first argument is usually the source queryable
                current = methodCall.Arguments[0];
                continue;
            }
            
            // Check if this is a ConstantExpression containing a JsonQueryable<T>
            if (current is ConstantExpression constantExpr)
            {
                var value = constantExpr.Value;
                if (value != null)
                {
                    var valueType = value.GetType();
                    
                    // Check if it's a JsonQueryable<T>
                    if (valueType.IsGenericType && valueType.GetGenericTypeDefinition().Name.StartsWith("JsonQueryable"))
                    {
                        // Get the T from JsonQueryable<T>
                        var genericArgs = valueType.GetGenericArguments();
                        if (genericArgs.Length > 0)
                        {
                            return genericArgs[0];
                        }
                    }
                }
            }
            
            // If we can't go further, break
            break;
        }
        
        // Fallback: get element type from the expression type
        return QueryTranslator.GetElementType(expression) ?? typeof(object);
    }

    /// <summary>
    /// Extracts the element type from a query expression type.
    /// </summary>
    /// <param name="type">The type to analyze.</param>
    /// <returns>The element type.</returns>
    /// <exception cref="ArgumentException">Thrown if the element type cannot be determined.</exception>
    private static Type GetElementType(Type type)
    {
        // Check if it's IQueryable<T>
        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                return genericArgs[0];
            }
        }

        // Check implemented interfaces
        var queryableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && 
                               i.GetGenericTypeDefinition() == typeof(IQueryable<>));

        if (queryableInterface != null)
        {
            return queryableInterface.GetGenericArguments()[0];
        }

        // Check if it's a direct type
        if (!type.IsGenericType && type != typeof(object))
        {
            return type;
        }

        throw new ArgumentException($"Cannot determine element type from {type.Name}.",
            nameof(type));
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes the provider and releases any underlying resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _context.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the provider and releases any underlying resources.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await _context.DisposeAsync().ConfigureAwait(false);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}

using Blazing.Json.Queryable.Exceptions;

namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Represents a compiled query execution plan containing all operations to apply.
/// Uses span-optimized data structures for minimal allocations during query execution.
/// </summary>
/// <remarks>
/// <para>
/// The execution plan stores query operations using span-optimized data structures:
/// <list type="bullet">
/// <item>Property paths as ReadOnlyMemory&lt;char&gt; (not string[])</item>
/// <item>Compiled predicates as Func&lt;T, bool&gt;</item>
/// <item>Skip/Take as nullable integers</item>
/// </list>
/// </para>
/// <para>
/// This plan is created by QueryTranslator from expression trees and consumed by
/// IQueryExecutor implementations for both sync and async execution.
/// </para>
/// <para>
/// Supports ordered execution via ExecutionSteps collection for queries with
/// operations after projections (e.g., .Select().Distinct()).
/// </para>
/// </remarks>
public class QueryExecutionPlan
{
    /// <summary>
    /// Ordered list of execution steps.
    /// When non-empty, this takes precedence over legacy flat properties.
    /// </summary>
    public List<ExecutionStep>? ExecutionSteps { get; set; }

    /// <summary>
    /// Property paths used in filter predicates.
    /// Stored as ReadOnlyMemory&lt;char&gt; for zero-allocation span access.
    /// </summary>
    public ReadOnlyMemory<char>[]? FilterPropertyPaths { get; set; }

    /// <summary>
    /// Compiled predicate functions for filtering.
    /// Each predicate corresponds to a Where clause.
    /// </summary>
    public Delegate[]? Predicates { get; set; }

    /// <summary>
    /// Property paths used in projection (Select).
    /// Stored as ReadOnlyMemory&lt;char&gt; for zero-allocation span access.
    /// </summary>
    public ReadOnlyMemory<char>[]? ProjectionPropertyPaths { get; set; }

    /// <summary>
    /// Projection selector function for Select operations.
    /// Can transform from source type to result type.
    /// </summary>
    public Delegate? ProjectionSelector { get; set; }

    /// <summary>
    /// Property paths used for sorting (OrderBy/ThenBy).
    /// Stored as ReadOnlyMemory&lt;char&gt; for zero-allocation span access.
    /// </summary>
    public ReadOnlyMemory<char>[]? SortPropertyPaths { get; set; }

    /// <summary>
    /// Sort directions corresponding to SortPropertyPaths.
    /// True for ascending, false for descending.
    /// </summary>
    public bool[]? SortDirections { get; set; }

    /// <summary>
    /// Number of items to skip (for Skip operation).
    /// Null if no skip is specified.
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// Number of items to take (for Take operation).
    /// Null if no limit is specified.
    /// </summary>
    public int? Take { get; set; }

    /// <summary>
    /// The source type being queried.
    /// </summary>
    public Type? SourceType { get; set; }

    /// <summary>
    /// The result type after projections.
    /// Same as SourceType if no projection is applied.
    /// </summary>
    public Type? ResultType { get; set; }

    // Filtering Operations
    /// <summary>
    /// Type filter for OfType operation.
    /// </summary>
    public Type? TypeFilter { get; set; }

    // Projection Operations
    /// <summary>
    /// SelectMany collection selector function.
    /// </summary>
    public Delegate? SelectManyCollectionSelector { get; set; }

    /// <summary>
    /// SelectMany result selector function (combines source and collection element).
    /// </summary>
    public Delegate? SelectManyResultSelector { get; set; }

    // Sorting Operations
    /// <summary>
    /// Indicates if Reverse operation should be applied.
    /// </summary>
    public bool Reverse { get; set; }

    // Aggregation Operations
    /// <summary>
    /// Type of aggregation operation.
    /// </summary>
    public AggregationType? AggregationType { get; set; }

    /// <summary>
    /// Selector function for aggregation operations (e.g., Sum(x => x.Price)).
    /// </summary>
    public Delegate? AggregationSelector { get; set; }

    /// <summary>
    /// Key selector for MinBy/MaxBy operations.
    /// </summary>
    public Delegate? KeySelector { get; set; }

    /// <summary>
    /// Seed value for Aggregate operation.
    /// </summary>
    public object? AggregateSeed { get; set; }

    /// <summary>
    /// Accumulator function for Aggregate operation.
    /// </summary>
    public Delegate? AggregateFunc { get; set; }

    /// <summary>
    /// Result selector for Aggregate operation.
    /// </summary>
    public Delegate? AggregateResultSelector { get; set; }

    // Set Operations
    /// <summary>
    /// Type of set operation.
    /// </summary>
    public SetOperationType? SetOperationType { get; set; }

    /// <summary>
    /// Equality comparer for set and other operations.
    /// </summary>
    public object? Comparer { get; set; }

    /// <summary>
    /// Key comparer for key-based operations (DistinctBy, UnionBy, etc.).
    /// </summary>
    public object? KeyComparer { get; set; }

    /// <summary>
    /// Second sequence for set operations (Union, Intersect, Except).
    /// </summary>
    public object? SecondSequence { get; set; }

    // Grouping Operations
    /// <summary>
    /// Element selector for GroupBy/ToLookup operations.
    /// </summary>
    public Delegate? ElementSelector { get; set; }

    /// <summary>
    /// Result selector for GroupBy operations.
    /// </summary>
    public Delegate? GroupByResultSelector { get; set; }

    // Join Operations
    /// <summary>
    /// Inner sequence for Join/GroupJoin operations.
    /// </summary>
    public object? InnerSequence { get; set; }

    /// <summary>
    /// Outer key selector for Join/GroupJoin operations.
    /// </summary>
    public Delegate? OuterKeySelector { get; set; }

    /// <summary>
    /// Inner key selector for Join/GroupJoin operations.
    /// </summary>
    public Delegate? InnerKeySelector { get; set; }

    /// <summary>
    /// Result selector for Join/GroupJoin operations.
    /// </summary>
    public Delegate? JoinResultSelector { get; set; }

    // Element Operations
    /// <summary>
    /// Element index for ElementAt operation.
    /// </summary>
    public int? ElementIndex { get; set; }

    /// <summary>
    /// Element index from end (using Index type) for ElementAt operation.
    /// </summary>
    public Index? ElementIndexFromEnd { get; set; }

    /// <summary>
    /// Predicate for Last/LastOrDefault operations.
    /// </summary>
    public Delegate? LastPredicate { get; set; }

    // Quantifier Operations
    /// <summary>
    /// Type of quantifier operation.
    /// </summary>
    public QuantifierType? QuantifierType { get; set; }

    /// <summary>
    /// Item to search for in Contains operation.
    /// </summary>
    public object? ContainsItem { get; set; }

    // Sequence Operations
    /// <summary>
    /// Type of sequence operation.
    /// </summary>
    public SequenceOperationType? SequenceOperationType { get; set; }

    /// <summary>
    /// Element to append.
    /// </summary>
    public object? AppendElement { get; set; }

    /// <summary>
    /// Element to prepend.
    /// </summary>
    public object? PrependElement { get; set; }

    /// <summary>
    /// Zip result selector function.
    /// </summary>
    public Delegate? ZipSelector { get; set; }

    /// <summary>
    /// Third sequence for three-way Zip operation.
    /// </summary>
    public object? ThirdSequence { get; set; }

    // Partitioning Operations
    /// <summary>
    /// Type of partitioning operation.
    /// </summary>
    public PartitioningType? PartitioningType { get; set; }

    /// <summary>
    /// Count for TakeLast/SkipLast operations.
    /// </summary>
    public int? PartitionCount { get; set; }

    /// <summary>
    /// Predicate with index for TakeWhile/SkipWhile operations.
    /// </summary>
    public Delegate? PartitionPredicateWithIndex { get; set; }

    /// <summary>
    /// Chunk size for Chunk operation.
    /// </summary>
    public int? ChunkSize { get; set; }

    // Conversion Operations
    /// <summary>
    /// Type of conversion operation.
    /// </summary>
    public ConversionType? ConversionType { get; set; }

    // Utility Operations
    /// <summary>
    /// Default value for DefaultIfEmpty operation.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Indicates if default value was explicitly specified.
    /// </summary>
    public bool HasDefaultValue { get; set; }

    /// <summary>
    /// Indicates if the query requires materialization (OrderBy present).
    /// When true, streaming executors must buffer all results before sorting.
    /// </summary>
    public bool RequiresMaterialization => 
        ExecutionSteps is { Count: > 0 } ? RequiresMaterializationFromSteps() :
        SortPropertyPaths?.Length > 0 || 
        Reverse ||
        AggregationType.HasValue ||
        SetOperationType.HasValue ||
        GroupByResultSelector != null ||
        InnerSequence != null ||
        LastPredicate != null ||
        PartitioningType == Core.PartitioningType.TakeLast ||
        PartitioningType == Core.PartitioningType.SkipLast;

    private bool RequiresMaterializationFromSteps()
    {
        if (ExecutionSteps == null) return false;
        
        foreach (var step in ExecutionSteps)
        {
            if (step.OperationType == OperationType.OrderBy ||
                step.OperationType == OperationType.ThenBy ||
                step.OperationType == OperationType.Reverse ||
                step.OperationType == OperationType.SetOperation ||
                step.OperationType == OperationType.GroupBy ||
                step.OperationType == OperationType.Join)
            {
                return true;
            }
            
            if (step is { OperationType: OperationType.Partitioning, Data: PartitioningType pt })
            {
                if (pt == Core.PartitioningType.TakeLast || pt == Core.PartitioningType.SkipLast)
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// Gets a sort property path as a span for zero-allocation access.
    /// </summary>
    /// <param name="index">The index of the property path</param>
    /// <returns>ReadOnlySpan of the property path</returns>
    public ReadOnlySpan<char> GetSortPath(int index)
    {
        if (SortPropertyPaths == null || index < 0 || index >= SortPropertyPaths.Length)
        {
            return ReadOnlySpan<char>.Empty;
        }

        return SortPropertyPaths[index].Span;
    }

    /// <summary>
    /// Validates the execution plan for consistency.
    /// </summary>
    /// <exception cref="InvalidQueryException">Thrown when plan is invalid</exception>
    public void Validate()
    {
        // Validate predicates match filter paths
        if (FilterPropertyPaths?.Length > 0 && Predicates == null)
        {
            throw new InvalidQueryException("Filter paths defined but no predicates provided", "Validate");
        }

        // Validate sort directions match sort paths
        if (SortPropertyPaths?.Length > 0 && 
            (SortDirections == null || SortDirections.Length != SortPropertyPaths.Length))
        {
            throw new InvalidQueryException("Sort paths and directions count mismatch", "Validate");
        }

        // Validate Skip/Take values
        if (Skip is < 0)
        {
            throw new InvalidQueryException("Skip value cannot be negative", "Validate");
        }

        if (Take is < 0)
        {
            throw new InvalidQueryException("Take value cannot be negative", "Validate");
        }

        // Validate types are set
        if (SourceType == null)
        {
            throw new InvalidQueryException("SourceType must be set", "Validate");
        }

        if (ResultType == null)
        {
            throw new InvalidQueryException("ResultType must be set", "Validate");
        }

        // Validate aggregation operations (but not GroupBy which also uses KeySelector)
        if (AggregationType.HasValue && AggregationSelector == null && GroupByResultSelector == null &&
            AggregationType != Core.AggregationType.Count && 
            AggregationType != Core.AggregationType.LongCount &&
            AggregationType != Core.AggregationType.MinBy &&
            AggregationType != Core.AggregationType.MaxBy &&
            AggregationType != Core.AggregationType.Aggregate)
        {
            throw new InvalidQueryException("Aggregation selector required for aggregation type", "Validate");
        }

        // Validate set operations
        if (SetOperationType.HasValue)
        {
            var requiresSecondSequence = SetOperationType == Core.SetOperationType.Union ||
                                        SetOperationType == Core.SetOperationType.UnionBy ||
                                        SetOperationType == Core.SetOperationType.Intersect ||
                                        SetOperationType == Core.SetOperationType.IntersectBy ||
                                        SetOperationType == Core.SetOperationType.Except ||
                                        SetOperationType == Core.SetOperationType.ExceptBy;
            
            if (requiresSecondSequence && SecondSequence == null)
            {
                throw new InvalidQueryException("Second sequence required for set operation", "Validate");
            }
        }

        // Validate join operations
        if (InnerSequence != null)
        {
            if (OuterKeySelector == null || InnerKeySelector == null || JoinResultSelector == null)
            {
                throw new InvalidQueryException("Join operations require key selectors and result selector", "Validate");
            }
        }

        // Validate partitioning operations
        if (ChunkSize is <= 0)
        {
            throw new InvalidQueryException("Chunk size must be greater than zero", "Validate");
        }

        if (PartitionCount is < 0)
        {
            throw new InvalidQueryException("Partition count cannot be negative", "Validate");
        }
    }
}
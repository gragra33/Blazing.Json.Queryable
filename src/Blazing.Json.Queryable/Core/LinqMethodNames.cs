namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Well-known LINQ method names used for expression tree analysis.
/// Provides compile-time safety and refactoring support for method name comparisons.
/// </summary>
internal static class LinqMethodNames
{
    // Filtering Operations
    /// <summary>Where filter operation.</summary>
    public const string Where = nameof(Where);
    
    /// <summary>OfType filter operation.</summary>
    public const string OfType = nameof(OfType);

    // Projection Operations
    /// <summary>Select projection operation.</summary>
    public const string Select = nameof(Select);
    
    /// <summary>SelectMany projection operation.</summary>
    public const string SelectMany = nameof(SelectMany);

    // Type Conversion Operations
    /// <summary>Cast type conversion operation.</summary>
    public const string Cast = nameof(Cast);

    // Sorting Operations
    /// <summary>OrderBy sorting operation.</summary>
    public const string OrderBy = nameof(OrderBy);
    
    /// <summary>OrderByDescending sorting operation.</summary>
    public const string OrderByDescending = nameof(OrderByDescending);
    
    /// <summary>ThenBy sorting operation.</summary>
    public const string ThenBy = nameof(ThenBy);
    
    /// <summary>ThenByDescending sorting operation.</summary>
    public const string ThenByDescending = nameof(ThenByDescending);
    
    /// <summary>Order sorting operation (C# 14).</summary>
    public const string Order = nameof(Order);
    
    /// <summary>OrderDescending sorting operation (C# 14).</summary>
    public const string OrderDescending = nameof(OrderDescending);
    
    /// <summary>Reverse operation.</summary>
    public const string Reverse = nameof(Reverse);

    // Paging Operations
    /// <summary>Skip paging operation.</summary>
    public const string Skip = nameof(Skip);
    
    /// <summary>Take paging operation.</summary>
    public const string Take = nameof(Take);

    // Element Operations
    /// <summary>First operation.</summary>
    public const string First = nameof(First);
    
    /// <summary>FirstOrDefault operation.</summary>
    public const string FirstOrDefault = nameof(FirstOrDefault);
    
    /// <summary>Single operation.</summary>
    public const string Single = nameof(Single);
    
    /// <summary>SingleOrDefault operation.</summary>
    public const string SingleOrDefault = nameof(SingleOrDefault);
    
    /// <summary>Last operation.</summary>
    public const string Last = nameof(Last);
    
    /// <summary>LastOrDefault operation.</summary>
    public const string LastOrDefault = nameof(LastOrDefault);
    
    /// <summary>ElementAt operation.</summary>
    public const string ElementAt = nameof(ElementAt);
    
    /// <summary>ElementAtOrDefault operation.</summary>
    public const string ElementAtOrDefault = nameof(ElementAtOrDefault);

    // Aggregation Operations
    /// <summary>Count aggregation operation.</summary>
    public const string Count = nameof(Count);
    
    /// <summary>LongCount aggregation operation.</summary>
    public const string LongCount = nameof(LongCount);
    
    /// <summary>Sum aggregation operation.</summary>
    public const string Sum = nameof(Sum);
    
    /// <summary>Average aggregation operation.</summary>
    public const string Average = nameof(Average);
    
    /// <summary>Min aggregation operation.</summary>
    public const string Min = nameof(Min);
    
    /// <summary>Max aggregation operation.</summary>
    public const string Max = nameof(Max);
    
    /// <summary>MinBy aggregation operation.</summary>
    public const string MinBy = nameof(MinBy);
    
    /// <summary>MaxBy aggregation operation.</summary>
    public const string MaxBy = nameof(MaxBy);
    
    /// <summary>Aggregate operation.</summary>
    public const string Aggregate = nameof(Aggregate);

    // Quantifier Operations
    /// <summary>Any quantifier operation.</summary>
    public const string Any = nameof(Any);
    
    /// <summary>All quantifier operation.</summary>
    public const string All = nameof(All);
    
    /// <summary>Contains quantifier operation.</summary>
    public const string Contains = nameof(Contains);
    
    /// <summary>SequenceEqual operation.</summary>
    public const string SequenceEqual = nameof(SequenceEqual);

    // Set Operations
    /// <summary>Distinct set operation.</summary>
    public const string Distinct = nameof(Distinct);
    
    /// <summary>DistinctBy set operation.</summary>
    public const string DistinctBy = nameof(DistinctBy);
    
    /// <summary>Union set operation.</summary>
    public const string Union = nameof(Union);
    
    /// <summary>UnionBy set operation.</summary>
    public const string UnionBy = nameof(UnionBy);
    
    /// <summary>Intersect set operation.</summary>
    public const string Intersect = nameof(Intersect);
    
    /// <summary>IntersectBy set operation.</summary>
    public const string IntersectBy = nameof(IntersectBy);
    
    /// <summary>Except set operation.</summary>
    public const string Except = nameof(Except);
    
    /// <summary>ExceptBy set operation.</summary>
    public const string ExceptBy = nameof(ExceptBy);

    // Grouping Operations
    /// <summary>GroupBy operation.</summary>
    public const string GroupBy = nameof(GroupBy);

    // Join Operations
    /// <summary>Join operation.</summary>
    public const string Join = nameof(Join);
    
    /// <summary>GroupJoin operation.</summary>
    public const string GroupJoin = nameof(GroupJoin);

    // Sequence Operations
    /// <summary>Append sequence operation.</summary>
    public const string Append = nameof(Append);
    
    /// <summary>Prepend sequence operation.</summary>
    public const string Prepend = nameof(Prepend);
    
    /// <summary>Concat sequence operation.</summary>
    public const string Concat = nameof(Concat);
    
    /// <summary>Zip sequence operation.</summary>
    public const string Zip = nameof(Zip);

    // Partitioning Operations
    /// <summary>TakeLast partitioning operation.</summary>
    public const string TakeLast = nameof(TakeLast);
    
    /// <summary>SkipLast partitioning operation.</summary>
    public const string SkipLast = nameof(SkipLast);
    
    /// <summary>TakeWhile partitioning operation.</summary>
    public const string TakeWhile = nameof(TakeWhile);
    
    /// <summary>SkipWhile partitioning operation.</summary>
    public const string SkipWhile = nameof(SkipWhile);
    
    /// <summary>Chunk partitioning operation.</summary>
    public const string Chunk = nameof(Chunk);

    // Conversion Operations
    /// <summary>ToArray conversion operation.</summary>
    public const string ToArray = nameof(ToArray);
    
    /// <summary>ToList conversion operation.</summary>
    public const string ToList = nameof(ToList);
    
    /// <summary>ToDictionary conversion operation.</summary>
    public const string ToDictionary = nameof(ToDictionary);
    
    /// <summary>ToHashSet conversion operation.</summary>
    public const string ToHashSet = nameof(ToHashSet);
    
    /// <summary>ToLookup conversion operation.</summary>
    public const string ToLookup = nameof(ToLookup);

    // Utility Operations
    /// <summary>DefaultIfEmpty operation.</summary>
    public const string DefaultIfEmpty = nameof(DefaultIfEmpty);
}

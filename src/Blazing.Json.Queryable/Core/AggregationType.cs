namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Types of aggregation operations.
/// </summary>
public enum AggregationType
{
    /// <summary>Count aggregation.</summary>
    Count,
    /// <summary>LongCount aggregation.</summary>
    LongCount,
    /// <summary>Sum aggregation.</summary>
    Sum,
    /// <summary>Average aggregation.</summary>
    Average,
    /// <summary>Min aggregation.</summary>
    Min,
    /// <summary>Max aggregation.</summary>
    Max,
    /// <summary>MinBy aggregation.</summary>
    MinBy,
    /// <summary>MaxBy aggregation.</summary>
    MaxBy,
    /// <summary>Aggregate operation.</summary>
    Aggregate
}
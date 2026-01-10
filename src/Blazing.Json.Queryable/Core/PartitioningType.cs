namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Types of partitioning operations.
/// </summary>
public enum PartitioningType
{
    /// <summary>TakeLast operation.</summary>
    TakeLast,
    /// <summary>SkipLast operation.</summary>
    SkipLast,
    /// <summary>TakeWhile operation.</summary>
    TakeWhile,
    /// <summary>SkipWhile operation.</summary>
    SkipWhile,
    /// <summary>Chunk operation.</summary>
    Chunk
}